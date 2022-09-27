using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Globalization;
using System.Linq;

namespace ST.Utils.TypeConverters
{
  /// <summary>
  /// Класс для преобразования типов, унаследованных от указанного, в строки и обратно.
  /// </summary>
  /// <typeparam name="T">Базовый тип.</typeparam>
  public class TypeListConverter<T> : TypeConverter
  {
    #region .Fields
    private Type _type = typeof( T );
    private Type[] _types;
    private TypeConverter.StandardValuesCollection _values;
    #endregion

    #region CanConvertFrom
    public override bool CanConvertFrom( ITypeDescriptorContext context, Type sourceType )
    {
      return sourceType == typeof( string ) || base.CanConvertFrom( context, sourceType );
    }
    #endregion

    #region CanConvertTo
    public override bool CanConvertTo( ITypeDescriptorContext context, Type destinationType )
    {
      return destinationType == typeof( string ) || base.CanConvertTo( context, destinationType );
    }
    #endregion

    #region ConvertFrom
    public override object ConvertFrom( ITypeDescriptorContext context, CultureInfo culture, object value )
    {
      Type type = null;

      var name = value as string;

      if( name != null )
        type = _types.FirstOrDefault( t => t.FullName == name );

      return type ?? base.ConvertFrom( context, culture, value );
    }
    #endregion

    #region ConvertTo
    public override object ConvertTo( ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType )
    {
      if( destinationType == typeof( string ) && value is Type )
        return value == null ? null : (value as Type).FullName;

      return base.ConvertTo( context, culture, value, destinationType );
    }
    #endregion

    #region GetStandardValues
    public override TypeConverter.StandardValuesCollection GetStandardValues( ITypeDescriptorContext context )
    {
      if( _values == null )
      {
        var tds = context.GetService( typeof( ITypeDiscoveryService ) ) as ITypeDiscoveryService;

        _types = tds == null ? new Type[0] : tds.GetTypes( _type, false ).Cast<Type>().Where( t => t != _type ).OrderBy( t => t.FullName ).ToArray();

        _values = new TypeConverter.StandardValuesCollection( _types );
      }

      return _values;
    }
    #endregion

    #region GetStandardValuesExclusive
    public override bool GetStandardValuesExclusive( ITypeDescriptorContext context )
    {
      return true;
    }
    #endregion

    #region GetStandardValuesSupported
    public override bool GetStandardValuesSupported( ITypeDescriptorContext context )
    {
      return true;
    }
    #endregion
  }
}
