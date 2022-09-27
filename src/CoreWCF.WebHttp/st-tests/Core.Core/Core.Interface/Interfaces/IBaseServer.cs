using System;
using System.Threading;
using System.Threading.Tasks;
using ST.Utils;

namespace ST.Core
{
  /// <summary>
  /// Базовый интерфейс сервера.
  /// </summary>
  [ServerInterface]
  public interface IBaseServer
  {
    /// <summary>
    /// Возвращает конфигурацию модуля (определяется атрибутом ConfigurableAttribute).
    /// </summary>
    /// <typeparam name="T">Тип конфигурации.</typeparam>
    /// <returns>Экземпляр конфигурации.</returns>
    T GetConfiguration<T>()
      where T : ModuleConfig;

    /// <summary>
    /// Возвращает значение параметра модуля.
    /// </summary>
    /// <typeparam name="T">Тип параметра. Поддерживаются только значимые типы, строка или массив байт.</typeparam>
    /// <param name="name">Название параметра.</param>
    /// <returns>Значение параметра.</returns>
    T GetParameter<T>( string name );

    /// <summary>
    /// Возвращает сервис указанного типа (первый найденный).
    /// </summary>
    /// <typeparam name="T">Тип сервиса (должен быть интерфейсом).</typeparam>
    /// <returns>Сервис.</returns>
    T GetService<T>()
      where T : class;

    /// <summary>
    /// Возвращает сервис указанного типа, если сервер его реализует.
    /// </summary>
    /// <typeparam name="T">Тип сервиса (должен быть интерфейсом).</typeparam>
    /// <returns></returns>
    T GetServerInterface<T>()
      where T : class;

    /// <summary>
    /// Возвращает сервис указанного типа (первый найденный).
    /// </summary>
    /// <typeparam name="T">Тип сервиса (должен быть интерфейсом).</typeparam>
    /// <param name="context">Контекст.</param>
    /// <returns>Сервис.</returns>
    T GetService<T>( ProviderContext context )
      where T : class;

    /// <summary>
    /// Возвращает список сервисов указанного типа.
    /// </summary>
    /// <typeparam name="T">Тип сервиса (должен быть интерфейсом).</typeparam>
    /// <returns>Список сервисов.</returns>
    T[] GetServices<T>()
      where T : class;

    /// <summary>
    /// Возвращает список сервисов указанного типа.
    /// </summary>
    /// <typeparam name="T">Тип сервиса (должен быть интерфейсом).</typeparam>
    /// <param name="context">Контекст.</param>
    /// <returns>Список сервисов.</returns>
    T[] GetServices<T>( ProviderContext context )
      where T : class;

    /// <summary>
    /// Устанавливает конфигурацию модуля (определяется атрибутом ConfigurableAttribute).
    /// </summary>
    /// <param name="config">Экземпляр конфигурации.</param>
    void SetConfiguration( ModuleConfig config );

    /// <summary>
    /// Устанавливает значение параметра модуля.
    /// </summary>
    /// <typeparam name="T">Тип параметра. Поддерживаются только значимые типы, строка или массив байт.</typeparam>
    /// <param name="name">Название параметра.</param>
    /// <param name="value">Значение параметра. Если null, то значение будет удалено.</param>
    void SetParameter<T>( string name, T value );

    /// <summary>
    /// Записывает сообщение в журнал событий сервера, обрезая сообщение до 31838 символов.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    /// <param name="type">Тип сообщения.</param>
    void WriteToLog( string message, ServerLogEntryType type );

    /// <summary>
    /// Записывает информацию об исключении в журнал событий сервера.
    /// </summary>
    /// <param name="exc">Исключение.</param>
    /// <param name="includeStackTrace">Признак того, что в текст исключения необходимо включить трассировку стэка исключения.</param>
    void WriteToLog( Exception exc, bool includeStackTrace );

    /// <summary>
    /// Записывает информацию об исключении в журнал событий сервера.
    /// </summary>
    /// <param name="exc">Исключение.</param>
    /// <param name="title">Поясняющая информация об исключении.</param>
    /// <param name="includeStackTrace">Признак того, что в текст исключения необходимо включить трассировку стэка исключения.</param>
    void WriteToLog( Exception exc, string title, bool includeStackTrace );

    /// <summary>
    /// Записывает сообщение в журнал событий сервера. Если сообщение получится длиной более 31838, то сообщение делится на части, и в заголовке каждого соощнения указывается номер части и полное количество частей.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    /// <param name="type">Тип события.</param>
    void WriteToLogMultiple( string message, ServerLogEntryType type );

    /// <summary>
    /// Записывает сообщение в журнал событий сервера, без заголовка, включающего имя модуля, обрезая сообщение до 31838 символов.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    /// <param name="type">Тип сообщения.</param>
    void WriteToLogWithoutModuleHeader( string message, ServerLogEntryType type );

    /// <summary>
    /// Осуществляет отправку udp сигнала на телематический сервер.
    /// </summary>
    /// <param name="signal">Сигнал.</param>
    void SendUDPSignal( string signal );

    /// <summary>
    /// Осуществляет отправку сообщения по udp на телематический сервер.
    /// </summary>
    /// <param name="type">Тип сообщения.</param>
    /// <param name="message">Текст сообщения.</param>
    void SendUDPLog(ServerLogEntryType type, string message);
  }
}
