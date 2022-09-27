using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
//using System.Management;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
//using System.Windows.Forms;

using Castle.DynamicProxy;

using ST.Utils;
using ST.Utils.Attributes;
using ST.Utils.Config;
using ST.Utils.Licence;
using ST.Utils.Threading;

namespace ST.Core
{
  /// <summary>
  /// Базовый класс сервера.
  /// </summary>
  public partial class BaseServer : IBaseServer
  {
    private const int c_EventLog_Max_Event_Message_Chars = ST.Utils.Constants.EVENTLOG_MAXEVENTMESSAGEBYTES / 2;

    #region .Static Fields
    private static readonly FieldInfo _serverField = typeof(BaseModule).GetField("_", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly ProxyGenerator _proxyGenerator = new ProxyGenerator();

    private static BaseServer _instance;
    private static object _instanceLock = new object();

    private object _logLock = new object();

    [ThreadStatic]
    [ThreadStaticContext]
    private static BaseModule _callerModule;

    internal protected static readonly int PID = Process.GetCurrentProcess().Id;
    #endregion

    #region .Static Properties
    /// <summary>
    /// Текущий экземпляр сервера.
    /// </summary>
    protected static BaseServer ServerInstance
    {
      get { return _instance; }
    }

    internal static bool UseDefaultConfig { get; set; }
    #endregion

    #region .Fields
    private static readonly Encoding _unicode = Encoding.Unicode;
    private static Encoding __win1251Enc = null;
    private static Encoding _win1251Enc() => __win1251Enc ??= Encoding.GetEncoding("windows-1251");

    private readonly Type[] _supportedInterfaces;

    // Array of char representation for ServerLogEntryType enum (index of array corresponds an int value of enum)
    private readonly char[] _logMsgTypes = { ' ', 'E', 'W', ' ', 'I' };

    private ServerState _serverState = ServerState.Stopped;

    private EventLog _log;
    private readonly object _logUDPLock = new object();

    private readonly List<BaseModule> _modules = new List<BaseModule>();
    private ReadOnlyCollection<BaseModule> _modulesRO = new ReadOnlyCollection<BaseModule>(new BaseModule[0]);

    //private readonly Dictionary<BaseModule, ManagementEventWatcher> _configWatchers = new Dictionary<BaseModule, ManagementEventWatcher>(ModuleComparer.Instance);
    //private ManagementEventWatcher _servirerConfigWatcher;

    /// <summary>
    /// Object for Task instances cancellation
    /// </summary>
    private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

    /// <summary>
    /// UDP client for logging (lazy initialization is used)
    /// </summary>
    private UdpClient _logUDPClient = null;

    private Assembly[] _assemblies = null;
    #endregion

    #region .Properties
    /// <summary>
    /// Журнал событий сервера.
    /// </summary>
    public EventLog ServerLog
    {
      get { return _log; }
    }

    /// <summary>
    /// Тип сервера.
    /// </summary>
    protected virtual ServerType ServerType
    {
      get { return ServerType.Unknown; }
    }

    /// <summary>
    /// Состояние сервера.
    /// </summary>
    protected ServerState ServerState
    {
      get { return _serverState; }
      private set
      {
        if (_serverState != value)
        {
          _serverState = value;

          Exec.Try(OnServerStateChanged);
        }
      }
    }

    /// <summary>
    /// Список загруженных модулей.
    /// </summary>
    protected ReadOnlyCollection<BaseModule> Modules
    {
      get { return _modulesRO; }
    }

    /// <summary>
    /// Текущий модуль - модуль, который в данный момент обращается к серверу.
    /// </summary>
    protected BaseModule CallerModule
    {
      get { return _callerModule; }
    }

    protected string UDPHostName { get; set; }
    protected int UDPPort { get; set; }
    /// <summary>
    /// Идентификатор для внешнего сервера-логгера
    /// </summary>   
    protected ushort UDPLoggerId { get; set; }
    #endregion

    #region .Ctor
    static BaseServer()
    {
    }

    public BaseServer()
    {
      SetInstance(this);

      _supportedInterfaces = GetType().GetInterfaces().Where(i => i.IsDefined<ServerInterfaceAttribute>() && i != typeof(IBaseServer)).ToArray();

      ThreadPool.SetMinThreads(4, 4);
      ThreadPool.SetMaxThreads(128, 128);
    }
    #endregion

    #region .Dtor
    ~BaseServer()
    {
      SetInstance(null);
    }
    #endregion

    #region AddModule
    private void AddModule(BaseModule module)
    {
      _modules.Add(module);

      UpdateModules();
    }
    #endregion

    #region DoForAllModules
    private void DoForAllModules(bool reverseOrder, Action<BaseModule> action, Func<BaseModule, Exception, Exception> exceptionGetter, Func<BaseModule, int?> timeoutGetter = null)
    {
      timeoutGetter = timeoutGetter ?? (_ => (int?)null);

      lock (_modules)
      {
        var list = reverseOrder ? _modules.Reverse<BaseModule>() : _modules;

        foreach (var m in list)
          try
          {
            if (!Task.Factory.StartNew(obj => (obj as ThreadStaticContext).Execute(() => action(m)), ThreadStaticContext.Capture(), TaskCreationOptions.PreferFairness).Wait(timeoutGetter(m) ?? GetOperationTimeout()))
              throw new ModuleOperationTimeOutException();
          }
          catch (Exception exc)
          {
            WriteToLog(exceptionGetter(m, exc));
          }
      }
    }
    #endregion

    #region GetConfig
    /// <summary>
    /// Возвращает конфигурацию сервера.
    /// </summary>
    /// <typeparam name="T">Тип конфигурации сервера.</typeparam>
    /// <param name="serverType">Тип сервера.</param>
    /// <returns>Конфигурация сервера.</returns>
    public static T GetConfig<T>(ServerType serverType)
      where T : BaseServerConfig, new()
    {
      return ConfigController.Get<T>(GetConfigRootPath(serverType), GetConfigSubPath(serverType), null) ?? new T();
    }
    #endregion

    #region GetConfigPath
    /// <summary>
    /// Возвращает корнево путь, по которому хранится конфигурация сервера.
    /// </summary>
    /// <param name="serverType">Тип сервера.</param>
    /// <returns>Путь реестра.</returns>
    public static string GetConfigRootPath(ServerType serverType)
    {
      return serverType == ServerType.ApplicationServer ? Constants.SERVER_REGISTRY_ROOT_PATH : Constants.SHELL_REGISTRY_ROOT_PATH;
    }

    /// <summary>
    /// Возвращает относительный путь раздела, по которому хранится конфигурация сервера.
    /// </summary>
    /// <param name="serverType">Тип сервера.</param>
    /// <returns>Путь реестра.</returns>
    public static string GetConfigSubPath(ServerType serverType)
    {
      return serverType == ServerType.ApplicationServer ? Constants.SERVER_REGISTRY_SUB_PATH : Constants.SHELL_REGISTRY_SUB_PATH;
    }
    #endregion

    #region GetModuleConfig
    /// <summary>
    /// Возвращает конфигурацию модуля.
    /// </summary>
    /// <param name="moduleType">Тип модуля.</param>
    /// <param name="serverType">Тип сервера.</param>
    /// <returns>Конфигурация модуля.</returns>
    public static ModuleConfig GetModuleConfig([NotNull] Type moduleType, ServerType serverType)
    {
      var configType = moduleType.GetAttribute<ConfigurableAttribute>();

      if (configType == null)
        return null;

      ModuleConfig config;

      if (UseDefaultConfig)
        config = configType.ConfigType.CreateFast() as ModuleConfig;
      else
      {
        config = ConfigController.Get<ModuleConfig>(GetModuleConfigRootPath(moduleType, serverType), GetModuleConfigSubPath(moduleType, serverType), null);

        if (config == null || config.GetType() != configType.ConfigType)
          config = configType.ConfigType.CreateFast() as ModuleConfig;
      }

      return config;
    }
    #endregion

    #region GetModuleConfigPath
    /// <summary>
    /// Возвращает корневой путь, по которому хранится конфигурация модуля.
    /// </summary>
    /// <param name="moduleType">Тип модуля.</param>
    /// <param name="serverType">Тип сервера.</param>
    /// <returns>Путь реестра.</returns>
    public static string GetModuleConfigRootPath([NotNull] Type moduleType, ServerType serverType)
    {
      return (serverType == ServerType.ApplicationServer ? Constants.SERVER_REGISTRY_ROOT_PATH : Constants.SHELL_REGISTRY_ROOT_PATH);
    }

    /// <summary>
    /// Возвращает относительный путь раздела, по которому хранится конфигурация модуля.
    /// </summary>
    /// <param name="moduleType">Тип модуля.</param>
    /// <param name="serverType">Тип сервера.</param>
    /// <returns>Путь реестра.</returns>
    public static string GetModuleConfigSubPath([NotNull] Type moduleType, ServerType serverType)
    {
      return (serverType == ServerType.ApplicationServer ? Constants.SERVER_MODULES_REGISTRY_PATH : Constants.SHELL_MODULES_REGISTRY_PATH) + "\\" + moduleType.FullName;
    }
    #endregion

    #region GetModuleEnabled
    /// <summary>
    /// Возвращает признак активности модуля.
    /// </summary>
    /// <param name="moduleType">Тип модуля.</param>
    /// <param name="serverType">Тип сервера.</param>
    /// <returns>True - модуль включен, False - модуль отключен.</returns>
    public static bool GetModuleEnabled([NotNull] Type moduleType, ServerType serverType)
    {
      return !GetModuleParameter<bool>(moduleType, serverType, Constants.MODULE_DISABLED_PARAMETER);
    }
    #endregion

    #region GetModuleParameter
    /// <summary>
    /// Возвращает значение параметра модуля.
    /// </summary>
    /// <typeparam name="T">Тип параметра. Поддерживаются только значимые типы, строка или массив байт.</typeparam>
    /// <param name="moduleType">Тип модуля.</param>
    /// <param name="serverType">Тип сервера.</param>
    /// <param name="name">Название параметра.</param>
    /// <returns>Значение параметра.</returns>
    public static T GetModuleParameter<T>([NotNull] Type moduleType, ServerType serverType, [NotNullNotEmpty] string name)
    {
      return GetParameter<T>(GetModuleConfigRootPath(moduleType, serverType), GetModuleConfigSubPath(moduleType, serverType), name);
    }
    #endregion

    #region GetOperationTimeout
    /// <summary>
    /// Получает заданный тайм-аут операции.
    /// </summary>
    protected virtual int GetOperationTimeout()
    {
      return Constants.OPERATION_TIMEOUT;
    }
    #endregion

    #region GetUnloadOperationTimeout
    /// <summary>
    /// Получает заданный тайм-аут операции выгрузки.
    /// </summary>
    protected virtual int GetUnloadOperationTimeout()
    {
      return Constants.UNLOAD_OPERATION_TIMEOUT;
    }
    #endregion

    #region GetParameter
    /// <summary>
    /// Возвращает значение параметра сервера.
    /// </summary>
    /// <typeparam name="T">Тип параметра. Поддерживаются только значимые типы, строка или массив байт.</typeparam>
    /// <param name="serverType">Тип сервера.</param>
    /// <param name="name">Название параметра.</param>
    /// <returns>Значение параметра.</returns>
    public static T GetParameter<T>(ServerType serverType, [NotNullNotEmpty] string name)
    {
      return GetParameter<T>(GetConfigRootPath(serverType), GetConfigSubPath(serverType), name);
    }

    private static T GetParameter<T>(string rootPath, string subPath, string name)
    {
      return ConfigController.GetValue<T>(rootPath, subPath, name);
    }
    #endregion

    #region IfModuleIs
    /// <summary>
    /// Выполняет действие, только если текущий модуль поддерживает указанный интерфейс.
    /// </summary>
    /// <typeparam name="T">Тип требуемого интерфейса.</typeparam>
    /// <param name="action">Действие.</param>
    protected void IfModuleIs<T>(Action<T> action)
      where T : class
    {
      CallerModule.IfIs<T>(action);
    }
    #endregion

    #region IsModuleTypeLoadable
    /// <summary>
    /// Возвращает признак того, что тип модуля может быть загружен сервером.
    /// </summary>
    /// <param name="type">Тип модуля.</param>
    /// <returns>True - модуль будет загружен сервером, False - модуль не будет загружен сервером.</returns>
    protected virtual bool IsModuleTypeLoadable(Type type)
    {
      return GetModuleEnabled(type, ServerType);
    }
    #endregion

    #region IsServiceSupported
    private static bool IsServiceSupported<T>()
    {
      var t = typeof(T);

      return t.IsInterface && !t.IsDefined<NotServiceInterfaceAttribute>() && t != typeof(IServiceProvider) && t != typeof(IContextServiceProvider);
    }
    #endregion

    #region LoadModule
    /// <summary>
    /// Загружает модуль указанного типа.
    /// </summary>
    /// <param name="type">Тип модуля.</param>
    /// <returns>True - модуль загружен без ошибок, False - возникли ошибки при загрузке модуля.</returns>
    [DebuggerStepThrough]
    protected bool LoadModule([InheritedFrom(typeof(BaseModule))] Type type)
    {
      lock (_modules)
        return LoadModuleImpl(type);
    }
    #endregion

    #region LoadModuleImpl
    [DebuggerStepThrough]
    private bool LoadModuleImpl(Type type)
    {
      var complete = true;

      if (_modules.All(m => m.GetType() != type))

        MesureAndLog(() =>
       {

         try
         {
           OnModuleCreating(type);

           var module = MesureAndLog(() => type.CreateFast() as BaseModule
             , string.Format("--- Modules ---\r\nBaseModule.ctor(). Создание модуля '{0}'", type.GetDisplayName(false, type.FullName)));

           _serverField.SetValue(module, _proxyGenerator.CreateInterfaceProxyWithTargetInterface(typeof(IBaseServer), _supportedInterfaces, this, new ServerInterceptor(module)));

           OnModuleInitializing(module);

           AddModule(module);

           try
           {
             if (!Task.Factory.StartNew(obj => (obj as ThreadStaticContext).Execute(() => MesureAndLog(module.Initialize, string.Format("--- Modules ---\r\nBaseModule.Initialize. Инициализация модуля '{0}'", module.GetDisplayName(false, type.FullName)))), ThreadStaticContext.Capture(), TaskCreationOptions.PreferFairness).Wait(module.GetInitializeTimeout() ?? GetOperationTimeout()))
             {
               WriteToLog(new ModuleLoadException(new ModuleOperationTimeOutException(), type));

               complete = false;
             }
           }
           catch
           {
             Exec.Try(() => OnModuleUninitializing(module));

             RemoveModule(_modules.Count - 1);

             throw;
           }

           Exec.Try(() => OnModuleInitialized(module), exc => { WriteToLog(new ModuleLoadException(exc, type)); complete = false; });
         }
         catch (Exception exc)
         {
           WriteToLog(new ModuleLoadException(exc, type));

           throw;
         }

       }, string.Format("--- Modules ---\r\nBaseServer.LoadModuleImpl. Загрузка и инициализация модуля '{0}'", type.GetDisplayName(false, type.FullName)));

      return complete;
    }
    #endregion

    #region OnModuleInitialized
    /// <summary>
    /// Вызывается после инициализации модуля.
    /// </summary>
    /// <param name="module">Модуль.</param>
    protected virtual void OnModuleInitialized(BaseModule module)
    {
      var type = module.GetType();

      if (type.IsDefined<ConfigurableAttribute>())
      {
        //var hive = ServerType == ServerType.ApplicationServer ? Constants.SERVER_REGISTRY_HIVE : "HKEY_USERS";

        //var keyPathBase = ServerType == ServerType.ApplicationServer ? "" : WindowsIdentity.GetCurrent().User.Value + "\\";

        SetModuleConfig(type, ServerType, GetModuleConfig(type, ServerType));

        if (ConfigController.IsFileConfig(GetModuleConfigRootPath(type, ServerType), GetModuleConfigSubPath(type, ServerType)))
        {

        }
        else
        {
        //var modulePath = GetModuleConfigPath(type, ServerType);

        //var watcher = new ManagementEventWatcher(string.Format("SELECT * FROM RegistryValueChangeEvent WHERE Hive='{0}' AND KeyPath='{1}{2}' AND ValueName=''",
        //                                                         hive, keyPathBase, modulePath.Substring(modulePath.IndexOf('\\') + 1)).Replace("\\", "\\\\"));

        //watcher.Scope.Path.NamespacePath = @"root\default"; // Необходимо для работы на Windows XP.

        //_configWatchers.Add(module, watcher);

        //watcher.EventArrived += (sender, args) => module.OnConfigurationChanged();

        //watcher.Start();
}
      }
    }
    #endregion

    #region OnServerConfigChanged
    protected virtual void OnServerConfigChanged()
    {
    }
    #endregion

    #region ServerConfigWatcher
    protected void StartServerConfigWatcher()
    {
    //  var query = @"SELECT * FROM RegistryValueChangeEvent WHERE Hive='HKEY_LOCAL_MACHINE' AND KeyPath='Software\\SpaceTeam\\SpaceTeamLab\\Application Server' AND ValueName=''";
    //  _servirerConfigWatcher = new ManagementEventWatcher(query);
    //  _servirerConfigWatcher.Scope.Path.NamespacePath = @"root\default";
    //  _servirerConfigWatcher.EventArrived += ServerConfigWatcherEventArrived;
    //  _servirerConfigWatcher.Start();
    }

    //private void ServerConfigWatcherEventArrived(object sender, EventArrivedEventArgs e)
    //{
    //  OnServerConfigChanged();
    //}

    protected void StopServerConfigWatcher()
    {
    //  if (_servirerConfigWatcher != null)
    //  {
    //    _servirerConfigWatcher.Dispose();
    //    _servirerConfigWatcher = null;
    //  }
    }
    #endregion

    #region OnModuleCreating
    /// <summary>
    /// Вызывается перед созданием экземпляра модуля.
    /// </summary>
    /// <param name="type">Класс модуля.</param>
    protected virtual void OnModuleCreating(Type type)
    {
    }
    #endregion

    #region OnModuleInitializing
    /// <summary>
    /// Вызывается перед инициализацией модуля.
    /// </summary>
    /// <param name="module">Модуль.</param>
    protected virtual void OnModuleInitializing(BaseModule module)
    {
    }
    #endregion

    #region OnModulesPostInitialized
    /// <summary>
    /// Вызывается после доступности функционала и данных всех модулей.
    /// </summary>
    protected virtual void OnModulesPostInitialized()
    {

    }
    #endregion

    #region OnModulesLoaded
    /// <summary>
    /// Вызывается после загрузки модулей.
    /// </summary>
    protected virtual void OnModulesLoaded()
    {
#if DEBUG
      //this.IfIs<WcfServer>(wcf => wcf.LogHostsInfo());
#endif

    }
    #endregion

    #region GetModuleAssemblies
    /// <summary>
    /// В сервере наследнике возвращает список сборок, доступных для поиска в них модулей.
    /// </summary>
    /// <returns></returns>
    protected virtual Assembly[] GetModuleAssemblies()
    {
      throw new InvalidOperationException();
    }
    #endregion

    #region OnModulesLoading
    /// <summary>
    /// Вызывается перед загрузкой модулей.
    /// </summary>
    protected virtual void OnModulesLoading()
    {
      MesureAndLog(() => _assemblies = GetModuleAssemblies(), "--- Modules ---\r\nAssemblyHelper.Load(*.dll). Загрузка файлов модулей");
    }
    #endregion

    #region OnModulesUnloaded
    /// <summary>
    /// Вызывается после выгрузки модулей.
    /// </summary>
    protected virtual void OnModulesUnloaded()
    {
    }
    #endregion

    #region OnModulesUnloading
    /// <summary>
    /// Вызывается перед выгрузкой модулей.
    /// </summary>
    protected virtual void OnModulesUnloading()
    {
    }
    #endregion

    #region OnModuleUninitialized
    /// <summary>
    /// Вызывается после деинициализации модуля.
    /// </summary>
    /// <param name="module">Модуль.</param>
    protected virtual void OnModuleUninitialized(BaseModule module)
    {
    }
    #endregion

    #region OnModuleUninitializing
    /// <summary>
    /// Вызывается перед деинициализацией модуля.
    /// </summary>
    /// <param name="module">Модуль.</param>
    protected virtual void OnModuleUninitializing(BaseModule module)
    {
      //var watcher = _configWatchers.GetAndRemove(module);

      //if (watcher != null)
      //{
      //  // Unfortunately Dispose does not call Stop and it will be called later in destructor!
      //  // watcher.Stop();
      //  watcher.Dispose();
      //}
    }
    #endregion

    #region OnServerStateChanged
    /// <summary>
    /// Вызывается при изменении состояния сервера.
    /// </summary>
    protected virtual void OnServerStateChanged()
    {
    }
    #endregion

    #region OnThreadException
    private void OnThreadException(object sender, ThreadExceptionEventArgs e)
    {
      OnUnhandledUIException(e.Exception);
    }
    #endregion

    #region OnUnhandledDomainException
    private void OnUnhandledDomainException(object sender, UnhandledExceptionEventArgs e)
    {
      if (e.ExceptionObject is Exception)
        OnUnhandledException(e.ExceptionObject as Exception);
    }
    #endregion

    #region OnUnhandledException
    /// <summary>
    /// Вызывается при возникновении необработанного исключения.
    /// </summary>
    /// <param name="exc">Исключение.</param>
    protected virtual void OnUnhandledException(Exception exc)
    {
      exc = exc.GetRealException();

      if (!(exc is ThreadAbortException))
        WriteToLog(exc);
    }
    #endregion

    #region OnUnhandledUIException
    /// <summary>
    /// Вызывается при возникновении необработанного исключения на UI-потоке.
    /// </summary>
    /// <param name="exc">Исключение.</param>
    protected virtual void OnUnhandledUIException(Exception exc)
    {
      OnUnhandledException(exc);
    }
    #endregion

    #region OnUnobservedTaskException
    private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
      e.SetObserved();

      OnUnhandledException(e.Exception);
    }
    #endregion

