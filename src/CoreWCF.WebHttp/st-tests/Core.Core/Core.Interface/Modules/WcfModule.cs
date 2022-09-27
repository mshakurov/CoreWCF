using ST.Utils;
using ST.Utils.Attributes;

namespace ST.Core
{
  /// <summary>
  /// Базовый класс модуля, которому требуется работа с сервером приложений.
  /// </summary>
  public abstract class WcfModule : SubscriberModule
  {
    #region .Properties
    /// <summary>
    /// Признак наличия соединения с сервером.
    /// </summary>
    public bool IsConnected
    {
      get { return ServerInterface.IfIs( (IWcfClient s) => s.IsConnected ); }
    }
    #endregion

    #region GetWcfService
    /// <summary>
    /// Создает канал для обращения к WCF-сервису.
    /// </summary>
    /// <typeparam name="T">Тип интерфейса WCF-сервиса.</typeparam>
    /// <param name="address">Относительный адрес WCF-сервиса (без относительного адреса интерфейса). Полный адрес к интерфейсу WCF-сервиса будет сформирован так: {Адрес сервера}/{address}/{название T}.</param>
    /// <param name="nameSpace">Пространство имен WCF-сервиса (если null, то используется пространство имен по умолчанию).</param>
    /// <returns>Канал для обращения к WCF-сервису.</returns>
    public T GetWcfService<T>( [NotNullNotEmpty] string address, string nameSpace = null )
      where T : class
    {
      return ServerInterface.IfIs( (IWcfClient s) => s.GetWcfService<T>( address, nameSpace ) );
    }
    #endregion
  }
}
