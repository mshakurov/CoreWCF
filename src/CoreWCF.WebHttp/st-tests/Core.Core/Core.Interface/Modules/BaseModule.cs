using System;
using System.Threading;
using System.Threading.Tasks;
using ST.Utils;
using ST.Utils.Attributes;

namespace ST.Core
{
  /// <summary>
  /// Базовый класс модуля, подключаемого к серверу.
  /// </summary>
  public abstract class BaseModule
  {
    #region .Fields
#pragma warning disable 0649
    private object _;
#pragma warning restore 0649
    /// <summary>
    /// Object for Task instances cancellation
    /// </summary>
    private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    #endregion

    #region .Properties
    /// <summary>
    /// Сервер.
    /// </summary>
    protected object ServerInterface
    {
      get { return _; }
    }
    #endregion

    #region SetCancellationToken
    /// <summary>
    /// Активирует признак остановки выполнения модуля.
    /// </summary>
    public virtual bool SetCancellationToken()
    {
      if ( _cancellationTokenSource != null )
      {
        _cancellationTokenSource.Cancel();
        return true;
      }
      else
        return false;
    }
    #endregion

    #region GetCancellationToken
    /// <summary>
    /// Возвращает объект-признак остановки выполнения модуля 
    /// для применения в объектах-потоках типа Task. 
    /// </summary>
    /// <exception cref="ObjectDisposedException">Генерирует в случае, если родительский объект типа СancellationTokenSource не создан</exception>
    public virtual CancellationToken GetCancellationToken()
    {
      if ( _cancellationTokenSource != null )
        return _cancellationTokenSource.Token;
      else
        throw new ObjectDisposedException( "cancellationTokenSource" );
    }
    #endregion

    #region ThrowIfCancellationTokenRequested
    /// <summary>
    /// Анализирует признак остановки выполнения модуля 
    /// и активирует исключение если признак остановки активен.
    /// </summary>
    /// /// <exception cref="OperationCanceledException"></exception>
    public virtual void ThrowIfCancellationTokenRequested()
    {
      if ( _cancellationTokenSource != null )
        _cancellationTokenSource.Token.ThrowIfCancellationRequested();
    }
    #endregion

    #region GetConfiguration
    /// <summary>
    /// Возвращает конфигурацию модуля.
    /// </summary>
    /// <typeparam name="T">Тип конфигурации.</typeparam>
    /// <returns>Экземпляр конфигурации.</returns>
    public T GetConfiguration<T>()
      where T : ModuleConfig
    {
      return ServerInterface.IfIs( ( IBaseServer s ) => s.GetConfiguration<T>() );
    }
    #endregion

    #region GetParameter
    /// <summary>
    /// Возвращает значение параметра модуля.
    /// </summary>
    /// <typeparam name="T">Тип параметра. Поддерживаются только значимые типы, строка или массив байт.</typeparam>
    /// <param name="name">Название параметра.</param>
    /// <returns>Значение параметра.</returns>
    public T GetParameter<T>( [NotNullNotEmpty] string name )
    {
      return ServerInterface.IfIs( ( IBaseServer s ) => s.GetParameter<T>( name ) );
    }
    #endregion

    #region GetServerInterface
    /// <summary>
    /// Возвращает сервис указанного типа, если он предоставляется сервером.
    /// </summary>
    /// <typeparam name="T">Тип сервиса (должен быть интерфейсом).</typeparam>
    /// <returns>Сервис</returns>
    public T GetServerInterface<T>()
      where T : class
    {
      return ServerInterface.IfIs( ( IBaseServer s ) => s.GetServerInterface<T>() );
    }
    #endregion

    #region GetService
    /// <summary>
    /// Возвращает сервис, предоставляемый другим модулем (если таких модулей несколько, то первого загруженного сервером).
    /// Во время запуска/остановки сервера возвращается null.
    /// </summary>
    /// <typeparam name="T">Тип сервиса (должен быть интерфейсом).</typeparam>
    /// <returns>Сервис.</returns>
    public T GetService<T>()
      where T : class
    {
      return ServerInterface.IfIs( ( IBaseServer s ) => s.GetService<T>() );
    }

