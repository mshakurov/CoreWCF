using System;
using System.Diagnostics;
using ST.Utils;
using ST.Utils.Attributes;

namespace ST.Core
{
  /// <summary>
  /// Базовый класс модуля, которому требуется работать с сессиями.
  /// </summary>
  public abstract class SessionModule : SubscriberModule
  {
    #region .Properties
    /// <summary>
    /// Менеджер сессии.
    /// </summary>
    public ISessionManager SessionManager
    {
      [DebuggerStepThrough]
      get { return ServerInterface as ISessionManager; }
    }
    #endregion

    #region Send
    /// <summary>
    /// Посылает коммуникационное сообщение всем подписчикам с возможностью фильтрации сессий, в которые сообщение должно быть отправлено.
    /// </summary>
    /// <typeparam name="T">Тип коммуникационного сообщения.</typeparam>
    /// <param name="msg">Сообщение.</param>
    /// <param name="filter">Метод фильтрации. Вызывается в контексте сессии, для которой необходимо определить необходимость отправки сообщения.</param>
    [DebuggerStepThrough]
    public void Send<T>( [NotNull] T msg, [NotNull] Func<T, bool> filter )
      where T : CommunicationMessage
    {
      ServerInterface.IfIs<IWcfServer>( s => s.Send( msg, filter ) );
    }
    #endregion
  }
}