    #region RemoveModule
    private void RemoveModule(int index)
    {
      _modules.RemoveAt(index);

      UpdateModules();
    }
    #endregion

    #region SetConfig
    /// <summary>
    /// Сохраняет конфигурацию сервера.
    /// </summary>
    /// <param name="config">Конфигурация сервера.</param>
    /// <param name="serverType">Тип сервера.</param>
    public static void SetConfig([NotNull] BaseServerConfig config, ServerType serverType)
    {
      ConfigController.Set(config, GetConfigRootPath(serverType), GetConfigSubPath(serverType), null);
    }
    #endregion

    #region SetInstance
    private static void SetInstance(BaseServer instance)
    {
      lock (_instanceLock)
      {
        if (instance != null && _instance != null && _instance.ServerState != ServerState.Stopped)
          throw new InvalidOperationException("It's possible existence of only one instance of the running server.");

        ClientContext.IsActive = ServerContext.IsActive = false;

        if (_instance != null)
        {
          AppDomain.CurrentDomain.UnhandledException -= _instance.OnUnhandledDomainException;
          TaskScheduler.UnobservedTaskException -= _instance.OnUnobservedTaskException;
          //Application.ThreadException -= _instance.OnThreadException;
        }

        _instance = instance;

        if (_instance != null)
        {
          if (_instance.ServerType == ServerType.Shell)
            ClientContext.IsActive = true;
          else
            if (_instance.ServerType == ServerType.ApplicationServer)
            ServerContext.IsActive = true;

          AppDomain.CurrentDomain.UnhandledException += _instance.OnUnhandledDomainException;
          TaskScheduler.UnobservedTaskException += _instance.OnUnobservedTaskException;
          //Application.ThreadException += _instance.OnThreadException;
        }
      }
    }
    #endregion

