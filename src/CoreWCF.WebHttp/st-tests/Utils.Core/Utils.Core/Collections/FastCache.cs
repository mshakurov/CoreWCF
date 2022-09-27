using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ST.Utils.Threading;

namespace ST.Utils.Collections
{
  /// <summary>
  /// Высокопроизводительный потокобезопасный кэш. Предназначен для использования в тех случаях, когда изменение данных происходит очень редко.
  /// </summary>
  /// <typeparam name="TKey">Тип ключа.</typeparam>
  /// <typeparam name="TValue">Тип значения.</typeparam>
  public class FastCache<TKey, TValue> where TKey : notnull
  {
    #region .Fields
    private ConcurrentDictionary<TKey, TValue> _dict = new ConcurrentDictionary<TKey, TValue>();
    private ConcurrentDictionary<TKey, TValue> _syncDict;

    private readonly Locker _locker = new Locker();

    private readonly bool _allowDefaults;

    private bool _isValid;
    #endregion

    #region .Properties
    /// <summary>
    /// Словарь для изменения данных. Доступен только после вызова метода CreateSynchronizationContext и до вызова метода IDisposable.Dispose.
    /// </summary>
    protected ConcurrentDictionary<TKey, TValue> Dictionary
    {
      get { return _locker.Owner == -1 ? null : (_syncDict ??= new ConcurrentDictionary<TKey, TValue>( _dict )); }
    }

    /// <summary>
    /// Возвращает/устанавливает значение.
    /// </summary>
    /// <param name="key">Ключ.</param>
    /// <returns>Значение.</returns>
    public TValue this[TKey key]
    {
      get { return Get( key ); }
      set { Set( key, value ); }
    }
    #endregion

    #region .Ctor
    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="allowDefaults">Признак того, что значением кэша может быть default( TValue ). Если не установлен, то при заполнении кэша всегда будет производиться попытка получить значение по ключу, если предыдущая попытка вернула default( TValue ).</param>
    public FastCache( bool allowDefaults )
    {
      _allowDefaults = allowDefaults;

      _locker.Released += LockReleased;
    }
    #endregion

    #region AcquireLock
    /// <summary>
    /// Получает эксклюзивный доступ к кэшу.
    /// Предназначен для использования в конструкции using: using( AcquireLock() ) { ... }.
    /// </summary>
    /// <returns>Объект для освобождения эксклюзивного доступа.</returns>
    protected IDisposable AcquireLock()
    {
      return _locker.Acquire();
    }
    #endregion

    #region Clear
    /// <summary>
    /// Очищает коллекцию.
    /// </summary>
    public void Clear()
    {
      using( AcquireLock() )
        _syncDict?.Clear();
    }
    #endregion

    #region Get
    /// <summary>
    /// Возвращает значение.
    /// </summary>
    /// <param name="key">Ключ.</param>
    /// <returns>Значение.</returns>
    public TValue Get( TKey key )
    {
      return _dict.GetValue( key );
    }

    /// <summary>
    /// Возвращает значение.
    /// </summary>
    /// <param name="key">Ключ.</param>
    /// <param name="valueFactory">Фабрика для получения значения, если его не существует в кэше.</param>
    /// <returns>Значение.</returns>
    public TValue Get( TKey key, Func<TKey, TValue> valueFactory )
    { // Ради производительности параметры не валидируются.
      TValue value;

      if( !_dict.TryGetValue( key, out value ) )
        using( _locker.Acquire() )
          if( !_dict.TryGetValue( key, out value ) )
            value = Set( key, valueFactory( key ) );

      return value;
    }

    /// <summary>
    /// Возвращает список всех значений.
    /// </summary>
    /// <param name="keyGetter">Метод, возвращающий ключ по значению.</param>
    /// <param name="valuesFactory">Фабрика для получения списка всех значений, если список кэша не является актуальным.</param>
    /// <returns>Список значений.</returns>
    public TValue[]Get( Func<TValue, TKey> keyGetter, Func<TValue[]> valuesFactory )
    { // Ради производительности параметры не валидируются.
      if( !_isValid )
        using( _locker.Acquire() )
          if( !_isValid )
          {
            Remove( _dict.Keys );

            Set( keyGetter, valuesFactory() );

            _isValid = true;
          }

      return ToArray();
    }

