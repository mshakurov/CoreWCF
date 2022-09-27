using System;

namespace ST.Core
{
  /// <summary>
  /// Интерфейс WCF-сервера.
  /// </summary>
  [ServerInterface]
  public interface IWcfServer
  {
    /// <summary>
    /// Посылает коммуникационное сообщение всем подписчикам с возможностью фильтрации сессий, в которые сообщение должно быть отправлено.
    /// </summary>
    /// <typeparam name="T">Тип коммуникационного сообщения.</typeparam>
    /// <param name="msg">Сообщение.</param>
    /// <param name="filter">Метод фильтрации. Вызывается в рамках сессии, для которой необходимо определить необходимость отправки сообщения.</param>
    void Send<T>( T msg, Func<T, bool> filter )
      where T : CommunicationMessage;

    /// <summary>
    /// Выполняет действие в контексте определенной сессии.
    /// </summary>
    /// <param name="sessionId">Идентификатор сессии.</param>
    /// <param name="action">Действие</param>
    void ExecuteForSession( ulong sessionId, Action action );
  }
}
