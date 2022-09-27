using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;

namespace ST.Utils
{
  public static class SerializeHelper
  {
    private static ConcurrentDictionary<Type, MemberInfo[]> _dictSerializableMembers = new ConcurrentDictionary<Type, MemberInfo[]>();

    /// <summary>
    /// сериализует объект в xml-строку в зависимости от наличия атрибутов DataContract или Serializable
    /// </summary>
    /// <param name="Obj"></param>
    /// <param name="knownOrExtraTypes">An System.Collections.Generic.IEnumerable<T> of System.Type that contains the types that may be present in the object graph.</param>
    /// <returns></returns>
    public static string ToXmlString(this object Obj, Type[] knownOrExtraTypes = null)
    {
      var _type = Obj.GetType();
      DataContractSerializer dcser = null;
      knownOrExtraTypes = knownOrExtraTypes.DefaultIfNull().ToArray();
      bool isDC = Obj.IsDefined<DataContractAttribute>();
      Exception exDCSer = null;
      if (isDC)
        try
        {
          dcser = new DataContractSerializer(_type, knownOrExtraTypes);
        }
        catch (Exception ex)
        {
          exDCSer = ex;
        }
      XmlSerializer ser = null;
      if (dcser == null)
        try
        {
          ser = new XmlSerializer(_type, knownOrExtraTypes);
        }
        catch
        {
          if (isDC && exDCSer != null)
            throw exDCSer;
          throw;
        }
      using (var sw = new StringWriter())
      using (var xw = XmlWriter.Create(sw))
      {
        if (dcser != null)
          dcser.WriteObject(xw, Obj);
        else
        if (ser != null)
          ser.Serialize(xw, Obj);
        xw.Flush();
        return sw.ToString();
      }
    }

    public static string ToXmlStringWithS(this object Obj, Type[] extraTypes = null)
    {
      var _type = Obj.GetType();
      var ser = new XmlSerializer(_type, extraTypes.DefaultIfNull().ToArray());
      using (var sw = new StringWriter())
      using (var xw = XmlWriter.Create(sw))
      {
        ser.Serialize(xw, Obj);
        xw.Flush();
        return sw.ToString();
      }
    }

    /// <summary>
    /// сериализует объект в xml-строку с помощью DataContractSerializer
    /// </summary>
    /// <returns></returns>
    public static string ToXmlStringWithDCS(this object Obj)
    {
      return ToXmlStringWithDCS(Obj, Obj.GetType());
    }

    public static string ToXmlStringWithDCS<T>(this object Obj)
    {
      return ToXmlStringWithDCS(Obj, typeof(T));
    }

    public static string ToXmlStringWithDCS(this object Obj, Type baseType)
    {
      var dcser = new DataContractSerializer(baseType);
      using (var sw = new StringWriter())
      using (var xw = XmlWriter.Create(sw))
      {
        dcser.WriteObject(xw, Obj);
        xw.Flush();
        return sw.ToString();
      }
    }

    public static string ToXmlStringWithDCS(this object Obj, Type[] knownTypes)
    {
      var dcser = new DataContractSerializer(Obj.GetType(), knownTypes);
      using (var sw = new StringWriter())
      using (var xw = XmlWriter.Create(sw))
      {
        dcser.WriteObject(xw, Obj);
        xw.Flush();
        return sw.ToString();
      }
    }

    /// <summary>
    /// десериализует объект из xml-строки пробуя сначала DataContractSerializer, затем XmlSerializer
    /// </summary>
    /// <param name="ObjXmlString"></param>
    /// <param name="type"></param>
    /// <param name="knownOrExtraTypes">An System.Collections.Generic.IEnumerable<T> of System.Type that contains the types that may be present in the object graph.</param>
    /// <returns></returns>
    public static object DeserializeObject( this string ObjXmlString, Type type, Type[] knownOrExtraTypes = null )
    {
      DataContractSerializer dcser = null;
      Exception exDCSer = null;
      knownOrExtraTypes = knownOrExtraTypes.DefaultIfNull().ToArray();
      try
      {
        dcser = new DataContractSerializer(type, knownOrExtraTypes);
      }
      catch (Exception ex)
      {
        exDCSer = ex;
      }
      XmlSerializer ser = null;
      if (dcser == null)
        try
        {
          ser = new XmlSerializer(type, knownOrExtraTypes);
        }
        catch 
        {
          if (exDCSer != null)
            throw exDCSer;
          throw;
        }
      using (var sr = new StringReader(ObjXmlString))
      using (var xr = XmlReader.Create(sr))
      {
        if (dcser != null)
          return dcser.ReadObject(xr);
        else
        if (ser != null)
          return ser.Deserialize(xr);
      }

      return null;
    }

    public static object DeserializeObjectWithDCS( this string ObjXmlString, Type type )
    {
      var dcser = new DataContractSerializer(type);
      using (var sr = new StringReader(ObjXmlString))
      using (var xr = XmlReader.Create(sr))
        return dcser.ReadObject(xr);
    }

