using System;

using CoreWCF;
using CoreWCF.Web;

namespace ST.BusinessEntity.Server
{
    /// <summary>
    /// Интерфейс для работы с типами бизнес-сущностей.
    /// </summary>
    //[WcfService( Constants.MODULE_ADDRESS, Constants.MODULE_NAMESPACE )]
    [ServiceContract(Namespace = Constants.MODULE_NAMESPACE)]
    public interface IEntityType
    {
        /// <summary>
        /// Возвращает тип бизнес-сущности.
        /// </summary>
        /// <param name="entityTypeId">Идентификатор типа бизнес-сущности.</param>
        /// <param name="options">Параметры выборки.</param>
        /// <param name="result">Параметр результата выборки.</param>
        /// <returns>Тип бизнес-сущности.</returns>
        [WebInvoke(Method = "POST", UriTemplate = "GetEntityType", BodyStyle = WebMessageBodyStyle.WrappedRequest, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        EntityType GetEntityType(int entityTypeId, EntityQueryOption options = EntityQueryOption.Default, EntityTypeResult result = EntityTypeResult.Default);

        /// <summary>
        /// Возвращает тип бизнес-сущности.
        /// </summary>
        /// <param name="code">Код типа бизнес-сущности.</param>
        /// <param name="options">Параметры выборки.</param>
        /// <param name="result">Параметр результата выборки.</param>
        /// <returns>Тип бизнес-сущности.</returns>
        [WebInvoke(Method = "POST", UriTemplate = "GetEntityTypeByCode", BodyStyle = WebMessageBodyStyle.WrappedRequest, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        EntityType GetEntityTypeByCode(string code, EntityQueryOption options = EntityQueryOption.Default, EntityTypeResult result = EntityTypeResult.Default);

        /// <summary>
        /// Возвращает список всех типов бизнес-сущностей.
        /// </summary>
        /// <returns>Массив типов бизнес-сущностей.</returns>
        [WebInvoke(UriTemplate = "GetEntityTypeList", ResponseFormat = WebMessageFormat.Json)]
        EntityType[] GetEntityTypeList(EntityQueryOption options = EntityQueryOption.Default);

        /// <summary>
        /// Удаляет тип бизнес-сущности.
        /// </summary>
        /// <param name="entityTypeId">Идентификатор типа бизнес-сущности.</param>
        [WebInvoke(Method = "POST", UriTemplate = "RemoveEntityType", RequestFormat = WebMessageFormat.Json)]
        [FaultContract(typeof(EntityFault))]
        void RemoveEntityType(int entityTypeId);

        /// <summary>
        /// Добавляет/изменяет тип бизнес-сущности.
        /// </summary>
        /// <param name="entityType">Тип бизнес-сущности.</param>
        /// <returns>Добавленный/измененный тип бизнес-сущности.</returns>
        [WebInvoke(Method = "POST", UriTemplate = "SetEntityType", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        [FaultContract(typeof(EntityFault))]
        EntityType SetEntityType(EntityType entityType);
    }
}
