
namespace ST.Core
{
  /// <summary>
  /// Интерфейс сервера, поддерживающего взаимодействие с сервером приложений.
  /// </summary>
  [ServerInterface]
  public interface IWcfClient
  {
    /// <summary>
    /// Признак наличия соединения с сервером приложений.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Возвращает WCF-сервис, размещенный на сервере приложений.
    /// </summary>
    /// <typeparam name="T">Тип WCF-сервиса.</typeparam>
    /// <param name="address">Относительный адрес WCF-сервера. Полный адрес к интерфейсу WCF-сервиса будет сформирован так: {Адрес сервера приложений}/{address}/{название T}.</param>
    /// <param name="nameSpace">Пространство имен WCF-сервиса (если null, то используется пространство имен по умолчанию).</param>
    /// <returns>WCF-сервис.</returns>
    T GetWcfService<T>( string address, string nameSpace = null )
      where T : class;
  }
}
