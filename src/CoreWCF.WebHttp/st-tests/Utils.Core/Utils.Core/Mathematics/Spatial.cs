using Microsoft.SqlServer.Types;

using System;
using System.Windows;

namespace ST.Utils.Mathematics
{
  /// <summary>
  /// Вспомогательный класс для работы с пространственными данными.
  /// </summary>
  public static class Spatial
  {
    #region GetBearing
    /// <summary>
    /// Возвращает азимут между двумя точками на Земле (угол между направлениями от первой точки на север и на вторую точку).
    /// Угол отсчитывается от напрвления на север от первой точки по часовой стрелке и лежит в диапазоне [0, 360] градусов.
    /// </summary>
    /// <param name="p1">Координаты первой точки в градусах.</param>
    /// <param name="p2">Координаты второй точки в градусах.</param>
    /// <returns>Азимут.</returns>
    public static double GetBearing( Point p1, Point p2 )
    {
      return GetBearing( p1.X, p1.Y, p2.X, p2.Y );
    }

    /// <summary>
    /// Возвращает азимут между двумя точками на Земле (угол между направлениями от первой точки на север и на вторую точку).
    /// Угол отсчитывается от напрвления на север от первой точки по часовой стрелке и лежит в диапазоне [0, 360] градусов.
    /// </summary>
    /// <param name="long1">Долгота первой точки в градусах.</param>
    /// <param name="lat1">Широта первой точки в градусах.</param>
    /// <param name="long2">Долгота второй точки в градусах.</param>
    /// <param name="lat2">Широта второй точки в градусах.</param>
    /// <returns>Азимут.</returns>
    public static double GetBearing( double long1, double lat1, double long2, double lat2 )
    {
      if( long1 == long2 && lat1 == lat2 )
        return 0.0;

      lat1 = Math.PI * lat1 / 180.0;
      lat2 = Math.PI * lat2 / 180.0;

      var delta = Math.PI * (long2 - long1) / 180.0;

      var y = Math.Sin( delta ) * Math.Cos( lat2 );
      var x = Math.Cos( lat1 ) * Math.Sin( lat2 ) - Math.Sin( lat1 ) * Math.Cos( lat2 ) * Math.Cos( delta );

      return (Math.Atan2( y, x ) * 180.0 / Math.PI + 360.0) % 360.0;
    }
    #endregion

    #region GetDistance
    /// <summary>
    /// Возвращает расстояние в метрах между двумя точками на Земле.
    /// </summary>
    /// <param name="p1">Координаты первой точки в градусах.</param>
    /// <param name="p2">Координаты второй точки в градусах.</param>
    /// <returns>Расстояние.</returns>
    public static double GetDistance( Point p1, Point p2 )
    {
      return GetDistance( p1.X, p1.Y, p2.X, p2.Y );
    }

    /// <summary>
    /// Возвращает расстояние в метрах между двумя точками на Земле.
    /// </summary>
    /// <param name="long1">Долгота первой точки в градусах.</param>
    /// <param name="lat1">Широта первой точки в градусах.</param>
    /// <param name="long2">Долгота второй точки в градусах.</param>
    /// <param name="lat2">Широта второй точки в градусах.</param>
    /// <returns>Расстояние.</returns>
    public static double GetDistance( double long1, double lat1, double long2, double lat2 )
    {
      if( long1 == long2 && lat1 == lat2 )
        return 0.0;

      var longDelta = Math.PI * (long2 - long1) / 180.0;
      var latDelta = Math.PI * (lat2 - lat1) / 180.0;

      lat1 = Math.PI * lat1 / 180.0;
      lat2 = Math.PI * lat2 / 180.0;

      var a = Math.Sin( latDelta / 2 ) * Math.Sin( latDelta / 2 ) + Math.Sin( longDelta / 2 ) * Math.Sin( longDelta / 2 ) * Math.Cos( lat1 ) * Math.Cos( lat2 );

      return 2 * Math.Atan2( Math.Sqrt( a ), Math.Sqrt( 1 - a ) ) * Constants.EARTH_RADIUS;
    }
    #endregion
  }
}
