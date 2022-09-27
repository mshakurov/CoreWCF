using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using ST.Utils.Attributes;
using System.Reflection;

namespace ST.Utils
{
  /// <summary>
  /// Вспомогательный класс для работы с коллекциями.
  /// </summary>
  public static class CollectionHelper
  {
    #region .Static Fields
    [ThreadStatic]
    private static Dictionary<Type, DataTable> _identifierTables;

    private static ConcurrentDictionary<Type, DataTable> _dataTables = new ConcurrentDictionary<Type, DataTable>();
    private static ConcurrentDictionary<Type, MemberInfo[]> _dataTablesMembers = new ConcurrentDictionary<Type, MemberInfo[]>();
    #endregion

    #region AddOrUpdate
    /// <summary>
    /// Добавляет пару ключ/значение в словарь, если указанный ключ еще не существует, или обновляет пару ключ/значение, если указанный ключ уже существует.
    /// Метод не является потокобезопасным.
    /// </summary>
    /// <typeparam name="TKey">Тип ключа.</typeparam>
    /// <typeparam name="TValue">Тип значения.</typeparam>
    /// <param name="dict">Словарь.</param>
    /// <param name="key">Ключ.</param>
    /// <param name="addUpdateValue">Значение для добавления/обновления.</param>
    /// <returns>Добавленное или обновленное значение.</returns>
    public static TValue AddOrUpdate<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue addUpdateValue)
    { // Ради производительности параметры не валидируются.
      return dict.AddOrUpdate(key, addUpdateValue, k => addUpdateValue);
    }

