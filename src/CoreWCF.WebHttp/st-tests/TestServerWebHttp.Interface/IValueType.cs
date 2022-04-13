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
}