    #region SetModuleConfig
    /// <summary>
    /// Устанавливает конфигурацию модуля.
    /// </summary>
    /// <param name="moduleType">Тип модуля.</param>
    /// <param name="serverType">Тип сервера.</param>
    /// <param name="config">Конфигурация модуля.</param>
    public static void SetModuleConfig([NotNull] Type moduleType, ServerType serverType, [NotNull] ModuleConfig config)
    {
      ConfigController.Set(config, GetModuleConfigRootPath(moduleType, serverType), GetModuleConfigSubPath(moduleType, serverType), null);
    }
    #endregion

    #region SetModuleEnabled
    /// <summary>
    /// Устанавливает признак активности модуля.
    /// </summary>
    /// <param name="moduleType">Тип модуля.</param>
    /// <param name="serverType">Тип сервера.</param>
    /// <param name="enabled">True - модуль включен, False - модуль отключен.</param>
    public static void SetModuleEnabled([NotNull] Type moduleType, ServerType serverType, bool enabled)
    {
      SetModuleParameter<bool>(moduleType, serverType, Constants.MODULE_DISABLED_PARAMETER, !enabled);
    }
    #endregion

    #region SetModuleParameter
    /// <summary>
    /// Устанавливает значение параметра модуля.
    /// </summary>
    /// <typeparam name="T">Тип параметра. Поддерживаются только значимые типы, строка или массив байт.</typeparam>
    /// <param name="moduleType">Тип модуля.</param>
    /// <param name="serverType">Тип сервера.</param>
    /// <param name="name">Название параметра.</param>
    /// <param name="value">Значение параметра. Если null, то значение будет удалено.</param>
    public static void SetModuleParameter<T>([NotNull] Type moduleType, ServerType serverType, [NotNullNotEmpty] string name, T value)
    {
      SetParameter(GetModuleConfigRootPath(moduleType, serverType), GetModuleConfigSubPath(moduleType, serverType), name, value);
    }
    #endregion

