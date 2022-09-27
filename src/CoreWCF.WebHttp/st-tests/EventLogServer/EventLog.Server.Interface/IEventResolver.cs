
namespace ST.EventLog.Server
{
  /// <summary>
  /// Интерфейс для получения дополнительной информации по событиям.
  /// </summary>
  public interface IEventResolver
  {
    /// <summary>
    /// Устанавливает дополнительную информацию для события.
    /// </summary>
    /// <param name="evt">Событие.</param>
    void Resolve( Event evt );

    /// <summary>
    /// Устанавливает дополнительную информацию для списка событий.
    /// </summary>
    /// <param name="events">Массив событий.</param>
    void Resolve( Event[] events );

    /// <summary>
    /// Признак доступности события.
    /// </summary>
    /// <param name="evt">Событие.</param>
    /// <param name="userId">Текущий пользователь.</param>
    /// <returns>Признак доступности события текущему пользователю.</returns>
    bool IsAvailable( Event evt, int userId );
  }
}
