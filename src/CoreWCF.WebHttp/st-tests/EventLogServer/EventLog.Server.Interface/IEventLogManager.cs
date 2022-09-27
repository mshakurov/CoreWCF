using CoreWCF;
using CoreWCF.Web;

using ST.Core;

namespace ST.EventLog.Server
{
  /// <summary>
  /// Интерфейс управления событиями.
  /// </summary>
  //[WcfService( Constants.MODULE_ADDRESS, Constants.MODULE_NAMESPACE )]
  [ServiceContract(Namespace = Constants.MODULE_NAMESPACE)]
  [LogonFaultAttribute]
  public interface IEventLogManager
  {
    /// <summary>
    /// Возвращает доступность к типу для пользователя.
    /// </summary>
    /// <param name="typeId">Идентификатор типа.</param>
    /// <param name="userId">Идентификатор пользователя.</param>
    [WebInvoke( Method = "POST", UriTemplate = "GetEventTypeUser", BodyStyle = WebMessageBodyStyle.WrappedRequest, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json )]
    EventTypeUser GetEventTypeUser( int typeId, int userId );

    /// <summary>
    /// Возвращает доступность к типу для текущего пользователя (без проверки права выполнять проверку доступности).
    /// </summary>
    /// <param name="typeId"></param>
    /// <returns>Доступность пользователя к типу события</returns>
    [WebInvoke( Method = "POST", UriTemplate = "GetEventTypeCurrentUser", BodyStyle = WebMessageBodyStyle.WrappedRequest, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json )]
    EventTypeUser GetEventTypeCurrentUser( int typeId );

    /// <summary>
    /// Возвращает доступность к типам.
    /// </summary>
    [WebGet( UriTemplate = "GetEventTypeUserList", ResponseFormat = WebMessageFormat.Json )]
    EventTypeUser[] GetEventTypeUserList();

    /// <summary>
    /// Возвращает доступность к типу.
    /// </summary>
    /// <param name="typeId">Идентификатор типа.</param>
    [WebInvoke( Method = "POST", UriTemplate = "GetListByType", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json )]
    EventTypeUser[] GetListByType( int typeId );

    /// <summary>
    /// Возвращает доступность к типу для пользователя.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    [WebInvoke( Method = "POST", UriTemplate = "GetListByUser", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json )]
    EventTypeUser[] GetListByUser( int userId );

    /// <summary>
    /// Возвращает список типов событий.
    /// </summary>
    [WebGet( UriTemplate = "GetEventTypeDescriptorList", ResponseFormat = WebMessageFormat.Json )]
    EventTypeDescriptor[] GetEventTypeDescriptorList();

    /// <summary>
    /// Удаляет доступность для пользователя.
    /// </summary>
    /// <param name="typeId">Идентификатор типа.</param>
    /// <param name="userId">Идентификатор пользователя.</param>
    [WebInvoke( Method = "POST", UriTemplate = "Remove", BodyStyle = WebMessageBodyStyle.WrappedRequest, RequestFormat = WebMessageFormat.Json )]
    void Remove( int typeId, int userId );

    /// <summary>
    /// Устанавливает доступность для пользователя.
    /// </summary>
    /// <param name="data">Доступность для пользователя.</param>
    [WebInvoke( Method = "POST", UriTemplate = "Set", RequestFormat = WebMessageFormat.Json )]
    void Set( EventTypeUser data );

    /// <summary>
    /// Только для внутреннего использования.
    /// </summary>
    /// <param name="data">Доступность для пользователя.</param>
    [WebInvoke( Method = "POST", UriTemplate = "SetInternal", RequestFormat = WebMessageFormat.Json )]
    void SetInternal( EventTypeUser data );

    /// <summary>
    /// Только для внутреннего использования.
    /// </summary>
    /// <param name="typeId">Идентификатор типа.</param>
    /// <param name="userId">Идентификатор пользователя.</param>
    [WebInvoke( Method = "POST", UriTemplate = "RemoveInternal", BodyStyle = WebMessageBodyStyle.WrappedRequest, RequestFormat = WebMessageFormat.Json )]
    void RemoveInternal( int typeId, int userId );

    /// <summary>
    /// Возвращает признак подписки типа события для пользователя.
    /// </summary>
    /// <param name="typeId">Идентификатор типа.</param>
    /// <param name="userId">Идентификатор пользователя.</param>
   [WebInvoke( Method = "POST", UriTemplate = "IsSubscribed", BodyStyle = WebMessageBodyStyle.WrappedRequest, RequestFormat = WebMessageFormat.Json )]
    bool IsSubscribed( int? typeId, int userId );
  }
}
