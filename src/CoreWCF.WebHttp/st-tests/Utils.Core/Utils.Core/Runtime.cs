using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ST.Utils.Attributes;

namespace ST.Utils
{
  /// <summary>
  /// Вспомогательный класс для манипуляции объектами в runtime'е.
  /// </summary>
  public static class Runtime
  {
    #region .Static Fields
    private static readonly ConcurrentDictionary<Type, Func<object>> _creators = new ConcurrentDictionary<Type, Func<object>>();
    private static readonly ConcurrentDictionary<Type, Delegate> _copiers = new ConcurrentDictionary<Type, Delegate>();
    #endregion

    #region CastUp
    /// <summary>
    /// Создает экземпляр базового типа на основе значений открытых свойств экземпляра унаследованного типа.
    /// Дублирует метод ConvertUp во избежании поиска перегруженной версии последнего с использованием Reflection.
    /// </summary>
    /// <typeparam name="TDerived">Унаследованный тип.</typeparam>
    /// <typeparam name="TBase">Базовый тип.</typeparam>
    /// <param name="src">Экземпляр унаследованного типа.</param>
    /// <returns>Экземпляр базового типа.</returns>
    public static TBase CastUp<TDerived, TBase>( this TDerived src )
      where TBase : class, new()
      where TDerived : class, TBase
    {
      return src.ConvertUp( new TBase() );
    }
    #endregion

    #region CastUpWithInstance
    /// <summary>
    /// Создает экземпляр базового типа на основе значений открытых свойств экземпляра унаследованного типа.
    /// Дублирует метод ConvertUp во избежании поиска перегруженной версии последнего с использованием Reflection.
    /// </summary>
    /// <typeparam name="TDerived">Унаследованный тип.</typeparam>
    /// <typeparam name="TBase">Базовый тип.</typeparam>
    /// <param name="src">Экземпляр унаследованного типа.</param>
    /// <param name="dest">Экземпляр базового типа.</param>
    /// <returns>Экземпляр базового типа.</returns>
    public static TBase CastUpWithInstance<TDerived, TBase>( this TDerived src, TBase dest )
      where TBase : class
      where TDerived : class, TBase
    {
      return src.CopyFast( dest );
    }
    #endregion

    #region ConvertDown
    /// <summary>
    /// Создает экземпляр унаследованного типа на основе значений открытых свойств экземпляра базового типа.
    /// </summary>
    /// <typeparam name="TBase">Базовый тип.</typeparam>
    /// <typeparam name="TDerived">Унаследованный тип.</typeparam>
    /// <param name="src">Экземпляр базового типа.</param>
    /// <returns>Экземпляр унаследованного типа.</returns>
    public static TDerived ConvertDown<TBase, TDerived>( this TBase src )
      where TBase : class
      where TDerived : class, TBase, new()
    {
      return src.ConvertDown( new TDerived() );
    }

    /// <summary>
    /// Создает экземпляр унаследованного типа на основе значений открытых свойств экземпляра базового типа. Предоставляется возможность поработать с экземпляром после его создания.
    /// </summary>
    /// <typeparam name="TBase">Базовый тип.</typeparam>
    /// <typeparam name="TDerived">Унаследованный тип.</typeparam>
    /// <param name="src">Экземпляр базового типа.</param>
    /// <param name="setter">Метод пост обработки результирующего объекта.</param>
    /// <returns>Экземпляр унаследованного типа.</returns>
    public static TDerived ConvertDown<TBase, TDerived>( this TBase src, Action<TDerived> setter )
      where TBase : class
      where TDerived : class, TBase, new()
    {
      var derived = src.ConvertDown( new TDerived() );
      setter( derived );
      return derived;
    }

    /// <summary>
    /// Создает экземпляр унаследованного типа на основе значений открытых свойств экземпляра базового типа.
    /// </summary>
    /// <typeparam name="TBase">Базовый тип.</typeparam>
    /// <typeparam name="TDerived">Унаследованный тип.</typeparam>
    /// <param name="src">Экземпляр базового типа.</param>
    /// <param name="dest">Экземпляр унаследованного типа.</param>
    /// <returns>Экземпляр унаследованного типа.</returns>
    public static TDerived ConvertDown<TBase, TDerived>( this TBase src, TDerived dest )
      where TBase : class
      where TDerived : class, TBase
    {
      return src.CopyFast( dest ) as TDerived;
    }