    #region SetParameter
    /// <summary>
    /// Устанавливает значение параметра сервера.
    /// </summary>
    /// <typeparam name="T">Тип параметра. Поддерживаются только значимые типы, строка или массив байт.</typeparam>
    /// <param name="serverType">Тип сервера.</param>
    /// <param name="name">Название параметра.</param>
    /// <param name="value">Значение параметра. Если null, то значение будет удалено.</param>
    public static void SetParameter<T>(ServerType serverType, [NotNullNotEmpty] string name, T value)
    {
      SetParameter(GetConfigRootPath(serverType), GetConfigSubPath(serverType), name, value);
    }

    private static void SetParameter<T>(string rootPath, string subPath, string name, T value)
    {
      ConfigController.SetValue(value, rootPath, subPath, name);
    }
    #endregion



    #region SetServerLog
    /// <summary>
    /// Устанавливает источник сообщений для журнала сервера.
    /// При отсутствии источника в системе он создается (для этого нужны права администратора).
    /// </summary>
    /// <param name="sourceName">Название источника.</param>
    protected void SetServerLog(string sourceName)
    {
      lock (_logLock)
      {
        if (_log != null)
          _log.Close();

        _log = null;

        EventLogUtils.EnsureSourceAndName(sourceName, Constants.LOG_NAME);

        _log = EventLogUtils.Create(sourceName);

        //_log.MaximumKilobytes = Constants.LOG_SIZE;
      }
    }
    #endregion

