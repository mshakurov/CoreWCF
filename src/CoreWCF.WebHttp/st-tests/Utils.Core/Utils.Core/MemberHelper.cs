using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using ST.Utils.Attributes;

namespace ST.Utils
{
  /// <summary>
  /// Вспомогательный класс для получения информации о членах класса.
  /// </summary>
  public static class MemberHelper
  {
    #region GetField
    /// <summary>
    /// Возвращает информацию о статическом поле.
    /// </summary>
    /// <typeparam name="T">Тип поля.</typeparam>
    /// <param name="e">Лямбда-выражение, содержащее обращение к полю.</param>
    /// <returns>Информация о поле.</returns>
    [DebuggerStepThrough]
    public static FieldInfo GetField<T>( Expression<Func<T>> e )
    {
      return GetInfo( e.Body ) as FieldInfo;
    }

    /// <summary>
    /// Возвращает информацию об экземплярном поле объекта.
    /// </summary>
    /// <typeparam name="T">Тип объекта.</typeparam>
    /// <typeparam name="TValue">Тип поля.</typeparam>
    /// <param name="e">Лямбда-выражение, содержащее обращение к полю.</param>
    /// <returns>Информация о поле.</returns>
    [DebuggerStepThrough]
    public static FieldInfo GetField<T, TValue>( Expression<Func<T, TValue>> e )
    {
      return GetInfo( e.Body ) as FieldInfo;
    }
    #endregion

    #region GetFields
    /// <summary>
    /// Возвращает список всех открытых полей типа.
    /// </summary>
    /// <typeparam name="T">Тип.</typeparam>
    /// <returns>Список открытых полей.</returns>
    [DebuggerStepThrough]
    public static ReadOnlyCollection<FieldInfo> GetFields<T>()
      where T : class
    {
      return Members<T>.Fields;
    }
    #endregion

    #region GetGenericArguments
    /// <summary>
    /// Возвращает типы параметров базового обобщенного типа, на основе которого сконструирован указанный родительский тип.
    /// </summary>
    /// <param name="type">Родительский тип.</param>
    /// <param name="baseType">Базовый обобщенный тип.</param>
    /// <returns>Массив типов обобщенных параметров.</returns>
    public static Type[] GetGenericArguments( [NotNull] this Type type, [NotNull] Type baseType )
    {
      Type t = null;

      if( baseType.IsGenericTypeDefinition && type != baseType )
        if( baseType.IsInterface )
          t = type.GetInterfaces().FirstOrDefault( i => i.IsGenericType && i.GetGenericTypeDefinition() == baseType );
        else
          while( type != null )
          {
            if( type.IsGenericType && type.GetGenericTypeDefinition() == baseType )
            {
              t = type;

              break;
            }

            type = type.BaseType;
          }

      return t?.GetGenericArguments() ?? Array.Empty<Type>();
    }
    #endregion

    #region GetGetter
    /// <summary>
    /// Возвращает делегат для получения значения свойства объекта.
    /// </summary>
    /// <typeparam name="R">Тип свойства.</typeparam>
    /// <param name="obj">Объект в котором определено свойство.</param>
    /// <param name="property">Название свойства.</param>
    /// <returns>Делегат.</returns>
    [DebuggerStepThrough]
    public static Func<R> GetGetter<R>( [NotNull] this object obj, [NotNull] string property )
    {
      return () => AccessorCache.GetGetter<R>( obj.GetType(), property )( obj );
    }
    #endregion

    #region GetInfo
    [DebuggerStepThrough]
    private static MemberInfo GetInfo( Expression e )
    {
      if( e is UnaryExpression eu )
        e = eu.Operand;

      var member = e as MemberExpression;

      if( member != null )
        return member.Member;

      var method = e as MethodCallExpression;

      if( method != null )
        return method.Method;

      throw new ArgumentException( "'" + e + "' is not a field, property or method." );
    }
    #endregion