    public static object DeserializeObjectWithDCS( this string ObjXmlString, Type type, Type[] knownTypes )
    {
      var dcser = new DataContractSerializer(type, knownTypes);
      using (var sr = new StringReader(ObjXmlString))
      using (var xr = XmlReader.Create(sr))
        return dcser.ReadObject(xr);
    }

    public static object DeserializeObjectWithS( this string ObjXmlString, Type type, Type[] extraTypes = null )
    {
      var ser = new XmlSerializer(type, extraTypes.DefaultIfNull().ToArray());
      using (var sr = new StringReader(ObjXmlString))
      using (var xr = XmlReader.Create(sr))
        return ser.Deserialize(xr);
    }

    /// <summary>
    /// десериализует объект из xml-строки пробуя сначала DataContractSerializer, затем XmlSerializer
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="ObjXmlString"></param>
    /// <param name="knownOrExtraTypes">An System.Collections.Generic.IEnumerable<T> of System.Type that contains the types that may be present in the object graph.</param>
    /// <returns></returns>
    public static T DeserializeObject<T>( this string ObjXmlString, Type[] knownOrExtraTypes = null )
    {
      return (T)DeserializeObject( ObjXmlString, typeof( T ), knownOrExtraTypes.DefaultIfNull().ToArray() );
    }

    public static T DeserializeObjectWithS<T>( this string ObjXmlString, Type[] extraTypes = null )
    {
      return (T)DeserializeObjectWithS(ObjXmlString, typeof(T), extraTypes.DefaultIfNull().ToArray());
    }

    public static T DeserializeObjectWithDCS<T>(this string ObjXmlString)
    {
      return (T)DeserializeObjectWithDCS(ObjXmlString, typeof(T));
    }

    public static T DeserializeObjectWithDCS<T>( this string ObjXmlString, Type[] knownTypes )
    {
      return (T)DeserializeObjectWithDCS( ObjXmlString, typeof( T ), knownTypes );
    }

    public static string ToXmlStringWithP( this IEnumerable<object> objList, bool ident = true, string identChars = "  ", bool nullTags = true )
    {
      Func<object, MemberInfo, object> getMemberValue = ( obj, mi ) =>
      {
        var fi = mi as FieldInfo;
        if (fi != null)
          return fi.GetValue(obj);
        var pi = mi as PropertyInfo;
        if (pi != null)
          return pi.GetValue(obj, null);
        return null;
      };

      var values = new HashSet<object>();

      void writeItem(object val, string elementName , XmlWriter writer)
      {
        if (val == null)
        {
          if (nullTags)
          {
            writer.WriteStartElement(elementName);
            writer.WriteEndElement();
          }
        }
        else
        {
          var type = val.GetType();

          if (type.IsSimple())
          {
            writer.WriteStartElement(elementName);
            writer.WriteValue(Exec.Try(() => string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}", val), ex => val.ToString()));
            writer.WriteEndElement();
          }
          else
          {
            if (values.Contains(val))
              return;

            values.Add(val);

            object[] array = null;

            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
              array = (val as System.Collections.IEnumerable)?.Cast<object>().ToArray();

            if (array != null)
            {
              writer.WriteStartElement(elementName);

              foreach (var item in array)
              {
                writeItem(item, "item", writer);
              }

              writer.WriteEndElement();
            }
            else
            {
              writer.WriteStartElement(elementName);

              var flags = BindingFlags.Public | BindingFlags.Instance;
              if (type.IsSerializable)
                flags |= BindingFlags.NonPublic;
              var members = _dictSerializableMembers.GetOrAdd(type, _ => type.GetMembers(flags)
               .Where(m => m.IfIs<PropertyInfo, bool>(p =>
               {
                 if (!(p.CanRead && p.GetIndexParameters().Length == 0))
                   return false;
                 if (p.GetAttributes<XmlIgnoreAttribute>(true).Any())
                   return false;
                 return true;
               }, () => m.IfIs<FieldInfo, bool>(f =>
               {
                 if (f.Name.IndexOf(">k__BackingField", StringComparison.InvariantCultureIgnoreCase) >= 0)
                   return false;
                 if (f.Name.IndexOf("<") >= 0 && f.Name.IndexOf(">") >= 1)
                   return false;
                 if (f.GetAttributes<XmlIgnoreAttribute>(true).Any())
                   return false;
                 return true;
               }, () => false))).ToArray());

              foreach (var m in members)
              {
                writeItem(getMemberValue(val, m), m.Name, writer);
              }

              writer.WriteEndElement();
            }
          }
        }
      }

      var sw = new StringWriter();
      var sets = new XmlWriterSettings()
      {
        Indent = ident,
        IndentChars = identChars,
      };
      using (var writer = XmlWriter.Create(sw, sets))
      {
        writer.WriteStartDocument();
        writer.WriteStartElement("root");

        foreach (var obj in objList)
          writeItem(obj, "item", writer);

        writer.WriteEndDocument();
      }

      var str = sw.ToString();
      sw.Close();

      return str;
    }
  }
}
