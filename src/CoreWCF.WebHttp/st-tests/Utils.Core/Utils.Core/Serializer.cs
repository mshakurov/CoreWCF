using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;

using BinaryFormatter;



using ST.Utils.Attributes;
using ST.Utils.Collections;

namespace ST.Utils
{
  /// <summary>
  /// Вспомогательный класс для сериализации и десериализации объектов.
  /// </summary>
  public static class Serializer
  {
    #region .Static Fields
    [ThreadStatic]
    private static BinaryConverter _formatter;
    //private static System.Runtime.Serialization.Formatters.Binary.BinaryFormatter _formatter;

    private static readonly FastCache<Type, PropertyInfoEx[]> _properties = new FastCache<Type, PropertyInfoEx[]>(false);

    private static readonly Dictionary<Type, TypeConverter> _types = new Dictionary<Type, TypeConverter>();

    private static readonly XmlWriterSettings SerializeXml2_Settings = new XmlWriterSettings() { CheckCharacters = true, ConformanceLevel = ConformanceLevel.Fragment, Indent = false, OmitXmlDeclaration = true };
    #endregion

    #region .Static Properties
    private static BinaryConverter Formatter
    {
      get { return _formatter ?? (_formatter = new BinaryConverter()); }
    }
    //private static System.Runtime.Serialization.Formatters.Binary.BinaryFormatter Formatter
    //{
    //  get { return _formatter ?? (_formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter()); }
    //}
    #endregion

    #region .Ctor
    static Serializer()
    {
      foreach (var t in new[] { typeof( string ), typeof( int ), typeof( int? ), typeof( uint ), typeof( uint? ), typeof( long ), typeof( long? ), typeof( ulong ), typeof( ulong? ),
                                typeof( short ), typeof( short? ), typeof( ushort ), typeof( ushort? ), typeof( double ), typeof( double? ), typeof( bool ), typeof( bool? ),
                                typeof( DateTime ), typeof( DateTime? ), typeof( Guid ), typeof( Guid? ), typeof( decimal ), typeof( decimal? ), typeof( float ), typeof( float? ) })
        _types.Add(t, TypeDescriptor.GetConverter(t));
    }
    #endregion

    #region DeepClone
    /// <summary>
    /// Выполняет глубокое клонирование объекта (копирование всего графа объекта).
    /// </summary>
    /// <typeparam name="T">Тип объекта.</typeparam>
    /// <param name="obj">Объект.</param>
    /// <returns>Копия объекта.</returns>
    [DebuggerStepThrough]
    public static T DeepClone<T>( [NotNull] this T obj )
      where T : class
    {
      return Formatter.Deserialize<T>(Formatter.Serialize(obj));
    }
    //public static T DeepClone<T>( [NotNull] this T obj )
    //  where T : class
    //{
    //  using (var ms = new MemoryStream())
    //  {
    //    Formatter.Serialize(ms, obj);

    //    ms.Position = 0;

    //    return Formatter.Deserialize(ms) as T;
    //  }
    //}
    #endregion

    #region Deserialize
    ///// <summary>
    ///// Десериализует объект.
    ///// </summary>
    ///// <param name="array">Массив байт сериализованного объекта.</param>
    ///// <returns>Объект.</returns>
    //[DebuggerStepThrough]
    //public static object Deserialize( [NotNull] this byte[] array )
    //{
    //  using ( var ms = new MemoryStream( array ) )
    //    return ms.Deserialize();
    //}

    ///// <summary>
    ///// Десериализует объект.
    ///// </summary>
    ///// <param name="stream">Поток с сериализованным объектом.</param>
    ///// <returns>Объект.</returns>
    //[DebuggerStepThrough]
    //public static object Deserialize( [NotNull] this Stream stream )
    //{
    //  var buffer = new byte[stream.Length];
    //  stream.Read(buffer);
    //  return Formatter.Deserialize(buffer);
    //}

