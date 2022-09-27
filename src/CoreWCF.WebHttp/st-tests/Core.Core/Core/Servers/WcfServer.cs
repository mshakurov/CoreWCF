using CoreWCF;
using CoreWCF.Channels;
using CoreWCF.Description;
using CoreWCF.Dispatcher;
using CoreWCF.Web;

using Ionic.Zip;

using Newtonsoft.Json;

using ST.Utils;
using ST.Utils.Attributes;
using ST.Utils.Exceptions;
using ST.Utils.Licence;
using ST.Utils.Security;
using ST.Utils.Threading;
using ST.Utils.TypeConverters;
using ST.Utils.Wcf;

using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Linq;

namespace ST.Core
{
  /// <summary>
  /// Класс сервера, поддерживающего хостинг WCF-сервисов, WCF-сессии и обмен коммуникационными сообщениями.
  /// </summary>
  [WcfServer(Interface.Constants.SERVER_ADDRESS, Interface.Constants.SERVER_NAMESPACE)]
  public class WcfServer : PublisherServer, IServiceBehavior, IDispatchMessageInspector, IErrorHandler, ISessionManager, ICommunication, IServerEntryPoint, IWcfServer
  {
    #region .Static Fields
    private const int c_watcherActualShellPathTimer_IntervalMillis = 10000;

    private static readonly char[] _cookieSeparators = new char[] { ';', ',', '=' };

    private static readonly BindingInfo[] _bindings = new[]
    {
      new BindingInfo( BindingHelper.GetBinding<NetTcpBinding>(), WcfProtocolType.Tcp ),
      new BindingInfo( BindingHelper.GetBinding<NetTcpBinding>(  authenticationType : ST.Utils.Wcf.BindingHelper.AuthenticationType.Windows ), WcfProtocolType.WinTcp ),
      new BindingInfo( BindingHelper.GetBinding<WSHttpBinding>(), WcfProtocolType.Http, (sh =>
                       {
                         sh.Description.Behaviors.Add( new ServiceMetadataBehavior { HttpsGetEnabled = true } );
                         //TODO: Mex //sh.AddServiceEndpoint( typeof( IMetadataExchange ), MetadataExchangeBindings.CreateMexHttpsBinding(), "mex" );
                       } ) ),

      new BindingInfo( BindingHelper.GetBinding<WebHttpBinding>( useTransportLevelSecurity: true ), WcfProtocolType.SecJson, (sh =>
                       {
                         sh.Description.Behaviors.Add( new ServiceMetadataBehavior { HttpsGetEnabled = true } );
                         //TODO: Mex //sh.AddServiceEndpoint( typeof( IMetadataExchange ), MetadataExchangeBindings.CreateMexHttpsBinding(), "mex" );
                       } ) ),
      new BindingInfo( BindingHelper.GetBinding<WebHttpBinding>(), WcfProtocolType.Json ),
      new BindingInfo( BindingHelper.GetBinding<WebHttpBinding>(), WcfProtocolType.OpenJson ),
      new BindingInfo( BindingHelper.GetBinding<BasicHttpBinding>( useTransportLevelSecurity: true ), WcfProtocolType.SecHttp, (sh =>
                       {
                         sh.Description.Behaviors.Add( new ServiceMetadataBehavior { HttpsGetEnabled = true } );
                         //TODO: Mex //sh.AddServiceEndpoint( typeof( IMetadataExchange ), MetadataExchangeBindings.CreateMexHttpsBinding(), "mex" );
                       } ) ),
      new BindingInfo( BindingHelper.GetBinding<BasicHttpBinding>( authenticationType : ST.Utils.Wcf.BindingHelper.AuthenticationType.Windows, useTransportLevelSecurity: true ), WcfProtocolType.WinSecHttp, (sh =>
                       {
                         sh.Description.Behaviors.Add( new ServiceMetadataBehavior { HttpsGetEnabled = true } );
                         //TODO: Mex //sh.AddServiceEndpoint( typeof( IMetadataExchange ), MetadataExchangeBindings.CreateMexHttpsBinding(), "mex" );
                       } ) ),

      new BindingInfo( BindingHelper.GetBinding<BasicHttpBinding>(  authenticationType : ST.Utils.Wcf.BindingHelper.AuthenticationType.Basic ), WcfProtocolType.WinHttp, (sh =>
                       {
                         sh.Description.Behaviors.Add( new ServiceMetadataBehavior { HttpGetEnabled = true } );
                         //TODO: Mex //sh.AddServiceEndpoint( typeof( IMetadataExchange ), MetadataExchangeBindings.CreateMexHttpBinding(), "mex" );
                       } ) ),
      new BindingInfo( BindingHelper.GetBinding<BasicHttpBinding>(), WcfProtocolType.OpenHttp, (sh =>
                       {
                         sh.Description.Behaviors.Add( new ServiceMetadataBehavior { HttpGetEnabled = true } );
                         //TODO: Mex //sh.AddServiceEndpoint( typeof( IMetadataExchange ), MetadataExchangeBindings.CreateMexHttpBinding(), "mex" );
                       } ) ),
      new BindingInfo( BindingHelper.GetBinding<BasicHttpBinding>(), WcfProtocolType.BasicHttp, (sh =>
                       {
                         sh.Description.Behaviors.Add( new ServiceMetadataBehavior { HttpGetEnabled = true } );
                         //TODO: Mex //sh.AddServiceEndpoint( typeof( IMetadataExchange ), MetadataExchangeBindings.CreateMexHttpBinding(), "mex" );
                       } ) )

    };

    private static readonly Regex _inranetRegex = new Regex(@"127.0.0.1|::1|10\..*|192\.168\..*|172\.(1[6-9]|2[0-9]|3[01])\..*|FE([89]|[A-F]).*", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

#if DEBUG
    private static readonly bool _isDebugLogonEnabled = EnvironmentHelper.IsCommandLineArgumentDefined("EnableDebugLogon");
#endif
    #endregion

    #region .Fields
    private WcfServerConfig _config;

    private X509Certificate2 _certificate;

    private readonly SimpleTask _checkSessionsTask;

    //private readonly Dictionary<Type, List<ServiceHost>> _hosts = new Dictionary<Type, List<ServiceHost>>();

    private readonly Dictionary<int, CustomBindingItem> _bindingDic = new Dictionary<int, CustomBindingItem>();

    private SessionConcurrentDictionary<ulong, Session> _sessions = new SessionConcurrentDictionary<ulong, Session>(null);

    private ConcurrentDictionary<SessionKey, ulong> _loginSessions = new ConcurrentDictionary<SessionKey, ulong>();

    private ConcurrentDictionary<string, string> _shellFileDic = new ConcurrentDictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
    private FileSystemWatcher watcherActualShellPath;
    private Timer watcherActualShellPathTimer;
    private bool _shellFileDicReadFirstTime = true;
    #endregion

    #region .Properties
    protected override ServerType ServerType
    {
      get { return ServerType.ApplicationServer; }
    }

    private WcfServerConfig Config
    {
      get { return _config == null ? _config = GetConfig() : _config; }
    }

    /// <summary>
    /// Сертификат, используемый для шифрования данных.
    /// </summary>
    protected X509Certificate2 Certificate
    {
      get
      {
        if (_certificate == null)
        {
          var certificate = GetParameter<byte[]>(Constants.CERTIFICATE_PARAMETER);

          if (certificate == null)
          {
            certificate = HttpSsl.CreateCertificate();

            SetParameter(Constants.CERTIFICATE_PARAMETER, certificate);
          }

          _certificate = new X509Certificate2(certificate, string.Empty, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);
        }

        return _certificate;
      }
    }
    #endregion

    #region .Ctor
    static WcfServer()
    {
    }

    public WcfServer()
    {
      _checkSessionsTask = new SimpleTask(CheckSessionsTask, TaskCreationOptions.LongRunning);

      UDPHostName = Config.UDPHostName;
      UDPPort = Config.UDPPort;
      UDPLoggerId = Config.UDPLoggerId;

      CoreManager.SetParameters( WriteToLog, () => ServerContext.Session );
    }
    #endregion

    #region IsModuleTypeLoadable
    protected override bool IsModuleTypeLoadable( Type type )
    {
      return base.IsModuleTypeLoadable(type) && GetModuleItem(type) != null;
    }
    #endregion

    #region OnModuleInitializing
    protected override void OnModuleInitializing( BaseModule module )
    {
      base.OnModuleInitializing(module);

      module.IfIs<IModuleItemProvider>(m => m.ModuleItem = GetModuleItem(module.GetType()));
    }
    #endregion

    #region GetModuleItem
    private ModuleItem GetModuleItem( Type moduleType )
    {
      return Config?.ModuleItems?.FirstOrDefault(mi => mi.AssemblyFileName.IsEqualCI(Path.GetFileName(moduleType.Assembly.Location)));
    }
    #endregion

    #region OnShellFileChanged
    private void OnShellFileChanged( object sender, FileSystemEventArgs e )
    {
#if DEBUG
      this.WriteToLog(string.Format("OnShellFileChanged. Обнаружено изменения файла исходников клиента (Config.ActualShellPath: {0}, WatcherTimer Is Null: {1})", Config.ActualShellPath, watcherActualShellPathTimer == null), 1, EventLogEntryType.Warning);
#endif

      if (watcherActualShellPathTimer == null)
        watcherActualShellPathTimer = new Timer(_ => FillShellFiles(Config.ActualShellPath), null, c_watcherActualShellPathTimer_IntervalMillis, Timeout.Infinite);
      else
      {
        watcherActualShellPathTimer.Change(Timeout.Infinite, Timeout.Infinite);
        watcherActualShellPathTimer.Change(c_watcherActualShellPathTimer_IntervalMillis, Timeout.Infinite);
      }
    }
    #endregion

    #region FillShellFiles
    private void FillShellFiles( string path )
    {
#if DEBUG
      this.WriteToLog(string.Format("FillShellFiles. Перечитываем исходники файлов (path: {0}, exists: {1})", path, File.Exists(path)), 1, EventLogEntryType.Warning);
#endif

      path = (path ?? string.Empty).Trim();

      if (path.NullIfWhiteSpace() == null || !File.Exists(path))
      {
        // очищаем только если уже известно нет файла
        _shellFileDic.Clear();

        return;
      }

      // не трогаем основной словарь, работаем с временным
      var tempDict = new ConcurrentDictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

      try
      {
        using (var zipFileZream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Delete | FileShare.ReadWrite))
        using (var zip = ZipFile.Read(zipFileZream))
        {
          foreach (var e in zip.ToList())
          {
            if (e.IsDirectory)
              continue;

            using (var ms = new MemoryStream())
            {
              e.Extract(ms);

              ms.Seek(0, SeekOrigin.Begin);
              ms.Flush();

              tempDict.GetOrAdd(e.FileName.Replace('\\', '/'), SecurityHelper.GetMD5Hash(ms));
            }
          }
        }

        bool isUpdated = !_shellFileDic.ToArray().Select(kv => new[] { kv.Key, kv.Value }).SequenceEqual(tempDict.ToArray().Select(kv => new[] { kv.Key, kv.Value }), StringComparer.InvariantCultureIgnoreCase, StringComparer.Ordinal);

        // заменяем основной словарь на новый
        Interlocked.Exchange(ref _shellFileDic, tempDict);

        if (!_shellFileDicReadFirstTime)
          // логируем об обновлении
          this.WriteToLog(SR.GetString(RI.ActualShellHashUpdated) + (isUpdated ? SR.GetString(RI.ActualShellHashChanged) : ""), 1, EventLogEntryType.Information);
      }
      catch (ZipException ex)
      {
        if (!(ex.InnerException is BadReadException))
          WriteToLog(ex, SR.GetString(RI.ActualShellPathReadError));

        // BadReadException возникает когда файл все еще копируется на диск
#if DEBUG
        WriteToLog(ex, SR.GetString(RI.ActualShellPathReadError));
#endif
      }
      catch (Exception ex)
      {
        WriteToLog(ex, SR.GetString(RI.ActualShellPathReadError));
      }
    }
    #endregion

    #region IsNeedUpdate
    private bool IsNeedUpdate( string[][] fileHash )
    {
      // создаем снапшот словаря _shellFileDic
      var dictServer = _shellFileDic.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.InvariantCultureIgnoreCase);

      if (dictServer.Count == 0 || fileHash == null || fileHash.Length == 0)
        return false;

      var dictClient = fileHash.ToDictionary(h => h[0], h => h[1], StringComparer.InvariantCultureIgnoreCase);