    #region GetMemberInfo
    /// <summary>
    /// Возвращает информацию об элементе для указанного объекта.
    /// </summary>
    /// <param name="obj">Объект.</param>
    /// <returns>Информацию об элементе.</returns>
    public static MemberInfo GetMemberInfo( [NotNull] this object obj )
    {
      if (obj == null) return null;

      var mi = obj as MemberInfo;

      if( mi == null )
      {
        var t = obj.GetType();

        mi = t.IsEnum ? t.GetField( obj.ToString() ) : t as MemberInfo;
      }

      return mi;
    }
    #endregion

    #region GetMethod
    /// <summary>
    /// Возвращает информацию о статическом методе, который ничего не возвращает.
    /// </summary>
    /// <param name="e">Лямбда-выражение, содержащее обращение к методу.</param>
    /// <returns>Информация о методе.</returns>
    [DebuggerStepThrough]
    public static MethodInfo GetMethod( Expression<Action> e )
    {
      return GetInfo( e.Body ) as MethodInfo;
    }

    /// <summary>
    /// Возвращает информацию о статическом методе, который возвращает значение.
    /// </summary>
    /// <typeparam name="T">Тип возвращаемого методом значения.</typeparam>
    /// <param name="e">Лямбда-выражение, содержащее обращение к методу.</param>
    /// <returns>Информация о методе.</returns>
    [DebuggerStepThrough]
    public static MethodInfo GetMethod<T>( Expression<Func<T>> e )
    {
      return GetInfo( e.Body ) as MethodInfo;
    }

    /// <summary>
    /// Возвращает информацию об экземплярном методе объекта, который ничего не возвращает.
    /// </summary>
    /// <typeparam name="T">Тип объекта.</typeparam>
    /// <param name="e">Лямбда-выражение, содержащее обращение к методу.</param>
    /// <returns>Информация о методе.</returns>
    [DebuggerStepThrough]
    public static MethodInfo GetMethod<T>( Expression<Action<T>> e )
    {
      return GetInfo( e.Body ) as MethodInfo;
    }

    /// <summary>
    /// Возвращает информацию об экземплярном методе объекта, который возвращает значение.
    /// </summary>
    /// <typeparam name="T">Тип объекта.</typeparam>
    /// <typeparam name="TValue">Тип возвращаемого значения.</typeparam>
    /// <param name="e">Лямбда-выражение, содержащее обращение к методу.</param>
    /// <returns>Информация о методе.</returns>
    [DebuggerStepThrough]
    public static MethodInfo GetMethod<T, TValue>( Expression<Func<T, TValue>> e )
    {
      return GetInfo( e.Body ) as MethodInfo;
    }
    #endregion

    #region GetProperty
    /// <summary>
    /// Возвращает информацию о статическом свойстве.
    /// </summary>
    /// <typeparam name="T">Тип свойства.</typeparam>
    /// <param name="e">Лямбда-выражение, содержащее обращение к свойству.</param>
    /// <returns>Информация о свойстве.</returns>
    [DebuggerStepThrough]
    public static PropertyInfo GetProperty<T>( Expression<Func<T>> e )
    {
      return GetInfo( e.Body ) as PropertyInfo;
    }

    /// <summary>
    /// Возвращает информацию об экземплярном свойстве объекта.
    /// </summary>
    /// <typeparam name="T">Тип объекта.</typeparam>
    /// <typeparam name="TValue">Тип свойства.</typeparam>
    /// <param name="e">Лямбда-выражение, содержащее обращение к свойству.</param>
    /// <returns>Информация о свойстве.</returns>
    [DebuggerStepThrough]
    public static PropertyInfo GetProperty<T, TValue>( Expression<Func<T, TValue>> e )
    {
      // !!! Должно быть так:
      // return GetInfo( e.Body ) as PropertyInfo;
      // Но сраный баг: http://connect.microsoft.com/VisualStudio/feedback/details/554853/propertyinfo-retrieved-from-lambdaexpression-reports-wrong-reflectedtype-when-property-is-derived-from-another-class

      var property = GetInfo( e.Body ) as PropertyInfo;

      if( property != null )
        property = typeof( T ).GetProperty( property.Name );

      return property;
    }
    #endregion

