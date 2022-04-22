using CoreWCF;
using CoreWCF.Web;

namespace ST.VideoIntegration.Server
{
    /// <summary>
    /// Интерфейс для работы по интеграции
    /// </summary>
    //[WcfService( Constants.MODULE_ADDRESS, Constants.MODULE_NAMESPACE )]
    [ServiceContract(Namespace = Constants.MODULE_NAMESPACE)]
    public interface IVideoIntegration
    {
        /// <summary>
        /// Возвращает навигационны+е данные.
        /// </summary>
        /// <param name="monObjId">Идентификатор объектов контроля.</param>
        /// <param name="camNum">Номер камеры, с которой следует транслировать онлайн видео.</param>
        /// <param name="streamType">Код типа видеопотока, затребованного для просмотра.</param>
        /// <returns>Массив навигационных данных.</returns>
        [WebInvoke(Method = "POST", UriTemplate = "GetOnlineVideoUrl", BodyStyle = WebMessageBodyStyle.WrappedRequest, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        [FaultContract(typeof(InvalidParamFault))]
        string GetOnlineVideoUrl(int monObjId, int camNum, int streamType);
    }
}
