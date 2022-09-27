using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ST.Utils.Mathematics
{
  /// <summary>
  /// Вспомогательный класс для интерполяции данных.
  /// </summary>
  public static class Interpolation
  {
    /// <summary>
    /// Линейная интерполяция. Нахождение Y координаты точки, расположенной на отрезке (X0,Y0) - (X1,Y1), в заданной X координате.
    /// </summary>
    /// <param name="x">Х координата искомого значения Y</param>
    /// <param name="x0">X координата начала отрезка</param>
    /// <param name="x1">X координата конца отрезка</param>
    /// <param name="y0">Y координата начала отрезка</param>
    /// <param name="y1">Y координата конца отрезка</param>
    /// <returns>Y координата точки на отрезке с заданой координатой X</returns>
    public static double Linear( double x, double x0, double x1, double y0, double y1 )
    {
      if ( ( x1 - x0 ) == 0 )
      {
        return ( y0 + y1 ) / 2;
      }
      return y0 + ( x - x0 ) * ( y1 - y0 ) / ( x1 - x0 );
    }
  }
}
