using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace ST.Utils.Attributes
{
  /// <summary>
  /// Если свойство помечено этим атрибутом и имеет значение null, 
  /// то при отображении в пользовательском интерфейсе будет использоваться строка 
  /// из ресурсов в соответствии с текущей культурой.
  /// </summary>
  [PSerializable]
  [AttributeUsage( AttributeTargets.Property, AllowMultiple = false, Inherited = true )]
  public sealed class NullValueLocalizedAttribute : LocalizationAspect
  {
    #region .Ctor
    public NullValueLocalizedAttribute( string resourceId ) : base( typeof( StringLocalizedAttribute ), resourceId, typeof( NullValueLocalizedConverter ) )
    {
    }
    #endregion

    #region CompileTimeValidate
    public override bool CompileTimeValidate( object target )
    {
      return (base.CompileTimeValidate( target ) && !(target as PropertyInfo).PropertyType.IsValueType) || AspectHelper.Fail( 1, "NullValueLocalizedAttribute can't be applied to property of value type." );
    }
    #endregion

    ///<summary>
    /// Класс только для внутреннего использования.
    ///</summary>
    public class NullValueLocalizedConverter : ExpandableObjectConverter
    {
      #region CanConvertTo
      public override bool CanConvertTo( ITypeDescriptorContext context, Type destType )
      {
        return (destType == typeof( string )) ? true : base.CanConvertTo( context, destType );
      }
      #endregion

      #region ConvertTo
      public override object ConvertTo( ITypeDescriptorContext context, CultureInfo culture, object value, Type destType )
      {
        if( value == null )
        {
          var attr = context.PropertyDescriptor.Attributes[typeof( StringLocalizedAttribute )] as StringLocalizedAttribute;

          if( attr != null )
            return attr.LocalizedString;
        }

        return base.ConvertTo( context, culture, value, destType );
      }
      #endregion
    }
  }
}
