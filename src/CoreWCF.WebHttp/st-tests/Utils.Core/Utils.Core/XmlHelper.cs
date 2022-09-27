using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;

namespace ST.Utils
{
  /// <summary>
  /// Вспомогательный класс для работы с Xml.
  /// </summary>
  public static class XmlHelper
  {
    #region ArrayToXml
    /// <summary>
    /// Создает XML вида: &lt;rootElementName&gt;&lt;elementName attributeName="T.ToString()"&gt;&lt;/rootElementName&gt;
    /// </summary>
    /// <typeparam name="T">Тип элемента массива.</typeparam>
    /// <param name="array">Массив значний типа T.</param>
    /// <param name="rootElementName">Название корневого элемента XML.</param>
    /// <param name="elementName">Название элемента XML.</param>
    /// <param name="attributeName">Название атрибута XML.</param>
    /// <returns>Возвращает XML представление массива элементов типа T.</returns>
    public static string ArrayToXml<T>( T[] array, string rootElementName, string elementName, string attributeName )
    {
      //return ArrayToXml( array, rootElementName, elementName, new string[] { attributeName } );
      if( array == null )
        return null;

      var sb = new StringBuilder( string.Format( "<{0}>", rootElementName ) );

      foreach( T value in array )
        sb.AppendFormat( @"<{0} {1}=""{2}""/>", elementName, attributeName, value );

      sb.Append( string.Format( "</{0}>", rootElementName ) );

      return sb.ToString();
    }

    public static string ArrayToXml<T>( T[] array, string rootElementName, string elementName, string[] attributeNames )
    {
      if( array == null )
        return null;

      var sb = new StringBuilder( string.Format( "<{0}>", rootElementName ) );

      foreach( T value in array )
      {
        sb.AppendFormat( @"<{0} ", elementName );

        attributeNames.ForEach( a => { sb.AppendFormat( @" {0}=""{1}""", a, value.GetGetter<string>( a )() ); } );
        
        sb.Append("/>");
      }

      sb.AppendFormat("</{0}>", rootElementName );

      return sb.ToString();
    }
    #endregion

    #region GetInnerXml
    /// <summary>
    /// Возвращает вложенный Xml.
    /// </summary>
    /// <param name="element">Элемент, для которого необходимо вернуть вложенный Xml.</param>
    /// <returns>Строка, содержащая вложенный Xml.</returns>
    public static string GetInnerXml( this XElement element )
    {
      using( var reader = element.CreateReader() )
      {
        reader.MoveToContent();
        
        return reader.ReadInnerXml();
      }
    }
    #endregion

    #region XmlToArray
    /// <summary>
    /// Создает массив типа Т на основе XML представления.
    /// </summary>
    /// <typeparam name="T">Тип элемента массива.</typeparam>
    /// <param name="xml">XML представление вида: &lt;rootElementName&gt;&lt;elementName attributeName="T.ToString()"&gt;&lt;/rootElementName&gt;</param>
    /// <param name="rootElementName">Название корневого элемента XML.</param>
    /// <param name="elementName">Название элемента XML.</param>
    /// <param name="attributeName">Название атрибута XML.</param>
    /// <param name="convert">Делегат для преобразования значения атрибута в тип T.</param>
    /// <returns>Возвращает массив типа T.</returns>
    public static T[] XmlToArray<T>( string xml, string rootElementName, string elementName, string attributeName, Func<string, T> convert )    
    {
      return XmlToArray( xml, rootElementName, elementName, new string[] { attributeName }, v => convert( v[0] ) );
    }

    /// <summary>
    /// Создает массив типа Т на основе XML представления.
    /// </summary>
    /// <typeparam name="T">Тип элемента массива.</typeparam>
    /// <param name="xml">XML представление вида: &lt;rootElementName&gt;&lt;elementName attributeName="T.ToString()"&gt;&lt;/rootElementName&gt;</param>
    /// <param name="rootElementName">Название корневого элемента XML.</param>
    /// <param name="elementName">Название элемента XML.</param>
    /// <param name="attributeNames">Название атрибутов XML.</param>
    /// <param name="convert">Делегат для преобразования значения атрибутов в тип T.</param>
    /// <returns>Возвращает массив типа T.</returns>
    public static T[] XmlToArray<T>( string xml, string rootElementName, string elementName, string[] attributeNames, Func<string[], T> convert )
    {
      if( xml == null )
        return null;

      var list = new List<T>();

      using( var sr = new StringReader( xml ) )
      {
        var nodeIterator = new XPathDocument( sr ).CreateNavigator().Select( string.Format( "/{0}/{1}", rootElementName, elementName ) );

        while( nodeIterator.MoveNext() )
        {
          var attributeValues = new string[attributeNames.Length];

          for ( int i = 0; i < attributeNames.Length; i++ )
            attributeValues[i] = nodeIterator.Current.GetAttribute( attributeNames[i], "" );
          
          list.Add( convert( attributeValues ) );
        }
      }

      return list.ToArray();
    }
    #endregion
  }
}
