namespace ST.Core
{
  /// <summary>
  /// Интерфейс издателя.
  /// </summary>
  [ServerInterface]
  public interface IPublisher
  {
    /// <summary>
    /// Посылает сообщение всем подписчикам.
    /// </summary>
    /// <param name="msg">Сообщение.</param>
    void Send( BaseMessage msg );

    /// <summary>
    /// Позволяет подписаться на сообщение указанного типа и всех его потомков.
    /// </summary>
    /// <typeparam name="T">Тип сообщения.</typeparam>
    void Subscribe<T>()
      where T : BaseMessage;

    /// <summary>
    /// Позволяет отписаться от сообщения указанного типа и всех его потомков.
    /// </summary>
    /// <typeparam name="T">Тип сообщения.</typeparam>
    void Unsubscribe<T>()
      where T : BaseMessage;
  }
}