    /// <summary>
    /// Десериализует объект.
    /// </summary>
    /// <typeparam name="T">Тип объекта.</typeparam>
    /// <param name="array">Массив байт сериализованного объекта.</param>
    /// <returns>Объект.</returns>
    [DebuggerStepThrough]
    public static T Deserialize<T>([NotNull] this byte[] array)
      where T : class
    {
      return Formatter.Deserialize<T>(array);
      //using (var ms = new MemoryStream(array))
      //  return ms.Deserialize<T>();
    }

    /// <summary>
    /// Десериализует объект.
    /// </summary>
    /// <typeparam name="T">Тип объекта.</typeparam>
    /// <param name="stream">Поток с сериализованным объектом.</param>
    /// <returns>Объект.</returns>
    [DebuggerStepThrough]
    public static T Deserialize<T>([NotNull] this Stream stream)
      where T : class
    {
      var buffer = new byte[stream.Length];
      stream.Read(buffer);
      return Formatter.Deserialize<T>(buffer);
      //return Formatter.Deserialize(stream) as T;
    }
    #endregion

    #region DeserializeXml
    /// <summary>
    /// Десериализует объект из упрощенного XML-представления (см. SerializeXml).
    /// </summary>
    /// <typeparam name="T">Тип объекта.</typeparam>
    /// <param name="xml">Упрощенное XML-представление объекта.</param>
    /// <param name="rootElementName">Название корневого элемента.</param>
    /// <returns>Объект.</returns>
    [DebuggerStepThrough]
    public static T DeserializeXml<T>(string xml, string rootElementName = "Object")
      where T : class, new()
    {
      return DeserializeXml(typeof(T), xml, rootElementName) as T;
    }

    /// <summary>
    /// Десериализует объект из упрощенного XML-представления (см. SerializeXml).
    /// </summary>
    /// <param name="type">Тип объекта.</param>
    /// <param name="xml">Упрощенное XML-представление объекта.</param>
    /// <param name="rootElementName">Название корневого элемента.</param>
    /// <returns>Объект.</returns>
    [DebuggerStepThrough]
    public static object DeserializeXml([NotNull] Type type, [NotNull] string xml, [NotNullNotEmpty] string rootElementName = "Object")
    {
      return DeserializeXml2(type, xml, rootElementName);

      //using ( var reader = XmlReader.Create( new StringReader( xml ) ) )
      //{
      //  reader.Read();

      //  if ( reader.Name != rootElementName || !reader.HasAttributes )
      //    return null;

      //  var properties = GetProperties( type );

      //  var obj = type.CreateFast();

      //  var index = 0;

      //  while ( reader.MoveToNextAttribute() )
      //    if ( !string.IsNullOrEmpty( reader.Value ) )
      //      for ( var i = 0; i < properties.Length; i++ )
      //      {
      //        var p = properties[index++ % properties.Length];

      //        if ( string.Compare( p.Name, reader.Name, StringComparison.Ordinal ) == 0 && p.Setter != null )
      //        {
      //          try
      //          {
      //            var value = p.Converter.ConvertFromInvariantString( reader.Value );

      //            if ( value != null )
      //              p.Setter( obj, value );
      //          }
      //          catch
      //          {
      //          }

      //          break;
      //        }
      //      }

      //  return obj;
      //}
    }