      foreach (var kvServer in dictServer)
      {
        var hashClient = dictClient.GetValue(kvServer.Key);
        // серверный файл не найден среди клиентских
        if (hashClient == null)
          return true;
        // если хэши не совпадают
        if (hashClient != kvServer.Value)
          return true;
      }

      return false;
    }
    #endregion

    #region CheckSessionCountByOrgId
    private bool CheckSessionCountByOrgId( int dbOrgGroupId, int dbSessionCount )
    {
      var sessionCount = 0;

      foreach (var session in _sessions.Values)
        if (session.OrgGroupId.HasValue && session.OrgGroupId.Value == dbOrgGroupId)
          sessionCount++;

      return sessionCount >= dbSessionCount;
    }
    #endregion

    #region CheckSessionsTask
    private void CheckSessionsTask( CancellationToken token )
    {
      while (true)
      {
        if (token.WaitHandle.WaitOne(Constants.SESSION_CHECK_TIME))
          token.ThrowIfCancellationRequested();

        _sessions.Values.ForEach(s => DateTime.UtcNow >= s.ExpirationTime, DeleteSession);

        token.ThrowIfCancellationRequested();

        _sessions.Values.ForEach(s =>
       {
         CommunicationMessage.Wrapper msg;

         int removedCount = 0;

         while (s.Messages.TryPeek(out msg) && DateTime.UtcNow >= msg.ExpirationTime)
         {
           s.Messages.TryDequeue(out msg);

           if (++removedCount >= 100)
           {
             removedCount = 0;

             token.ThrowIfCancellationRequested();
           }
         }
       });
      }
    }
    #endregion

    #region CloseHosts
    //private void CloseHosts( object server )
    //{
    //  _hosts.GetAndRemove(server.GetType()).IfNotNull(hosts => Parallel.ForEach(hosts, h => Exec.Try(() => h.StopAsync(TimeSpan.FromSeconds(10)))));
    //}
    #endregion

    #region DeleteSession
    private void DeleteSession()
    {
      if (_sessions.RemoveSession(ServerContext.Session.Id))
        try
        {
          UpdateTraffic(null, null);

          ServerContext.Session.AuthModule.OnSessionDeleted();
        }
        catch (Exception exc)
        {
          OnUnhandledException(new DeletingSessionException(exc));
        }
        finally
        {
          MemoryHelper.Collect();
        }
    }

    private void DeleteSession( Session session )
    {
      ServerContext.Session = session;

      DeleteSession();
    }
    #endregion

    #region ExecForSession
    /// <summary>
    /// Выполняет действие в рамках указанной сессии.
    /// </summary>
    /// <param name="session">Сессия.</param>
    /// <param name="action">Действие.</param>
    protected void ExecForSession( [NotNull] Session session, [NotNull] Action action )
    {
      var currentSession = ServerContext.Session;

      try
      {
        ServerContext.Session = session;

        action();
      }
      finally
      {
        ServerContext.Session = currentSession;
      }
    }

    /// <summary>
    /// Выполняет действие в рамках указанной сессии.
    /// </summary>
    /// <typeparam name="T">Тип возвращаемого функцией значения.</typeparam>
    /// <param name="session">Сессия.</param>
    /// <param name="func">Функция.</param>
    /// <returns>Значение, возвращаемое функцией.</returns>
    protected T ExecForSession<T>( [NotNull] Session session, [NotNull] Func<T> func )
    {
      var currentSession = ServerContext.Session;

      try
      {
        ServerContext.Session = session;

        return func();
      }
      finally
      {
        ServerContext.Session = currentSession;
      }
    }
    #endregion

    #region GetAuthModule
    IAuthModule GetAuthModule()
    {
      var logonModule = Modules.OfType<IAuthModule>().FirstOrDefault();

#if DEBUG
      if (_isDebugLogonEnabled)
        logonModule = DebugLogonModule.Instance;
#endif

      //if( logonModule == null )
      //  throw new AuthIsNotAvailableException();

      return logonModule;
    }
    #endregion

    #region GetConfig
    /// <summary>
    /// Возвращает конфигурацию WCF-сервера.
    /// </summary>
    /// <returns>Конфигурация сервера.</returns>
    public static WcfServerConfig GetConfig()
    {
      return GetConfig<WcfServerConfig>();
    }

    /// <summary>
    /// Возвращает конфигурацию WCF-сервера.
    /// </summary>
    /// <typeparam name="T">Тип конфигурации сервера.</typeparam>
    /// <returns>Конфигурация сервера.</returns>
    public static T GetConfig<T>()
      where T : WcfServerConfig, new()
    {
      return BaseServer.GetConfig<T>(ServerType.ApplicationServer) as T;
    }
    #endregion

    #region GetConfigPath
    /// <summary>
    /// Возвращает корневой путь, по которому хранится конфигурация сервера.
    /// </summary>
    /// <returns>Путь реестра.</returns>
    public static string GetConfigRootPath()
    {
      return BaseServer.GetConfigRootPath(ServerType.ApplicationServer);
    }
    #endregion

    #region GetOperationTimeout
    protected override int GetOperationTimeout()
    {
      return (Config != null && Config.OperationTimeout.HasValue) ? Config.OperationTimeout.Value * 1000 : base.GetOperationTimeout();
    }
    #endregion

    #region GetHost
    //private ServiceHost GetHost( Binding binding, object server, Type[] interfaces, string schemeName, int port )
    //{
    //  var t = server.GetType();

    //  var sb = t.GetAttribute<ServiceBehaviorAttribute>();

    //  var nameSpace = sb != null && string.IsNullOrWhiteSpace( sb.Namespace ) ? string.Format( "{0}/{1}", ST.Utils.Constants.BASE_NAMESPACE, t.Name ) : sb.Namespace;
    //  var name = sb != null && string.IsNullOrWhiteSpace( sb.Name ) ? t.Name : sb.Name;

    //  var host = new ServiceHost( server, new Uri( schemeName + "://localhost:" + port + "/" + name ) );

    //  if( !host.Description.Behaviors.Contains( typeof( ServiceBehaviorAttribute ) ) )
    //    host.Description.Behaviors.Add( new ServiceBehaviorAttribute() );

    //  sb = host.Description.Behaviors[typeof( ServiceBehaviorAttribute )] as ServiceBehaviorAttribute;

    //  sb.InstanceContextMode = InstanceContextMode.Single;
    //  sb.ConcurrencyMode = ConcurrencyMode.Multiple;
    //  sb.IgnoreExtensionDataObject = true;
    //  sb.Namespace = nameSpace;
    //  sb.Name = name;
    //  sb.MaxItemsInObjectGraph = int.MaxValue;

    //  host.Description.Behaviors.Add( new ServiceThrottlingBehavior
    //  {
    //    MaxConcurrentCalls = int.MaxValue,
    //    MaxConcurrentInstances = int.MaxValue,
    //    MaxConcurrentSessions = int.MaxValue
    //  } );

    //  if( server != this )
    //    host.Description.Behaviors.Add( this );

    //  foreach( var i in interfaces )
    //  {
    //    if( i.GetAttribute<WcfServiceAttribute>( true ).Address != sb.Name )
    //      continue; //throw new WcfServiceException( new InvalidWcfServiceAddressException( i ), host, server as BaseModule );

    //    var e = host.AddServiceEndpoint( i, binding, i.Name );

    //    if( binding is WebHttpBinding )
    //      e.Behaviors.Add( new JsonErrorWebHttpBehavior { DefaultOutgoingResponseFormat = WebMessageFormat.Json, AutomaticFormatSelectionEnabled = false } );

    //    e.ExtendOperations();

    //    if( i == typeof( ICommunication ) )
    //      e.Contract.Operations.Find( "Send" ).Behaviors.Add( SendOperationBehavior.Instance );
    //  }

    //  if( binding is WSHttpBinding /*|| (binding is NetTcpBinding && (binding as NetTcpBinding).Security.Transport.ClientCredentialType != TcpClientCredentialType.Windows)*/ )
    //    host.Credentials.ServiceCertificate.Certificate = HttpSsl.FindCertificate( Certificate.Thumbprint );

    //  return host;
    //}
    #endregion

    #region GetHosts
    //private List<ServiceHost> GetHosts( object server )
    //{
    //  var hosts = new List<ServiceHost>();

    //  if( server.IsDefined<WcfServerAttribute>( true ) )
    //  {
    //    var interfaces = (from i in server.GetType().GetInterfaces()
    //                      where i.IsDefined<WcfServiceAttribute>() //&& i.GetMethods().Any( m => m.IsDefined<OperationContractAttribute>() )
    //                      select i).ToArray();

    //    if( interfaces.Length > 0 )
    //    {
    //      _bindings.ForEach( b =>
    //      {
    //        var port = Config.GetPort( b.Protocol );

    //        if( port > 0 )
    //        {
    //          var host = GetHost( b.Binding, server, interfaces, b.Protocol.GetDescription(), port );

    //          if( b.PostConfigure != null )
    //            b.PostConfigure( host );

    //          hosts.Add( host );
    //        }
    //      } );

    //      _config.CustomBindings.IfNotNull( c => c.ForEach( cb =>
    //      {
    //        if( cb.Port > 0 )
    //        {
    //          var bInfo = GetBindingInfo( cb );

    //          var host = GetHost( bInfo.Binding, server, interfaces, cb.UseTransportLevelSecurity ? "https" : cb.TransferType.GetDescription(), cb.Port );

    //          if( bInfo.PostConfigure != null )
    //            bInfo.PostConfigure( host );

    //          hosts.Add( host );

    //          _bindingDic.GetOrAdd( cb.Port, cb );
    //        }
    //      } ) );
    //    }

    //    //if( server.ToString() == "ST.Security.Server.PM" )
    //    //{
    //    //  var host = new ServiceHost( server, new Uri( "http://localhost:8000/SecurityServer" ) );

    //    //  host.Description.Behaviors.Add( new ServiceThrottlingBehavior
    //    //  {
    //    //    MaxConcurrentCalls = int.MaxValue,
    //    //    MaxConcurrentInstances = int.MaxValue,
    //    //    MaxConcurrentSessions = int.MaxValue
    //    //  } );

    //    //  if( server != this )
    //    //    host.Description.Behaviors.Add( this );

    //    //  var binding = new WebHttpBinding();

    //    //  var endpointBehavior = new WebHttpBehavior();

    //    //  foreach( var i in interfaces )
    //    //  {
    //    //    var e = host.AddServiceEndpoint( i, binding, i.Name );

    //    //    e.Behaviors.Add( endpointBehavior );

    //    //    e.ExtendOperations();
    //    //  }

    //    //  try
    //    //  {
    //    //    host.Open();
    ////  }
    //    //  catch( Exception ex )
    //    //  {
    //    //    var text = ex.Message;
    //    //  }

    //    //  //hosts.Add( host );
    ////}
    //  }

    //  return hosts;
    //}
    #endregion