    /// <summary>
    /// Создает экземпляры унаследованного типа на основе значений открытых свойств экземпляров базового типа.
    /// Данный метод следует вызывать используя явное указание на то, что базовый тип реализует интерфейс IEnumerable&lt;T&gt; для
    /// того, чтобы компилятор не вызывал данный перегруженный метод для одиночного объекта, например:
    /// objList.ConvertDown&lt;string&gt;() или objList.AsEnumerable().ConvertDown().
    /// </summary>
    /// <typeparam name="TBase">Базовый тип.</typeparam>
    /// <typeparam name="TDerived">Унаследованный тип.</typeparam>
    /// <param name="src">Список экземпляров базового типа.</param>
    /// <returns>Список экземпляров унаследованного типа.</returns>
    public static List<TDerived> ConvertDown<TBase, TDerived>( this IEnumerable<TBase> src )
      where TBase : class
      where TDerived : class, TBase, new()
    {
      return src == null ? null : src.Select( obj => obj.ConvertDown<TBase, TDerived>() ).ToList();
    }

    /// <summary>
    /// Создает экземпляры унаследованного типа на основе значений открытых свойств экземпляров базового типа.
    /// Данный метод следует вызывать используя явное указание на то, что базовый тип реализует интерфейс IEnumerable&lt;T&gt; для
    /// того, чтобы компилятор не вызывал данный перегруженный метод для одиночного объекта, например:
    /// objList.ConvertDown&lt;string&gt;() или objList.AsEnumerable().ConvertDown().
    /// </summary>
    /// <typeparam name="TBase">Базовый тип.</typeparam>
    /// <typeparam name="TDerived">Унаследованный тип.</typeparam>
    /// <param name="src">Список экземпляров базового типа.</param>
    /// <param name="creator">Метод, создающий объект-приемник.</param>
    /// <returns>Список экземпляров унаследованного типа.</returns>
    public static List<TDerived> ConvertDown<TBase, TDerived>( this IEnumerable<TBase> src, Func<TBase, TDerived> creator )
      where TBase : class
      where TDerived : class, TBase
    {
      return src == null || creator == null ? null : src.Select( obj => obj.ConvertDown<TBase, TDerived>( creator( obj ) ) ).ToList();
    }
    #endregion

    #region ConvertUp
    /// <summary>
    /// Создает экземпляр базового типа на основе значений открытых свойств экземпляра унаследованного типа.
    /// </summary>
    /// <typeparam name="TDerived">Унаследованный тип.</typeparam>
    /// <typeparam name="TBase">Базовый тип.</typeparam>
    /// <param name="src">Экземпляр унаследованного типа.</param>
    /// <returns>Экземпляр базового типа.</returns>
    public static TBase ConvertUp<TDerived, TBase>( this TDerived src )
      where TBase : class, new()
      where TDerived : class, TBase
    {
      return src.ConvertUp( new TBase() );
    }

    /// <summary>
    /// Создает экземпляр базового типа на основе значений открытых свойств экземпляра унаследованного типа.
    /// </summary>
    /// <typeparam name="TDerived">Унаследованный тип.</typeparam>
    /// <typeparam name="TBase">Базовый тип.</typeparam>
    /// <param name="src">Экземпляр унаследованного типа.</param>
    /// <param name="dest">Экземпляр базового типа.</param>
    /// <returns>Экземпляр базового типа.</returns>
    public static TBase ConvertUp<TDerived, TBase>( this TDerived src, TBase dest )
      where TBase : class
      where TDerived : class, TBase
    {
      return src.CopyFast( dest );
    }

