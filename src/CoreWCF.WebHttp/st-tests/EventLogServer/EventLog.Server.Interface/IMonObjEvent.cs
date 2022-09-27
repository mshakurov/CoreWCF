
namespace ST.EventLog.Server
{
  /// <summary>
  /// Событие содержит ОК
  /// </summary>
  public interface IMonObjEvent
  {
    /// <summary>
    /// Возвращает идентификатор ОК.
    /// </summary>
    /// <returns>Идентификатор ОК.</returns>
    int? GetMonObjId();
  }
}
