using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ST.Utils.Attributes;

namespace ST.Utils.Config
{
  /// <summary>
  /// Базовый класс конфигурации.
  /// </summary>
  public abstract class BaseConfig
  {
    #region .Fields
    private readonly ConcurrentDictionary<string, ItemConfig> _items = new ConcurrentDictionary<string, ItemConfig>();
    #endregion

    #region .Properties
    /// <summary>
    /// Путь, по которому располагается конфигурация.
    /// </summary>
    protected string Path { get; private set; }
    #endregion

    #region Contains
    /// <summary>
    /// Определяет наличие элемента в наборе.
    /// </summary>
    /// <typeparam name="T">Тип проверяемого элемента.</typeparam>
    /// <param name="name">Имя элемента.</param>
    /// <returns>True элемент имеется в наборе, иначе - False.</returns>
    public bool Contains<T>( string name = null )
      where T : ItemConfig
    {
      return Find<T>( name ) != null;
    }
    #endregion

    #region Find
    private T Find<T>( string name )
      where T : ItemConfig
    {
      return _items.GetValue( name.GetEmptyOrTrimmed() ) as T;
    }
    #endregion

    #region Get
    /// <summary>
    /// Возвращает элемент.
    /// </summary>
    /// <typeparam name="T">Тип возвращаемого элемента.</typeparam>
    /// <param name="name">Название элемента.</param>
    /// <returns>Элемент типа T.</returns>
    public T Get<T>( string name = null )
      where T : ItemConfig
    {
      var item = Find<T>( name );

      return item == null ? null : item.DeepCloneByJson();
    }

    /// <summary>
    /// Возвращает элемент конфигурации.
    /// </summary>
    /// <typeparam name="TConfig">Тип конфигурации.</typeparam>
    /// <typeparam name="T">Тип элемента.</typeparam>
    /// <param name="path">Путь, по которому располагается конфигурация.</param>
    /// <param name="name">Название элемента.</param>
    /// <returns>Элемент конфигурации.</returns>
    internal static TItemConfig Get<TConfigType, TItemConfig>( string path = null, string name = null )
      where TConfigType : BaseConfig, new()
      where TItemConfig : ItemConfig
    {
      return CreateConfig<TConfigType>( path ).GetFromSource<TItemConfig>( name );
    }
    #endregion

    #region CreateConfig
    /// <summary>
    /// Возвращает конфигурацию требуемого типа.
    /// </summary>
    /// <typeparam name="TConfigType">Тип конфигурации.</typeparam>
    /// <param name="path">Путь, по которому располагается конфигурация.</param>
    /// <returns>Конфигурация.</returns>
    public static TConfigType CreateConfig<TConfigType>( string path = null )
      where TConfigType : BaseConfig, new()
    {
      TConfigType factory = new TConfigType { Path = path };

      factory.Initialize();

      return factory;
    }
    #endregion

    #region GetFromSource
    /// <summary>
    /// Возвращает элемент, минуя кэш конфигурации.
    /// Элемент возвращается непосредственно из источника данных конфигурации.
    /// </summary>
    /// <typeparam name="T">Тип возвращаемого элемента.</typeparam>
    /// <param name="name">Название элемента.</param>
    /// <returns>Элемент типа T.</returns>
    internal virtual TItemConfig GetFromSource<TItemConfig>( string name = null )
      where TItemConfig : ItemConfig
    {
      throw new NotImplementedException();
    }
    #endregion

    #region GetList
    /// <summary>
    /// Возвращает список элементов указанного типа.
    /// </summary>
    /// <typeparam name="T">Тип элемента.</typeparam>
    /// <returns>Список элементов.</returns>
    public Dictionary<string, T> GetList<T>()
      where T : ItemConfig
    {
      return _items.Where( i => i.Value is T ).ToDictionary( i => i.Key, i => i.Value as T );
    }
    #endregion

    #region Initialize
    /// <summary>
    /// Вызывается при инициализации конфигурации.
    /// </summary>
    protected virtual void Initialize()
    {
    }
    #endregion

    #region IsValid
    /// <summary>
    /// Проверяет, поддерживает ли конфигурация указанный тип элемента.
    /// </summary>
    /// <param name="itemType">Тип элемента.</param>
    /// <returns>True если тип элемента поддерживается конфигурацией, иначе - False.</returns>
    public virtual bool IsValid( Type itemType )
    {
      return typeof( ItemConfig ).IsAssignableFrom( itemType );
    }
    #endregion

    #region Load
    /// <summary>
    /// Загружает конфигурацию.
    /// Должен быть переопределен в конкретной реализации конфигурации.
    /// Базовое поведение: очистка спика элементов.
    /// </summary>
    public virtual void Load()
    {
      _items.Clear();
    }
    #endregion

    #region Remove
    /// <summary>
    /// Удаляет элемент.
    /// </summary>
    /// <typeparam name="T">Тип удаляемого элемента.</typeparam>
    /// <param name="name">Название элемента, который необходимо удалить.</param>
    /// <returns>True - элемент удален, иначе - False.</returns>
    public bool Remove<T>( string name = null )
      where T : ItemConfig
    {
      name = name.GetEmptyOrTrimmed();

      return Contains<T>( name ) ? _items.RemoveValue( name ) : false;
    }
    #endregion

    #region Save
    /// <summary>
    /// Сохраняет конфигурацию.
    /// Должен быть переопределен в конкретной реализации конфигурации.
    /// </summary>
    public abstract void Save();
    #endregion

    #region Set
    /// <summary>
    /// Добавляет/обновляет элемент.
    /// </summary>
    /// <param name="item">Элемент.</param>
    /// <param name="name">Название элемента.</param>
    public void Set( [NotNull] ItemConfig item, string name = null )
    {
      if( !IsValid( item.GetType() ) )
        throw new ArgumentException( "Element's type is wrong." );

      //var clonedItem = item.DeepClone();
      //var clonedItem = item.ToXmlStringWithS().DeserializeObjectWithS<ItemConfig>();
      var clonedItem = item.DeepCloneByJson();

      _items.AddOrUpdate( name.GetEmptyOrTrimmed(), clonedItem, (n, i) => clonedItem );
    }

    /// <summary>
    /// Добавляет/обновляет элемент конфигурации.
    /// </summary>
    /// <typeparam name="TConfig">Тип конфигурации.</typeparam>
    /// <param name="item">Элемент.</param>
    /// <param name="path">Путь, по которому располагается конфигурация.</param>
    /// <param name="name">Название элемента.</param>
    internal static void Set<TConfig>( ItemConfig item, string path = null, string name = null )
      where TConfig : BaseConfig, new()
    {
      CreateConfig<TConfig>( path ).SetToSource( item, name );
    }
    #endregion

    #region SetToSource
    /// <summary>
    /// Добавляет/обновляет элемент, минуя кэш конфигурации.
    /// Элемент добавляется/обновляется непосредственно в источник(е) данных конфигурации.
    /// </summary>
    /// <param name="item">Элемент.</param>
    /// <param name="name">Название элемента.</param>
    internal virtual void SetToSource( [NotNull] ItemConfig item, string name = null )
    {
      throw new NotImplementedException();
    }
    #endregion
  }
}
