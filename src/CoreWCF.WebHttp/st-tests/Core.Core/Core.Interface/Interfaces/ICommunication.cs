using CoreWCF.Web;

namespace ST.Core
{
  /// <summary>
  /// Интерфейс для обмена коммуникационными сообщениями.
  /// </summary>
  [WcfService( Interface.Constants.SERVER_ADDRESS, Interface.Constants.SERVER_NAMESPACE )]
  public interface ICommunication
  {
    /// <summary>
    /// Получает все оставшиеся сообщения для текущей сессии.
    /// </summary>
    /// <param name="lastMessageId">Идентификатор последнего полученного сообщения.</param>
    /// <returns>Массив сообщений.</returns>
    [WebInvoke( Method = "POST", UriTemplate = "Get", BodyStyle = WebMessageBodyStyle.Wrapped, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json )]
    [return: HasInheritedDateTimes]
    CommunicationMessage[] Get( long lastMessageId );

    /// <summary>
    /// Посылает сообщение всем подписчикам.
    /// </summary>
    /// <param name="msg">Экземпляр сообщения.</param>
    [WebInvoke( Method = "POST", UriTemplate = "Send", BodyStyle = WebMessageBodyStyle.Wrapped, RequestFormat = WebMessageFormat.Json )]
    void Send( [HasInheritedDateTimes] CommunicationMessage msg );

    /// <summary>
    /// Подписывает на сообщение с указанным типом.
    /// </summary>
    /// <param name="messageType">Название типа сообщения.</param>
    [WebInvoke( Method = "POST", UriTemplate = "Subscribe", BodyStyle = WebMessageBodyStyle.Wrapped, RequestFormat = WebMessageFormat.Json )]
    void Subscribe( string messageType );

    /// <summary>
    /// Подписывает на несколько сообщений с указанными типами.
    /// </summary>
    /// <param name="messageTypes">Список названий типов сообщений.</param>
    [WebInvoke( Method = "POST", UriTemplate = "SubscribeMany", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json )]
    void SubscribeMany( string[] messageTypes );

    /// <summary>
    /// Отписывает от сообщения указанного типа.
    /// </summary>
    /// <param name="messageType">Название типа сообщения.</param>
    [WebInvoke( Method = "POST", UriTemplate = "Unsubscribe", BodyStyle = WebMessageBodyStyle.Wrapped, RequestFormat = WebMessageFormat.Json )]
    void Unsubscribe( string messageType );

    /// <summary>
    /// Отписывает от всех типов сообщений.
    /// </summary>
    [WebGet( UriTemplate = "UnsubscribeAll" )]
    void UnsubscribeAll();

    /// <summary>
    /// Отписывает от нескольких сообщений указанных типов.
    /// </summary>
    /// <param name="messageTypes">Список названий типов сообщений.</param>
    [WebInvoke( Method = "POST", UriTemplate = "UnsubscribeMany", BodyStyle = WebMessageBodyStyle.Wrapped, RequestFormat = WebMessageFormat.Json )]
    void UnsubscribeMany( string[] messageTypes );
  }
}