    #region GetBindingInfo
    private BindingInfo GetBindingInfo( CustomBindingItem bindingItem )
    {
      Binding binding = null;
      Action<ServiceHostBase> postConfigure = null;

      if (bindingItem.TransferType == BindingHelper.TransferType.Tcp)
      {
        if (bindingItem.ProtocoType == BindingHelper.ProtocolType.Soap)
        {
          //if (bindingItem.ZippedType == BindingHelper.ZippedType.None)
          //  binding = BindingHelper.GetBinding<NetTcpBinding>(authenticationType: bindingItem.AuthenticationType, useTransportLevelSecurity: bindingItem.UseTransportLevelSecurity);
          //else
          //  if (bindingItem.ZippedType == BindingHelper.ZippedType.All)
          //  binding = BindingHelper.GetBinding<GzipBinding>(authenticationType: bindingItem.AuthenticationType, useTransportLevelSecurity: bindingItem.UseTransportLevelSecurity);
          //else
          //    if (bindingItem.ZippedType == BindingHelper.ZippedType.Read)
          //  binding = BindingHelper.GetBinding<GzipBindingIn>(authenticationType: bindingItem.AuthenticationType, useTransportLevelSecurity: bindingItem.UseTransportLevelSecurity);
          //else
          //      if (bindingItem.ZippedType == BindingHelper.ZippedType.Write)
          //  binding = BindingHelper.GetBinding<GzipBindingOut>(authenticationType: bindingItem.AuthenticationType, useTransportLevelSecurity: bindingItem.UseTransportLevelSecurity);
        }

      }
      else // http
      {
        if (bindingItem.ProtocoType == BindingHelper.ProtocolType.Soap)
        {
          binding = BindingHelper.GetBinding<BasicHttpBinding>(authenticationType: bindingItem.AuthenticationType, useTransportLevelSecurity: bindingItem.UseTransportLevelSecurity);
        }
        else // Json
        {
          //if (bindingItem.ZippedType == BindingHelper.ZippedType.None)
          //  binding = BindingHelper.GetBinding<CustomWebHttpBinding>(authenticationType: bindingItem.AuthenticationType, useTransportLevelSecurity: bindingItem.UseTransportLevelSecurity);
          //else
          //  if (bindingItem.ZippedType == BindingHelper.ZippedType.All)
          //  binding = BindingHelper.GetBinding<JsonGzipBinding>(authenticationType: bindingItem.AuthenticationType, useTransportLevelSecurity: bindingItem.UseTransportLevelSecurity);
        }

        if (bindingItem.UseTransportLevelSecurity)
          postConfigure = sh =>
          {
            sh.Description.Behaviors.Add(new ServiceMetadataBehavior { HttpsGetEnabled = bindingItem.UseTransportLevelSecurity });
            //TODO: mex sh.AddServiceEndpoint(typeof(IMetadataExchange), MetadataExchangeBindings.CreateMexHttpsBinding(), "mex");
          };
        else
          postConfigure = sh =>
          {
            sh.Description.Behaviors.Add(new ServiceMetadataBehavior { HttpGetEnabled = bindingItem.UseTransportLevelSecurity });
            //TODO: mex sh.AddServiceEndpoint(typeof(IMetadataExchange), MetadataExchangeBindings.CreateMexHttpBinding(), "mex");
          };

      }

      return new BindingInfo(binding, WcfProtocolType.Http, postConfigure);
    }
    #endregion

    #region GetModuleConfig
    /// <summary>
    /// Возвращает конфигурацию модуля.
    /// </summary>
    /// <param name="moduleType">Тип модуля.</param>
    /// <returns>Конфигурация модуля.</returns>
    public static ModuleConfig GetModuleConfig( [NotNull] Type moduleType )
    {
      return BaseServer.GetModuleConfig(moduleType, ServerType.ApplicationServer);
    }
    #endregion

    #region GetModuleEnabled
    /// <summary>
    /// Возвращает признак активности модуля.
    /// </summary>
    /// <param name="moduleType">Тип модуля.</param>
    /// <returns>True - модуль включен, False - модуль отключен.</returns>
    public static bool GetModuleEnabled( [NotNull] Type moduleType )
    {
      return BaseServer.GetModuleEnabled(moduleType, ServerType.ApplicationServer);
    }
    #endregion

    #region GetModuleParameter
    /// <summary>
    /// Возвращает значение параметра модуля.
    /// </summary>
    /// <typeparam name="T">Тип параметра. Поддерживаются только значимые типы, строка или массив байт.</typeparam>
    /// <param name="moduleType">Тип модуля.</param>
    /// <param name="name">Название параметра.</param>
    /// <returns>Значение параметра.</returns>
    public static T GetModuleParameter<T>( [NotNull] Type moduleType, [NotNullNotEmpty] string name )
    {
      return BaseServer.GetModuleParameter<T>(moduleType, ServerType.ApplicationServer, name);
    }
    #endregion

    #region GetParameter
    /// <summary>
    /// Возвращает значение параметра сервера.
    /// </summary>
    /// <typeparam name="T">Тип параметра. Поддерживаются только значимые типы, строка или массив байт.</typeparam>
    /// <param name="name">Название параметра.</param>
    /// <returns>Значение параметра.</returns>
    public static T GetParameter<T>( [NotNullNotEmpty] string name )
    {
      return BaseServer.GetParameter<T>(ServerType.ApplicationServer, name);
    }
    #endregion

    #region GetSessionData
    private IEnumerable<T> GetSessionData<T>()
      where T : class
    {
      return ServerContext.Session.Data.Values.Select(data => data.GetValue(typeof(T)) as T).Where(obj => obj != null);
    }
    #endregion

    #region GetSessionIdFromCookies
    private string GetSessionIdFromCookies( string cookies )
    {
      var values = cookies.Split(_cookieSeparators, StringSplitOptions.RemoveEmptyEntries);

      for (int i = 0; i < values.Length; i += 2)
        if (values[i].IsEqualCI("SessionId"))
        {
          if (i + 1 < values.Length)
            return values[i + 1];
          else
            return string.Empty;
        }

      return string.Empty;
    }
    #endregion

    #region OnMessageReceived
    protected override void OnMessageReceived( BaseMessage msg )
    {
      var comMsg = msg as CommunicationMessage;

      if (comMsg != null)
        SendToSessions(comMsg);
    }
    #endregion

    #region OnModuleInitialized
    protected override void OnModuleInitialized( BaseModule module )
    {
      base.OnModuleInitialized(module);

      //OpenHosts(module);
    }
    #endregion

    #region OnModulesLoaded
    protected override void OnModulesLoaded()
    {
      base.OnModulesLoaded();

      //#if DEBUG
      //      this.IfIs<WcfServer>(wcf => wcf.LogHostsInfo());
      //#endif

      ServerContext.RegisterPermissions(AssemblyHelper.GetSubtypes(false, new[] { typeof(Permission) }, typeof(PlatformAssemblyAttribute)).Select(t => new PermissionDescriptor(t)));

      //OpenHosts(this);

      StartWatcherActualShellPath();
    }
    #endregion

    #region StartWatcherActualShellPath
    private void StartWatcherActualShellPath()
    {
      if (!string.IsNullOrWhiteSpace(Config.ActualShellPath))
      {
        watcherActualShellPath = new FileSystemWatcher(Path.GetDirectoryName(Config.ActualShellPath), Path.GetFileName(Config.ActualShellPath));
        watcherActualShellPath.NotifyFilter = NotifyFilters.LastWrite;
        watcherActualShellPath.Changed += OnShellFileChanged;

        FillShellFiles(Config.ActualShellPath);

        _shellFileDicReadFirstTime = false;

        watcherActualShellPath.EnableRaisingEvents = true;
      }
    }
    #endregion

    #region OnServerConfigChanged
    protected override void OnServerConfigChanged()
    {
      _config = GetConfig();

      if (watcherActualShellPath != null)
      {
        Exec.Try(() =>
         {
           watcherActualShellPath.EnableRaisingEvents = false;
           watcherActualShellPath.Changed -= OnShellFileChanged;
           watcherActualShellPath.Dispose();
         });
        watcherActualShellPath = null;
      }

      StartWatcherActualShellPath();
    }
    #endregion

    #region OnModulesPostInitialized
    protected override void OnModulesPostInitialized()
    {
      base.OnModulesPostInitialized();

      var authModule = GetAuthModule();

      _sessions.AuthModule = authModule;

      //#if RELEASE
      if (authModule != null)
      {
        var dbSessions = authModule.RestoreDBSessions();

        for (int i = 0; i < dbSessions.Length; i++)
        {
          var dbSession = dbSessions[i];

          var session = new Session(dbSession.SessionId, authModule, dbSession.Login, dbSession.CreatedIP, dbSession.Permissions, dbSession.Culture != null ? new CultureInfo(dbSession.Culture) : Config.Culture, dbSession.OrgGroupId, Config.ProductName, Config.SaveTrafficTime);

          session.MessageTypes.AddRangeSafe(dbSession.MessageTypes);

          _sessions.GetOrAdd(session.Id, session);

          ServerContext.Session = session;

          authModule.OnSessionCreated(dbSession.AuthenticationResult, () => DeleteSession(session));

          ServerContext.Session = Session.Empty;
        }
      }
      //#endif
    }
    #endregion

    #region GetModuleAssemblies
    protected override Assembly[] GetModuleAssemblies()
    {
      return AssemblyHelper.LoadAssemblies(Config.ModuleItems?.Select(mi => mi.AssemblyFileName)?.ToArray() ?? new string[0], typeof(PlatformAssemblyAttribute));
    } 
    #endregion

    #region GetModules
    public object[] GetModules() => Modules.ToArray(); 
    #endregion

    #region OnModulesLoading
    protected override void OnModulesLoading()
    {
      base.OnModulesLoading();

      _config = null;

      //HttpSsl.InstallAndBindToPort(Config.GetPort(WcfProtocolType.Http), Certificate);

      _checkSessionsTask.Start();

      SetCultures();
    }
    #endregion

    #region OnModulesUnloaded
    protected override void OnModulesUnloaded()
    {
      if (watcherActualShellPath != null)
        Exec.Try(watcherActualShellPath.Dispose);

      _checkSessionsTask.Stop();

      _sessions.AuthModule = null;

      _sessions.ClearSessions();

      _loginSessions.Clear();

      Session.Empty.Data.Clear();

      ServerContext.ClearPermissions();

      //CloseHosts(this);

      HttpSsl.UninstallAndUnbindFromPort(Config.GetPort(WcfProtocolType.Http), Certificate.Thumbprint);

      base.OnModulesUnloaded();
    }
    #endregion

    #region OnModulesUnloading
    protected override void OnModulesUnloading()
    {
      SetCultures();

      base.OnModulesUnloading();
    }
    #endregion

    #region OnModuleUninitializing
    protected override void OnModuleUninitializing( BaseModule module )
    {
      //CloseHosts(module);

      base.OnModuleUninitializing(module);
    }
    #endregion

    #region OpenHosts
    //private void OpenHosts( object server )
    //{
    //  var hosts = GetHosts( server );

    //  if( hosts.Count > 0 )
    //  {
    //    var exceptions = new List<Exception>();

    //    Parallel.ForEach( hosts, h => Exec.Try( h.Open, exc => exceptions.Add( new WcfServiceException( exc, h.BaseAddresses[0].ToString(), server as BaseModule ) ) ) );

    //    if( exceptions.Count > 0 )
    //      throw new AggregateException( exceptions );

    //    _hosts.Add( server.GetType(), hosts );
    //  };
    //}
    #endregion

#if DEBUG
    #region LogHostsInfo
    //internal void LogHostsInfo()
    //{
    //  System.Text.StringBuilder sb = new System.Text.StringBuilder();
    //  sb.AppendFormat( "{0}:\r\n", this.GetType().FullName );
    //  sb.AppendFormat( "Hosts ({0}):\r\n", _hosts.Count );
    //  foreach( var pair in _hosts )
    //  {
    //    sb.AppendFormat( "- {0} Adresses:\r\n", pair.Key.FullName );
    //    foreach( var sh in pair.Value )
    //      foreach( var adr in sh.BaseAddresses )
    //        sb.AppendFormat( "- - {0}\r\n", adr.AbsoluteUri );
    //  }
    //  EventLog.WriteEntry( ST.Core.Constants.LOG_NAME, sb.ToString() );
    //}
    #endregion
#endif

    #region SendToSessions
    /// <summary>
    /// Посылает сообщение сессиям.
    /// </summary>
    /// <param name="msg">Сообщение.</param>
    /// <param name="filter">Метод отбора сессиий, которым сообщение должно быть послано.</param>
    protected void SendToSessions( [NotNull] CommunicationMessage msg, Func<Session, bool> filter = null )
    {
      var typeName = CommunicationMessage.GetMessageTypeName(msg.GetType());

      //msg.Id = Interlocked.Increment( ref CommunicationMessage.MessageId );

      foreach (var session in _sessions.Values)
        if (session.MessageTypes.ContainsSafe(typeName) && (filter == null || filter(session)))
          session.Messages.Enqueue(new CommunicationMessage.Wrapper { Message = msg, ExpirationTime = DateTime.UtcNow.AddMilliseconds(Constants.COMMUNICATION_MESSAGE_EXPIRE_TIME) });
    }
    #endregion

    #region SetConfig
    /// <summary>
    /// Устанавливает конфигурацию сервера.
    /// </summary>
    /// <param name="config">Конфигурация сервера.</param>
    public static void SetConfig( [NotNull] WcfServerConfig config )
    {
      BaseServer.SetConfig(config, ServerType.ApplicationServer);
    }
    #endregion

    #region SetCultures
    /// <summary>
    /// Устанавливает рабочие культуры сервера приложений.
    /// </summary>
    protected void SetCultures()
    {
      Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
      Thread.CurrentThread.CurrentUICulture = Config.Culture;
    }
    #endregion