    /// <summary>
    /// Десериализует объект из упрощенного XML-представления (см. SerializeXml).
    /// </summary>
    /// <param name="type">Тип объекта.</param>
    /// <param name="xml">Упрощенное XML-представление объекта.</param>
    /// <param name="rootElementName">Название корневого элемента.</param>
    /// <returns>Объект.</returns>
    [DebuggerStepThrough]
    public static object DeserializeXml2([NotNull] Type type, [NotNull] string xml, [NotNullNotEmpty] string rootElementName = "Object")
    {
      using (var reader = XmlReader.Create(new StringReader(xml)))
      {
        reader.Read();

        if (reader.Name != rootElementName /* || !reader.HasAttributes */ )
          return null;

        var properties = GetProperties(type);

        var obj = type.CreateFast();

        var index = 0;

        // чтение из атрибутов
        while (reader.MoveToNextAttribute())
          if (!string.IsNullOrEmpty(reader.Value))
            for (var i = 0; i < properties.Length; i++)
            {
              var p = properties[index++ % properties.Length];

              if (string.Compare(p.Name, reader.Name, StringComparison.Ordinal) == 0 && p.Setter != null)
              {
                try
                {
                  var value = p.Converter.ConvertFromInvariantString(reader.Value);

                  if (value != null)
                    p.Setter(obj, value);
                }
                catch
                {
                }

                break;
              }
            }

        index = 0;

        // чтение из элементов
        reader.Read();

        while (!reader.EOF)
        {
          if (reader.NodeType == XmlNodeType.Element)
          {
            PropertyInfoEx prop = null;

            for (var i = 0; i < properties.Length; i++)
            {
              var p = properties[index++ % properties.Length];

              if (string.Compare(p.Name, reader.Name, StringComparison.Ordinal) == 0 && p.Setter != null)
              {
                prop = p;

                break;
              }
            }

            if (prop != null)
              try
              {
                var content = reader.ReadElementContentAsString();

                var value = prop.Converter.ConvertFromInvariantString(WebUtility.HtmlDecode(content));

                if (value != null)
                  prop.Setter(obj, value);
              }
              catch
              {
              }
            else
              reader.Skip();
          }
          else
            reader.Skip();
        }

        return obj;
      }
    }
    #endregion

    #region GetProperties
    private static PropertyInfoEx[] GetProperties(Type type)
    {
      return _properties.Get(type, t =>
     {
       var list = new List<PropertyInfo>();

       foreach (var p in t.GetProperties())
       {
         if (!_types.ContainsKey(p.PropertyType))
           if (p.PropertyType.IsEnum)
             _types.Add(p.PropertyType, TypeDescriptor.GetConverter(p.PropertyType));
           else
             continue;

         list.Add(p);
       }

       return list.Select(p => new PropertyInfoEx(p)).ToArray();
     });
    }
    #endregion

    #region Serialize
    /// <summary>
    /// Сериализует объект.
    /// </summary>
    /// <typeparam name="T">Тип объекта.</typeparam>
    /// <param name="obj">Объект.</param>
    /// <returns>Массив байт сериализованного объекта.</returns>
    [DebuggerStepThrough]
    public static byte[] Serialize<T>([NotNull] this T obj)
      where T : class
    {
      return Formatter.Serialize(obj);
      //using (var ms = new MemoryStream())
      //{
      //  Formatter.Serialize(ms, obj);

      //  return ms.GetBuffer();
      //}
    }
    #endregion

    #region SerializeXml
    /// <summary>
    /// Сериализует объект в упрощенное XML-представление (запрещено использование встроенных символов xml).
    /// Формат упрощенного XML-представления: &lt;{rootElementName} {Название свойства 1}="{Значение свойства 1}" ... {Название свойства N}="{Значение свойства N}"/&gt;.
    /// Сериализуются только значения свойств значимых, строковых и Nullable типов.
    /// </summary>
    /// <param name="obj">Объект.</param>
    /// <param name="rootElementName">Название корневого элемента.</param>
    /// <returns>Упрощенное XML-представление объекта.</returns>
    [DebuggerStepThrough]
    public static string SerializeXml([NotNull] object obj, [NotNullNotEmpty] string rootElementName = "Object")
    {
      var sb = new StringBuilder("<" + rootElementName);

      foreach (var p in GetProperties(obj.GetType()))
        if (p.Getter != null)
        {
          var value = p.Getter(obj);

          if (value != null)
            sb.AppendFormat(@" {0}=""{1}""", p.Name, p.Converter.ConvertToInvariantString(value));
        }

      return sb.Append("/>").ToString();
    }
    #endregion

