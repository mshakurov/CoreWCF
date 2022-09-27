using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Sockets;

using Castle.DynamicProxy;
using System.ServiceModel;

using ST.Utils.Attributes;
using ST.Utils.Reflection;


namespace ST.Utils.Wcf
{
  /// <summary>
  /// Канал для обращения к WCF-сервису.
  /// </summary>
  /// <typeparam name="T">Тип интерфейса WCF-сервиса.</typeparam>
  public class WcfChannel<T> : SimpleInterceptor
    where T : class
  {
    #region .Static Fields
    private static readonly ProxyGenerator _proxyGenerator = new ProxyGenerator();
    #endregion

    #region .Fields
    private CommunicationState _state = CommunicationState.Closed;
    #endregion

    #region .Properties
    /// <summary>
    /// Точка доступа к WCF-серверу, на основе которой создана фабрика клиентского WCF-канала.
    /// </summary>
    protected WcfServerEndpoint? Endpoint { get; private set; }

    /// <summary>
    /// Фабрика клиентского WCF-канала.
    /// </summary>
    protected ChannelFactory<T>? Factory { get; private set; }

    /// <summary>
    /// Клиентский WCF-канал.
    /// </summary>
    protected IClientChannel? Channel { get; private set; }

    protected object? State { get; private set; }
    #endregion

    #region .Ctor
    protected WcfChannel()
    {
    }
    #endregion

    #region .Dtor
    [DebuggerStepThrough]
    ~WcfChannel()
    {
      Reset();
    }
    #endregion

    #region Get
    /// <summary>
    /// Возвращает прокси для канала для обращения к WCF-сервису.
    /// </summary>
    /// <param name="endpoint">Точка доступа к WCF-серверу.</param>
    /// <param name="address">Относительный адрес интерфейса WCF-сервиса.</param>
    /// <param name="nameSpace">Пространство имен WCF-сервиса.</param>
    /// <param name="identity">Идентификация для точки доступа к WCF-сервису.</param>
    /// <returns>Прокси для канала для обращения к WCF-сервису.</returns>
    [DebuggerStepThrough]
    public static T Get(WcfServerEndpoint endpoint, string address, string nameSpace, EndpointIdentity? identity = null)
    {
      return Get(new WcfChannel<T>(), endpoint, address, nameSpace, identity);
    }

    public static T Get([NotNull] WcfChannel<T> channel, [NotNull] WcfServerEndpoint endpoint, [NotNullNotEmpty] string address, [NotNullNotEmpty] string nameSpace, EndpointIdentity? identity = null)
    {
       return Get(channel, endpoint, address, nameSpace, identity, null);
    }

    public static T Get( [NotNull] WcfChannel<T> channel, [NotNull] WcfServerEndpoint endpoint, [NotNullNotEmpty] string address, [NotNullNotEmpty] string nameSpace, EndpointIdentity? identity, object? state = null )
    {
      if (!typeof(T).IsInterface || !typeof(T).IsDefined<ServiceContractAttribute>())
        throw new ArgumentException("The type '" + typeof(T).FullName + "' must be an interface and it must be marked with ServiceContractAttribute attribute.");

      var uri = new Uri(string.Format("{0}/{1}", endpoint, address));

      channel.Endpoint = endpoint;
      channel.Factory = new ChannelFactory<T>(endpoint.ProtocolType.GetBinding(nameSpace), identity == null ? new EndpointAddress(uri) : new EndpointAddress(uri, identity));
      channel.Channel = null;
      channel.State = state;

      channel.Initialize();

      return _proxyGenerator.CreateInterfaceProxyWithTargetInterface<T>(null, channel);
    }
    #endregion

    #region Initialize
    /// <summary>
    /// Инициализирует канал.
    /// </summary>
    protected virtual void Initialize()
    {
      Factory.Endpoint.ExtendOperations();
    }
    #endregion

    #region IsConnectionResetException
    private bool IsConnectionResetException(Exception exc)
    {
      while (exc != null)
      {
        if ((exc is SocketException) && (exc as SocketException).SocketErrorCode == SocketError.ConnectionReset)
          return true;

        exc = exc.InnerException;
      }

      return false;
    }
    #endregion

    #region OnBeforeInvoke
    [DebuggerStepThrough]
    protected override void OnBeforeInvoke(IInvocation invocation)
    {
      this.SafeAccess(() => Channel == null || (_state = Channel.State) != CommunicationState.Opened, () =>
     {
       Reset();

       (Channel = Factory.CreateChannel() as IClientChannel).Open();
     });

      (invocation as IChangeProxyTarget).ChangeInvocationTarget(Channel);
    }
    #endregion

    #region OnCatch
    [DebuggerStepThrough]
    protected override void OnCatch(IInvocation invocation, Exception exc)
    {
      if (_state == CommunicationState.Opened && IsConnectionResetException(exc))
        Intercept(invocation);
      else
        base.OnCatch(invocation, exc);
    }
    #endregion

    #region Reset
    [DebuggerStepThrough]
    private void Reset()
    {
      if (Channel != null)
      {
        if (Channel.State == CommunicationState.Opened)
          Exec.Try(Channel.Close);

        Channel.Abort();
      }
    }
    #endregion
  }
}