    #region SetModuleConfig
    /// <summary>
    /// Устанавливает конфигурацию модуля.
    /// </summary>
    /// <param name="moduleType">Тип модуля.</param>
    /// <param name="config">Конфигурация модуля.</param>
    public static void SetModuleConfig( [NotNull] Type moduleType, [NotNull] ModuleConfig config )
    {
      BaseServer.SetModuleConfig(moduleType, ServerType.ApplicationServer, config);
    }
    #endregion

    #region SetModuleEnabled
    /// <summary>
    /// Устанавливает признак активности модуля.
    /// </summary>
    /// <param name="moduleType">Тип модуля.</param>
    /// <param name="enabled">True - модуль включен, False - модуль отключен.</param>
    public static void SetModuleEnabled( [NotNull] Type moduleType, bool enabled )
    {
      BaseServer.SetModuleEnabled(moduleType, ServerType.ApplicationServer, enabled);
    }
    #endregion

    #region SetModuleParameter
    /// <summary>
    /// Устанавливает значение параметра модуля.
    /// </summary>
    /// <typeparam name="T">Тип параметра. Поддерживаются только значимые типы, строка или массив байт.</typeparam>
    /// <param name="moduleType">Тип модуля.</param>
    /// <param name="name">Название параметра.</param>
    /// <param name="value">Значение параметра.</param>
    public static void SetModuleParameter<T>( [NotNull] Type moduleType, [NotNullNotEmpty] string name, [NotNull] T value )
    {
      BaseServer.SetModuleParameter<T>(moduleType, ServerType.ApplicationServer, name, value);
    }
    #endregion

    #region SetParameter
    /// <summary>
    /// Устанавливает значение параметра сервера.
    /// </summary>
    /// <typeparam name="T">Тип параметра. Поддерживаются только значимые типы, строка или массив байт.</typeparam>
    /// <param name="name">Название параметра.</param>
    /// <param name="value">Значение параметра.</param>
    public static void SetParameter<T>( [NotNullNotEmpty] string name, [NotNull] T value )
    {
      BaseServer.SetParameter<T>(ServerType.ApplicationServer, name, value);
    }
    #endregion

    #region ICommunication
    void ICommunication.Send( CommunicationMessage msg )
    {
#if TEST_COMM_EX
      Trace.WriteLine( string.Format( "TEST_COMM_EX. ICommunication.Send({0} - '({1}): {2}')", msg.GetType().Name, msg.Id, msg.ToString() ) );
#endif

      Send(msg);

      SendToSessions(msg, s => s != ServerContext.Session);
    }

    CommunicationMessage[] ICommunication.Get( long lastMessageId )
    {
      var now = DateTime.UtcNow;

      ServerContext.Session.Messages.Where(msg => msg.Message.Id <= lastMessageId && now < msg.ExpirationTime).ForEach(msg => msg.ExpirationTime = DateTime.MinValue);

      return (from msg in ServerContext.Session.Messages
              where now < msg.ExpirationTime
              select msg.Message).Take(64).ToArray();
    }

    void ICommunication.Subscribe( string messageType )
    {
      ServerContext.Session.AddRangeSafe(new string[] { messageType });
    }

    void ICommunication.SubscribeMany( string[] messageTypes )
    {
      ServerContext.Session.AddRangeSafe(messageTypes);
    }

    void ICommunication.Unsubscribe( string messageType )
    {
      ServerContext.Session.MessageTypes.RemoveSafe(messageType);
    }

    void ICommunication.UnsubscribeAll()
    {
      ServerContext.Session.MessageTypes.ClearSafe();
    }

    void ICommunication.UnsubscribeMany( string[] messageTypes )
    {
      ServerContext.Session.MessageTypes.RemoveRangeSafe(messageTypes);
    }
    #endregion

    #region IsTrustedLogOn
    private bool IsTrustedLogOn()
    {
      var action = OperationContext.Current.IncomingMessageHeaders.Action;
      var operationName = action.Substring(action.LastIndexOf("/", StringComparison.OrdinalIgnoreCase) + 1);

      return operationName == "TrustedLogOn";
    }
    #endregion

    #region IDispatchMessageInspector
    [DebuggerStepThrough]
    object IDispatchMessageInspector.AfterReceiveRequest( ref Message request, IClientChannel channel, InstanceContext instanceContext )
    {
      request.IfNotNull(req => WebRequestLog.Log(req.Headers.Action ?? req.Headers.To.AbsolutePath, () => req.ToString()));

      var binding = instanceContext.Host.Description.Endpoints[0].Binding;

      //if( instanceContext.Host.Description.Endpoints[0].ListenUri.Port.In( new int[]{ Config.GetPort( WcfProtocolType.Json ), 
      //                                                                                Config.GetPort( WcfProtocolType.SecJson ), 
      //                                                                                Config.GetPort( WcfProtocolType.JsonZipped ) } ) && IsTrustedLogOn() )
      //  throw new ActionSupportedException();


      var bindingItem = _bindingDic.GetValue(instanceContext.Host.Description.Endpoints[0].ListenUri.Port);

      Session session = null;

      //if( instanceContext.Host.Description.Endpoints[0].ListenUri.Port.In( new int[]{ Config.GetPort( WcfProtocolType.WinSecHttp ), 
      //                                                                                Config.GetPort( WcfProtocolType.SecHttp ), 
      //                                                                                //Config.GetPort( WcfProtocolType.OpenJson ),
      //                                                                                Config.GetPort( WcfProtocolType.OpenHttp ) } ) )
      if (bindingItem != null && bindingItem.ContextUserType == BindingHelper.ContextUserType.None)
      {
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        Thread.CurrentThread.CurrentUICulture = Config.Culture;

        ServerContext.Session = null;

        return null;
      }
      else
        if (bindingItem == null || bindingItem.ContextUserType == BindingHelper.ContextUserType.SessionId)
      {
        if (!request.IsEmpty || binding is WebHttpBinding) // Для mex-запросов request.IsEmpty = true.
        {
          if (ServerState != ServerState.Started)
            throw new ServerTooBusyException();

          SetCultures();

          var sessionId = 0UL;

          //var session = _sessions.GetValue( request.GetHeader<ulong>( Constants.SESSION_HEADER_NAME, ST.Utils.Constants.BASE_NAMESPACE ) );

          if (binding is WebHttpBinding)
          {
            var cookieHeader = WebOperationContext.Current.IncomingRequest.Headers[HttpRequestHeader.Cookie];

            if (cookieHeader != null)
              ulong.TryParse(GetSessionIdFromCookies(cookieHeader), out sessionId);

            if (sessionId == 0)
            {
              var rawRequest = OperationContext.Current.IncomingMessageProperties["Via"] as Uri;

              if (rawRequest != null)
                ulong.TryParse(HttpUtility.ParseQueryString(rawRequest.Query)["sessionId"], out sessionId);
            }

            var prop = request.Properties["httpRequest"] as HttpRequestMessageProperty;

            if (prop != null)
            {
              var accept = prop.Headers[HttpRequestHeader.AcceptEncoding];

              if (!string.IsNullOrEmpty(accept) && accept.Contains("gzip") && !accept.Contains("deflate"))
              {
                var item = new DoCompressExtension();
                OperationContext.Current.Extensions.Add(item);
              }
            }
          }
          else
            try
            {
              sessionId = CustomHeader.ReadHeader(request).IfNotNull(header => header.SessionId);
            }
            catch (Exception e)
            {
              WriteToLog(request.ToString(), 1);

              WriteToLog(e);
            }

          session = _sessions.GetValue(sessionId);

          if (session != null)
            session.Refresh();
          else
          {
            if (sessionId != 0)
            {
              var authModule = GetAuthModule();

              var dbSession = authModule.GetDBSession(sessionId);

              if (dbSession == null)
                throw new NotLoggedOnException();

              session = new Session(dbSession.SessionId, authModule, dbSession.Login, dbSession.CreatedIP, dbSession.Permissions, dbSession.Culture != null ? new CultureInfo(dbSession.Culture) : Config.Culture, dbSession.OrgGroupId, Config.ProductName, Config.SaveTrafficTime);

              session.MessageTypes.AddRangeSafe(dbSession.MessageTypes);

              _sessions.GetOrAdd(session.Id, session);

              (ServerContext.Session = session).AuthModule.OnSessionCreated(dbSession.AuthenticationResult, () => DeleteSession(session));

            }
            else
              if (!(OperationContext.Current.EndpointDispatcher.ContractName == typeof(IServerEntryPoint).Name && OperationContext.Current.EndpointDispatcher.ContractNamespace == Interface.Constants.SERVER_NAMESPACE))
            {
              if (!(OperationContext.Current.EndpointDispatcher.ContractName == typeof(IServerEntryPoint).Name))
                throw new NotLoggedOnException();
            }
          }
        }
      }
      else
          if (bindingItem.ContextUserType == BindingHelper.ContextUserType.IP)
      {
        var logonModule = GetAuthModule();

        try
        {
          var ar = logonModule.AuthenticateByIP(ServerContext.ClientIP) as ILogonUser;

          if (ar == null)
            throw new UnknownUserException(ServerContext.ClientIP ?? "");

          var key = new SessionKey { IP = ServerContext.ClientIP };

          session = _sessions.GetValue(_loginSessions.GetValue(key));

          if (session != null)
            session.Refresh();
          else
          {
            ServerContext.Session = new Session(0, null, null, null, PermissionList.Empty, ar.Culture != null ? new CultureInfo(ar.Culture) : null, null, null, 0);

            var permissions = logonModule.Authorize(ar);

            if (permissions != null)
            {
              if (!_inranetRegex.IsMatch(ServerContext.ClientIP) && !permissions.Contains<Permissions.RemoteAccess>())
                throw new RemoteAccessRequiredException(ar.Login);

              session = new Session(Session.GetNextSessionId(), logonModule, ar.Login, null, permissions, ar.Culture != null ? new CultureInfo(ar.Culture) : null, ar.OrgGroupId, Config.ProductName, Config.SaveTrafficTime);

              _loginSessions.AddOrUpdate(key, session.Id, ( k, v ) => session.Id);
              _sessions.GetOrAddSessions(session.Id, session);

              (ServerContext.Session = session).AuthModule.OnSessionCreated(ar, () => DeleteSession(session));
            }
          }
        }
        catch (Exception exc)
        {
          if (exc.GetType() == typeof(OrgGroupLicenceException) || exc.GetType().IsInheritedFrom(typeof(LogonFailedException<>)))
            throw;

          OnUnhandledException(new UnknownAuthException(exc, ServerContext.ClientIP ?? "", logonModule));

          throw new AuthFailException();
        }

        if (session == null && !(OperationContext.Current.EndpointDispatcher.ContractName == typeof(IServerEntryPoint).Name && OperationContext.Current.EndpointDispatcher.ContractNamespace == Interface.Constants.SERVER_NAMESPACE))
        {
          if (!(OperationContext.Current.EndpointDispatcher.ContractName == typeof(IServerEntryPoint).Name))
            throw new NotLoggedOnException();
        }
      }
      else
            if (bindingItem.ContextUserType == BindingHelper.ContextUserType.Login)
      {
        var login = string.Empty;

        if (ServiceSecurityContext.Current != null && ServiceSecurityContext.Current.WindowsIdentity != null)
          login = ServiceSecurityContext.Current.WindowsIdentity.Name;

        if (string.IsNullOrEmpty(login))
        {
          var cred = ExtractCredentials(request);

          if (cred != null && cred.Length == 2)
            login = cred[0];
        }

        if (string.IsNullOrEmpty(login))
          throw new AuthFailException();

        var key = new SessionKey { IP = ServerContext.ClientIP, Login = login };

        session = _sessions.GetValue(_loginSessions.GetValue(key));

        if (session != null)
          session.Refresh();
        else
        {
          var logonModule = GetAuthModule();

          try
          {
            //var adGroups = GetADGroups();

            var ar = logonModule.Authenticate(key.Login, null) as ILogonUser;

            if (ar != null)
            {
              ServerContext.Session = new Session(0, null, null, null, PermissionList.Empty, ar.Culture != null ? new CultureInfo(ar.Culture) : null, null, null, 0);

              var permissions = logonModule.Authorize(ar);

              if (permissions != null)
              {
                if (!_inranetRegex.IsMatch(ServerContext.ClientIP) && !permissions.Contains<Permissions.RemoteAccess>())
                  throw new RemoteAccessRequiredException(ar.Login);

                session = new Session(Session.GetNextSessionId(), logonModule, ar.Login, null, permissions, ar.Culture != null ? new CultureInfo(ar.Culture) : null, ar.OrgGroupId, Config.ProductName, Config.SaveTrafficTime);

                _loginSessions.AddOrUpdate(key, session.Id, ( k, v ) => session.Id);
                _sessions.GetOrAddSessions(session.Id, session);

                (ServerContext.Session = session).AuthModule.OnSessionCreated(ar, () => DeleteSession(session));
              }
            }
          }
          catch (Exception exc)
          {
            if (exc.GetType() == typeof(OrgGroupLicenceException) || exc.GetType().IsInheritedFrom(typeof(LogonFailedException<>)))
              throw;

            OnUnhandledException(new UnknownAuthException(exc, ServerContext.ClientIP ?? "", logonModule));

            throw new AuthFailException();
          }
        }

        if (session == null && !(OperationContext.Current.EndpointDispatcher.ContractName == typeof(IServerEntryPoint).Name && OperationContext.Current.EndpointDispatcher.ContractNamespace == Interface.Constants.SERVER_NAMESPACE))
        {
          if (!(OperationContext.Current.EndpointDispatcher.ContractName == typeof(IServerEntryPoint).Name))
            throw new NotLoggedOnException();
        }
      }

      Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
      Thread.CurrentThread.CurrentUICulture = session == null ? Config.Culture : session.Culture;

