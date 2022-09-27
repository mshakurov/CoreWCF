using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

namespace ST.Utils.TypeConverters
{
  /// <summary>
  /// Базовый класс для преобразования перечислений.
  /// </summary>
  public abstract class BaseEnumConverter<T> : EnumConverter
  {
    #region .Fields
    private readonly Dictionary<Enum, T> _dictTo = new Dictionary<Enum, T>();
    private readonly Dictionary<T, Enum> _dictFrom = new Dictionary<T, Enum>();
    #endregion

    #region .Ctor
    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="type">Тип перечисления.</param>
    public BaseEnumConverter( Type type ) : base( type )
    {
      if( !type.IsEnum )
        throw new ArgumentException( "Parameter 'type' must be enum type." );

      foreach( Enum enumValue in GetStandardValues() )
      {
        var value = GetValue( enumValue );

        if( !value.Equals( default( T ) ) )
        {
          _dictTo.Add( enumValue, value );
          _dictFrom.Add( value, enumValue );
        }
      }
    }
    #endregion

    #region CanConvertFrom
    public override bool CanConvertFrom( ITypeDescriptorContext context, Type sourceType )
    {
      if( sourceType == typeof( T ) )
        return true;

      return base.CanConvertFrom( context, sourceType );
    }
    #endregion

    #region CanConvertTo
    public override bool CanConvertTo( ITypeDescriptorContext context, Type destinationType )
    {
      if( destinationType == typeof( T ) )
        return true;

      return base.CanConvertTo( context, destinationType );
    }
    #endregion

    #region ConvertFrom
    public override object ConvertFrom( ITypeDescriptorContext context, CultureInfo culture, object value )
    {
      var d = value != null && value.GetType() == typeof( T ) ? _dictFrom.GetValue( (T) value ) : default( Enum );

      return d.Equals( default( Enum ) ) ? base.ConvertFrom( context, culture, value ) : d;
    }
    #endregion

    #region ConvertTo
    public override object ConvertTo( ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType )
    {
      var d = destinationType == typeof( T ) ? _dictTo.GetValue( (Enum) value ) : default( T );

      return d.Equals( default( T ) ) ? base.ConvertTo( context, culture, value, destinationType ) : d;
    }
    #endregion

    #region GetValue
    /// <summary>
    /// Вызывается для получения значения, соответствующего элементу перечисления.
    /// </summary>
    /// <param name="enumValue">Элемент перечисления.</param>
    /// <returns>Значение.</returns>
    protected abstract T GetValue( Enum enumValue );
    #endregion
  }
}