    #region Start
    /// <summary>
    /// Запускает сервер.
    /// </summary>
    /// <returns>True - сервер загружен без ошибок, False - возникли ошибки при загрузке сервера.</returns>
    //[DebuggerStepThrough]
    public bool Start()
    {
      var complete = true;

      lock (_instanceLock)
        if (ServerState == ServerState.Stopped)
        {
          ServerState = ServerState.Starting;

          try
          {
            MesureAndLog(OnModulesLoading, "--- Modules ---\r\nOnModulesLoading. Подготовка к загрузке модулей");

            lock (_modules)
              (from t in AssemblyHelper.GetSubtypes(_assemblies, true, new[] { typeof(BaseModule) }, typeof(PlatformAssemblyAttribute))
               where IsModuleTypeLoadable(t)
               orderby
                 (t.GetAttribute<ModuleLoadingPriorityAttribute>() ?? ModuleLoadingPriorityAttribute.Default).Priority descending,
                  t.FullName ascending
               select t).ToArray().ForEach(t => complete = Exec.Try(() => LoadModuleImpl(t), e => false) && complete);

            Exec.Try(OnModulesLoaded, exc => { WriteToLog(new ServerStartingException(exc)); complete = false; });

            DoForAllModules(false, m => MesureAndLog(m.PostInitialize, string.Format("--- Modules ---\r\nBaseModule.PostInitialize. Пост-инициализация модуля '{0}'", m.GetDisplayName(false, m.GetType().FullName))), (m, exc) => new ModuleLoadException(exc, m.GetType()), m => m.GetPostInitializeTimeout());

            Exec.Try(OnModulesPostInitialized, exc => { WriteToLog(new ServerStartingException(exc)); complete = false; });

            ServerState = ServerState.Started;

            _assemblies = null;
          }
          catch (Exception exc)
          {
            complete = false;

            Exec.Try(OnModulesUnloading);

            WriteToLog(new ServerStartingException(exc));

            ServerState = ServerState.Stopped;

            throw;
          }
        }

      return complete;
    }
    #endregion

    #region Stop
    /// <summary>
    /// Останавливает сервер.
    /// </summary>
    [DebuggerStepThrough]
    public void Stop()
    {
      lock (_instanceLock)
        if (ServerState == ServerState.Started)
        {
          ServerState = ServerState.Stopping;

          DoForAllModules(true, m => MesureAndLog(m.PreUninitialize, string.Format("--- Modules ---\r\nBaseModule.PreUninitialize. Подготовка к выгрузке модуля '{0}'", m.GetDisplayName(false, m.GetType().FullName))), (m, exc) => new ModuleUnloadException(exc, m), m => m.GetPostInitializeTimeout());

          Exec.Try(OnModulesUnloading, exc => WriteToLog(new ServerStoppingException(exc)));

          lock (_modules)
            for (var i = _modules.Count - 1; i >= 0; i--)
              UnloadModuleImpl(i);

          Exec.Try(OnModulesUnloaded, exc => WriteToLog(new ServerStoppingException(exc)));

          MemoryHelper.Collect();

          ServerState = ServerState.Stopped;
        }
    }
    #endregion