    /// <summary>
    /// Возвращает сервис, предоставляемый другим модулем (если таких модулей несколько, то первого загруженного сервером).
    /// Во время запуска/остановки сервера возвращается null.
    /// </summary>
    /// <typeparam name="T">Тип сервиса (должен быть интерфейсом).</typeparam>
    /// <param name="context">Контекст.</param>
    /// <returns>Сервис.</returns>
    public T GetService<T>( ProviderContext context )
      where T : class
    {
      return ServerInterface.IfIs( ( IBaseServer s ) => s.GetService<T>( context ) );
    }
    #endregion

    #region GetServices
    /// <summary>
    /// Возвращает список сервисов, предоставляемых другими модулями.
    /// Во время запуска/остановки сервера возвращается пустой список.
    /// </summary>
    /// <typeparam name="T">Тип сервиса (должен быть интерфейсом).</typeparam>
    /// <returns>Список сервисов.</returns>
    public T[] GetServices<T>()
      where T : class
    {
      return ServerInterface.IfIs( ( IBaseServer s ) => s.GetServices<T>() ?? new T[0] );
    }

    /// <summary>
    /// Возвращает список сервисов, предоставляемых другими модулями.
    /// Во время запуска/остановки сервера возвращается пустой список.
    /// </summary>
    /// <typeparam name="T">Тип сервиса (должен быть интерфейсом).</typeparam>
    /// <param name="context">Контекст.</param>
    /// <returns>Список сервисов.</returns>
    public T[] GetServices<T>( ProviderContext context )
      where T : class
    {
      return ServerInterface.IfIs( ( IBaseServer s ) => s.GetServices<T>( context ) ?? new T[0] );
    }
    #endregion

    #region Initialize
    /// <summary>
    /// Инициализирует модуль. С этого момента можно обращаться к серверу, но большинство функционала и данных еще не доступны.
    /// </summary>
    protected internal virtual void Initialize()
    {
    }
    #endregion

    #region OnConfigurationChanged
    /// <summary>
    /// Вызывается после изменения конфигурации модуля.
    /// </summary>
    protected internal virtual void OnConfigurationChanged()
    {
    }
    #endregion

    #region PostInitialize
    /// <summary>
    /// Вызывается после запуска сервера и загрузки всех модулей. С этого момента доступен весь функционал и данные.
    /// </summary>
    protected internal virtual void PostInitialize()
    {
    }
    #endregion

    #region PreUninitialize
    /// <summary>
    /// Вызывается перед остановкой сервера и выгрузкой всех модулей. В этот момент еще доступен весь функционал и данные.
    /// </summary>
    protected internal virtual void PreUninitialize()
    {
      SetCancellationToken();
    }
    #endregion

    #region SetConfiguration
    /// <summary>
    /// Устанавливает конфигурацию модуля.
    /// </summary>
    /// <typeparam name="T">Тип конфигурации.</typeparam>
    /// <param name="config">Экземпляр конфигурации.</param>
    public void SetConfiguration<T>( T config )
      where T : ModuleConfig
    {
      ServerInterface.IfIs<IBaseServer>( s => s.SetConfiguration( config ) );
    }
    #endregion

    #region SetParameter
    /// <summary>
    /// Устанавливает значение параметра модуля.
    /// </summary>
    /// <typeparam name="T">Тип параметра. Поддерживаются только значимые типы, строка или массив байт.</typeparam>
    /// <param name="name">Название параметра.</param>
    /// <param name="value">Значение параметра.</param>
    public void SetParameter<T>( [NotNullNotEmpty] string name, [NotNull] T value )
    {
      ServerInterface.IfIs<IBaseServer>( s => s.SetParameter<T>( name, value ) );
    }
    #endregion

    #region Uninitialize
    /// <summary>
    /// Деинициализирует модуль. В этот момент еще можно обращаться к серверу, но большинство функционала и данных уже не доступны.
    /// </summary>
    protected internal virtual void Uninitialize()
    {
    }
    #endregion

