
namespace ST.EventLog.Server
{
  /// <summary>
  /// Константы.
  /// </summary>
  public static class Constants
  {
    #region .Constants
    /// <summary>
    /// Название пространства имен модуля.
    /// </summary>
    public const string MODULE_NAMESPACE = ST.Utils.Constants.BASE_NAMESPACE + "/EventLog";

    /// <summary>
    /// Адрес WCF-сервера модуля.
    /// </summary>
    public const string MODULE_ADDRESS = "EventLogServer";

    /// <summary>
    /// Название корневого элемента в упрощенном XML-представлении.
    /// </summary>
    public const string ROOT_ELEMENT_NAME = "Event";

    /// <summary>
    /// Название метода разрешения дополнительной информации по событию.
    /// </summary>
    public const string RESOLVER_METHOD_NAME = "GetEventResolver";
    #endregion
  }
}