    #region UnloadModule
    /// <summary>
    /// Выгружает модуль.
    /// </summary>
    /// <param name="type">Тип модуля.</param>
    [DebuggerStepThrough]
    protected void UnloadModule([InheritedFrom(typeof(BaseModule))] Type type)
    {
      lock (_modules)
      {
        var i = _modules.FindIndex(m => m.GetType() == type);

        if (i >= 0)
          UnloadModuleImpl(i);
      }
    }
    #endregion

    #region UnloadModuleImpl
    [DebuggerStepThrough]
    private void UnloadModuleImpl(int index)
    {
      var module = _modules[index];

      Exec.Try(() => OnModuleUninitializing(module), exc => WriteToLog(new ModuleUnloadException(exc, module)));

      try
      {
        if (!Task.Factory.StartNew(obj => (obj as ThreadStaticContext).Execute(() => MesureAndLog(module.Uninitialize, string.Format("--- Modules ---\r\nBaseModule.Uninitialize. Выгрузка модуля '{0}'", module.GetDisplayName(false, module.GetType().FullName)))), ThreadStaticContext.Capture(), TaskCreationOptions.PreferFairness).Wait(module.GetUnloadTimeout() ?? GetUnloadOperationTimeout()))
          throw new ModuleOperationTimeOutException();
      }
      catch (Exception exc)
      {
        WriteToLog(new ModuleUnloadException(exc, module));
      }

      RemoveModule(index);

      Exec.Try(() => OnModuleUninitialized(module), exc => WriteToLog(new ModuleUnloadException(exc, module)));
    }
    #endregion

    #region UpdateModules
    private void UpdateModules()
    {
      _modulesRO = new ReadOnlyCollection<BaseModule>(_modules.ToArray()); // Копирование коллекции здесь обязательно.
    }
    #endregion

    #region WriteToLog
    /// <summary>
    /// Записывает сообщение в журнал событий сервера, без заголовка, включающего имя модуля, обрезая сообщение до c_EventLog_Max_Event_Message_Chars символов.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    /// <param name="type">Тип сообщения.</param>
    protected void WriteToLogWithoutModuleHeader([NotNullNotEmpty, Localized] string message, ushort eventId, EventLogEntryType type = EventLogEntryType.Error)
    {
      lock (_logLock)
        if (_log != null)
          Exec.Try(() => _log.WriteEntry(message.TrimLength(c_EventLog_Max_Event_Message_Chars), type, eventId));
    }

    /// <summary>
    /// Записывает сообщение в журнал событий сервера, обрезая сообщение до c_EventLog_Max_Event_Message_Chars символов.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    /// <param name="eventId">Идентификатор события.</param>
    /// <param name="type">Тип события.</param>
    protected void WriteToLog([NotNullNotEmpty, Localized] string message, ushort eventId, EventLogEntryType type = EventLogEntryType.Error)
    {
      if (CallerModule != null)
        message = SR.GetString(RI.ModuleLogMessage, CallerModule.GetDisplayName()) + Environment.NewLine + Environment.NewLine + message;

      WriteToLogWithoutModuleHeader(message, eventId, type);
    }


    /// <summary>
    /// Записывает сообщение в журнал событий сервера. Если сообщение получится длиной более c_EventLog_Max_Event_Message_Chars, то сообщение делится на части, и в заголовке каждого соощнения указывается номер части и полное количество частей.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    /// <param name="eventId">Идентификатор события.</param>
    /// <param name="type">Тип события.</param>
    protected void WriteToLogMultiple([NotNullNotEmpty, Localized] string message, ushort eventId, EventLogEntryType type = EventLogEntryType.Error)
    {
      var prefix = SR.GetString(RI.ModuleLogMessage, CallerModule?.GetDisplayName()) + Environment.NewLine;

      Exec.Try(() =>
       {
         var messageToStore = prefix + Environment.NewLine + message;
         if (messageToStore.Length <= c_EventLog_Max_Event_Message_Chars)
         {
           WriteToLogWithoutModuleHeader(messageToStore, eventId, type);
           return;
         }

         var messages = message.SplitByLength(c_EventLog_Max_Event_Message_Chars).ToList();
         var messagePartPrefixLen = (SR.GetString(RI.ModuleLogMessageBig, messages.Count + 1, messages.Count + 1) + Environment.NewLine).Length;
         messages = message.SplitByLength(c_EventLog_Max_Event_Message_Chars - (prefix.Length + messagePartPrefixLen + Environment.NewLine.Length + 10)).ToList();
         messages = messages.Select((m, i) => prefix + SR.GetString(RI.ModuleLogMessageBig, i + 1, messages.Count) + Environment.NewLine + m).ToList();
         foreach (var msg in messages)
           WriteToLogWithoutModuleHeader(msg, eventId, type);
       });
    }

    /// <summary>
    /// Записывает информацию об исключении в журнал событий сервера.
    /// </summary>
    /// <param name="exc">Исключение.</param>
    /// <param name="eventId">Идентификатор события.</param>
    /// <param name="includeStackTrace">Признак того, что в текст исключения необходимо включить трассировку стэка исключения.</param>
    protected void WriteToLog([NotNull] Exception exc, ushort eventId = LogEventId.UnhandledException, bool includeStackTrace = true)
    {
      WriteToLog(exc.GetFullMessage(false, includeStackTrace), exc is ServerActionException ? (exc as ServerActionException).LogEventId : eventId);
    }

    /// <summary>
    /// Записывает информацию об исключении в журнал событий сервера.
    /// </summary>
    /// <param name="exc">Исключение.</param>
    /// <param name="title">Поясняющая информация об исключении.</param>
    /// <param name="eventId">Идентификатор события.</param>
    /// <param name="includeStackTrace">Признак того, что в текст исключения необходимо включить трассировку стэка исключения.</param>
    protected void WriteToLog([NotNull] Exception exc, string title, ushort eventId = LogEventId.UnhandledException, bool includeStackTrace = true)
    {
      WriteToLog(title + Environment.NewLine + Environment.NewLine + exc.GetFullMessage(false, includeStackTrace), exc is ServerActionException ? (exc as ServerActionException).LogEventId : eventId);
    }