      ServerContext.Session = session;

      var buffer = request.CreateBufferedCopy(int.MaxValue);

      request = buffer.CreateMessage();

      UpdateTraffic(buffer.CreateMessage(), null);

      return null;
    }

    void IDispatchMessageInspector.BeforeSendReply( ref Message reply, object correlationState )
    {
      if (OperationContext.Current.Extensions.OfType<DoCompressExtension>().Count() > 0)
      {
        var httpResponseProperty = (HttpResponseMessageProperty)reply.Properties[HttpResponseMessageProperty.Name] ?? new HttpResponseMessageProperty();

        httpResponseProperty.Headers.Add(HttpResponseHeader.ContentEncoding, "gzip");
        reply.Properties[HttpResponseMessageProperty.Name] = httpResponseProperty;
      }

      if (reply != null)
      {
        var buffer = reply.CreateBufferedCopy(int.MaxValue);

        reply = buffer.CreateMessage();

        UpdateTraffic(null, buffer.CreateMessage());
      }

      ServerContext.Session = null;
    }
    #endregion

    #region ExtractCredentials
    private string[] ExtractCredentials( Message requestMessage )
    {
      var request = (HttpRequestMessageProperty)requestMessage.Properties[HttpRequestMessageProperty.Name];

      var authHeader = request.Headers["Authorization"];

      WriteToLog(authHeader, 1);

      if (authHeader != null && authHeader.StartsWith("Basic"))
      {
        string encodedUserPass = authHeader.Substring(6).Trim();

        Encoding encoding = Encoding.GetEncoding("iso-8859-1");
        string userPass = encoding.GetString(Convert.FromBase64String(encodedUserPass));
        int separator = userPass.IndexOf(':');

        var credentials = new string[2];
        credentials[0] = userPass.Substring(0, separator);
        credentials[1] = userPass.Substring(separator + 1);

        return credentials;
      }

      return new string[] { };
    }
    #endregion

    #region UpdateTraffic
    private void UpdateTraffic( Message request, Message reply )
    {
      if (Config.SaveTrafficTime > 0 && ServerContext.Session != null && ServerContext.Session != Session.Empty)
        ServerContext.Session.UpdateTraffic(GetMessageSize(request), GetMessageSize(reply));
    }
    #endregion

    #region GetMessageSize
    private long? GetMessageSize( Message msg )
    {
      if (msg == null)
        return null;

      if (msg.IsEmpty)
        return Encoding.Unicode.GetByteCount(msg.Headers.To.ToString());

      using (MemoryStream ms = new MemoryStream())
      using (var memWriter = XmlDictionaryWriter.CreateTextWriter(ms))
      {
        msg.WriteMessage(memWriter);
        memWriter.Flush();

        return ms.Position;
      }

      //return Encoding.Unicode.GetByteCount( msg.ToString() );
    }
    #endregion

    #region IErrorHandler
    bool IErrorHandler.HandleError( Exception error )
    {
      error = error.GetRealException();

      if (!(error is CommunicationException) || (error is FaultException && (error as FaultException).InnerException != null))
        OnUnhandledException(error);

      return false;
    }

    void IErrorHandler.ProvideFault( Exception error, MessageVersion version, ref Message fault )
    {
      if (!(error is FaultException))
      {
        error = error.GetRealException();

        var faultException = error is FaultException ? error as FaultException :
                             error is IFaultExceptionProvider ? (error as IFaultExceptionProvider).GetFaultException(ServerContext.Session.Culture) :
                             null;

        if (faultException == null)
          faultException = new FaultException(error.GetFullMessage(), FaultCode.CreateSenderFaultCode(error.GetType().Name, error.GetType().Namespace));

        fault = Message.CreateMessage(version, faultException.CreateMessageFault(), null);
      }
    }
    #endregion

    #region IServerEntryPoint
    ServerCultureInfo[] IServerEntryPoint.GetSupportedCultures()
    {
      return new[]
      {
        new ServerCultureInfo{ Code = "ru", Name = "Русский" },
        //new LanguageInfo{ Code = "en", Name = "English" },
      };
    }

    void IServerEntryPoint.Logoff()
    {
      DeleteSession();
    }

    //[DebuggerStepThrough]
    AuthorizationToken IServerEntryPoint.Logon( UserCredentials credentials, ClientInfo clientInfo, string createdIP, string[][] fileHash, bool isRepeat )
    {
      var culture = GetCulture(clientInfo);

      //ServerContext.Session = new Session( 0, null, null, PermissionList.Empty, culture, null );

      credentials = credentials ?? new UserCredentials();

      credentials.Login = string.IsNullOrWhiteSpace(credentials.Login) ? null : credentials.Login.Trim();
      credentials.PasswordMD5 = string.IsNullOrWhiteSpace(credentials.PasswordMD5) ? null : credentials.PasswordMD5;

      if ((credentials.Login != null && credentials.PasswordMD5 == null) /*|| ServiceSecurityContext.Current.WindowsIdentity == null*/ )
        throw new InvalidCredentialsException();

      if (credentials.Login == null)
      {
        credentials.Login = ServiceSecurityContext.Current.WindowsIdentity.Name;
        credentials.PasswordMD5 = null;
      }

      var logonModule = GetAuthModule();

      try
      {
        var ar = logonModule.Authenticate(credentials.Login, credentials.PasswordMD5) as ILogonUser;

        if (ar != null)
        {
          if ((logonModule.IsWindowsUserOnly() && !ar.IsWindowsUser) || ar.ParentUserId.HasValue)
            throw new UnknownUserException(ar.Login ?? "");

          if (ar.OrgGroupId.HasValue && ar.SessionCount.HasValue && CheckSessionCountByOrgId(ar.OrgGroupId.Value, ar.SessionCount.Value))
            throw new OrgGroupLicenceException();

          if (ar.ExpireDate != null && ar.ExpireDate < DateTime.UtcNow)
            throw new UserAccountExpiredException(credentials.Login ?? "");

          ServerContext.Session = new Session(0, null, null, null, PermissionList.Empty, culture, null, null, 0);

          var permissions = logonModule.Authorize(ar);

          if (permissions != null)
          {
            if (!_inranetRegex.IsMatch(string.IsNullOrEmpty(createdIP) ? ServerContext.ClientIP : createdIP) && !permissions.Contains<Permissions.RemoteAccess>())
              throw new RemoteAccessRequiredException(credentials.Login);

            if (!permissions.Contains<Permissions.TCPIPAccess>() && (OperationContext.Current.InstanceContext.Host.Description.Endpoints[0].Binding is NetTcpBinding))
              throw new TCPIPOnlyAccessRequiredException(credentials.Login);

            if (permissions.Contains<Permissions.TCPIPOnlyAccess>() && !(OperationContext.Current.InstanceContext.Host.Description.Endpoints[0].Binding is NetTcpBinding))
              throw new TCPIPOnlyAccessRequiredException(credentials.Login);

            var session = new Session(Session.GetNextSessionId(), logonModule, credentials.Login, createdIP, permissions, ar.Culture != null ? new CultureInfo(ar.Culture) : culture, ar.OrgGroupId, Config.ProductName, Config.SaveTrafficTime);

            if (!permissions.Contains<Permissions.MultipleLogon>() && _sessions.Values.Any(s => s.Login.IsEqualCI(credentials.Login)))
            {
              if (_config.IsRequestExclusion && !isRepeat)
                throw new UserEnterExistException();

              Task.Factory.StartNew(() => _sessions.Values.ForEach(s => s.Login.IsEqualCI(credentials.Login), s => DeleteSession(s)), TaskCreationOptions.PreferFairness);
            }

            _sessions.GetOrAddSessions(session.Id, session);

            //Task.Factory.StartNew( () => (ServerContext.Session = session).AuthModule.OnSessionCreated( ar, () => DeleteSession( session ) ), TaskCreationOptions.PreferFairness );
            (ServerContext.Session = session).AuthModule.OnSessionCreated(ar, () => DeleteSession(session));

            var expireDate = ar.ExpireDate != null && logonModule.GetUserAccountExpirationWarningNecessary(ar.ExpireDate) ? ar.ExpireDate : null;

            return new AuthorizationToken
            {
              SessionId = session.Id,
              Permissions = session.Permissions.ToArray(),
              OrganizationId = ar.OrganizationId,
              UserAccountExpireDate = expireDate,
              NodeId = ar.NodeId,
              NodeUrl = ar.NodeUrl,
              NeedUpdate = IsNeedUpdate(fileHash),
              Login = ar.Login,
              AlternativeOrgGroups = logonModule.GetAlternativeOrgGroupList(ar.Login)
            };
          }
        }

        WriteToLog( string.Format( "{0}{1}Login: {2}{3}{4}", 
          SR.GetString( RI.UnknownUserException ), Environment.NewLine, credentials.Login ?? "", Environment.NewLine, string.IsNullOrEmpty( createdIP ) ? ServerContext.ClientIP : createdIP ), 1, EventLogEntryType.Warning );

        throw new UnknownUserException(credentials.Login ?? "");
      }
      catch (Exception exc)
      {
        if (exc.GetType() == typeof(OrgGroupLicenceException) || exc.GetType().IsInheritedFrom(typeof(LogonFailedException<>)))
          throw;

        OnUnhandledException(new UnknownAuthException(exc, credentials.Login ?? "", logonModule));

        throw new AuthFailException();
      }
    }

    AuthorizationToken IServerEntryPoint.LogonAs( int userId, ClientInfo clientInfo, string createdIP )
    {
      var culture = GetCulture(clientInfo);

      if (userId <= 0)
        throw new InvalidCredentialsException();

      var logonModule = GetAuthModule();

      try
      {
        var ar = logonModule.AuthenticateAs(userId) as ILogonUser;

        if (ar != null)
        {
          var currentSession = ServerContext.Session;

          if (ar.OrgGroupId.HasValue && ar.SessionCount.HasValue && CheckSessionCountByOrgId(ar.OrgGroupId.Value, ar.SessionCount.Value))
            throw new OrgGroupLicenceException();

          if (ar.ExpireDate != null && ar.ExpireDate < DateTime.UtcNow)
            throw new UserAccountExpiredException(ar.Login ?? "");

          ServerContext.Session = new Session(0, null, null, null, PermissionList.Empty, culture, null, null, 0);

          var permissions = logonModule.Authorize(ar);

          if (permissions != null)
          {
            if (!_inranetRegex.IsMatch(ServerContext.ClientIP) && !permissions.Contains<Permissions.RemoteAccess>())
              throw new RemoteAccessRequiredException(ar.Login);

            if (!permissions.Contains<Permissions.TCPIPAccess>() && (OperationContext.Current.InstanceContext.Host.Description.Endpoints[0].Binding is NetTcpBinding))
              throw new TCPIPOnlyAccessRequiredException(ar.Login);

            if (permissions.Contains<Permissions.TCPIPOnlyAccess>() && !(OperationContext.Current.InstanceContext.Host.Description.Endpoints[0].Binding is NetTcpBinding))
              throw new TCPIPOnlyAccessRequiredException(ar.Login);

            var session = new Session(Session.GetNextSessionId(), logonModule, ar.Login, createdIP, permissions, ar.Culture != null ? new CultureInfo(ar.Culture) : culture, ar.OrgGroupId, Config.ProductName, Config.SaveTrafficTime);

            _sessions.GetOrAddSessions(session.Id, session);

            if (!permissions.Contains<Permissions.MultipleLogon>())
              Task.Factory.StartNew(() => _sessions.Values.ForEach(s => s.Id != session.Id && s.Login.IsEqualCI(ar.Login), s => DeleteSession(s)), TaskCreationOptions.PreferFairness);

            //Task.Factory.StartNew( () => (ServerContext.Session = session).AuthModule.OnSessionCreated( ar, () => DeleteSession( session ) ), TaskCreationOptions.PreferFairness );
            (ServerContext.Session = session).AuthModule.OnSessionCreated(ar, () => DeleteSession(session));

            var expireDate = ar.ExpireDate != null && logonModule.GetUserAccountExpirationWarningNecessary(ar.ExpireDate) ? ar.ExpireDate : null;

            Task.Factory.StartNew(() => DeleteSession(currentSession), TaskCreationOptions.PreferFairness);

            return new AuthorizationToken
            {
              SessionId = session.Id,
              Permissions = session.Permissions.ToArray(),
              OrganizationId = ar.OrganizationId,
              UserAccountExpireDate = expireDate,
              NodeId = ar.NodeId,
              NodeUrl = ar.NodeUrl,
              AlternativeOrgGroups = logonModule.GetAlternativeOrgGroupList(ar.Login)
            };
          }
        }

        throw new UnknownUserException(userId.ToString());
      }
      catch (Exception exc)
      {
        if (exc.GetType() == typeof(OrgGroupLicenceException) || exc.GetType().IsInheritedFrom(typeof(LogonFailedException<>)))
          throw;

        OnUnhandledException(new UnknownAuthException(exc, userId.ToString(), logonModule));

        throw new AuthFailException();
      }
    }