    /// <summary>
    /// Возвращает значение.
    /// </summary>
    /// <param name="context">Контекст.</param>
    /// <param name="keyFactory">Фабрика для получения ключа.</param>
    /// <param name="valueFactory">Фабрика для получения значения, если его не существует в кэше.</param>
    /// <returns>Значение.</returns>
    public TValue Get<TContext>( TContext context, Func<TContext, TKey> keyFactory, Func<TContext, TKey, TValue> valueFactory )
    { // Ради производительности параметры не валидируются.
      TValue value;

      var key = keyFactory( context );

      if( !_dict.TryGetValue( key, out value ) )
        using( _locker.Acquire() )
          if( !_dict.TryGetValue( key, out value ) )
            value = Set( key, valueFactory( context, key ) );

      return value;
    }
    #endregion

    #region GetKeys
    /// <summary>
    /// Возвращает список ключей.
    /// </summary>
    /// <param name="predicate">Предикат.</param>
    /// <returns>Список ключей.</returns>
    public TKey[] GetKeys( Func<TKey, TValue, bool> predicate )
    {
      return predicate == null ? _dict.Keys.ToArray() : _dict.Where( kvp => predicate( kvp.Key, kvp.Value ) ).Select( kvp => kvp.Key ).ToArray();
    }
    #endregion

    #region LockReleased
    private void LockReleased()
    {
      if( _syncDict != null )
      {
        _dict = _syncDict;

        _syncDict = null;
      }
    }
    #endregion

    #region OnAdded
    /// <summary>
    /// Вызывается после добавления значения.
    /// </summary>
    /// <param name="key">Добавленный ключ.</param>
    /// <param name="value">Добавленное значение.</param>
    protected virtual void OnAdded( TKey key, TValue value )
    {
    }
    #endregion

    #region OnRemoved
    /// <summary>
    /// Вызывается после удаления значения.
    /// </summary>
    /// <param name="key">Удаленный ключ.</param>
    /// <param name="value">Удаленное значение.</param>
    protected virtual void OnRemoved( TKey key, TValue value )
    {
    }
    #endregion

    #region Remove
    /// <summary>
    /// Удаляет значение.
    /// </summary>
    /// <param name="key">Ключ.</param>
    public void Remove( TKey key )
    {
      Remove( key, true );
    }

    private void Remove( TKey key, bool invalidate )
    {
      if( key != null )
        using( _locker.Acquire() )
        {
          var value = Get( key );

          if( value != null )
          {
            Dictionary?.TryRemove( key, out _);

            OnRemoved( key, value );

            if( invalidate )
              _isValid = false;
          }
        }
    }

    /// <summary>
    /// Удаляет несколько значений.
    /// </summary>
    /// <param name="keys">Список ключей.</param>
    public void Remove( IEnumerable<TKey> keys )
    {
      if( keys != null )
        using( _locker.Acquire() )
          keys.ForEach( Remove );
    }
    #endregion

    #region Set
    /// <summary>
    /// Устанавливает значение.
    /// </summary>
    /// <param name="key">Ключ.</param>
    /// <param name="value">Значение.</param>
    /// <returns>Значение.</returns>
    public TValue Set( TKey key, TValue value )
    {
      if( key != null && (!object.Equals( value, default( TValue ) ) || _allowDefaults) )
        using( _locker.Acquire() )
        {
          Remove( key, false );

          if( !Dictionary?.ContainsKey( key ) ?? true )
            Dictionary?.AddOrUpdate( key, value, (k, ov) => value );

          OnAdded( key, value );
        }

      return value;
    }

    /// <summary>
    /// Устанавливает значение.
    /// </summary>
    /// <param name="keyGetter">Метод, возвращающий ключ по значению.</param>
    /// <param name="value">Значение.</param>
    /// <returns>Значение.</returns>
    public TValue Set( Func<TValue, TKey> keyGetter, TValue value )
    {
      return object.Equals(value, default(TValue)) ? default( TValue ) : Set( keyGetter( value ), value );
    }

    /// <summary>
    /// Устанавливает несколько значений.
    /// </summary>
    /// <param name="keyGetter">Метод, возвращающий ключ по значению.</param>
    /// <param name="values">Список значений.</param>
    public void Set( Func<TValue, TKey> keyGetter, IEnumerable<TValue> values )
    { // Ради производительности параметры не валидируются.
      if( values != null )
        using( _locker.Acquire() )
          values.ForEach( value => Set( keyGetter, value ) );
    }
    #endregion

    #region ToArray
    /// <summary>
    /// Возвращает список всех значений.
    /// </summary>
    /// <returns>Список значений.</returns>
    public TValue[] ToArray()
    {
      return _dict.Values.ToArray();
    }
    #endregion
  }
}