    #region GetProperties
    /// <summary>
    /// Возвращает список всех открытых свойств типа.
    /// </summary>
    /// <typeparam name="T">Тип.</typeparam>
    /// <returns>Список открытых свойств.</returns>
    [DebuggerStepThrough]
    public static ReadOnlyCollection<PropertyInfo> GetProperties<T>()
      where T : class
    {
      return Members<T>.Properties;
    }
    #endregion

    #region GetSetter
    /// <summary>
    /// Возвращает делегат для установки значения свойства объекта.
    /// </summary>
    /// <typeparam name="V">Тип свойства.</typeparam>
    /// <param name="obj">Объект в котором определено свойство.</param>
    /// <param name="property">Название свойства.</param>
    /// <returns>Делегат.</returns>
    [DebuggerStepThrough]
    public static Action<V> GetSetter<V>( [NotNull] this object obj, [NotNull] string property )
    {
      return value => AccessorCache.GetSetter<V>( obj.GetType(), property )( obj, value );
    }

    /// <summary>
    /// Возвращает делегат для установки значения свойства объекта.
    /// </summary>
    /// <typeparam name="T">Тип объекта.</typeparam>
    /// <typeparam name="TValue">Тип свойства.</typeparam>
    /// <param name="e">Лямбда-выражение, содержащее обращение к свойству.</param>
    /// <returns>Делегат.</returns>
    [DebuggerStepThrough]
    public static Action<T, TValue> GetSetter<T, TValue>( Expression<Func<T, TValue>> e )
        where T : class
    {
      return AccessorCache.GetSetter( e );
    }
    #endregion

    #region GetUniqueToken
    /// <summary>
    /// Возвращает уникальный идентификатор описателя элемента типа.
    /// </summary>
    /// <param name="mi">Описатель элемента типа.</param>
    /// <returns>Уникальный идентификатор.</returns>
    public static ulong GetUniqueToken( [NotNull] this MemberInfo mi )
    {
      // Такая реализация приводит к ошибкам, т.к., например, MetadataToken унаследованного свойства совпадает с MetadataToken этого свойства в базовом классе.
      //return (((ulong) mi.Module.ModuleHandle.GetHashCode()) << 32) | ((uint) mi.MetadataToken);

      return ( ( (ulong)mi.Module.Assembly.GetHashCode() ) << 32 ) | ( (uint)( mi.ReflectedType.FullName + "." + mi.Name ).GetUniqueHash() );
    }
    #endregion

    #region IsInheritedFrom
    /// <summary>
    /// Проверяет, унаследован ли один тип от другого (включая обобщенные типы).
    /// </summary>
    /// <param name="type">Проверяемый тип.</param>
    /// <param name="baseType">Базовый тип.</param>
    /// <returns>True - тип type унаследован от baseType, иначе - False.</returns>
    public static bool IsInheritedFrom( [NotNull] this Type type, [NotNull] Type baseType )
    {
      if( baseType.IsGenericTypeDefinition ) // Не IsGeneric, т.к. IsAssignableFrom отрабатывает корректно для закрытых обобщенных типов.
        if( baseType.IsInterface )
          return type?.GetInterfaces().Any( i => i.IsGenericType && i.GetGenericTypeDefinition().IsAssignableFrom( baseType ) ) ?? false;
        else
        {
          while( type != null )
          {
            if( type.IsGenericType && type.GetGenericTypeDefinition().IsAssignableFrom( baseType ) )
              return true;

            type = type.BaseType;
          }

          return false;
        }

      return baseType.IsAssignableFrom( type );
    }
    #endregion

    public static object GetValue( string propertyOrField, object obj )
    {
      return GetValue( obj.GetType().GetMember( propertyOrField ).FirstOrDefault(), obj );
    }

