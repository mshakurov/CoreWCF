using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.SqlServer.Types
{
  public struct Point
  {
    private double _x;
    private double _y;

    public Point(double x, double y) : this()
    {
      _x = x;
      _y = y;
    }

    public double X { get => _x; set => _x = value; }
    public double Y { get => _y; set => _y = value; }

    //
    // Сводка:
    //     Создает System.String представление этого System.Windows.Point.
    //
    // Возврат:
    //     A System.String содержащий System.Windows.Point.X и System.Windows.Point.Y значения
    //     этого System.Windows.Point структуры.
    public override string ToString()
    {
      return ConvertToString(null, null);
    }

    internal string ConvertToString(string format, IFormatProvider provider)
    {
      const char numericListSeparator = ',';
      return string.Format(provider, "{1:" + format + "}{0}{2:" + format + "}", numericListSeparator, _x, _y);
    }

  }
}
