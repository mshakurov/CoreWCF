using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace ST.Utils.Attributes
{
  /// <summary>
  /// Атрибут, позволяющий изменить стандартное отображение коллекций в зависимости от текущей культуры.
  /// Ресурсная строка для этого атрибута состоит из частей (части разделены символом ';'):
  /// - Форматная строка для заголовочной строки списка (правая часть первой строки), параметр {0} заменяется на к-во элементов в коллекции;
  /// - Строка для заголовочной строки списка (правая часть первой строки) в случае, если коллекция пуста или отсутствует;
  /// - Форматная строка для заголовка элемента (левая часть строк-элементов), параметр {0} заменяется на порядковый номер элемента.
  /// </summary>
  [PSerializable]
  [AttributeUsage( AttributeTargets.Property, AllowMultiple = false, Inherited = true )]
  public sealed class CollectionLocalizedAttribute : LocalizationAspect
  {
    #region .Ctor
    public CollectionLocalizedAttribute( string resourceId ) : base( typeof( StringLocalizedAttribute ), resourceId, typeof( CollectionLocalizedConverter ) )
    {
    }
    #endregion

    #region CompileTimeValidate
    public override bool CompileTimeValidate( object target )
    {
      return (base.CompileTimeValidate( target ) && typeof( ICollection ).IsAssignableFrom( (target as PropertyInfo).PropertyType )) || AspectHelper.Fail( 1, "CollectionLocalizedAttribute applicable to property of type that implements interface ICollection only." );
    }
    #endregion

    ///<summary>
    /// Класс только для внутреннего использования.
    ///</summary>
    public class CollectionLocalizedConverter : ExpandableObjectConverter
    {
      #region .Constants
      private const string DEFAULT_ITEM_NAME_FORMAT = "{0}.";
      #endregion

      #region CanConvertTo
      public override bool CanConvertTo( ITypeDescriptorContext context, Type destType )
      {
        return (destType == typeof( string )) ? true : base.CanConvertTo( context, destType );
      }
      #endregion

      #region ConvertTo
      public override object ConvertTo( ITypeDescriptorContext context, CultureInfo culture, object value, Type destType )
      {
        var allAttrs = context.PropertyDescriptor.Attributes;

        var attr = allAttrs.OfType<StringLocalizedAttribute>().SingleOrDefault();

        if( attr == null )
          return base.ConvertTo( context, culture, value, destType );

        var collection = value as ICollection;

        var strings = attr.LocalizedString.Split( Constants.RESOURCE_COLLECTION_ITEM_SEPARATOR );

        return (collection == null || collection.Count == 0) ? string.Format( strings.Length < 2 ? strings[0] : strings[1], 0 ) :
                                                               string.Format( strings[0], collection.Count );
      }
      #endregion

      #region GetProperties
      public override PropertyDescriptorCollection GetProperties( ITypeDescriptorContext context, object value, Attribute[] attributes )
      {
        var attr = context.PropertyDescriptor.Attributes[typeof( StringLocalizedAttribute )] as StringLocalizedAttribute;

        if( attr == null )
          return base.GetProperties( context, value, attributes );

        var strings = attr.LocalizedString.Split( Constants.RESOURCE_COLLECTION_ITEM_SEPARATOR );

        var itemNameFormat = (strings.Length < 3) ? DEFAULT_ITEM_NAME_FORMAT : strings[2];

        var idx = 0;

        var pdc = new PropertyDescriptorCollection( null );

        foreach( var item in (value as ICollection) )
        {
          pdc.Add( new CollectionItemPropertyDescriptor( item, idx, itemNameFormat ) );

          idx++;
        }

        return pdc;
      }
      #endregion

      private class CollectionItemPropertyDescriptor : PropertyDescriptor
      {
        #region .Constants
        private const string NAME_PREFIX = "#";
        #endregion

        #region .Fields
        private readonly object _itemValue;

        private readonly int _itemIndex;

        private readonly string _itemNameFormat;
        #endregion

        #region .Properties
        public override AttributeCollection Attributes
        {
          get { return new AttributeCollection( null ); }
        }

        public override Type ComponentType
        {
          get { return typeof( ICollection ); }
        }

        public override string DisplayName
        {
          get { return string.Format( _itemNameFormat, _itemIndex + 1 ); }
        }

        public override string Description
        {
          get { return _itemValue.ToString(); }
        }

        public override bool IsReadOnly
        {
          get { return true; }
        }

        public override string Name
        {
          get { return NAME_PREFIX + _itemIndex.ToString(); }
        }

        public override Type PropertyType
        {
          get { return _itemValue.GetType(); }
        }
        #endregion

        #region .Ctor
        public CollectionItemPropertyDescriptor( object item, int index, string itemNameFormat ) : base( NAME_PREFIX + index.ToString(), null )
        {
          _itemValue = item;

          _itemIndex = index;

          _itemNameFormat = itemNameFormat;
        }
        #endregion

        #region CanResetValue
        public override bool CanResetValue( object component )
        {
          return true;
        }
        #endregion

        #region GetValue
        public override object GetValue( object component )
        {
          return _itemValue;
        }
        #endregion

        #region ResetValue
        public override void ResetValue( object component )
        {
        }
        #endregion

        #region SetValue
        public override void SetValue( object component, object value )
        {
        }
        #endregion

        #region ShouldSerializeValue
        public override bool ShouldSerializeValue( object component )
        {
          return true;
        }
        #endregion
      }
    }
  }
}