    AuthorizationToken IServerEntryPoint.TrustedLogOn( UserCredentials credentials, ClientInfo clientInfo, string createdIP )
    {
      var culture = GetCulture(clientInfo);

      credentials = credentials ?? new UserCredentials();

      credentials.Login = string.IsNullOrWhiteSpace(credentials.Login) ? null : credentials.Login.Trim();
      credentials.PasswordMD5 = string.IsNullOrWhiteSpace(credentials.PasswordMD5) ? null : credentials.PasswordMD5;

      if ((credentials.Login == null) /*|| ServiceSecurityContext.Current.WindowsIdentity == null*/ )
        throw new InvalidCredentialsException();

      var logonModule = GetAuthModule();

      try
      {
        var ar = logonModule.Authenticate(credentials.Login, credentials.PasswordMD5) as ILogonUser;

        if (ar != null)
        {
          if (ar.ParentUserId.HasValue && !logonModule.IsParentUser(ar.Login))
            throw new UnknownUserException(ar.Login ?? "");

          if (ar.OrgGroupId.HasValue && ar.SessionCount.HasValue && CheckSessionCountByOrgId(ar.OrgGroupId.Value, ar.SessionCount.Value))
            throw new OrgGroupLicenceException();

          if (ar.ExpireDate != null && ar.ExpireDate < DateTime.UtcNow)
            throw new UserAccountExpiredException(ar.Login ?? "");

          ServerContext.Session = new Session(0, null, null, null, PermissionList.Empty, culture, null, null, 0);

          var permissions = logonModule.Authorize(ar);

          if (permissions != null)
          {
            if (!_inranetRegex.IsMatch(ServerContext.ClientIP) && !permissions.Contains<Permissions.RemoteAccess>())
              throw new RemoteAccessRequiredException(ar.Login);

            if (!permissions.Contains<Permissions.TCPIPAccess>() && (OperationContext.Current.InstanceContext.Host.Description.Endpoints[0].Binding is NetTcpBinding))
              throw new TCPIPOnlyAccessRequiredException(credentials.Login);

            if (permissions.Contains<Permissions.TCPIPOnlyAccess>() && !(OperationContext.Current.InstanceContext.Host.Description.Endpoints[0].Binding is NetTcpBinding))
              throw new TCPIPOnlyAccessRequiredException(ar.Login);

            var session = new Session(Session.GetNextSessionId(), logonModule, ar.Login, createdIP, permissions, ar.Culture != null ? new CultureInfo(ar.Culture) : culture, ar.OrgGroupId, Config.ProductName, Config.SaveTrafficTime);

            _sessions.GetOrAddSessions(session.Id, session);

            if (!permissions.Contains<Permissions.MultipleLogon>())
              Task.Factory.StartNew(() => _sessions.Values.ForEach(s => s.Id != session.Id && s.Login.IsEqualCI(ar.Login), s => DeleteSession(s)), TaskCreationOptions.PreferFairness);

            //Task.Factory.StartNew( () => (ServerContext.Session = session).AuthModule.OnSessionCreated( ar, () => DeleteSession( session ) ), TaskCreationOptions.PreferFairness );
            (ServerContext.Session = session).AuthModule.OnSessionCreated(ar, () => DeleteSession(session));

            var expireDate = ar.ExpireDate != null && logonModule.GetUserAccountExpirationWarningNecessary(ar.ExpireDate) ? ar.ExpireDate : null;

            return new AuthorizationToken
            {
              SessionId = session.Id,
              Permissions = session.Permissions.ToArray(),
              OrganizationId = ar.OrganizationId,
              UserAccountExpireDate = expireDate,
              NodeId = ar.NodeId,
              NodeUrl = ar.NodeUrl,
              AlternativeOrgGroups = logonModule.GetAlternativeOrgGroupList(ar.Login)
            };
          }
        }

        throw new UnknownUserException(credentials.Login ?? "");
      }
      catch (Exception exc)
      {
        if (exc.GetType() == typeof(OrgGroupLicenceException) || exc.GetType().IsInheritedFrom(typeof(LogonFailedException<>)))
          throw;

        OnUnhandledException(new UnknownAuthException(exc, credentials.Login ?? "", logonModule));

        throw new AuthFailException();
      }
    }

    AuthorizationToken IServerEntryPoint.TrustedLogonAlternative( int orgGroupId )
    {
      var logonModule = GetAuthModule();

      var uCreds = logonModule.GetUserCredentials(orgGroupId);

      if (uCreds == null)
        throw new UnknownUserException("");

      var currentSession = ServerContext.Session;

      var res = (this as IServerEntryPoint).TrustedLogOn(new UserCredentials { Login = uCreds.Login, PasswordMD5 = uCreds.PasswordMD5 }, null, null);

      Task.Factory.StartNew(() => DeleteSession(currentSession), TaskCreationOptions.PreferFairness);

      return res;
    }

    private CultureInfo GetCulture( ClientInfo clientInfo )
    {
      var culture = Config.Culture;

      if (clientInfo != null && !string.IsNullOrWhiteSpace(clientInfo.Culture))
        Exec.Try(() => culture = CultureInfo.GetCultureInfo(clientInfo.Culture));

      return culture;
    }

    ServerInfo IServerEntryPoint.GetInfo()
    {
      var assembly = Assembly.GetEntryAssembly();

      var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);