    #endregion

    #region IBaseServer
    T IBaseServer.GetConfiguration<T>()
    {
      return GetModuleConfig(CallerModule.GetType(), ServerType) as T;
    }

    T IBaseServer.GetParameter<T>([NotNullNotEmpty] string name)
    {
      return GetModuleParameter<T>(CallerModule.GetType(), ServerType, name);
    }

    T IBaseServer.GetServerInterface<T>()
    {
      return this as T;
    }

    T IBaseServer.GetService<T>()
    {
      if (IsServiceSupported<T>())
        foreach (var m in Modules)
          if (!ModuleComparer.Instance.Equals(m, CallerModule))
            if (m is T)
              return _proxyGenerator.CreateInterfaceProxyWithTargetInterface<T>(m as T, new ServiceInterceptor(CallerModule)) as T;
            else
              if (m is IServiceProvider)
            {
              var service = (m as IServiceProvider).GetService(typeof(T));

              if (service != null)
                return _proxyGenerator.CreateInterfaceProxyWithTargetInterface<T>(service as T, new ServiceImplicitInterceptor(CallerModule, m)) as T;
            }

      return null;
    }

    T IBaseServer.GetService<T>(ProviderContext context)
    {
      if (IsServiceSupported<T>())
        foreach (var m in Modules)
          if (!ModuleComparer.Instance.Equals(m, CallerModule) && m is IContextServiceProvider)
          {
            var service = (m as IContextServiceProvider).GetService<T>(context);

            if (service != null)
              return _proxyGenerator.CreateInterfaceProxyWithTargetInterface<T>(service as T, new ServiceImplicitInterceptor(CallerModule, m)) as T;
          }

      return null;
    }

    T[] IBaseServer.GetServices<T>()
    {
      var services = new List<T>();

      if (IsServiceSupported<T>())
        foreach (var m in Modules)
          if (!ModuleComparer.Instance.Equals(m, CallerModule))
          {
            if (m is T)
              services.Add(_proxyGenerator.CreateInterfaceProxyWithTargetInterface<T>(m as T, new ServiceInterceptor(CallerModule)) as T);

            if (m is IServiceProvider)
            {
              var service = (m as IServiceProvider).GetService(typeof(T));

              if (service != null)
                services.Add(_proxyGenerator.CreateInterfaceProxyWithTargetInterface<T>(service as T, new ServiceImplicitInterceptor(CallerModule, m)) as T);
            }
          }

      return services.ToArray();
    }

    T[] IBaseServer.GetServices<T>(ProviderContext context)
    {
      var services = new List<T>();

      if (IsServiceSupported<T>())
        foreach (var m in Modules)
          if (!ModuleComparer.Instance.Equals(m, CallerModule) && m is IContextServiceProvider)
          {
            var service = (m as IContextServiceProvider).GetService<T>(context);

            if (service != null)
              services.Add(_proxyGenerator.CreateInterfaceProxyWithTargetInterface<T>(service as T, new ServiceImplicitInterceptor(CallerModule, m)) as T);
          }

      return services.ToArray();
    }

    void IBaseServer.SetConfiguration(ModuleConfig config)
    {
      SetModuleConfig(CallerModule.GetType(), ServerType, config);
    }

    void IBaseServer.SetParameter<T>([NotNullNotEmpty] string name, [NotNull] T value)
    {
      SetModuleParameter<T>(CallerModule.GetType(), ServerType, name, value);
    }

    void IBaseServer.WriteToLog(string message, ServerLogEntryType type)
    {
      WriteToLog(message, (ushort)(LogEventId.ModuleLogMessage + (ushort)type), (EventLogEntryType)type);
    }

    void IBaseServer.WriteToLogMultiple(string message, ServerLogEntryType type)
    {
      WriteToLogMultiple(message, (ushort)(LogEventId.ModuleLogMessage + (ushort)type), (EventLogEntryType)type);
    }

    void IBaseServer.WriteToLog(Exception exc, bool includeStackTrace)
    {
      WriteToLog(exc, (ushort)(LogEventId.ModuleLogMessage + (ushort)ServerLogEntryType.Error), includeStackTrace);
    }

    void IBaseServer.WriteToLog(Exception exc, string title, bool includeStackTrace)
    {
      WriteToLog(exc, title, (ushort)(LogEventId.ModuleLogMessage + (ushort)ServerLogEntryType.Error), includeStackTrace);
    }

    void IBaseServer.WriteToLogWithoutModuleHeader(string message, ServerLogEntryType type)
    {
      WriteToLogWithoutModuleHeader(message, (ushort)(LogEventId.ModuleLogMessage + (ushort)type), (EventLogEntryType)type);
    }

    void IBaseServer.SendUDPSignal(string signal)
    {
      if (string.IsNullOrEmpty(signal) || string.IsNullOrEmpty(UDPHostName) || UDPPort <= 0)
        return;

      var length = signal.Length;

      var data = new byte[length + 10];

      data[0] = (byte)data.Length;
      data[1] = 1;

      for (int i = 2; i < 6; i++)
      {
        data[i] = 250;
        data[i + 4] = 1;
      }

      Array.ConstrainedCopy(Encoding.Convert(_unicode, _win1251Enc(), _unicode.GetBytes(signal)), 0, data, 10, length);

      using (UdpClient c = new UdpClient())
        c.Send(data, data.Length, UDPHostName, UDPPort);
    }

