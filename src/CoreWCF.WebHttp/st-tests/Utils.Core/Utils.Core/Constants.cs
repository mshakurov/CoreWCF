
namespace ST.Utils
{
  /// <summary>
  /// Константы.
  /// </summary>
  public static class Constants
  {
    #region .Constants
    /// <summary>
    /// Базовое пространство имен.
    /// </summary>
    public const string BASE_NAMESPACE = "http://www.space-team.com";

    /// <summary>
    /// Пространство имен общедоступных типов.
    /// </summary>
    public const string DATA_TYPES_NAMESPACE = BASE_NAMESPACE + "/DataTypes";

    /// <summary>
    /// Пространство имен, содержащее контракты для работы с фильтром элементов.
    /// </summary>
    public const string QUERY_FILTER_NAMESPACE = BASE_NAMESPACE + "/QueryFilter";

    /// <summary>
    /// Разделитель элементов коллекций в ресурсных строк.
    /// </summary>
    public const char RESOURCE_COLLECTION_ITEM_SEPARATOR = ';';

    /// <summary>
    /// Идентификатор модели пространственных данных.
    /// </summary>
    //public const int SPATIAL_REFERENCE_ID = 4326; // GEOGCS["WGS 84", DATUM["World Geodetic System 1984", ELLIPSOID["WGS 84", 6378137, 298.257223563]], PRIMEM["Greenwich", 0], UNIT["Degree", 0.0174532925199433]].
    public const int SPATIAL_REFERENCE_ID = 104001; // GEOGCS["Unit Sphere", DATUM["Unit Sphere", SPHEROID["Sphere", 1.0, 0.0]], PRIMEM["Greenwich",0.0], UNIT["Degree", 0.0174532925199433]].

    /// <summary>
    /// Радиус земли в метрах.
    /// </summary>
    public const double EARTH_RADIUS = 6378137.0;

    /// <summary>
    /// Строка сообщения в EventLog - 31,839 байт (32,766 байт в операционных системах Windows до Windows Vista).
    /// </summary>
    public const int EVENTLOG_MAXEVENTMESSAGEBYTES = 31838;
    #endregion
  }
}
