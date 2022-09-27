using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace ST.Utils
{
  public static class PropertyMapper
  {
    #region .Static Fields
    private static readonly ConcurrentDictionary<string, DynamicMethod> _map = new ConcurrentDictionary<string, DynamicMethod>();
    #endregion

    #region Copy
    /// <summary>
    /// Копирует свойства объекта источника в соответствующие свойства объекта приемника.
    /// </summary>
    /// <typeparam name="S">Тип параметра источника.</typeparam>
    /// <typeparam name="T">Тип параметра приемника.</typeparam>
    /// <param name="source">Объект источник.</param>
    /// <param name="target">Объект приемник.</param>
    public static void Copy<S, T>( S source, T target )
      where S : class
      where T : class
    {
      if( source == null || target == null )
        return;

      var key = GetMapKey( typeof( S ), typeof( T ) );

      _map[key].Invoke( null, new object[] { source, target } );
    }

    /// <summary>
    /// Копирует свойства объекта источника в соответствующие свойства объекта приемника.
    /// </summary>
    /// <typeparam name="S">Тип параметра источника.</typeparam>
    /// <typeparam name="T">Тип параметра приемника.</typeparam>
    /// <param name="source">Объект источник.</param>
    /// <returns>Объект приемник.</returns>
    public static T Copy<S, T>( S source )
      where S : class
      where T : class, new()
    {
      if( source == null )
        return null;

      var key = GetMapKey( typeof( S ), typeof( T ) );

      var target = new T();

      _map[key].Invoke( null, new object[] { source, target } );

      return target;
    }

    /// <summary>
    /// Копирует свойства объекта источника в соответствующие свойства объекта приемника.
    /// </summary>
    /// <typeparam name="S">Тип параметра источника.</typeparam>
    /// <typeparam name="T">Тип параметра приемника.</typeparam>
    /// <param name="source">Список объектов источника.</param>
    /// <returns>Список объектов приемника.</returns>
    public static List<T> Copy<S, T>( IEnumerable<S> source )
      where S : class
      where T : class, new()
    {
      if( source == null )
        return null;

      var key = GetMapKey( typeof( S ), typeof( T ) );

      var list = new List<T>();

      var args = new object[2];

      foreach( var sourceItem in source )
      {
        var targetItem = new T();

        args[0] = sourceItem;
        args[1] = targetItem;

        _map[key].Invoke( null, args );

        list.Add( targetItem );
      }

      return list;
    }
    #endregion

    #region GetMapKey
    private static string GetMapKey( Type sourceType, Type targetType )
    {
      var key = sourceType.FullName + "_" + targetType.FullName;

      if( !_map.ContainsKey( key ) )
        MapTypes( key, sourceType, targetType );

      return key;
    }
    #endregion

    #region GetMatchingProperties
    private static IList<PropertyMap> GetMatchingProperties( Type sourceType, Type targetType )
    {
      var list = new List<PropertyMap>();

      var sourceProperties = sourceType.GetProperties();
      var targetProperties = targetType.GetProperties();

      foreach( var s in sourceProperties )
        foreach( var t in targetProperties )
          if( s.Name == t.Name && s.CanRead && t.CanWrite && s.PropertyType == t.PropertyType )
            list.Add( new PropertyMap { _sourceProperty = s, _targetProperty = t } );

      return list;
    }
    #endregion

    #region MapTypes
    private static void MapTypes( string key, Type sourceType, Type targetType )
    {
      var dynamicMethod = new DynamicMethod( key, null, new[] { sourceType, targetType }, sourceType, true );

      var il = dynamicMethod.GetILGenerator();

      foreach( var map in GetMatchingProperties( sourceType, targetType ) )
      {
        il.Emit( OpCodes.Ldarg_1 );
        il.Emit( OpCodes.Ldarg_0 );
        il.EmitCall( OpCodes.Callvirt, map._sourceProperty.GetGetMethod(), null );
        il.EmitCall( OpCodes.Callvirt, map._targetProperty.GetSetMethod(), null );
      }

      il.Emit( OpCodes.Ret );

      _map.GetOrAdd( key, dynamicMethod );
    }
    #endregion

    private class PropertyMap
    {
      #region .Fields
      public PropertyInfo _sourceProperty;
      public PropertyInfo _targetProperty;
      #endregion
    }
  }
}
