using System;

using CoreWCF;
using CoreWCF.Web;

namespace ST.BusinessEntity.Server
{
    /// <summary>
    /// Интерфейс для работы со значимыми типами данных.
    /// </summary>
    //[WcfService( Constants.MODULE_ADDRESS, Constants.MODULE_NAMESPACE )]
    [ServiceContract(Namespace = Constants.MODULE_NAMESPACE)]
    public interface IValueType
    {
        [WebGet(UriTemplate = "GetId", ResponseFormat = WebMessageFormat.Json)]
        int GetId();

        /// <summary>
        /// Возвращает список всех значимых типов данных.
        /// </summary>
        /// <returns>Массив значимых типов данных.</returns>
        [WebGet(UriTemplate = "GetValueTypeList", ResponseFormat = WebMessageFormat.Json)]
        ValueTypeData[] GetValueTypeList();

        /// <summary>
        /// Возвращает значимый тип данных.
        /// </summary>
        /// <param name="valueTypeId">Идентификатор значимого типа данных.</param>
        /// <returns>Значимый тип данных.</returns>
        [WebInvoke(Method = "POST", UriTemplate = "GetValueType", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        ValueTypeData GetValueType(int valueTypeId);

    }

    /// <summary>
    /// Интерфейс для работы со значимыми типами данных.
    /// </summary>
    //[WcfService( Constants.MODULE_ADDRESS, Constants.MODULE_NAMESPACE )]
    [ServiceContract(Namespace = Constants.MODULE_NAMESPACE)]
    public interface IValueType2
    {
        [WebGet(UriTemplate = "GetId2", ResponseFormat = WebMessageFormat.Json)]
        int GetId2();

        [WebInvoke(Method = "POST", UriTemplate = "GetValueType2", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        ValueTypeData GetValueType2(int valueTypeId);
    }
}