    #region SerializeXml2
    /// <summary>
    /// Сериализует объект в упрощенное XML-представление (допускает любые символы в строке).
    /// Формат упрощенного XML-представления: &lt;{rootElementName} {Название свойства 1}="{Значение свойства 1}" ... {Название свойства N}="{Значение свойства N}"/&gt;.
    /// Сериализуются только значения свойств значимых, строковых и Nullable типов.
    /// </summary>
    /// <param name="obj">Объект.</param>
    /// <param name="rootElementName">Название корневого элемента.</param>
    /// <returns>Упрощенное XML-представление объекта.</returns>
    [DebuggerStepThrough]
    public static string SerializeXml2([NotNull] object obj, [NotNullNotEmpty] string rootElementName = "Object")
    {
      //var sb = new StringBuilder( "<" + rootElementName + ">" );
      var sb = new StringBuilder();

      var settings = new XmlWriterSettings();
      settings.ConformanceLevel = ConformanceLevel.Fragment;
      settings.NewLineChars = "\r\n";
      settings.NewLineHandling = NewLineHandling.Entitize;
      settings.NamespaceHandling = NamespaceHandling.OmitDuplicates;

      using (var writer = XmlWriter.Create(new StringWriter(sb), settings))
      {
        writer.WriteStartElement(rootElementName);

        foreach (var p in GetProperties(obj.GetType()))
          if (p.Getter != null)
          {
            var value = p.Getter(obj);

            if (value != null)
            {
              writer.WriteStartElement(p.Name);
              writer.WriteString(WebUtility.HtmlEncode(p.Converter.ConvertToInvariantString(value)));
              writer.WriteEndElement();

              //sb.AppendFormat( @"<{0}>{1}</{0}>", p.Name, WebUtility.HtmlEncode( p.Converter.ConvertToInvariantString( value ) ) );
            }
          }

        writer.WriteEndElement();
      }

      //return sb.Append( "</" + rootElementName + ">" ).ToString();
      return sb.ToString();
    }
    #endregion

    private sealed class PropertyInfoEx
    {
      #region .Fields
      public string Name;

      public TypeConverter Converter;

      public Func<object, object> Getter;
      public Action<object, object> Setter;
      #endregion

      #region .Ctor
      public PropertyInfoEx(PropertyInfo pi)
      {
        Name = pi.Name;

        Converter = _types.GetValue(pi.PropertyType);

        var mi = pi.GetGetMethod();

        if (mi != null)
        {
          var obj = Expression.Parameter(typeof(object), "obj");

          var expr = Expression.Call(Expression.TypeAs(obj, pi.DeclaringType), mi);

          Getter = Expression.Lambda<Func<object, object>>(Expression.TypeAs(expr, typeof(object)), obj).Compile();
        }

        mi = pi.GetSetMethod(true);

        if (mi != null)
        {
          var obj = Expression.Parameter(typeof(object), "obj");

          var value = Expression.Parameter(typeof(object), "value");

          var expr = Expression.Call(Expression.TypeAs(obj, pi.DeclaringType), mi, pi.PropertyType.IsValueType ? Expression.Unbox(value, pi.PropertyType) : Expression.TypeAs(value, pi.PropertyType));

          Setter = Expression.Lambda<Action<object, object>>(expr, obj, value).Compile();
        }
      }
      #endregion
    }
  }

  [Serializable]
  public class SerializeDictionary<TKey, TValue> : ISerializable
  {
    #region .Fields
    private Dictionary<TKey, TValue> _dictionary;
    #endregion

    #region .Ctor
    public SerializeDictionary()
    {
      _dictionary = new Dictionary<TKey, TValue>();
    }
    public SerializeDictionary(SerializationInfo info, StreamingContext context)
    {
      _dictionary = new Dictionary<TKey, TValue>();
    }
    #endregion

    #region .Properties
    public TValue this[TKey key]
    {
      get { return _dictionary[key]; }
      set { _dictionary[key] = value; }
    }

    internal KeyValuePair<TKey, TValue>[] KeyValuePairs
    {
      get { return _dictionary.ToArray(); }
    }
    #endregion

    #region .Methods
    public void Add(TKey key, TValue value)
    {
      _dictionary.Add(key, value);
    }

    public void Concat(SerializeDictionary<TKey, TValue> sDic)
    {
      foreach (var kv in sDic.KeyValuePairs)
        _dictionary.Add(kv.Key, kv.Value);
    }

    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
      foreach (TKey key in _dictionary.Keys)
        info.AddValue(key.ToString(), _dictionary[key]);
    }
    #endregion
  }
}
