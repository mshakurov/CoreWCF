using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
using System.ComponentModel;

namespace ST.Utils.Config
{
  /// <summary>
  /// Базовый элемент конфигурации.
  /// Параметры конфигурации определяются свойствами классов, унаследованных от данного.
  /// </summary>
  [Serializable]
  //[KnownType(nameof(ItemConfig.GetKnownTypes))]
  public abstract class ItemConfig
  {
    [System.Xml.Serialization.XmlIgnore]
    [NonSerialized]
    private bool _isFile;

    #region .Ctor
    /// <summary>
    /// Конструктор.
    /// </summary>
    public ItemConfig()
    {
      InitializeInstance();
    }
    #endregion

    public bool IsFile() => _isFile;
    internal bool SetAsFile() => _isFile = true;

    internal bool SetAsNotFile() => _isFile = false;

    #region InitializeInstance
    /// <summary>
    /// Вызывается при создании экземпляра элемента через конструктор и во время десериализации для инициализации значений полей и свойств по умолчанию.
    /// </summary>
    protected virtual void InitializeInstance()
    {
    }
    #endregion

    #region OnDeserializing
    [OnDeserializing]
    private void OnDeserializingItemConfig(StreamingContext streamingContext)
    {
      InitializeInstance();
    }
    #endregion

    //public Type[] GetKnownTypes()
    //{
    //  var types = new HashSet<Type>();
    //  void CollectTypes(Type type)
    //  {
    //    Type baseType = type;
    //    while (baseType != null)
    //    {
    //      if (!types.Contains(baseType))
    //        types.Add(baseType);
    //      baseType = baseType.BaseType;
    //    }

    //    type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
    //      .Select(f => new { type = f.FieldType, mem = (System.Reflection.MemberInfo)f })
    //      .Concat(type.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
    //      .Select(f => new { type = f.PropertyType, mem = (System.Reflection.MemberInfo)f }))
    //      .Where(f => f.type.IsCassOrStruct() && !types.Contains(f.type) && !f.mem.IsSerialized())
    //      .ForEach(f =>
    //      {
    //        types.Add(f.type);
            
    //        CollectTypes(f.type);
    //      });
    //  }
    //  CollectTypes(this.GetType());
    //  return types.ToArray();
    //}
  }

  /// <summary>
  /// Элемент конфигурации, представляющий строку соединения.
  /// </summary>
  [Serializable]
  public class ConnectionItemConfig : ItemConfig
  {
    #region .Properties
    /// <summary>
    /// Строка соединения.
    /// </summary>
    public string Connection { get; set; }
    #endregion
  }

  /// <summary>
  /// Элемент конфигурации, представляющий любое значение.
  /// </summary>
  [Serializable]
  public class ValueItemConfig : ItemConfig
  {
    #region .Properties
    /// <summary>
    /// Значение элемента конфигурации.
    /// </summary>
    public object Value { get; set; }
    #endregion

    #region GetValue
    /// <summary>
    /// Возвращает типизированное значение элемента конфигурации.
    /// </summary>
    /// <typeparam name="T">Требуемый тип значения.</typeparam>
    /// <returns>Значение.</returns>
    public virtual T GetValue<T>()
    {
      var value = default(T);

      if (Value != null)
        Exec.Try(() => value = (T)(typeof(T).IsClass ? Value : Convert.ChangeType(Value, typeof(T), CultureInfo.InvariantCulture)));

      return value;
    }
    #endregion
  }

  public static class ItemConfigExtensions
  {
    public static T AsFile<T>(this T configItem)
      where T : ItemConfig
    {
      configItem.SetAsFile();
      return configItem;
    }
    public static T AsNotFileClone<T>(this T configItem)
      where T : ItemConfig
    {
      configItem = configItem.DeepClone();
      configItem.SetAsNotFile();
      return configItem;
    }

    public static bool IsCassOrStruct(this Type type) => type.IsClass || type.IsValueType && !type.IsPrimitive;

    public static bool IsSerialized(this System.Reflection.MemberInfo mem) => !mem.IsDefined(typeof(NonSerializedAttribute), true) && !mem.IsDefined(typeof(System.Xml.Serialization.XmlIgnoreAttribute), true);
  }
}