    /// <summary>
    /// Создает экземпляры базового типа на основе значений открытых свойств экземпляров унаследованного типа.
    /// Данный метод следует вызывать используя явное указание на то, что базовый тип реализует интерфейс IEnumerable&lt;T&gt; для
    /// того, чтобы компилятор не вызывал данный перегруженный метод для одиночного объекта, например:
    /// objList.ConvertUp&lt;string&gt;() или objList.AsEnumerable().ConvertUp().
    /// </summary>
    /// <typeparam name="TDerived">Унаследованный тип.</typeparam>
    /// <typeparam name="TBase">Базовый тип.</typeparam>
    /// <param name="src">Список экземпляров унаследованного типа.</param>
    /// <returns>Список экземпляров базового типа.</returns>
    public static List<TBase> ConvertUp<TDerived, TBase>( this IEnumerable<TDerived> src )
      where TBase : class, new()
      where TDerived : class, TBase
    {
      return src == null ? null : src.Select( obj => obj.ConvertUp<TDerived, TBase>() ).ToList();
    }

    /// <summary>
    /// Создает экземпляры базового типа на основе значений открытых свойств экземпляров унаследованного типа.
    /// Данный метод следует вызывать используя явное указание на то, что базовый тип реализует интерфейс IEnumerable&lt;T&gt; для
    /// того, чтобы компилятор не вызывал данный перегруженный метод для одиночного объекта, например:
    /// objList.ConvertUp&lt;string&gt;() или objList.AsEnumerable().ConvertUp().
    /// </summary>
    /// <typeparam name="TDerived">Унаследованный тип.</typeparam>
    /// <typeparam name="TBase">Базовый тип.</typeparam>
    /// <param name="src">Список экземпляров унаследованного типа.</param>
    /// <param name="creator">Метод, создающий объект-приемник.</param>
    /// <returns>Список экземпляров базового типа.</returns>
    public static List<TBase> ConvertUp<TDerived, TBase>( this IEnumerable<TDerived> src, Func<TDerived, TBase> creator )
      where TBase : class
      where TDerived : class, TBase
    {
      return src == null || creator == null ? null : src.Select( obj => obj.ConvertUp<TDerived, TBase>( creator( obj ) ) ).ToList();
    }
    #endregion

    #region CopyFast
    /// <summary>
    /// Создает объект-приемник и копирует в него значения открытых свойств объекта-источника.
    /// </summary>
    /// <typeparam name="T">Тип объекта.</typeparam>
    /// <param name="src">Объект-источник.</param>
    /// <returns>Объект-приемник.</returns>
    [DebuggerStepThrough]
    public static T CopyFast<T>( this T src )
      where T : class, new()
    {
      return src.CopyFast<T>( new T() );
    }

    /// <summary>
    /// Копирует значения открытых свойств из объекта-источника в объект-приемник.
    /// </summary>
    /// <typeparam name="T">Тип объекта.</typeparam>
    /// <param name="src">Объект-источник.</param>
    /// <param name="dest">Объект-приемник.</param>
    /// <returns>Объект-приемник.</returns>
    [DebuggerStepThrough]
    public static T CopyFast<T>( this T src, T dest )
      where T : class
    {
      if( src != null && dest != null )
        RuntimeInternal<T>.Copy( src, dest );

      return dest;
    }

    /// <summary>
    /// Копирует значения открытых свойств из объектов-источников в объекты-приемники.
    /// Данный метод следует вызывать используя явное указание на то, что тип объекта реализует интерфейс IEnumerable&lt;T&gt; для
    /// того, чтобы компилятор не вызывал данный перегруженный метод для одиночного объекта, например:
    /// objList.CopyFast&lt;string&gt;() или objList.AsEnumerable().CopyFast().
    /// </summary>
    /// <typeparam name="T">Тип объекта.</typeparam>
    /// <param name="src">Список объектов-источников.</param>
    /// <returns>Список объектов-приемников.</returns>
    public static List<T> CopyFast<T>( this IEnumerable<T> src )
      where T : class, new()
    {
      return src == null ? null : src.Select( obj => obj.CopyFast<T>() ).ToList();
    }

    /// <summary>
    /// Копирует значения открытых свойств из объектов-источников в объекты-приемники.
    /// Данный метод следует вызывать используя явное указание на то, что тип объекта реализует интерфейс IEnumerable&lt;T&gt; для
    /// того, чтобы компилятор не вызывал данный перегруженный метод для одиночного объекта, например:
    /// objList.CopyFast&lt;string&gt;() или objList.AsEnumerable().CopyFast().
    /// </summary>
    /// <typeparam name="T">Тип объекта.</typeparam>
    /// <param name="src">Список объектов-источников.</param>
    /// <param name="creator">Метод, создающий объект-приемник.</param>
    /// <returns>Список объектов-приемников.</returns>
    public static List<T> CopyFast<T>( this IEnumerable<T> src, Func<T, T> creator )
      where T : class
    {
      return src == null || creator == null ? null : src.Select( obj => obj.CopyFast<T>( creator( obj ) ) ).ToList();
    }
    #endregion

