using System;

using CoreWCF;
using CoreWCF.Web;

using ST.Core;
using ST.Utils.DataTypes;

namespace ST.EventLog.Server
{
    /// <summary>
    /// Интерфейс журнала событий.
    /// </summary>
    //[WcfService( Constants.MODULE_ADDRESS, Constants.MODULE_NAMESPACE )]
    [ServiceContract(Namespace = Constants.MODULE_NAMESPACE)]
    [LogonFaultAttribute]
    public interface IEventLog
    {
        /// <summary>
        /// Возвращает событие.
        /// </summary>
        /// <param name="id">Идентификатор события.</param>
        [WebInvoke(Method = "POST", UriTemplate = "Get", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        //[FaultContract( typeof( NotLoggedOnFault ) )]
        Event Get(int id);

        /// <summary>
        /// Возвращает категорию события.
        /// </summary>
        /// <param name="id">Идентификатор категории.</param>
        [WebInvoke(Method = "POST", UriTemplate = "GetCategory", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        CategoryDescriptor GetCategory(int id);

        /// <summary>
        /// Возвращает список категорий событий.
        /// </summary>
        [WebGet(UriTemplate = "GetCategoryList", ResponseFormat = WebMessageFormat.Json)]
        CategoryDescriptor[] GetCategoryList();

        /// <summary>
        /// Возвращает список событий за указанный интервал времени с учетом фильтра.
        /// </summary>
        /// <param name="time">Интервал времени.</param>
        /// <param name="filter">Фильтр.</param>
        /// <param name="details">Признак того, что необходимо получить детали события.</param>
        /// <param name="limit">Количество запрашиваемых событий.</param>
        /// <param name="startEventID">Идентификатор события, с которого начинать поиск событий.</param>
        /// <param name="startEventTime">Время события, с которого начинать поиск событий.</param>
        /// <param name="sortOrder">Порядок сортировки по дате и времени события</param>
        /// <param name="useExpiration">Учитывать актуальность событий.</param>
        /// <param name="getResolve">Получать информацию об обработке событий.</param>
        [WebInvoke(Method = "POST", UriTemplate = "GetList", BodyStyle = WebMessageBodyStyle.WrappedRequest, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Event[] GetList(TimeRange time, EventFilter filter, bool? details, int? limit, int? startEventID, DateTime? startEventTime, string sortOrder, bool useExpiration = false, bool getResolve = false);

        /// <summary>
        /// Возвращает список событий определенного типа, отработанных за указанный интервал времени.
        /// </summary>
        /// <param name="typeIds">Идентификаторы типов событий. Если пустой, то при выборке данных тип события учтен не будет.</param>
        /// <param name="time">Интервал времени.</param>
        [WebInvoke(Method = "POST", UriTemplate = "GetResolvedList", BodyStyle = WebMessageBodyStyle.WrappedRequest, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Event[] GetResolvedList(int[] typeIds, TimeRange time);

        /// <summary>
        /// Возвращает количество событий за указанный интервал времени с учетом фильтра.
        /// </summary>
        /// <param name="time">Интервал времени.</param>
        /// <param name="filter">Фильтр.</param>
        [WebInvoke(Method = "POST", UriTemplate = "GetListCoumt", BodyStyle = WebMessageBodyStyle.WrappedRequest, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        int GetListCount(TimeRange time, EventFilter filter);

        /// <summary>
        /// Возвращает информацмю, относящуюся к обработке события.
        /// </summary>
        /// <param name="id">Идентификатор события.</param>
        [WebInvoke(Method = "POST", UriTemplate = "GetProcessingInfo", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        ProcessingInfo[] GetProcessingInfo(int id);

        /// <summary>
        /// Возвращает источник события.
        /// </summary>
        /// <param name="id">Идентификатор источника.</param>
        [WebInvoke(Method = "POST", UriTemplate = "GetSource", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        SourceDescriptor GetSource(int id);

        /// <summary>
        /// Возвращает список источников событий.
        /// </summary>
        [WebGet(UriTemplate = "GetSourceList", ResponseFormat = WebMessageFormat.Json)]
        SourceDescriptor[] GetSourceList();

        /// <summary>
        /// Возвращает тип события.
        /// </summary>
        /// <param name="id">Идентификатор типа.</param>
        [WebInvoke(Method = "POST", UriTemplate = "GetType", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        EventTypeDescriptor GetType(int id);

        /// <summary>
        /// Возвращает список типов событий.
        /// </summary>
        /// <param name="rootTypeId">Идентификатор корневого типа события. Если 0, то будут возвращен список всех типов событий.</param>
        [WebInvoke(Method = "POST", UriTemplate = "GetTypeList", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        EventTypeDescriptor[] GetTypeList(int rootTypeId = 0);

        /// <summary>
        /// Возвращает событие c информацией по доступности пользователя к типу события.
        /// </summary>
        /// <param name="id">Идентификатор события.</param>
        [WebInvoke(Method = "POST", UriTemplate = "GetWithUserAccessInfo", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        EventData GetWithUserAccessInfo(int id);

        /// <summary>
        /// Возвращает признак необходимости обработки события текущим пользователем.
        /// </summary>
        /// <param name="eventId">Идентификатор события.</param>
        [WebInvoke(Method = "POST", UriTemplate = "IsProcessRequired", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        bool IsProcessRequired(int eventId);

        /// <summary>
        /// Обрабатывает событие.
        /// </summary>
        /// <param name="id">Идентификатор события.</param>
        /// <param name="description">Описание обрабатываемого события.</param>
        [WebInvoke(Method = "POST", UriTemplate = "ProcessEvent", BodyStyle = WebMessageBodyStyle.WrappedRequest, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        [FaultContract(typeof(UserPermissionEditFault))]
        EventState ProcessEvent(int id, string description);

        /// <summary>
        /// Обрабатывает массив событий.
        /// </summary>
        /// <param name="ids">Массив идентификаторов событий.</param>
        /// <param name="description">Описание обрабатываемыйх событий.</param>
        [WebInvoke(Method = "POST", UriTemplate = "ProcessEvents", BodyStyle = WebMessageBodyStyle.WrappedRequest, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        [FaultContract(typeof(UserPermissionEditFault))]
        EventResultState[] ProcessEvents(int[] ids, string description);


        [WebInvoke(Method = "POST", UriTemplate = "TestGetEvent", BodyStyle = WebMessageBodyStyle.WrappedRequest, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        TestEvent TestGetEvent(int id);

        [WebInvoke(Method = "POST", UriTemplate = "TestGetEvent", BodyStyle = WebMessageBodyStyle.WrappedRequest, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        TestEvent TestChangeEvent(TestEvent evt);

        [WebInvoke(Method = "POST", UriTemplate = "TestGetEvent", BodyStyle = WebMessageBodyStyle.WrappedRequest, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Event TestGetRealEvent(int id);
    }
}