      return new ServerInfo
      {
        HttpPort = Config.GetPort(WcfProtocolType.Http),
        TcpPort = Config.GetPort(WcfProtocolType.Tcp),
        WinTcpPort = Config.GetPort(WcfProtocolType.WinTcp),
        TcpZippedPort = Config.GetPort(WcfProtocolType.TcpZippedOut),
        WinTcpZippedPort = Config.GetPort(WcfProtocolType.WinTcpZippedOut),
        BasicHttpPort = Config.GetPort(WcfProtocolType.BasicHttp),
        JsonPort = Config.GetPort(WcfProtocolType.Json),
        SecHttpPort = Config.GetPort(WcfProtocolType.SecHttp),
        WinSecHttpPort = Config.GetPort(WcfProtocolType.WinSecHttp),
        SecJsonPort = Config.GetPort(WcfProtocolType.SecJson),
        OpenHttpPort = Config.GetPort(WcfProtocolType.OpenHttp),
        JsonZippedPort = Config.GetPort(WcfProtocolType.JsonZipped),
        OpenJsonPort = Config.GetPort(WcfProtocolType.OpenJson),
        WinHttpPort = Config.GetPort(WcfProtocolType.WinHttp),
        ServerVersion = fvi.FileVersion,
        ProductName = Config.ProductName,
        UseSHA256Hash = Config.UseSHA256Hash
      };
    }

    byte[] IServerEntryPoint.GetUpdateFile()
    {
      if (string.IsNullOrEmpty(Config.ActualShellPath))
        return null;

      return File.ReadAllBytes(Config.ActualShellPath);
    }

    bool IServerEntryPoint.CheckShellUpdate( string[][] fileHash )
    {
      return IsNeedUpdate(fileHash);
    }
    #endregion

    #region IServiceBehavior
    void IServiceBehavior.AddBindingParameters( ServiceDescription serviceDescription, ServiceHostBase serviceHost, Collection<CoreWCF.Description.ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters )
    {
    }

    void IServiceBehavior.ApplyDispatchBehavior( ServiceDescription serviceDescription, ServiceHostBase serviceHost )
    {
      var dispatchers = serviceHost.ChannelDispatchers.OfType<ChannelDispatcher>();

      dispatchers.ForEach(d =>
     {
       d.ErrorHandlers.Add(this);

       d.Endpoints.ForEach(e => e.DispatchRuntime.MessageInspectors.Add(this));
     });
    }

    void IServiceBehavior.Validate( ServiceDescription serviceDescription, ServiceHostBase serviceHost )
    {
    }
    #endregion

    #region ISessionManager
    T ISessionManager.Get<T>()
    {
      return GetSessionData<T>().FirstOrDefault();
    }

    List<T> ISessionManager.GetMany<T>()
    {
      return GetSessionData<T>().ToList();
    }

    void ISessionManager.Remove<T>()
    {
      var moduleData = ServerContext.Session.Data.GetValue(CallerModule);

      if (moduleData != null)
        moduleData.RemoveValue(typeof(T));
    }

    void ISessionManager.Set<T>( T data )
    {
      ServerContext.Session.Data.GetOrAdd(CallerModule, new ConcurrentDictionary<Type, object>()).AddOrUpdate(typeof(T), data, ( t, d ) => data);
    }
    #endregion

    #region IWcfServer
    void IWcfServer.Send<T>( [NotNull] T msg, [NotNull] Func<T, bool> filter )
    {
      Send(msg);

      SendToSessions(msg, s => ExecForSession(s, () => filter(msg)));
    }

    void IWcfServer.ExecuteForSession( ulong sessionId, Action action )
    {
      var session = _sessions.Values.FirstOrDefault(s => s.Id == sessionId);

      if (session != null)
        ExecForSession(session, action);
    }
    #endregion

    #region ConfigureEndpoint
    private void ConfigureEndpoint( Type interfaceType, Binding binding, ServiceEndpoint e, IServiceProvider serviceProvider )
    {
      if (binding is WebHttpBinding)
        e?.EndpointBehaviors?.Add(new JsonErrorWebHttpBehavior(serviceProvider) { DefaultOutgoingResponseFormat = WebMessageFormat.Json, AutomaticFormatSelectionEnabled = false });

      e.ExtendOperations();

      if (interfaceType == typeof(ICommunication))
        e?.Contract?.Operations?.Find("Send")?.OperationBehaviors.IfNotNull(op =>
        {
          if (!op.Contains(SendOperationBehavior.Instance))
            op.Add(SendOperationBehavior.Instance);
        });

    }

    private void ConfigureWebEndpoint( Type interfaceType, Binding binding, WebHttpBehavior e, IServiceProvider serviceProvider )
    {
      if (binding is WebHttpBinding)
      {
        e.DefaultOutgoingResponseFormat = WebMessageFormat.Json;
        e.AutomaticFormatSelectionEnabled = false;
      }
    }
    #endregion

    #region GetHostInfo
    private HostBindInfo GetHostInfo( Binding binding, object server, Type[] interfaces, string schemeName, int port )
    {
      HostBindInfo result = new ();

      var t = server.GetType();

      var sb = t.GetAttribute<ServiceBehaviorAttribute>();

      //var nameSpace = sb != null && string.IsNullOrWhiteSpace(sb.Namespace) ? string.Format("{0}/{1}", ST.Utils.Constants.BASE_NAMESPACE, t.Name) : sb.Namespace;
      var name = sb != null && string.IsNullOrWhiteSpace(sb.Name) ? t.Name : sb.Name;

      result.Binding = binding;
      result.Interfaces = interfaces;
      result.SchemeName = schemeName;
      result.Port = port;
      result.Name = name;
      result.ConfigureEndpoint = ConfigureEndpoint;
      result.ConfigureWebEndpoint = ConfigureWebEndpoint;

      foreach (var i in interfaces)
      {
        if (i.GetAttribute<WcfServiceAttribute>(true).Address != sb.Name)
          continue; //throw new WcfServiceException( new InvalidWcfServiceAddressException( i ), host, server as BaseModule );

      }

      return result;
    }
    #endregion

    #region GetHostInfos
    public HostBindInfo[] GetHostInfos( object server )
    {
      var hosts = new List<HostBindInfo>();

      if (server.IsDefined<WcfServerAttribute>(true))
      {
        var interfaces = (from i in server.GetType().GetInterfaces()
                          where i.IsDefined<WcfServiceAttribute>() //&& i.GetMethods().Any( m => m.IsDefined<OperationContractAttribute>() )
                          select i).ToArray();

        if (interfaces.Length > 0)
        {
          _bindings.ForEach(b =>
          {
            var port = Config.GetPort(b.Protocol);

            if (port > 0)
            {
              var host = GetHostInfo(b.Binding, server, interfaces, b.Protocol.GetDescription(), port);

              //if (b.PostConfigure != null)
              //  b.PostConfigure(host);

              hosts.Add(host);
            }
          });

          _config.CustomBindings.IfNotNull(c => c.ForEach(cb =>
          {
            if (cb.Port > 0)
            {
              var bInfo = GetBindingInfo(cb);

              var host = GetHostInfo(bInfo.Binding, server, interfaces, cb.UseTransportLevelSecurity ? "https" : cb.TransferType.GetDescription(), cb.Port);

              //if (bInfo.PostConfigure != null)
              //  bInfo.PostConfigure(host);

              hosts.Add(host);

              _bindingDic.GetOrAdd(cb.Port, cb);
            }
          }));
        }
      }

      return hosts.ToArray();
    } 
    #endregion

    private sealed class SendOperationBehavior : IOperationBehavior
    {
      #region .Static Fields
      public static SendOperationBehavior Instance = new SendOperationBehavior();
      #endregion

      #region IOperationBehavior
      void IOperationBehavior.AddBindingParameters( OperationDescription operationDescription, BindingParameterCollection bindingParameters )
      {
      }

      void IOperationBehavior.ApplyClientBehavior( OperationDescription operationDescription, ClientOperation clientOperation )
      {
      }

      void IOperationBehavior.ApplyDispatchBehavior( OperationDescription operationDescription, DispatchOperation dispatchOperation )
      {
        DataContractSerializerOperationBehavior serializerBehavior = operationDescription.OperationBehaviors.Find<DataContractSerializerOperationBehavior>();

        if (dispatchOperation.Formatter == null)
          (serializerBehavior as IOperationBehavior).ApplyDispatchBehavior(operationDescription, dispatchOperation);

        dispatchOperation.Formatter = new SendOperationMessageFormatter(dispatchOperation.Formatter);
      }

      void IOperationBehavior.Validate( OperationDescription operationDescription )
      {
      }
      #endregion
    }

    private sealed class SendOperationMessageFormatter : IDispatchMessageFormatter
    {
      #region .Fields
      private IDispatchMessageFormatter _baseFormatter;
      #endregion

      #region .Ctor
      public SendOperationMessageFormatter( IDispatchMessageFormatter baseFormatter )
      {
        _baseFormatter = baseFormatter;
      }
      #endregion

      #region IDispatchMessageFormatter
      void IDispatchMessageFormatter.DeserializeRequest( Message message, object[] parameters )
      {
        try
        {
          _baseFormatter.DeserializeRequest(message, parameters);
        }
        catch (FaultException exc)
        {
          if (exc.InnerException is SerializationException)
          {
            var throwException = true;

            var xEnvelope = XElement.Parse(message.ToString());

            var soapBody = xEnvelope.Descendants(XNamespace.Get(Constants.SOAP_ENVELOPE_NAMESPACE) + "Body").First();

            var sendElement = soapBody.Descendants(XNamespace.Get(Interface.Constants.SERVER_NAMESPACE) + "Send").FirstOrDefault();

            if (sendElement != null)
            {
              var msg = sendElement.Descendants().FirstOrDefault();

              if (msg != null)
              {
                var type = msg.Attribute(XNamespace.Get(Constants.XMLSCHEMA_INSTANCE_NAMESPACE) + "type");

                if (type != null)
                {
                  // Get all namespaces from msg element as Dictionary<prfix,namespaceName>.
                  var elementNamespaces = msg.Attributes().Where(a => a.IsNamespaceDeclaration).GroupBy(a => a.Name.Namespace == XNamespace.None ? String.Empty : a.Name.LocalName, a => a.Value).ToDictionary(g => g.Key, g => g.First());

                  foreach (var element in msg.DescendantsAndSelf())
                  {
                    element.Name = element.Name.LocalName;

                    var attributeList = element.Attributes().ToList();

                    element.Attributes().Remove();

                    foreach (var attribute in attributeList)
                      if (!attribute.IsNamespaceDeclaration)
                        element.Add(new XAttribute(attribute.Name.LocalName, attribute.Value));
                  }

                  var index = type.Value.IndexOf(':');

                  // There is a semicolon inside the attribute's value and it equals to namespace prefix
                  if (index != -1 && elementNamespaces.ContainsKey(type.Value.Substring(0, index)))
                    type.Value = type.Value.Substring(index + 1);

                  parameters[0] = new UntypedCommunicationMessage { Body = msg.GetInnerXml(), Type = type.Value };

                  throwException = false;
                }
              }
            }

            if (throwException)
              throw;
          }
          else
            throw;
        }
      }

      Message IDispatchMessageFormatter.SerializeReply( MessageVersion messageVersion, object[] parameters, object result )
      {
        return _baseFormatter.SerializeReply(messageVersion, parameters, result);
      }
      #endregion
    }

    private sealed class BindingInfo
    {
      #region .Properties
      public Binding Binding { get; set; }
      public WcfProtocolType Protocol { get; set; }
      public Action<ServiceHostBase> PostConfigure { get; set; }
      #endregion

      #region BindingInfo
      public BindingInfo( Binding binding, WcfProtocolType protocol, Action<ServiceHostBase> postConfigure = null )
      {
        Binding = binding;
        Protocol = protocol;
        PostConfigure = postConfigure;
      }
      #endregion
    }

    /// <summary>
    /// Идентификаторы событий для журнала событий (2000 - 2999).
    /// </summary>
    protected internal static new class LogEventId
    {
      #region .Constants
      /// <summary>
      /// Ошибка, возникшая при удалении сессии.
      /// </summary>
      public const ushort DeletingSessionError = 2000;

      /// <summary>
      /// Невозможно запустить WCF-сервис.
      /// </summary>
      public const ushort WcfServiceError = 2100;

      /// <summary>
      /// Ошибка аутентификации/авторизации пользователя.
      /// </summary>
      public const ushort UnknownAuthError = 2200;
      #endregion
    }

#if DEBUG
    private sealed class DebugLogonModule : IAuthModule
    {
      #region .Static Fields
      public static readonly DebugLogonModule Instance = new DebugLogonModule();

      private static readonly PermissionList _permissions = new PermissionList(new[]
      {
        Permission.GetCode<Permissions.RemoteAccess>(),
        Permission.GetCode<Permissions.MultipleLogon>(),
        Permissions.Special.DEBUG_LOGON
      }, true);
      #endregion

      #region IAuthModule
      object IAuthModule.Authenticate( string login, string passwordMD5 )
      {
        return passwordMD5 == null &&
               (Thread.CurrentPrincipal as WindowsPrincipal).IsInRole(WindowsBuiltInRole.Administrator) &&
               (Thread.CurrentPrincipal.Identity as WindowsIdentity).User == ServiceSecurityContext.Current.WindowsIdentity.User ? _permissions : null;
      }

      object IAuthModule.AuthenticateAs( int userId )
      {
        return null;
      }

      PermissionList IAuthModule.Authorize( object authenticationResult )
      {
        return _permissions;
      }

      void IAuthModule.OnSessionCreated( object authenticationResult, Action destroyer )
      {
      }

      void IAuthModule.OnSessionDeleted()
      {
      }

      bool IAuthModule.GetUserAccountExpirationWarningNecessary( DateTime? expireDate )
      {
        return false;
      }

      DBSession IAuthModule.AddDBSession( ulong sessionId, string Login, string Culture, string[] messageTypes, string createdIP )
      {
        return new DBSession();
      }

      DBSession[] IAuthModule.RestoreDBSessions()
      {
        return new DBSession[0];
      }

      void IAuthModule.ClearDBSessions()
      {
      }

      void IAuthModule.RemoveDBSessions( ulong sessionId )
      {
      }

      string[] IAuthModule.SetMessageTypes( ulong sessionId, string[] messageTypes )
      {
        return new string[0];
      }

      object IAuthModule.AuthenticateByIP( string ip )
      {
        throw new NotImplementedException();
      }

      DBSession IAuthModule.GetDBSession( ulong sessionId )
      {
        throw new NotImplementedException();
      }

      void IAuthModule.OnUserTrafficUpdate()
      {
        throw new NotImplementedException();
      }

      UserCredentials IAuthModule.GetUserCredentials( int orgGroupId )
      {
        throw new NotImplementedException();
      }

      AlternativeOrgGroup[] IAuthModule.GetAlternativeOrgGroupList( string login )
      {
        throw new NotImplementedException();
      }

      bool IAuthModule.IsParentUser( string login )
      {
        throw new NotImplementedException();
      }
      #endregion


      bool IAuthModule.IsWindowsUserOnly()
      {
        throw new NotImplementedException();
      }
    }