    #region CreateFast
    /// <summary>
    /// Создает объект указанного типа.
    /// Тип объекта должен поддерживать открытый конструктор без параметров.
    /// </summary>
    /// <param name="type">Тип объекта.</param>
    /// <returns>Объект указанного типа.</returns>
    [DebuggerStepThrough]
    public static object CreateFast( [NotNull] this Type type )
    {
      return GetCreator( type )();
    }
    #endregion

    #region GetCopier
    private static Action<T, T> GetCopier<T>()
      where T : class
    {
      return (_copiers.GetOrAdd( typeof( T ), t =>
      {
        var dm = t.IsInterface ? new DynamicMethod( "", null, new[] { t, t }, t.Module, true ) : new DynamicMethod( "", null, new[] { t, t }, t, true );

        var g = dm.GetILGenerator();

        var props = MemberHelper.GetProperties<T>();

        var propsOrdered = props.Where( p => p.IsDefined<CopyOrderAttribute>( false ) ).OrderBy( p => p.GetAttribute<CopyOrderAttribute>( false )?.Order );
        var propsNotOrdered = props.Where( p => !p.IsDefined<CopyOrderAttribute>( false ) ).OrderBy( p => p.Name );

        foreach( var pi in propsOrdered.Union( propsNotOrdered ) )
          if( pi.GetIndexParameters().Length == 0 )
          {
            var getMethod = pi.GetGetMethod( true );
            var setMethod = pi.GetSetMethod( true ) ?? pi.DeclaringType?.GetProperty( pi.Name, BindingFlags.Instance | BindingFlags.Public )?.GetSetMethod( true );

            if( getMethod != null && setMethod != null )
            {
              g.Emit( OpCodes.Ldarg_1 );
              g.Emit( OpCodes.Ldarg_0 );
              g.Emit( OpCodes.Callvirt, getMethod );
              g.Emit( OpCodes.Callvirt, setMethod );
            }
          }

        g.Emit( OpCodes.Ret );

        return dm.CreateDelegate( typeof( Action<T, T> ) );
      } ) as Action<T, T>);
    }
    #endregion

    #region GetCreator
    /// <summary>
    /// Возвращает функцию для быстрого создания объекта.
    /// Тип объекта должен поддерживать открытый конструктор без параметров.
    /// </summary>
    /// <param name="type">Тип создаваемого объекта.</param>
    /// <returns>Функция для создания объекта.</returns>
    [DebuggerStepThrough]
    public static Func<object> GetCreator( [NotNull] Type type )
    {
      return _creators.GetOrAdd( type, t =>
      {
        var ctor = t.GetConstructor( Type.EmptyTypes );

        if( !t.IsValueType && ctor == null )
        {
          Func<object> m = () => { throw new InvalidOperationException( "The type '" + t.FullName + "' does not support public constructor without parameters." ); };

          return m;
        }

        var dm = new DynamicMethod( "", typeof( object ), null, t, true );

        var g = dm.GetILGenerator();

        if( t.IsValueType )
        {
          var loc = g.DeclareLocal( t );

          g.Emit( OpCodes.Ldloca_S, loc );
          g.Emit( OpCodes.Initobj, t );
          g.Emit( OpCodes.Ldloc_0 );
          g.Emit( OpCodes.Box, t );
          g.Emit( OpCodes.Ret );
        }
        else
        {
          g.Emit( OpCodes.Newobj, ctor );
          g.Emit( OpCodes.Ret );
        }

        return dm.CreateDelegate( typeof( Func<object> ) ) as Func<object>;
      } );
    }
    #endregion

    private static class RuntimeInternal<T>
      where T : class
    {
      #region .Static Fields
      public static readonly Action<T, T> Copy;
      #endregion

      #region .Ctor
      static RuntimeInternal()
      {
        Copy = GetCopier<T>();
      }
      #endregion
    }
  }
}
