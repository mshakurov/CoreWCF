using System;

namespace ST.EventLog.Server
{
  /// <summary>
  /// Интерфейс логирования сообытий.
  /// </summary>
  public interface ILogWriter
  {
    /// <summary>
    /// Выполняет логирование события. (в строковых данных события запрещается использование встроенных символов xml)
    /// </summary>
    /// <param name="evt">Событие.</param>
    /// <param name="time">Время регистрации события.</param>
    /// <param name="level">Уровень событие.</param>
    /// <param name="expiration"></param>
    Event Write( EventBase evt, DateTime? time = null, EventLevel level = EventLevel.Information, short? expiration = null );

    /// <summary>
    /// Выполняет логирование события. (в строковых данных события запрещается использование встроенных символов xml)
    /// </summary>
    /// <typeparam name="T">Тип источника события.</typeparam>
    /// <param name="evt">Событие.</param>
    /// <param name="time">Время регистрации события.</param>
    /// <param name="level">Уровень событие.</param>
    /// <param name="expiration"></param>
    Event Write<T>( EventBase evt, DateTime? time = null, EventLevel level = EventLevel.Information, short? expiration = null )
      where T : EventSource;

    /// <summary>
    /// Выполняет логирование события. (в строковых данных события запрещается использование встроенных символов xml)
    /// </summary>
    /// <typeparam name="TSource">Тип источника события.</typeparam>
    /// <typeparam name="TCategory">Тип категории события.</typeparam>
    /// <param name="evt">Событие.</param>
    /// <param name="time">Время регистрации события.</param>
    /// <param name="level">Уровень событие.</param>
    /// <param name="expiration">Протухание</param>
    Event Write<TSource, TCategory>( EventBase evt, DateTime? time = null, EventLevel level = EventLevel.Information, int? expiration = null )
      where TSource : EventSource
      where TCategory : EventCategory;

    /// <summary>
    /// Сохраняет событие, допуская любые символы в строковых значениях
    /// </summary>
    /// <typeparam name="TSource">Тип источника события.</typeparam>
    /// <typeparam name="TCategory">Тип категории события.</typeparam>
    /// <param name="evt">Событие.</param>
    /// <param name="time">Время регистрации события.</param>
    /// <param name="level">Уровень событие.</param>
    /// <param name="expiration">Протухание</param>
    /// <returns>Сохраненная версия события</returns>
    Event Write2<TSource, TCategory>( EventBase evt, DateTime? time = null, EventLevel level = EventLevel.Information, int? expiration = null )
      where TSource : EventSource
      where TCategory : EventCategory;
  }
}
