using ST.Utils.Attributes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;

namespace ST.Utils
{
  /// <summary>
  /// Еще один класс копирования свойств объекта, с поддержкой исключающих свойств и с посиком не по строке, а по паре типов TSrc и TDst
  /// </summary>
  public static class PropertyCopier
  {
    #region .Static Fields
    private static readonly ConcurrentDictionary<TypeMapping, Delegate> _propsCopiers = new ConcurrentDictionary<TypeMapping, Delegate>();
    private static readonly ConcurrentDictionary<Type, string[]> _propsExcluded = new ConcurrentDictionary<Type, string[]>();
    #endregion

    /// <summary>
    /// Метод регистрации свойств, которые надо исключить при копировании
    /// </summary>
    /// <param name="types">Перечень типов, для которых задается перечень исключенных свойств</param>
    /// <param name="excludedPropNames">Исключаемые при копированиии свойства</param>
    public static void RegisterExcludedProps( Type[] types, string[] excludedPropNames )
    {
      excludedPropNames = excludedPropNames.Distinct( StringComparer.InvariantCultureIgnoreCase ).ToArray();
      types.ForEach( type => _propsExcluded.AddOrUpdate( type, excludedPropNames, ( t, o ) => excludedPropNames ) );
    }

    private static bool IsPropExcluded<TType>( string propName )
    {
      return IsPropExcluded( typeof( TType ), propName );
    }

    private static bool IsPropExcluded( Type TType, string propName )
    {
      string[] excludedProps;
      if ( _propsExcluded.TryGetValue( TType, out excludedProps ) )
        return excludedProps.Contains( propName, StringComparer.InvariantCultureIgnoreCase );
      return false;
    }

    #region FastCopyTo
    /// <summary>
    /// Копирование свойств объекта src в объект dst (копирование свойств, совпадающих по имени без учета регистра)
    /// </summary>
    /// <param name="src">Объект, чьи свойства должны быть скопированы в TDst dst</param>
    /// <param name="dst">Объект, в который должны быть скопированы значения свойств TSrc src</param>
    /// <returns>dst</returns>
    public static object FastCopyTo( this object src, object dst )
    {
      if ( src != null && dst != null && !object.ReferenceEquals( src, dst ) )
      {
        var copier = GetCopier( src.GetType(), dst.GetType() );
        copier( src, dst );
      }
      return dst;
    }
    #endregion

    #region FastClone
    /// <summary>
    /// Клонирование объекта
    /// </summary>
    /// <typeparam name="TSrc">Тип колнируемого объекта</typeparam>
    /// <param name="src">Объект, чьи свойства должны быть склонированы</param>
    /// <returns>Новый, склонированный из src, объект</returns>
    public static TSrc FastClone<TSrc>( this TSrc src )
      where TSrc : class, new()
    {
      if ( src != null )
      {
        object dst = Runtime.GetCreator( src.GetType() )();//Activator.CreateInstance( src.GetType() );
        return (TSrc)FastCopyTo( src, dst );
      }
      return null;
    }

    /// <summary>
    /// Клонирование объекта
    /// </summary>
    /// <typeparam name="TSrc">Тип колнируемого объекта</typeparam>
    /// <param name="src">Объект, чьи свойства должны быть склонированы</param>
    /// <param name="creator">Функция создания объекта</param>
    /// <returns>Новый, склонированный из src, объект</returns>
    public static TSrc FastClone<TSrc>( this TSrc src, Func<TSrc, TSrc> creator )
      where TSrc : class
    {
      if ( src != null )
      {
        object dst = creator( src );
        return (TSrc)FastCopyTo( src, dst );
      }
      return null;
    }

    /// <summary>
    /// Клонирование списка объектов
    /// </summary>
    /// <typeparam name="TSrc">Тип объекта, чьи свойства должны быть склонированы</typeparam>
    /// <param name="srcList">Объекты, чьи свойства должны быть склонированы</param>
    /// <returns>Список новых, склонированных из TSrc src, объектов</returns>
    public static TSrc[] FastCloneList<TSrc>( this IEnumerable<TSrc> srcList )
      where TSrc : class, new()
    {
      if ( srcList != null )
        return srcList.Select( src => src.FastClone() ).OfType<TSrc>().ToArray();
      return null;
    }
    #endregion

