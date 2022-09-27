namespace ST.Core
{
  /// <summary>
  /// Интерфейс подписчика.
  /// </summary>
  [NotServiceInterface]
  public interface ISubscriber
  {
    /// <summary>
    /// Обрабатчик сообщения.
    /// </summary>
    /// <param name="msg">Экземпляр сообщения.</param>
    void OnMessage( BaseMessage msg );
  }
}
