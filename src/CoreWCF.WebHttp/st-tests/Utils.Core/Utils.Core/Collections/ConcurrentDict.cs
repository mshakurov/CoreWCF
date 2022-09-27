using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ST.Utils.Collections
{
  /// <summary>
  /// Конкурентный словарь. В основе обычный словар. Все методы вполняются в защите.
  /// </summary>
  /// <typeparam name="TKey">Тип ключа</typeparam>
  /// <typeparam name="TValue">Тип значения</typeparam>
  [DebuggerDisplay( "Count = {Count}" )]
  public class ConcurrentDict<TKey, TValue>
  {
    #region .private
    private Dictionary<TKey, TValue> _dict;
    private object _locker = new object();
    #endregion

    #region class CancelArgs
    public class CancelArgs
    {
      public bool CancelAdd;

      protected CancelArgs()
      {
        CancelAdd = false;
      }
    }

    private sealed class CancelArgsCreator : CancelArgs
    {
      public CancelArgsCreator()
        : base()
      {
      }
    }
    #endregion

    #region .ctor
    public ConcurrentDict()
    {
      _dict = new Dictionary<TKey, TValue>();
    }

    public ConcurrentDict( int capacity )
    {
      _dict = new Dictionary<TKey, TValue>( capacity );
    }

    public ConcurrentDict( IEqualityComparer<TKey> comparer )
    {
      _dict = new Dictionary<TKey, TValue>( comparer );
    }

    public ConcurrentDict( int capacity, IEqualityComparer<TKey> comparer )
    {
      _dict = new Dictionary<TKey, TValue>( capacity, comparer );
    }

    public ConcurrentDict( IDictionary<TKey, TValue> dictionary )
    {
      _dict = new Dictionary<TKey, TValue>( dictionary );
    }

    public ConcurrentDict( IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer )
    {
      _dict = new Dictionary<TKey, TValue>( dictionary, comparer );
    }

    public ConcurrentDict( IEnumerable<KeyValuePair<TKey, TValue>> collection )
      : this()
    {
      foreach ( KeyValuePair<TKey, TValue> kv in collection )
        _dict.Add( kv.Key, kv.Value );
    }

    public ConcurrentDict( IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer )
      : this( comparer )
    {
      foreach ( KeyValuePair<TKey, TValue> kv in collection )
        _dict.Add( kv.Key, kv.Value );
    }
    #endregion

    public int Count
    {
      get
      {
        lock ( _locker )
          return _dict.Count;
      }
    }

    /// <summary>
    /// Заменяет словарь данными, получаемыми в заблокированном состоянии
    /// </summary>
    /// <param name="dictGetter">Функция, возвращающая словарь новых даных</param>
    public void Set(Func<Dictionary<TKey, TValue>> dictGetter)
    {
      lock (_locker)
        _dict = new Dictionary<TKey, TValue>(dictGetter());
    }

    public void Add( TKey key, TValue value )
    {
      lock ( _locker )
        _dict.Add( key, value );
    }

    public void Clear()
    {
      lock ( _locker )
        _dict.Clear();
    }

    public bool ContainsKey( TKey key )
    {
      lock ( _locker )
        return _dict.ContainsKey( key );
    }

    public bool ContainsValue( TValue value )
    {
      lock ( _locker )
        return _dict.ContainsValue( value );
    }

    public bool Remove( TKey key )
    {
      lock ( _locker )
        return _dict.Remove( key );
    }

    public void TryAdd( TKey key, TValue value )
    {
      lock ( _locker )
        if ( !_dict.ContainsKey( key ) )
          _dict.Add( key, value );
    }

    public void TryAddRange( Dictionary<TKey, TValue> loadedDict )
    {
      loadedDict.ForEach( kv => this.TryAdd( kv.Key, kv.Value ) );
    }

    public bool TryGetValue( TKey key, out TValue value )
    {
      lock ( _locker )
        return _dict.TryGetValue( key, out value );
    }

    public bool TryUpdate( TKey key, Func<TKey, TValue, TValue> valueFactory )
    {
      lock ( _locker )
      {
        TValue valueOld;
        if ( _dict.TryGetValue( key, out valueOld ) )
        {
          _dict[key] = valueFactory( key, valueOld );
          return true;
        }
      }
      return false;
    }

    public bool TryRemove( TKey key )
    {
      lock ( _locker )
        return _dict.Remove( key );
    }

    public bool TryRemove( TKey key, out TValue oldValue )
    {
      lock ( _locker )
        if ( _dict.TryGetValue( key, out oldValue ) )
          return _dict.Remove( key );
      return false;
    }

    public bool TryRemoveByValue( TValue value )
    {
      lock ( _locker )
      {
        foreach ( KeyValuePair<TKey, TValue> kv in _dict )
          if ( kv.Value.Equals( value ) )
            return _dict.Remove( kv.Key );
      }
      return false;
    }

    public bool TryRemoveByValue( TValue value, IEqualityComparer<TValue> valueComprer )
    {
      lock ( _locker )
      {
        foreach ( KeyValuePair<TKey, TValue> kv in _dict )
          if ( valueComprer.Equals( kv.Value, value ) )
            return _dict.Remove( kv.Key );
      }
      return false;
    }

    public bool TryRemoveIf( TKey key, Func<TKey, TValue, bool> valueChecker, out TValue value )
    {
      lock ( _locker )
        if ( _dict.TryGetValue( key, out value ) )
          if ( valueChecker( key, value ) )
            return _dict.Remove( key );
      return false;
    }

    public bool TryRemoveIf( TKey key, Func<TKey, TValue, bool> valueChecker )
    {
      TValue value;
      lock ( _locker )
        if ( _dict.TryGetValue( key, out value ) )
          if ( valueChecker( key, value ) )
            return _dict.Remove( key );
      return false;
    }

    public TValue GetValue( TKey key, bool throwIfNotFound = true )
    {
      TValue value = default( TValue );
      bool found = false;

      lock ( _locker )
        found = _dict.TryGetValue( key, out value );

      if ( !found )
        if ( throwIfNotFound )
          throw new KeyNotFoundException();

      return value;
    }

    public TValue AddOrUpdate( TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory )
    {
      TValue tOldValue;
      TValue tNewValue;

      lock ( _locker )
        if ( _dict.TryGetValue( key, out tOldValue ) )
        {
          _dict[key] = tNewValue = updateValueFactory( key, tOldValue );
        }
        else
        {
          _dict.Add( key, tNewValue = addValueFactory( key ) );
        }

      return tNewValue;
    }

    public TValue AddOrUpdate( TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory )
    {
      TValue tOldValue;
      TValue tNewValue;

      lock ( _locker )
        if ( _dict.TryGetValue( key, out tOldValue ) )
        {
          _dict[key] = tNewValue = updateValueFactory( key, tOldValue );
        }
        else
        {
          _dict.Add( key, tNewValue = addValue );
        }

      return tNewValue;
    }

    public TValue GetOrAdd( TKey key, Func<TKey, TValue> valueFactory )
    {
      TValue value;

      lock ( _locker )
        if ( !_dict.TryGetValue( key, out value ) )
          _dict.Add( key, value = valueFactory( key ) );

      return value;
    }

    public TValue GetOrAddOrCancel( TKey key, Func<TKey, CancelArgs, TValue> valueFactory )
    {
      TValue value;

      lock ( _locker )
        if ( !_dict.TryGetValue( key, out value ) )
        {
          CancelArgs args = new CancelArgsCreator();

          value = valueFactory( key, args );

          if ( !args.CancelAdd )
            _dict.Add( key, value );
        }

      return value;
    }

    public TValue GetOrAdd( TKey key, TValue value )
    {
      TValue tValue2;

      lock ( _locker )
        if ( !_dict.TryGetValue( key, out tValue2 ) )
          _dict.Add( key, tValue2 = value );

      return tValue2;
    }

    public TKey[] KeysToArray()
    {
      lock ( _locker )
        return _dict.Keys.ToArray();
    }

    public List<TKey> KeysToList()
    {
      lock ( _locker )
        return _dict.Keys.ToList();
    }

    public TValue[] ValuesToArray()
    {
      lock ( _locker )
        return _dict.Values.ToArray();
    }

    public List<TValue> ValuesToList()
    {
      lock ( _locker )
        return _dict.Values.ToList();
    }

    public KeyValuePair<TKey, TValue> FirstOrDefault( Func<KeyValuePair<TKey, TValue>, bool> predicate )
    {
      lock ( _locker )
        return _dict.FirstOrDefault( predicate );
    }

    public TValue ValuesFirstOrDefault( Func<TValue, bool> predicate )
    {
      lock ( _locker )
        return _dict.Values.FirstOrDefault( predicate );
    }

    public KeyValuePair<TKey, TValue>[] ToArray()
    {
      lock ( _locker )
        return _dict.ToArray();
    }

  }

}
