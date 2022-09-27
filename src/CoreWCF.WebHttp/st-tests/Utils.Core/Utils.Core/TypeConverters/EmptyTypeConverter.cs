using System;
using System.ComponentModel;

namespace ST.Utils.TypeConverters
{
  /// <summary>
  /// Пустой преобразователь типов - запрещает любые преобразования.
  /// </summary>
  public class EmptyTypeConverter : TypeConverter
  {
    #region CanConvertFrom
    public override bool CanConvertFrom( ITypeDescriptorContext context, Type sourceType )
    {
      return false;
    }
    #endregion

    #region CanConvertTo
    public override bool CanConvertTo( ITypeDescriptorContext context, Type destinationType )
    {
      return false;
    }
    #endregion
  }
}
