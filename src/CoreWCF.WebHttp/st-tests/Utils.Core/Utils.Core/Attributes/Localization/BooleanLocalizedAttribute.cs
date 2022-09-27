using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace ST.Utils.Attributes
{
  /// <summary>
  /// Применение этого атрибута к свойству, приводит к тому, что при отображении в пользовательском интерфейсе 
  /// значений true и false, строки выбираются из ресурсов в соответствии с текущей культурой.
  /// Части ресурсной строки, соответсвующие true и false, должны быть разделены между собой символом ';'.
  /// </summary>
  [PSerializable]
  [AttributeUsage( AttributeTargets.Property, AllowMultiple = false, Inherited = true )]
  public sealed class BooleanLocalizedAttribute : LocalizationAspect
  {
    #region .Ctor
    public BooleanLocalizedAttribute( string resourceId ) : base( typeof( StringLocalizedAttribute ), resourceId, typeof( BooleanLocalizedConverter ) )
    {
    }
    #endregion

    #region CompileTimeValidate
    public override bool CompileTimeValidate( object target )
    {
      return (base.CompileTimeValidate( target ) && (target as PropertyInfo).PropertyType == typeof( bool )) || AspectHelper.Fail( 1, "BooleanLocalizedAttribute applicable to boolean type only." );
    }
    #endregion

    ///<summary>
    /// Класс только для внутреннего использования.
    ///</summary>
    public class BooleanLocalizedConverter : BooleanConverter
    {
      #region .Constants
      private const char TRUE_FALSE_SEPARATOR = ';';
      #endregion

      #region CanConvertFrom
      public override bool CanConvertFrom( ITypeDescriptorContext context, Type srcType )
      {
        return srcType == typeof( string );
      }
      #endregion

      #region CanConvertTo
      public override bool CanConvertTo( ITypeDescriptorContext context, Type destType )
      {
        return destType == typeof( string );
      }
      #endregion

      #region ConvertFrom
      public override object ConvertFrom( ITypeDescriptorContext context, CultureInfo culture, object value )
      {
        var attr = context.PropertyDescriptor.Attributes[typeof( StringLocalizedAttribute )] as StringLocalizedAttribute;

        if( attr == null )
          return base.ConvertFrom( context, culture, value );

        var s = attr.LocalizedString;

        return (value as string) == s.Substring( 0, s.IndexOf( TRUE_FALSE_SEPARATOR ) );
      }
      #endregion

      #region ConvertTo
      public override object ConvertTo( ITypeDescriptorContext context, CultureInfo culture, object value, Type destType )
      {
        var attr = context.PropertyDescriptor.Attributes[typeof( StringLocalizedAttribute )] as StringLocalizedAttribute;

        if( attr == null )
          return base.ConvertTo( context, culture, value, destType );

        var s = attr.LocalizedString;

        var sepIdx = s.IndexOf( TRUE_FALSE_SEPARATOR );

        return (bool) value ? s.Substring( 0, sepIdx ) : s.Substring( sepIdx + 1 );
      }
      #endregion
    }
  }
}