    void IBaseServer.SendUDPLog(ServerLogEntryType type, string message)
    {
      if (string.IsNullOrEmpty(message) || string.IsNullOrEmpty(UDPHostName) || UDPPort <= 0 || UDPLoggerId == 0)
        return;
      // UtcNow is used because an external logger operates with the UTC time
      DateTime curTime = DateTime.UtcNow;
      var length = message.Length;

      var data = new byte[length + 26];

      data[0] = (byte)((data.Length < 0xFA ? data.Length : 0xF9) + 6);    // length 
      data[1] = 2;    // log message
      data[2] = (byte)(UDPLoggerId & 0xFF);
      data[3] = (byte)((UDPLoggerId >> 8) & 0xFF);
      data[4] = (byte)(3); // OF_LOG | OF_NET
      data[5] = (byte)(0);

      data[6] = (byte)(curTime.Year & 0xFF);
      data[7] = (byte)((curTime.Year >> 8) & 0xFF);
      data[8] = (byte)(curTime.Month & 0xFF);
      data[9] = (byte)((curTime.Month >> 8) & 0xFF);
      data[10] = (byte)((int)curTime.DayOfWeek & 0xFF);
      data[11] = (byte)(((int)curTime.DayOfWeek >> 8) & 0xFF);
      data[12] = (byte)(curTime.Day & 0xFF);
      data[13] = (byte)((curTime.Day >> 8) & 0xFF);
      data[14] = (byte)(curTime.Hour & 0xFF);
      data[15] = (byte)((curTime.Hour >> 8) & 0xFF);
      data[16] = (byte)(curTime.Minute & 0xFF);
      data[17] = (byte)((curTime.Minute >> 8) & 0xFF);
      data[18] = (byte)(curTime.Second & 0xFF);
      data[19] = (byte)((curTime.Second >> 8) & 0xFF);
      data[20] = (byte)(curTime.Millisecond & 0xFF);
      data[21] = (byte)((curTime.Millisecond >> 8) & 0xFF);

      data[22] = (byte)'(';
      data[23] = (byte)(_logMsgTypes.Length > (int)type ? _logMsgTypes[(int)type] : 'I');
      data[24] = (byte)')';
      data[25] = (byte)' ';

      Array.ConstrainedCopy(Encoding.Convert(_unicode, _win1251Enc(), _unicode.GetBytes(message)), 0, data, 26, length);

      lock (_logUDPLock)
      {
        if (_logUDPClient == null)
          _logUDPClient = new UdpClient();
        try
        {
          _logUDPClient.Send(data, data.Length, UDPHostName, UDPPort);
        }
        catch (Exception)
        {
          ;
        }
      }
    }
    #endregion

    #region MesureAndLog
    public void MesureAndLog(Action act, string actionLogText)
    {
#if DEBUG_LOG_MODULES
      Exec.Mesure(
        act, ( elapsed, started ) => this.WriteToLog( string.Format( "Выполнение действия '{0}' заняло {1} (старт в: {2:HH:mm:ss.fff dd.MM.yyyy})", actionLogText, elapsed, started ), LogEventId.ModuleLogMessage, EventLogEntryType.Information )
      );
#else
      act();
#endif
    }

    public TResult MesureAndLog<TResult>(Func<TResult> act, string actionLogText)
    {
#if DEBUG_LOG_MODULES
      return Exec.Mesure(
        act, ( elapsed, started, _ ) => this.WriteToLog( string.Format( "Выполнение действия '{0}' заняло {1} (старт в: {2:HH:mm:ss.fff dd.MM.yyyy})", actionLogText, elapsed, started ), LogEventId.ModuleLogMessage, EventLogEntryType.Information )
      );
#else
      return act();
#endif
    }
    #endregion

    /// <summary>
    /// Класс, позволяющий сравнивать экземпляры модулей.
    /// </summary>
    protected sealed class ModuleComparer : IEqualityComparer<BaseModule>
    {
      #region .Static Field
      /// <summary>
      /// Singleton-экземпляр.
      /// </summary>
      public static readonly ModuleComparer Instance = new ModuleComparer();
      #endregion

      #region .Ctor
      private ModuleComparer()
      {
      }
      #endregion

      #region Equals
      public bool Equals(BaseModule x, BaseModule y)
      {
        return ReferenceEquals(x, y);
      }
      #endregion

      #region GetHashCode
      public int GetHashCode(BaseModule obj)
      {
        return RuntimeHelpers.GetHashCode(obj);
      }
      #endregion
    }

    /// <summary>
    /// Идентификаторы событий для журнала событий (0 - 999).
    /// </summary>
    protected internal static class LogEventId
    {
      #region .Constants
      /// <summary>
      /// Идентификатор по умолчанию.
      /// </summary>
      public const ushort Default = 0;

      /// <summary>
      /// Необработанное исключение.
      /// </summary>
      public const ushort UnhandledException = 100;

      /// <summary>
      /// Ошибка при запуске сервера.
      /// </summary>
      public const ushort ServerStartingError = 200;

      /// <summary>
      /// Ошибка при остановке сервера.
      /// </summary>
      public const ushort ServerStoppingError = 300;

      /// <summary>
      /// Ошибка при загрузке модуля.
      /// </summary>
      public const ushort ModuleLoadError = 400;

      /// <summary>
      /// Ошибка при выгрузке модуля.
      /// </summary>
      public const ushort ModuleUnloadError = 500;

      /// <summary>
      /// Сообщение модуля (800 - 899).
      /// </summary>
      public const ushort ModuleLogMessage = 600;
      #endregion
    }
  }

  /// <summary>
  /// Базовая конфигурация сервера.
  /// </summary>
  [Serializable]
  public class BaseServerConfig : ItemConfig
  {
  }
  /// <summary>
  /// Тип сервера.
  /// </summary>
  public enum ServerType
  {
    #region .Static Fields
    /// <summary>
    /// Неизвестный тип.
    /// </summary>
    Unknown,

    /// <summary>
    /// Сервер приложений.
    /// </summary>
    ApplicationServer,

    /// <summary>
    /// Оболочка.
    /// </summary>
    Shell
    #endregion
  }

  /// <summary>
  /// Состояние сервера.
  /// </summary>
  public enum ServerState
  {
    #region .Static Fields
    /// <summary>
    /// Сервер остановлен.
    /// </summary>
    Stopped,

    /// <summary>
    /// Сервер останавливается.
    /// </summary>
    Stopping,

    /// <summary>
    /// Сервер запускается.
    /// </summary>
    Starting,

    /// <summary>
    /// Сервер запущен.
    /// </summary>
    Started
    #endregion
}
}