#endif

    private class ArrayQueryStringConverter : QueryStringConverter
    {
      #region CanConvert
      public override bool CanConvert( Type type )
      {
        return type.IsArray ? base.CanConvert(type.GetElementType()) : base.CanConvert(type);
      }
      #endregion

      #region ConvertStringToValue
      public override object ConvertStringToValue( string parameter, Type parameterType )
      {
        if (parameterType.IsArray && parameterType.GetElementType().IsPrimitive)
        {
          object result = null;

          try
          {
            result = JsonConvert.DeserializeObject(parameter, parameterType);
          }
          catch
          {
            throw;
          }

          return result;
        }
        else
          return base.ConvertStringToValue(parameter, parameterType);
      }
      #endregion
    }

    private class JsonErrorHandler : IErrorHandler
    {
      #region IErrorHandler Members
      public bool HandleError( Exception error )
      {
        return true;
      }

      public void ProvideFault( Exception error, MessageVersion version, ref Message fault )
      {
        var wbf = new WebBodyFormatMessageProperty(WebContentFormat.Json);

        var rmp = new HttpResponseMessageProperty();

        if (error is FaultException)
        {
          //var detail = error.GetType().GetProperty( "Detail" ).GetGetMethod().Invoke( error, null );
          //fault = Message.CreateMessage( version, "", detail, new DataContractJsonSerializer( detail.GetType() ) );

          fault = Message.CreateMessage(version, "", error.Message, new DataContractJsonSerializer(typeof(string)));

          rmp.StatusCode = HttpStatusCode.BadRequest;

          rmp.StatusDescription = "See fault string object for more information.";
        }
        else
        {
          fault = Message.CreateMessage(version, "", "An non-fault exception is occured.", new DataContractJsonSerializer(typeof(string)));

          rmp.StatusCode = HttpStatusCode.InternalServerError;

          rmp.StatusDescription = "Uknown exception.";
        }

        fault.Properties.Add(WebBodyFormatMessageProperty.Name, wbf);

        fault.Properties.Add(HttpResponseMessageProperty.Name, rmp);
      }
      #endregion
    }

    private class JsonErrorWebHttpBehavior : WebHttpBehavior
    {
      public JsonErrorWebHttpBehavior( IServiceProvider serviceProvider ) : base(serviceProvider)
      { }

      #region AddServerErrorHandlers
      protected override void AddServerErrorHandlers( CoreWCF.Description.ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher )
      {
        endpointDispatcher.ChannelDispatcher.ErrorHandlers.Clear();

        endpointDispatcher.ChannelDispatcher.ErrorHandlers.Add(new JsonErrorHandler());
      }
      #endregion

      #region GetQueryStringConverter
      protected override QueryStringConverter GetQueryStringConverter( OperationDescription operationDescription )
      {
        return new ArrayQueryStringConverter();
      }
      #endregion
    }

    private class SessionKey
    {
      #region .Properties
      public string Login { get; set; }
      public string IP { get; set; }
      #endregion

      #region Equals
      public override bool Equals( object obj )
      {
        var sd = obj as SessionKey;

        return sd.Login == Login && sd.IP == sd.IP;
      }
      #endregion

      #region GetHashCode
      public override int GetHashCode()
      {
        return (string.IsNullOrEmpty(Login) ? 0 : Login.GetHashCode()) ^ (string.IsNullOrEmpty(IP) ? 0 : IP.GetHashCode());
      }
      #endregion
    }
  }

  /// <summary>
  /// Конфигурация WCF-сервера.
  /// </summary>
  [Serializable]
  public class WcfServerConfig : BaseServerConfig
  {
    #region .Properties
    /// <summary>
    /// Порт сервера.
    /// </summary>
    [DisplayNameLocalized(RI.Port)]
    [CategoryLocalized(RI.ConnectionSettings)]
    public ushort Port { get; set; }

    /// <summary>
    /// Признак того, что сервер поддерживает работу через Tcp.
    /// </summary>
    [DisplayNameLocalized(RI.TcpEnabled)]
    [CategoryLocalized(RI.ConnectionSettings)]
    public bool TcpEnabled { get; set; }

    /// <summary>
    /// Признак того, что сервер поддерживает работу, используя аутентификацию Windows.
    /// </summary>
    [DisplayNameLocalized(RI.WindowsAuthenticationEnabled)]
    [CategoryLocalized(RI.ConnectionSettings)]
    public bool WindowsAuthenticationEnabled { get; set; }

    /// <summary>
    /// Признак того, что сервер поддерживает работу через Tcp со сжатием.
    /// </summary>
    [DisplayNameLocalized(RI.TcpZippedEnabled)]
    [CategoryLocalized(RI.ConnectionSettings)]
    public bool TcpZippedEnabled { get; set; }

    /// <summary>
    /// Признак того, что сервер поддерживает работу через Http.
    /// </summary>
    [DisplayNameLocalized(RI.HttpEnabled)]
    [CategoryLocalized(RI.ConnectionSettings)]
    public bool HttpEnabled { get; set; }

    /// <summary>
    /// Признак того, что сервер поддерживает работу через Http в формате Json.
    /// </summary>
    [DisplayNameLocalized(RI.JsonEnabled)]
    [CategoryLocalized(RI.ConnectionSettings)]
    public bool JsonEnabled { get; set; }

    /// <summary>
    /// Признак того, что сервер поддерживает работу через Http в формате Json со сжатием (zip).
    /// </summary>
    [DisplayNameLocalized(RI.JsonZippedEnabled)]
    [CategoryLocalized(RI.ConnectionSettings)]
    public bool JsonZippedEnabled { get; set; }

    /// <summary>
    /// Признак того, что сервер поддерживает работу через Http в формате Json c поддержкой security на уровне транспорта.
    /// </summary>
    [DisplayNameLocalized(RI.SecJsonEnabled)]
    [CategoryLocalized(RI.ConnectionSettings)]
    public bool SecJsonEnabled { get; set; }

    /// <summary>
    /// Признак того, что сервер поддерживает работу через Http c поддержкой security на уровне транспорта.
    /// </summary>
    [DisplayNameLocalized(RI.SecHttpEnabled)]
    [CategoryLocalized(RI.ConnectionSettings)]
    public bool SecHttpEnabled { get; set; }

    /// <summary>
    /// Признак того, что сервер поддерживает работу через Http c поддержкой security на уровне транспорта и Windows аутентификацией.
    /// </summary>
    [DisplayNameLocalized(RI.WinSecHttpEnabled)]
    [CategoryLocalized(RI.ConnectionSettings)]
    public bool WinSecHttpEnabled { get; set; }

    /// <summary>
    /// Признак того, что сервер поддерживает работу через Http c Windows аутентификацией.
    /// </summary>
    [DisplayNameLocalized(RI.WinHttpEnabled)]
    [CategoryLocalized(RI.ConnectionSettings)]
    public bool WinHttpEnabled { get; set; }

    /// <summary>
    /// Признак того, что сервер поддерживает работу через Http без аутентификации.
    /// </summary>
    [DisplayNameLocalized(RI.OpenHttpEnabled)]
    [CategoryLocalized(RI.ConnectionSettings)]
    public bool OpenHttpEnabled { get; set; }

    /// <summary>
    /// Признак того, что сервер поддерживает работу через Http без аутентификации.
    /// </summary>
    [DisplayNameLocalized(RI.OpenJsonEnabled)]
    [CategoryLocalized(RI.ConnectionSettings)]
    public bool OpenJsonEnabled { get; set; }

    /// <summary>
    /// Признак того, что сервер поддерживает работу через Http с аутентификацией по IP.
    /// </summary>
    [DisplayNameLocalized(RI.OpenJsonByIpEnabled)]
    [CategoryLocalized(RI.ConnectionSettings)]
    public bool OpenJsonByIpEnabled { get; set; }

    /// <summary>
    /// Основной язык (культура).
    /// </summary>
    [DisplayNameLocalized(RI.GlobalLanguage)]
    [DescriptionLocalized(RI.GlobalLanguageDescription)]
    [CategoryLocalized(RI.RegionSettings)]
    [TypeConverter(typeof(DisplayNameCultureInfoConverter))]
    public CultureInfo Culture { get; set; }

    /// <summary>
    /// Название продукта.
    /// </summary>
    [CategoryLocalized(RI.ClientSettings)]
    [DisplayNameLocalized(RI.ProductName)]
    [DescriptionLocalized(RI.ProductNameDescription)]
    public string ProductName { get; set; }

    [CategoryLocalized(RI.CategoryUDPSignal)]
    [DisplayNameLocalized(RI.UDPHostName)]
    public string UDPHostName { get; set; }

    [CategoryLocalized(RI.CategoryUDPSignal)]
    [DisplayNameLocalized(RI.UDPPort)]
    public int UDPPort { get; set; }

    [CategoryLocalized(RI.CategoryUDPLogging)]
    [DisplayNameLocalized(RI.UDPLoggerId)]
    public ushort UDPLoggerId { get; set; }

    [CategoryLocalized(RI.ServerSettings)]
    [DisplayNameLocalized(RI.OperationTimeout)]
    public int? OperationTimeout { get; set; }

    [CategoryLocalized(RI.ServerSettings)]
    [DisplayNameLocalized(RI.SaveTrafficTime)]
    public int SaveTrafficTime { get; set; }

    [CategoryLocalized(RI.ConnectionSettings)]
    [DisplayNameLocalized(RI.CustomBindingsString)]
    public CustomBindingItem[] CustomBindings { get; set; }

    [CategoryLocalized(RI.ClientSettings)]
    [DisplayNameLocalized(RI.ActualShellPath)]
    [DescriptionLocalized(RI.ActualShellPathDescription)]
    public string ActualShellPath { get; set; }

    [CategoryLocalized(RI.Sessions)]
    [DisplayNameLocalized(RI.IsRequestExclusion)]
    [DescriptionLocalized(RI.IsRequestExclusion)]
    public bool IsRequestExclusion { get; set; }

    [CategoryLocalized(RI.ServerSettings)]
    [DisplayNameLocalized(RI.UseSHA256Hash)]
    [DescriptionLocalized(RI.UseSHA256Hash)]
    public bool UseSHA256Hash { get; set; }

    [CategoryLocalized(RI.ModuleSettings)]
    [DisplayNameLocalized(RI.ModuleItems)]
    //[ReadOnly(true)]
    public ModuleItem[] ModuleItems { get; set; }
    #endregion

    #region GetPort
    /// <summary>
    /// Возвращает номер порта для указанного типа протокола.
    /// </summary>
    /// <param name="protocol">Тип протокола.</param>
    /// <returns>Номер порта.</returns>
    public ushort GetPort( WcfProtocolType protocol )
    {
      return (protocol == WcfProtocolType.Http ||
              ((TcpEnabled && protocol == WcfProtocolType.Tcp) ||
               (TcpZippedEnabled && protocol == WcfProtocolType.TcpZippedOut) ||
               (TcpEnabled && WindowsAuthenticationEnabled && protocol == WcfProtocolType.WinTcp) ||
               (TcpZippedEnabled && WindowsAuthenticationEnabled && protocol == WcfProtocolType.WinTcpZippedOut) ||
               (JsonZippedEnabled && protocol == WcfProtocolType.JsonZipped) ||
               (JsonEnabled && protocol == WcfProtocolType.Json) ||
               (SecJsonEnabled && protocol == WcfProtocolType.SecJson) ||
               (SecHttpEnabled && protocol == WcfProtocolType.SecHttp) ||
               (WinSecHttpEnabled && protocol == WcfProtocolType.WinSecHttp) ||
               (WinHttpEnabled && protocol == WcfProtocolType.WinHttp) ||
               (OpenHttpEnabled && protocol == WcfProtocolType.OpenHttp) ||
               (OpenJsonEnabled && protocol == WcfProtocolType.OpenJson) ||
               (HttpEnabled && protocol == WcfProtocolType.BasicHttp))) ? (ushort)(Port + protocol) :
             (ushort)0;
    }
    #endregion

    #region InitializeInstance
    protected override void InitializeInstance()
    {
      base.InitializeInstance();

      Port = 55555;
      TcpEnabled = true;
      Culture = SR.DefaultCulture;
      ProductName = SR.GetString(RI.DefaultProductName);

      //ModuleItems = new ModuleItem[]
      //{ new ModuleItem { Name = "BusinessEntity" },
      //  new ModuleItem { Name = "EventLog" },
      //  new SessionModuleItem { Name = "FlagmanWeb", MaxSessions = 99 },
      //  new ModuleItem { Name = "FuelCalculation" },
      //  new ModuleItem { Name = "Monitoring" },
      //  new ModuleItem { Name = "Reporting" },
      //  new ModuleItem { Name = "Security" },
      //  new TerminalModuleItem { Name = "Telematics", MaxTerminals = 10000 }
      //};
    }
    #endregion
  }

  public class DoCompressExtension : IExtension<OperationContext>
  {
    public void Attach( OperationContext owner ) { }
    public void Detach( OperationContext owner ) { }
  }

  public class WrappingMessage : Message
  {
    Message _innerMsg;
    MessageBuffer _msgBuffer;
    Action<long> _saveTraffic;

    public WrappingMessage( Message inner, Action<long> saveTraffic )
    {
      this._innerMsg = inner;
      _msgBuffer = _innerMsg.CreateBufferedCopy(int.MaxValue);
      _innerMsg = _msgBuffer.CreateMessage();
      _saveTraffic = saveTraffic;
    }

    public override MessageHeaders Headers
    {
      get { return _innerMsg.Headers; }
    }

    protected override void OnWriteBodyContents( XmlDictionaryWriter writer )
    {
      _innerMsg.WriteBodyContents(writer);
    }

    public override MessageProperties Properties
    {
      get { return _innerMsg.Properties; }
    }

    public override MessageVersion Version
    {
      get { return _innerMsg.Version; }
    }

    protected override void OnWriteMessage( XmlDictionaryWriter writer )
    {
      base.OnWriteMessage(writer);
      writer.Flush();

      var copy = _msgBuffer.CreateMessage();
      DumpEncoderSize(writer, copy);
    }

    private void DumpEncoderSize( System.Xml.XmlDictionaryWriter writer, Message copy )
    {
      var ms = new MemoryStream();
      string configuredEncoder = string.Empty;
      if (writer is IXmlTextWriterInitializer)
      {
        var w = (IXmlTextWriterInitializer)writer;
        w.SetOutput(ms, Encoding.UTF8, true);
        configuredEncoder = "Text";
      }
      //else if (writer is IXmlMtomWriterInitializer)
      //{
      //  var w = (IXmlMtomWriterInitializer)writer;
      //  w.SetOutput(ms, Encoding.UTF8, int.MaxValue, "", null, null, true, false);
      //  configuredEncoder = "MTOM";
      //}
      else if (writer is IXmlBinaryWriterInitializer)
      {
        var w = (IXmlBinaryWriterInitializer)writer;
        w.SetOutput(ms, null, null, false);
        configuredEncoder = "Binary";
      }

      copy.WriteMessage(writer);
      writer.Flush();
      var size = ms.Position;

      if (_saveTraffic != null)
        _saveTraffic(size);
    }
  }
}