    #region GetCopier
    private static Action<object, object> GetCopier( Type TSrc, Type TDst )
    {
      return _propsCopiers.GetOrAdd( new TypeMapping( TSrc, TDst ), tt =>
      {
        var membersSrc = tt.Item1.GetProperties()
          .Where( p => p.CanRead && p.GetIndexParameters().Length == 0 && !IsPropExcluded( tt.Item1, p.Name ) && !p.IsDefined<CopyIgnoreAttribute>() ).ToArray()
          .Concat( tt.Item1.GetMembers( BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance ).Where( m => m.MemberType.In( MemberTypes.Field, MemberTypes.Property ) && m.IsDefined<CopyExplisitlyAttribute>() ).ToArray() )
          .Distinct( m => m.Name )
          .ToArray();
        var membersDst = tt.Item2.GetProperties()
          .Where( p => p.CanWrite && p.GetIndexParameters().Length == 0 && !IsPropExcluded( tt.Item2, p.Name ) && !p.IsDefined<CopyIgnoreAttribute>() ).ToArray()
          .Concat( tt.Item2.GetMembers( BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance ).Where( m => m.MemberType.In( MemberTypes.Field, MemberTypes.Property ) && m.IsDefined<CopyExplisitlyAttribute>() ).ToArray() )
          .Distinct( m => m.Name )
          .ToArray(); ;
        var members = membersSrc.Join( membersDst, s => s.Name, d => d.Name, ( s, d ) => new
        {
          ms = s,
          md = d,
          ord = s.IsDefined<CopyOrderAttribute>( false ) ? s.GetAttribute<CopyOrderAttribute>( false ).Order : d.IsDefined<CopyOrderAttribute>( false ) ? d.GetAttribute<CopyOrderAttribute>( false ).Order : uint.MaxValue
        } ).OrderBy( p => p.ord ).ToArray();

        var epSrc = Expression.Parameter( typeof( object ) );
        var epDst = Expression.Parameter( typeof( object ) );
        var eSrc = Expression.Convert( epSrc, tt.Item1 );
        var eDst = Expression.Convert( epDst, tt.Item2 );
        var eBlockList = new List<Expression>( members.Length );

        //var mDbgWrLn = MemberHelper.GetMethod( () => System.Diagnostics.Debug.WriteLine( "123" ) );
        //eBlockList.Add( Expression.Call( mDbgWrLn, Expression.Constant( "--- Copy ---" ) ) );
        foreach ( var m in members )
        {
          if ( m.ms is PropertyInfo )
          {
            var piS = m.ms as PropertyInfo;
            var piD = m.md as PropertyInfo;

            var getMethod = piS.GetGetMethod( true );
            var setMethod = piD.GetSetMethod( true ) ?? piD.DeclaringType.GetProperty( piD.Name, BindingFlags.Instance | BindingFlags.Public ).GetSetMethod( true );

            if ( getMethod != null && setMethod != null )
              eBlockList.Add( Expression.Call( eDst, setMethod, Expression.Convert( Expression.Call( eSrc, getMethod ), piD.PropertyType ) ) );
          }
          else
          {
            var fiS = m.ms as FieldInfo;
            var fiD = m.md as FieldInfo;

            eBlockList.Add( Expression.Assign( Expression.Field( eDst, fiD ), Expression.Field( eSrc, fiS ) ) );
          }
        }
        if ( eBlockList.Count == 0 )
          throw new Exception( string.Format( "Нет свойств/полей для копирования из '{0}' в '{1}'", tt.Item1.FullName, tt.Item2.FullName ) );
        var eBlock = Expression.Block( eBlockList );
        Type actType = Expression.GetActionType( typeof( object ), typeof( object ) );
        var asgn = Expression.Lambda( actType, eBlock, epSrc, epDst );

        return asgn.Compile();
      } ) as Action<object, object>;
    }
    #endregion

    #region TypeType
    private sealed class TypeMapping : Tuple<Type, Type>
    {
      public TypeMapping( Type t1, Type t2 )
        : base( t1, t2 )
      { }
    }
    #endregion

  }
}