    /// <summary>
    /// Добавляет пару ключ/значение в словарь, если указанный ключ еще не существует, или обновляет пару ключ/значение, если указанный ключ уже существует.
    /// Метод не является потокобезопасным.
    /// </summary>
    /// <typeparam name="TKey">Тип ключа.</typeparam>
    /// <typeparam name="TValue">Тип значения.</typeparam>
    /// <param name="dict">Словарь.</param>
    /// <param name="key">Ключ.</param>
    /// <param name="addValue">Значение для добавления.</param>
    /// <param name="updateValue">Значение для обновления.</param>
    /// <returns>Добавленное или обновленное значение.</returns>
    public static TValue AddOrUpdate<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue addValue, TValue updateValue)
    { // Ради производительности параметры не валидируются.
      return dict.AddOrUpdate(key, addValue, k => updateValue);
    }

    /// <summary>
    /// Добавляет пару ключ/значение в словарь, если указанный ключ еще не существует, или обновляет пару ключ/значение, если указанный ключ уже существует.
    /// Метод не является потокобезопасным.
    /// </summary>
    /// <typeparam name="TKey">Тип ключа.</typeparam>
    /// <typeparam name="TValue">Тип значения.</typeparam>
    /// <param name="dict">Словарь.</param>
    /// <param name="key">Ключ.</param>
    /// <param name="addValue">Значение для добавления.</param>
    /// <param name="updateValueFactory">Функция для получения значения по ключу для обновления.</param>
    /// <returns>Добавленное или обновленное значение.</returns>
    public static TValue AddOrUpdate<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue addValue, Func<TKey, TValue> updateValueFactory)
      where TKey : notnull
    { // Ради производительности параметры не валидируются.
      if (key != null)
        if (dict.ContainsKey(key))
          dict[key] = addValue = updateValueFactory(key);
        else
          dict.Add(key, addValue);

      return addValue;
    }

    /// <summary>
    /// Добавляет пару ключ/значение в словарь, если указанный ключ еще не существует, или обновляет пару ключ/значение, если указанный ключ уже существует, с предоставлением предыдущего значения.
    /// Метод не является потокобезопасным.
    /// </summary>
    /// <typeparam name="TKey">Тип ключа.</typeparam>
    /// <typeparam name="TValue">Тип значения.</typeparam>
    /// <param name="dict">Словарь.</param>
    /// <param name="key">Ключ.</param>
    /// <param name="addValue">Значение для добавления.</param>
    /// <param name="updateValueFactory">Функция для получения значения по ключу для обновления (2-й параметр - предыдущее значение).</param>
    /// <returns>Добавленное или обновленное значение.</returns>
    public static TValue AddOrUpdate<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
      where TKey : notnull
    { // Ради производительности параметры не валидируются.
      if (key != null)
        if (dict.ContainsKey(key))
          dict[key] = addValue = updateValueFactory(key, dict[key]);
        else
          dict.Add(key, addValue);

      return addValue;
    }
    #endregion

    #region AddRangeSafe
    /// <summary>
    /// Потокобезопасно добавляет несколько элементов в набор.
    /// </summary>
    /// <typeparam name="T">Тип элементов набора.</typeparam>
    /// <param name="hashSet">Набор.</param>
    /// <param name="items">Список элементов.</param>
    [DebuggerStepThrough]
    public static void AddRangeSafe<T>([NotNull] this HashSet<T> hashSet, IEnumerable<T> items)
    {
      if (items != null)
        lock (hashSet)
          foreach (var item in items)
            hashSet.Add(item);
    }
    #endregion

    #region AddSafe
    /// <summary>
    /// Потокобезопасно добавляет указанный элемент в набор.
    /// </summary>
    /// <typeparam name="T">Тип элементов набора.</typeparam>
    /// <param name="hashSet">Набор.</param>
    /// <param name="item">Элемент.</param>
    /// <returns>True - элемент был добавлен, иначе - False.</returns>
    [DebuggerStepThrough]
    public static bool AddSafe<T>([NotNull] this HashSet<T> hashSet, T item)
    {
      lock (hashSet)
        return hashSet.Add(item);
    }
    #endregion

    #region Clear
    /// <summary>
    /// Очищает очередь.
    /// </summary>
    /// <typeparam name="T">Тип значения.</typeparam>
    /// <param name="queue">Очередь.</param>
    public static void Clear<T>( [NotNull] this ConcurrentQueue<T> queue )
    {
      T ignored;

      while ( queue.TryDequeue( out ignored ) )
        ;
    }
    #endregion

    #region ClearSafe
    /// <summary>
    /// Потокобезопасно удаляет из набора все элементы.
    /// </summary>
    /// <typeparam name="T">Тип элементов набора.</typeparam>
    /// <param name="hashSet">Набор.</param>
    [DebuggerStepThrough]
    public static void ClearSafe<T>([NotNull] this HashSet<T> hashSet)
    {
      lock (hashSet)
        hashSet.Clear();
    }
    #endregion

    #region ContainsSafe
    /// <summary>
    /// Потокобезопасно определяет, содержит ли набор указанный элемент.
    /// </summary>
    /// <typeparam name="T">Тип элементов набора.</typeparam>
    /// <param name="hashSet">Набор.</param>
    /// <param name="item">Элемент.</param>
    /// <returns>True - набор содержит элемент, иначе - False.</returns>
    [DebuggerStepThrough]
    public static bool ContainsSafe<T>([NotNull] this HashSet<T> hashSet, T item)
    {
      lock (hashSet)
        return hashSet.Contains(item);
    }
    #endregion

    #region Dequeue
    /// <summary>
    /// Возвращает значение из начала очереди.
    /// </summary>
    /// <typeparam name="T">Тип значения.</typeparam>
    /// <param name="queue">Очередь.</param>
    /// <returns>Значение.</returns>
    [DebuggerStepThrough]
    public static T Dequeue<T>( [NotNull] this ConcurrentQueue<T> queue )
    {
      T value;

      if (queue.TryDequeue(out value))
        return value;

      return default(T);
    }
    #endregion

    #region EnumToList
    /// <summary>
    /// Возвращает список объектов на основе значений перечисления.
    /// </summary>
    /// <typeparam name="TEnum">Тип перечисления.</typeparam>
    /// <typeparam name="TObject">Тип объекта списка.</typeparam>
    /// <param name="creator">Метод, возвращающий объект на основе значения перечисления и его отображаемого имени.</param>
    /// <returns>Список объектов.</returns>
    [DebuggerStepThrough]
    public static List<TObject> EnumToList<TEnum, TObject>([NotNull] Func<TEnum, string, TObject> creator)
    {
      return Enum.GetValues(typeof(TEnum)).Cast<TEnum>().Select(item => creator(item, item.GetDisplayName(false))).ToList();
    }
    #endregion

    #region ForEach
    /// <summary>
    /// Выполняет указанное действие для каждого элемента списка.
    /// </summary>
    /// <typeparam name="T">Тип элемента списка.</typeparam>
    /// <param name="list">Список элементов.</param>
    /// <param name="action">Действие.</param>
    /// <returns>Список элементов.</returns>
    [DebuggerStepThrough]
    public static IEnumerable<T> ForEach<T>([NotNull] this IEnumerable<T> list, Action<T> action)
    {
      foreach (var item in list)
        action(item);

      return list;
    }

    /// <summary>
    /// Выполняет указанное действие для каждого элемента списка.
    /// </summary>
    /// <typeparam name="T">Тип элемента списка.</typeparam>
    /// <param name="list">Список элементов.</param>
    /// <param name="predicate">Условие, при котором действие будет выполнено.</param>
    /// <param name="action">Действие.</param>
    /// <returns>Список элементов.</returns>
    [DebuggerStepThrough]
    public static IEnumerable<T> ForEach<T>([NotNull] this IEnumerable<T> list, Func<T, bool> predicate, Action<T> action)
    {
      foreach (var item in list)
        if (predicate(item))
          action(item);

      return list;
    }
    #endregion

    #region ForEachTry
    /// <summary>
    /// Выполняет код внутри конструкции try-catch для каждого элемента списка.
    /// Блок catch ничего не делает.
    /// </summary>
    /// <typeparam name="T">Тип элемента списка.</typeparam>
    /// <param name="list">Список элементов.</param>
    /// <param name="tryBlock">Код, выполняемый в блоке try.</param>
    /// <returns>Список элементов.</returns>
    [DebuggerStepThrough]
    public static IEnumerable<T> ForEachTry<T>([NotNull] this IEnumerable<T> list, Action<T> tryBlock)
    {
      foreach (var item in list)
        Exec.Try(() => tryBlock(item));

      return list;
    }

    /// <summary>
    /// Выполняет код внутри конструкции try-catch для каждого элемента списка.
    /// Блок catch ничего не делает.
    /// </summary>
    /// <typeparam name="T">Тип элемента списка.</typeparam>
    /// <param name="list">Список элементов.</param>
    /// <param name="predicate">Условие, при котором код будет выполнен.</param>
    /// <param name="tryBlock">Код, выполняемый в блоке try.</param>
    /// <returns>Список элементов.</returns>
    [DebuggerStepThrough]
    public static IEnumerable<T> ForEachTry<T>([NotNull] this IEnumerable<T> list, Func<T, bool> predicate, Action<T> tryBlock)
    {
      foreach (var item in list)
        if (predicate(item))
          Exec.Try(() => tryBlock(item));

      return list;
    }

    /// <summary>
    /// Выполняет код внутри конструкции try-catch для каждого элемента списка.
    /// </summary>
    /// <typeparam name="T">Тип элемента списка.</typeparam>
    /// <param name="list">Список элементов.</param>
    /// <param name="tryBlock">Код, выполняемый в блоке try.</param>
    /// <param name="catchBlock">Код, выполняемый в блоке catch.</param>
    [DebuggerStepThrough]
    public static void ForEachTry<T>([NotNull] this IEnumerable<T> list, Action<T> tryBlock, Action<T, Exception> catchBlock)
    {
      foreach (var item in list)
        Exec.Try(() => tryBlock(item), exc => catchBlock(item, exc));
    }

    /// <summary>
    /// Выполняет код внутри конструкции try-catch для каждого элемента списка.
    /// </summary>
    /// <typeparam name="T">Тип элемента списка.</typeparam>
    /// <param name="list">Список элементов.</param>
    /// <param name="predicate">Условие, при котором код будет выполнен.</param>
    /// <param name="tryBlock">Код, выполняемый в блоке try.</param>
    /// <param name="catchBlock">Код, выполняемый в блоке catch.</param>
    [DebuggerStepThrough]
    public static void ForEachTry<T>([NotNull] this IEnumerable<T> list, Func<T, bool> predicate, Action<T> tryBlock, Action<T, Exception> catchBlock)
    {
      foreach (var item in list)
        if (predicate(item))
          Exec.Try(() => tryBlock(item), exc => catchBlock(item, exc));
    }
    #endregion

    #region GetAndRemove
    /// <summary>
    /// Удаляет по ключу значение из словаря и возвращает удаленное значение.
    /// Метод не является потокобезопасным.
    /// </summary>
    /// <typeparam name="TKey">Тип ключа.</typeparam>
    /// <typeparam name="TValue">Тип значения.</typeparam>
    /// <param name="dict">Словарь.</param>
    /// <param name="key">Ключ.</param>
    /// <returns>Удаленное значение или default( TValue ), если значение не было удалено.</returns>
    [DebuggerStepThrough]
    public static TValue GetAndRemove<TKey, TValue>( this Dictionary<TKey, TValue> dict, TKey key )
    { // Ради производительности параметры не валидируются.
      TValue value;

      if (dict.TryGetValue(key, out value))
      {
        dict.Remove(key);

        return value;
      }

      return default(TValue);
    }

    /// <summary>
    /// Удаляет по ключу значение из потокобезопасного словаря и возвращает удаленное значение.
    /// </summary>
    /// <typeparam name="TKey">Тип ключа.</typeparam>
    /// <typeparam name="TValue">Тип значения.</typeparam>
    /// <param name="dict">Словарь.</param>
    /// <param name="key">Ключ.</param>
    /// <returns>Удаленное значение или default( TValue ), если значение не было удалено.</returns>
    [DebuggerStepThrough]
    public static TValue GetAndRemove<TKey, TValue>( this ConcurrentDictionary<TKey, TValue> dict, TKey key )
    { // Ради производительности параметры не валидируются.
      TValue value;

      if (dict.TryRemove(key, out value))
        return value;

      return default(TValue);
    }
    #endregion

    #region GetArray
    /// <summary>
    /// Возвращает массив элементов набора в виде массива.
    /// </summary>
    /// <typeparam name="T">Тип элементов набора.</typeparam>
    /// <param name="hashSet">Набор.</param>
    /// <returns>Массив элементов набора.</returns>
    [DebuggerStepThrough]
    public static T[] GetArray<T>([NotNull] this HashSet<T> hashSet)
    {
      var array = new T[hashSet.Count];

      hashSet.CopyTo(array);

      return array;
    }
    #endregion

    #region GetIdentifiersTable
    /// <summary>
    /// Возвращает таблицу, содержащую идентификаторы.
    /// Таблица содержит единственное поле с названием "Id" и типом T.
    /// </summary>
    /// <typeparam name="T">Тип идентификатора.</typeparam>
    /// <param name="ids">Список идентификаторов.</param>
    /// <returns>Таблица с идентификаторами.</returns>
    [DebuggerStepThrough]
    public static DataTable GetIdentifiersTable<T>(IEnumerable<T> ids)
      where T : struct
    {
      if (_identifierTables == null)
        _identifierTables = new Dictionary<Type, DataTable>();

      var table = _identifierTables.GetOrAdd(typeof(T), t => new DataTable() { Columns = { { "Id", t } } });

      table.Rows.Clear();

      if (ids != null)
        ids.ForEach(id => table.Rows.Add(id));

      return table;
    }
    #endregion

    #region GetStringIdentifiersTable
    /// <summary>
    /// Возвращает таблицу, содержащую идентификаторы.
    /// Таблица содержит единственное поле с названием "Id" и типом T.
    /// </summary>
    /// <param name="ids">Список идентификаторов.</param>
    /// <returns>Таблица с идентификаторами.</returns>
    [DebuggerStepThrough]
    public static DataTable GetStringIdentifiersTable(IEnumerable<string> ids)
    {
      var table = new DataTable() { Columns = { { "Id", typeof(string) } } };

      if (ids != null)
        ids.ForEach(id => table.Rows.Add(id));

      return table;
    }
    #endregion

    #region GetDataTable
    /// <summary>
    /// Возвращает таблицу, содержащую значения полей и свойств объектов класса, полученного из первого элемента коллекции.
    /// Используются только атрибуты с типом IsValueType (включая Nullable&lt;&gt;) или string
    /// </summary>
    /// <typeparam name="TBaseElement">Базовый класс объекта.</typeparam>
    /// <param name="data">Список объектов.</param>
    /// <returns>Таблица с данными.</returns>
    [DebuggerStepThrough]
    public static DataTable GetDataTable<TBaseElement>( IEnumerable<TBaseElement> data )
    {
      if (data == null || !data.Any())
        return null;

      var realElementType = data.First().GetType();

      Func<MemberInfo, Type> getType = m => m.IfIs<FieldInfo, Type>( fi => fi.FieldType, () => m.IfIs<PropertyInfo, Type>( pi => pi.PropertyType ) );
      Func<Type, bool> filterType = type => type == typeof(string) || type.IsValueType && (!type.IsGenericType || type.GetGenericTypeDefinition() == typeof(Nullable<>));
      Func<Type, Type> getValueType = type => type.IsValueType && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) ? type.GetGenericArguments().First() : type;

      var members = _dataTablesMembers.GetOrAdd(realElementType, t => t.GetProperties().Where(p => p.CanRead && p.GetIndexParameters().Length == 0).OfType<MemberInfo>().Concat(t.GetFields()).Where(m => filterType(getType(m))).ToArray());

      var table = _dataTables.GetOrAdd(realElementType, t =>
     {
       var tab = new System.Data.DataTable();

       tab.Columns.AddRange(members.Select(m => new System.Data.DataColumn(m.Name, getValueType(getType(m))) { AllowDBNull = getType(m).IfNotNull(mt => !mt.IsValueType || mt.IsGenericType) }).ToArray());

       return tab;
     });

      table.Rows.Clear();

      if (data != null)
        data.ForEach(dataItem => table.Rows.Add(members.Select(m => MemberHelper.GetValue(m, dataItem)).ToArray()));

      return table;
    }
    #endregion

    #region GetOrAdd
    /// <summary>
    /// Добавляет пару ключ/значение в словарь, если указанный ключ еще не существует.
    /// Метод не является потокобезопасным.
    /// </summary>
    /// <typeparam name="TKey">Тип ключа.</typeparam>
    /// <typeparam name="TValue">Тип значения.</typeparam>
    /// <param name="dict">Словарь.</param>
    /// <param name="key">Ключ.</param>
    /// <param name="value">Значение.</param>
    /// <returns>Существующее или добавленное значение.</returns>
    public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue value)
    { // Ради производительности параметры не валидируются.
      return dict.GetOrAdd(key, k => value);
    }

    /// <summary>
    /// Добавляет пару ключ/значение в словарь если указанный ключ еще не существует.
    /// Метод не является потокобезопасным.
    /// </summary>
    /// <typeparam name="TKey">Тип ключа.</typeparam>
    /// <typeparam name="TValue">Тип значения.</typeparam>
    /// <param name="dict">Словарь.</param>
    /// <param name="key">Ключ.</param>
    /// <param name="valueFactory">Функция для получения значения по ключу.</param>
    /// <returns>Существующее или добавленное значение.</returns>
    public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, Func<TKey, TValue> valueFactory)
      where TKey : notnull
    { // Ради производительности параметры не валидируются.
      var value = default(TValue);

      if (key != null && !dict.TryGetValue(key, out value))
        dict.Add(key, value = valueFactory(key));

      return value;
    }

    /// <summary>
    /// Выполняет следующие действия:
    /// 1) Добавляет пару [ключ->вложенный словарь] в словарь, если указанный ключ еще не существует.
    /// 2) Добавляет пару [ключ вложенного словаря->значение] во вложенный словарь, если указанный ключ вложенного словаря еще не существует.
    /// </summary>
    /// <typeparam name="TRootKey">Тип ключа.</typeparam>
    /// <typeparam name="TKey">Тип ключа вложенного словаря.</typeparam>
    /// <typeparam name="TValue">Тип значения вложенного словаря.</typeparam>
    /// <param name="dict">Словарь.</param>
    /// <param name="rootKey">Ключ.</param>
    /// <param name="key">Ключ вложенного словаря.</param>
    /// <param name="value">Значение вложенного словаря.</param>
    /// <returns>Существующее или добавленное значение вложенного словаря.</returns>
    public static TValue GetOrAdd<TRootKey, TKey, TValue>(this ConcurrentDictionary<TRootKey, ConcurrentDictionary<TKey, TValue>> dict, TRootKey rootKey, TKey key, TValue value)
      where TKey : notnull where TRootKey : notnull
    { // Ради производительности параметры не валидируются.
      return dict.GetOrAdd(rootKey, key, k => value);
    }

    /// <summary>
    /// Выполняет следующие действия:
    /// 1) Добавляет пару [ключ->вложенный словарь] в словарь, если указанный ключ еще не существует.
    /// 2) Добавляет пару [ключ вложенного словаря->значение] во вложенный словарь, если указанный ключ вложенного словаря еще не существует.
    /// </summary>
    /// <typeparam name="TRootKey">Тип ключа.</typeparam>
    /// <typeparam name="TKey">Тип ключа вложенного словаря.</typeparam>
    /// <typeparam name="TValue">Тип значения вложенного словаря.</typeparam>
    /// <param name="dict">Словарь.</param>
    /// <param name="rootKey">Ключ.</param>
    /// <param name="key">Ключ вложенного словаря.</param>
    /// <param name="valueFactory">Функция для получения значения по ключу вложенного словаря.</param>
    /// <returns>Существующее или добавленное значение вложенного словаря.</returns>
    public static TValue GetOrAdd<TRootKey, TKey, TValue>(this ConcurrentDictionary<TRootKey, ConcurrentDictionary<TKey, TValue>> dict, TRootKey rootKey, TKey key, Func<TKey, TValue> valueFactory)
      where TKey : notnull where TRootKey : notnull
    { // Ради производительности параметры не валидируются.
      TValue dictValue = default( TValue );

      if (rootKey != null && key != null)
        dictValue = dict.GetOrAdd(rootKey, rk => new ConcurrentDictionary<TKey, TValue>()).GetOrAdd(key, valueFactory);

      return dictValue;
    }
    #endregion

    #region GetValue
    /// <summary>
    /// Возвращает по ключу значение из словаря без выбрасывания исключения в случае
    /// отсутствия в словаре ключа.
    /// </summary>
    /// <typeparam name="TKey">Тип ключа.</typeparam>
    /// <typeparam name="TValue">Тип значения.</typeparam>
    /// <param name="dict">Словарь.</param>
    /// <param name="key">Ключ.</param>
    /// <returns>Значение.</returns>
    [DebuggerStepThrough]
    public static TValue GetValue<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
    { // Ради производительности параметры не валидируются.
      if (key != null)
      {
        TValue value;

        if (dict.TryGetValue(key, out value))
          return value;
      }

      return default( TValue );
    }

    /// <summary>
    /// Возвращает по ключу значение из словаря без выбрасывания исключения в случае
    /// отсутствия в словаре ключа.
    /// </summary>
    /// <typeparam name="TKey">Тип ключа.</typeparam>
    /// <typeparam name="TValue">Тип значения.</typeparam>
    /// <param name="dict">Словарь.</param>
    /// <param name="key">Ключ.</param>
    /// <param name="defaultValue">Значение по умолчанию.</param>
    /// <returns>Значение.</returns>
    [DebuggerStepThrough]
    public static TValue GetValue<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue defaultValue)
    { // Ради производительности параметры не валидируются.
      if (key != null)
      {
        TValue value;

        if (dict.TryGetValue(key, out value))
          return value;
      }

      return defaultValue;
    }
    #endregion

    #region In
    /// <summary>
    /// Проверяет, находится ли объект в указанном списке.
    /// </summary>
    /// <typeparam name="T">Тип объекта.</typeparam>
    /// <param name="obj">Искомый объект.</param>
    /// <param name="list">Список объектов для поиска вхождения.</param>
    /// <returns>True - искомый объект присутствует в списке, иначе - False.</returns>
    [DebuggerStepThrough]
    public static bool In<T>(this T obj, [NotNull] params T[] list)
    {
      return list.Contains(obj);
    }

    /// <summary>
    /// Проверяет, находится ли объект в указанном списке.
    /// </summary>
    /// <param name="str">Искомая строка.</param>
    /// <param name="list">Список объектов для поиска вхождения.</param>
    /// <returns>True - искомый объект присутствует в списке, иначе - False.</returns>
    [DebuggerStepThrough]
    public static bool InCI(this string str, [NotNull] params string[] list)
    {
      return list.Contains(str, StringComparer.InvariantCultureIgnoreCase);
    }
    #endregion

    #region Peek
    /// <summary>
    /// Возвращает значение из начала очереди без удаления.
    /// </summary>
    /// <typeparam name="T">Тип значения.</typeparam>
    /// <param name="queue">Очередь.</param>
    /// <returns>Значение.</returns>
    [DebuggerStepThrough]
    public static T Peek<T>( [NotNull] this ConcurrentQueue<T> queue )
    {
      T value;

      if (queue.TryPeek(out value))
        return value;

      return default(T);
    }
    #endregion

    #region RemoveAndDoIfEmpty
    /// <summary>
    /// Удаляет значение из коллекции и выполняет указанное действие, если коллекция после удаления оказывается пуста.
    /// </summary>
    /// <typeparam name="TValue">Тип значения.</typeparam>
    /// <param name="collection">Коллекция.</param>
    /// <param name="value">Значение.</param>
    /// <param name="action">Действие.</param>
    [DebuggerStepThrough]
    public static void RemoveAndDoIfEmpty<TValue>([NotNull] this ICollection<TValue> collection, TValue value, [NotNull] Action action)
    {
      collection.Remove(value);

      if (collection.Count == 0)
        action();
    }
    #endregion

    #region RemoveValue
    /// <summary>
    /// Удаляет по ключу значение из потокобезопасного словаря.
    /// </summary>
    /// <typeparam name="TKey">Тип ключа.</typeparam>
    /// <typeparam name="TValue">Тип значения.</typeparam>
    /// <param name="dict">Словарь.</param>
    /// <param name="key">Ключ.</param>
    /// <returns>True - значение удалено, иначе - False.</returns>
    [DebuggerStepThrough]
    public static bool RemoveValue<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict, TKey key)
      where TKey : notnull 
    { // Ради производительности параметры не валидируются.
      TValue value;

      return dict.TryRemove(key, out value);
    }
    #endregion

    #region RemoveRangeSafe
    /// <summary>
    /// Потокобезопасно удаляет несколько элементов из набора.
    /// </summary>
    /// <typeparam name="T">Тип элементов набора.</typeparam>
    /// <param name="hashSet">Набор.</param>
    /// <param name="items">Список элементов.</param>
    [DebuggerStepThrough]
    public static void RemoveRangeSafe<T>([NotNull] this HashSet<T> hashSet, IEnumerable<T> items)
    {
      if (items != null)
        lock (hashSet)
          foreach (var item in items)
            hashSet.Remove(item);
    }
    #endregion

    #region RemoveSafe
    /// <summary>
    /// Потокобезопасно удаляет указанный элемент из набора.
    /// </summary>
    /// <typeparam name="T">Тип элементов набора.</typeparam>
    /// <param name="hashSet">Набор.</param>
    /// <param name="item">Элемент.</param>
    /// <returns>True - элемент был удален, иначе - False.</returns>
    [DebuggerStepThrough]
    public static bool RemoveSafe<T>([NotNull] this HashSet<T> hashSet, T item)
    {
      lock (hashSet)
        return hashSet.Remove(item);
    }
    #endregion

    #region RemoveWhere
    /// <summary>
    /// Удаляет по условию значения из потокобезопасного словаря.
    /// </summary>
    /// <typeparam name="TKey">Тип ключа.</typeparam>
    /// <typeparam name="TValue">Тип значения.</typeparam>
    /// <param name="dict">Словарь.</param>
    /// <param name="predicate">Условие.</param>
    [DebuggerStepThrough]
    public static void RemoveWhere<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict, Func<TKey, TValue, bool> predicate)
      where TKey : notnull 
    { // Ради производительности параметры не валидируются.
      dict.ForEach(kvp => predicate(kvp.Key, kvp.Value), kvp => dict.RemoveValue(kvp.Key));
    }
    #endregion

    #region RemoveAll
    public static int RemoveAll<T>(this IList<T> list, Predicate<T> match)
    {
      int count = 0;
      for (int index = 0; index < list.Count; index++)
      {
        if (match(list[index]))
        {
          list.RemoveAt(index--);
          count++;
        }
      }
      return count;
    }
    #endregion

    #region AddRange
    public static void AddRange<T>(this IList<T> list, IEnumerable<T> collection)
    {
      foreach (T item in collection)
        list.Add(item);
    }
    #endregion

    #region UpdateEach
    /// <summary>
    /// Заменяет значение каждого элемента списка новым значением.
    /// </summary>
    /// <typeparam name="T">Тип элемента списка.</typeparam>
    /// <param name="list">Список элементов.</param>
    /// <param name="valueFactory">Функция для получения нового значения списка по старому значению.</param>
    [DebuggerStepThrough]
    public static void UpdateEach<T>([NotNull] this IList<T> list, Func<T, T> valueFactory)
    {
      for (int i = 0; i < list.Count; i++)
        list[i] = valueFactory(list[i]);
    }
    #endregion

    #region Difference
    public static IEnumerable<TSource> Difference<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
    {
      return first.Except(second).Union(second.Except(first));
    }
    #endregion

    #region ValidateArray
    public static TSource[] ValidateArray<TSource>(this TSource[] arr)
      where TSource : class
    {
      return arr == null ? new TSource[0] : arr.Length == 0 ? arr : arr.Where(e => e != null).ToArray();
    }
    #endregion

    #region DefaultIfNull
    /// <summary>
    /// Создает пустую коллекцию, если она null.
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <param name="collection"></param>
    /// <returns></returns>
    public static IEnumerable<TSource> DefaultIfNull<TSource>( this IEnumerable<TSource> collection )
    {
      return collection ?? Array.Empty<TSource>();
    }
    #endregion

    #region TryUpdate
    public static bool TryUpdate<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict, TKey key, Func<TKey, TValue, TValue> valueFactory)
      where TKey : notnull
    {
      TValue tValue;
      TValue tValueNew;
      while (dict.TryGetValue(key, out tValue))
      {
        tValueNew = valueFactory(key, tValue);
        if (dict.TryUpdate(key, tValueNew, tValue))
          return true;
      }
      return false;
    }
    #endregion

    #region TryRemoveIf
    public static bool TryRemoveIf<TKey, TValue>( this ConcurrentDictionary<TKey, TValue> dict, TKey key, Func<TKey, TValue, bool> valueChecker, out TValue value )
    {
      TValue tValue, tValue2;
      while (dict.TryGetValue(key, out tValue))
        if (valueChecker(key, tValue))
        {
          if (dict.TryGetValue(key, out tValue2))
            if (object.ReferenceEquals(tValue, tValue2))
              return dict.TryRemove(key, out value);
        }
        else
          break;
      value = default(TValue);
      return false;
    }
    #endregion

    #region JoinAsStrings
    public static string JoinAsStrings<TSource>( this IEnumerable<TSource> collection, int maxCount = 5, Func<TSource, string> formatter = null, string delimiter = ", ", bool addDebugInfo = false )
    {
      Func<TSource, int, string> formatterN = null;
      if (formatter != null)
        formatterN = (o, i) => formatter(o);

      return JoinAsStringsN(collection, maxCount, formatterN, delimiter, addDebugInfo);
    }

    public static string JoinAsStringsN<TSource>( this IEnumerable<TSource> collection, int maxCount = 5, Func<TSource, int, string> formatter = null, string delimiter = ", ", bool addDebugInfo = false )
    {
      return collection.IfNotNull(_coll =>
     {
       TSource[] arr = Exec.Try(() => _coll.ToArray(), exc => new TSource[0]);

       int count = arr.Length;

       if (count == 0)
         return string.Empty;

       int takeCount = Math.Min(count, maxCount);

       return string.Format("{0}{1}{2}{3}", addDebugInfo ? string.Format("[Array({0}): ", count) : string.Empty, string.Join(delimiter, arr.Take(takeCount).Select((o, i) => Exec.Try(() => formatter == null ? o.ToString() : formatter(o, i), ex => string.Format("#:{0}", ex.Message)))), takeCount < count ? (delimiter + "...") : string.Empty, addDebugInfo ? "]" : string.Empty);
     }) ?? String.Empty;
    }
    #endregion

    #region ListOrderedUpdate
    /// <summary>
    /// Метод обновления упорядоченного списка <paramref name="list"/> значениями из упорядоченной коллекции <paramref name="updated"/>. <para> Список <paramref name="list"/> не подвергается пересортировке.</para> <para> Списки <paramref name="list"/> и <paramref name="updated"/> должны быть упорядоченый в точности как этот порядок определяет функция сравнения <paramref name="comparer"/></para>
    /// </summary>
    /// <typeparam name="T">Тип элементов списка и коллекции </typeparam>
    /// <param name="list">Обновляемый упорядоченный список </param>
    /// <param name="updated">Упорядоченная коллекция элементов, замещающих список <paramref name="list"/> </param>
    /// <param name="comparer">Функция сравнения элементов списка <paramref name="list"/> и коллекции <paramref name="updated"/>.
    /// <para> Если сравнение == -1, элемент из коллекции <paramref name="updated"/> вставляется перед соответсвующим элементом из списка <paramref name="list"/></para>
    /// <para> Если сравнение ==  0, элемент из коллекции <paramref name="updated"/> замещает соответсвующий элемент из списка <paramref name="list"/></para>
    /// <para> Если сравнение ==  1, элемент из коллекции <paramref name="updated"/> добавляется вслед за соответствущим элементом из списка <paramref name="list"/>, при этом сам элемент из списка <paramref name="list"/> удаляется</para>
    /// </param>
    public static void ListOrderedUpdate<T>(this IList<T> list, IEnumerable<T> updated, Func<T, T, int> comparer)
    {
      if (updated == null)
      {
        list.Clear();
        return;
      }

      if (list.Count == 0)
      {
        list.AddRange(updated);
        return;
      }

      var eUpd = updated.GetEnumerator();
      if (!eUpd.MoveNext())
      {
        list.Clear();
        return;
      }

      int iList = 0;
      bool append = false;
      var upd = eUpd.Current;
      while (true)
      {
        if (append || iList >= list.Count)
        {
          list.Add(upd);
          append = true;

          if (!eUpd.MoveNext())
            break;
          upd = eUpd.Current;
        }
        else
        {
          var cmp = comparer(list[iList], upd);

          if (cmp == 0)
          {
            list[iList++] = upd;
            if (!eUpd.MoveNext())
              break;
            upd = eUpd.Current;
          }
          else
            if (cmp < 0)
          {
            list.RemoveAt(iList);
          }
          else
          {
            list.Insert(iList++, upd);
            if (!eUpd.MoveNext())
              break;
            upd = eUpd.Current;
          }
        }
      }
      if (!append)
        while (iList < list.Count)
        {
          list.RemoveAt(iList);
        }
    }
    #endregion

    #region SelectManyByPortions
    public static IEnumerable<TTargetElement> SelectManyByPortions<TSourceElement, TTargetElement>(this IEnumerable<TSourceElement> sourceCollection, int portion, Func<IEnumerable<TSourceElement>, IEnumerable<TTargetElement>> converter)
    {
      return sourceCollection.DefaultIfNull().Select((sourceElement, index) => new { sourceElement, groupIndex = index / portion }).GroupBy(it => it.groupIndex, (_, grouppedItemCollection) => grouppedItemCollection.Select(it => it.sourceElement)).SelectMany(itemGroup => converter(itemGroup));
    }
    #endregion

    #region ForEachByPortions
    public static void ForEachByPortions<TSourceElement>(this IEnumerable<TSourceElement> sourceCollection, int portion, Action<IEnumerable<TSourceElement>> action)
    {
      sourceCollection.DefaultIfNull().Select((sourceElement, index) => new { sourceElement, groupIndex = index / portion }).GroupBy(it => it.groupIndex, (_, grouppedItemCollection) => grouppedItemCollection.Select(it => it.sourceElement)).ForEach(action);
    }
    #endregion

    #region SequenceEqual
    /// <summary>
    /// Функция сравнения двух коллекций пар строк, с упорядочиванием коллекций по первой строке пары, и с задаваемыми компараторами для обоих строк пары
    /// </summary>
    /// <param name="pairs">Первая коллекция пар строк</param>
    /// <param name="otherPairs">Вторая коллекция пар строк</param>
    /// <param name="cmpItem1">Компаратор первой строки пары</param>
    /// <param name="cmpItem2">Компаратор второй строки пары</param>
    /// <returns>True - если все пары строк в каждой коллекции равны, иначе - Flase</returns>
    public static bool SequenceEqual(this IEnumerable<string[]> pairs, IEnumerable<string[]> otherPairs, StringComparer cmpItem1, StringComparer cmpItem2)
    {
      return pairs.Select(p => Tuple.Create(p[0], p[1])).OrderBy(p => p.Item1, cmpItem1).ToArray()
        .SequenceEqual(otherPairs.Select(p => Tuple.Create(p[0], p[1])).OrderBy(p => p.Item1, cmpItem1).ToArray()
        , AnonymousComparer.Create<Tuple<string, string>>((p1, p2) => cmpItem1.Equals(p1.Item1, p2.Item1) && cmpItem2.Equals(p1.Item2, p2.Item2), p => p.Item1.GetHashCode() ^ p.Item2.GetHashCode()));
    }
    #endregion
  }
}