    #region WriteToLog
    /// <summary>
    /// Записывает сообщение в журнал событий сервера, обрезая сообщение до 31838 символов.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    /// <param name="type">Тип сообщения.</param>
    public void WriteToLog( string message, ServerLogEntryType type )
    {
      ServerInterface.IfIs<IBaseServer>( s => s.WriteToLog( message, type ) );
    }

    /// <summary>
    /// Записывает сообщение в журнал событий сервера. Если сообщение получится длиной более 31838, то сообщение делится на части, и в заголовке каждого соощнения указывается номер части и полное количество частей.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    /// <param name="type">Тип события.</param>
    public void WriteToLogMultiple( string message, ServerLogEntryType type )
    {
      ServerInterface.IfIs<IBaseServer>( s => s.WriteToLogMultiple( message, type ) );
    }

    /// <summary>
    /// Записывает информацию об исключении в журнал событий сервера.
    /// </summary>
    /// <param name="exc">Исключение.</param>
    /// <param name="includeStackTrace">Признак того, что в текст исключения необходимо включить трассировку стэка исключения.</param>
    public void WriteToLog( Exception exc, bool includeStackTrace )
    {
      ServerInterface.IfIs<IBaseServer>( s => s.WriteToLog( exc, includeStackTrace ) );
    }

    /// <summary>
    /// Записывает информацию об исключении в журнал событий сервера.
    /// </summary>
    /// <param name="exc">Исключение.</param>
    /// <param name="title">Поясняющая информация об исключении.</param>
    /// <param name="includeStackTrace">Признак того, что в текст исключения необходимо включить трассировку стэка исключения.</param>
    public void WriteToLog( Exception exc, string title, bool includeStackTrace )
    {
      ServerInterface.IfIs<IBaseServer>( s => s.WriteToLog( exc, title, includeStackTrace ) );
    }

    /// <summary>
    /// Записывает сообщение в журнал событий сервера, без заголовка, включающего имя модуля, обрезая сообщение до 31838 символов.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    /// <param name="type">Тип сообщения.</param>
    public void WriteToLogWithoutModuleHeader( string message, ServerLogEntryType type )
    {
      ServerInterface.IfIs<IBaseServer>( s => s.WriteToLogWithoutModuleHeader( message, type ) );
    }
    #endregion

    #region SendUDPSignal
    /// <summary>
    /// Осуществляет отправку udp сигнала на телематический сервер.
    /// </summary>
    /// <param name="signal">Сигнал.</param>
    public void SendUDPSignal( string signal )
    {
      ServerInterface.IfIs<IBaseServer>( s => s.SendUDPSignal( signal ) );
    }
    #endregion

    #region SendUDPLog
    /// <summary>
    /// Осуществляет отправку сообщения по udp на внешний сервер-логгер.
    /// </summary>
    /// <param name="type">Тип сообщения.</param>
    /// <param name="message">Текст сообщения.</param>
    public void SendUDPLog( ServerLogEntryType type, string message )
    {
      ServerInterface.IfIs<IBaseServer>( s => s.SendUDPLog( type, message ) );
    }
    #endregion

    #region GetInitializeTimeout
    /// <summary>
    /// Получает специфический для модуля тайм-аут операции инициализации модуля.
    /// </summary>
    protected internal virtual int? GetInitializeTimeout()
    {
      return null;
    }
    #endregion

    #region GetPostInitializeTimeout
    /// <summary>
    /// Получает специфический для модуля тайм-аут операции пост-инициализации модуля.
    /// </summary>
    protected internal virtual int? GetPostInitializeTimeout()
    {
      return null;
    }
    #endregion

    #region GetUnloadTimeout
    /// <summary>
    /// Получает специфический для модуля тайм-аут операции выгрузки модуля.
    /// </summary>
    protected internal virtual int? GetUnloadTimeout()
    {
      return null;
    }
    #endregion

  }
}