    public static object GetValue( MemberInfo propertyOrField, object obj )
    {
      return propertyOrField.IfIs<PropertyInfo, object>( p => p.GetValue( obj, null ), () => propertyOrField.IfIs<FieldInfo, object>( f => f.GetValue( obj ) ) );
    }

    private static class Members<T>
      where T : class
    {
      #region .Static Fields
      internal static readonly ReadOnlyCollection<FieldInfo> Fields = typeof( T ).GetFields().ToList().AsReadOnly();
      internal static readonly ReadOnlyCollection<PropertyInfo> Properties = typeof( T ).GetProperties().ToList().AsReadOnly();

      internal static readonly ConcurrentDictionary<ulong, Delegate> PropertySetters = new ConcurrentDictionary<ulong, Delegate>();
      #endregion
    }

    private static class AccessorCache
    {
      #region .Static Fields
      private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, Delegate>> _getters = new ConcurrentDictionary<Type, ConcurrentDictionary<string, Delegate>>();
      private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, Delegate>> _setters = new ConcurrentDictionary<Type, ConcurrentDictionary<string, Delegate>>();
      #endregion

      #region GetGetter
      [DebuggerStepThrough]
      public static Func<object, R> GetGetter<R>( Type type, string property )
      {
        var dict = _getters.GetOrAdd( type, t => new ConcurrentDictionary<string, Delegate>( StringComparer.OrdinalIgnoreCase ) );

        return dict.GetOrAdd( property, p =>
        {
          Func<object, R> getter = obj => { throw new NotSupportedException( string.Format( "The Property '{0}' of type '{1}' does not exist in the type '{2}'.", property, typeof( R ), type ) ); };

          var prop = type.GetProperty( property );

          if( prop != null )
          {
            var obj = Expression.Parameter( typeof( object ), "object" );

            var expr = Expression.Property( Expression.TypeAs( obj, type ), prop );

            getter = Expression.Lambda<Func<object, R>>( expr, obj ).Compile();
          }

          return getter;
        } ) as Func<object, R>;
      }
      #endregion

      #region GetSetter
      [DebuggerStepThrough]
      public static Action<object, V> GetSetter<V>( Type type, string property )
      {
        var dict = _setters.GetOrAdd( type, t => new ConcurrentDictionary<string, Delegate>( StringComparer.OrdinalIgnoreCase ) );

        return dict.GetOrAdd( property, p =>
        {
          Action<object, V> setter = ( obj, value ) => { };

          var prop = type.GetProperty( property );

          if( prop != null )
          {
            var setMethod = prop.GetSetMethod();

            if( setMethod != null )
            {
              var obj = Expression.Parameter( typeof( object ), "object" );

              var value = Expression.Parameter( typeof( V ), "value" );

              var realObject = Expression.TypeAs( obj, type );

              var expr = Expression.Call( realObject, setMethod, value );

              setter = Expression.Lambda<Action<object, V>>( expr, obj, value ).Compile();
            }
          }

          return setter;
        } ) as Action<object, V>;
      }

      [DebuggerStepThrough]
      public static Action<T, TValue> GetSetter<T, TValue>( Expression<Func<T, TValue>> e )
        where T : class
      {
        var prop = GetProperty( e );

        if( prop == null )
          return null;

        return Members<T>.PropertySetters.GetOrAdd( prop.GetUniqueToken(), token =>
        {
          Action<T, TValue> setter = ( obj, value ) => { };

          var setMethod = prop.GetSetMethod();

          if( setMethod != null )
          {
            var obj = Expression.Parameter( typeof( T ), "object" );

            var value = Expression.Parameter( typeof( TValue ), "value" );

            var expr = Expression.Call( obj, setMethod, value );

            setter = Expression.Lambda<Action<T, TValue>>( expr, obj, value ).Compile();
          }

          return setter;
        } ) as Action<T, TValue>;
      }
      #endregion
    }
  }
}
