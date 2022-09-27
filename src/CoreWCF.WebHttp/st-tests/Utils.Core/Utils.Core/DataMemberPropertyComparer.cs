using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;

namespace ST.Utils
{
  /// <summary>
  /// Класс для сравнения свойств двух объектов, помеченных DataContract. Сравниваются только Property: помеченные DataMember, CanRead и GetIndexParameters().Length == 0
  /// </summary>
  public static class DataMemberPropertyComparer
  {
    private static readonly ConcurrentDictionary<Type, Delegate> _comparers = new ConcurrentDictionary<Type, Delegate>();
    private static readonly MethodInfo miEqIsEqualNullable = MemberHelper.GetMethod( () => Extensions.IsEqualNullable( null, null ) );

    /// <summary>
    /// Равны ли значения всех свойств двух объектов (свойства с CanRead и GetIndexParameters().Length == 0) 
    /// </summary>
    /// <typeparam name="T">Тип, чьи свойства сравниваются</typeparam>
    /// <param name="one">1-й объект</param>
    /// <param name="second">2-й объект</param>
    /// <returns>True, если все свойства равны</returns>
    public static bool EqualsDataMemberProperties<T>( this T one, T second )
      where T : class
    {
      if ( one == null
        || second == null )
        return false;
      return GetComparer( typeof( T ) ).IfNotNull( comparer => comparer( one, second ) );
    }

    public static Func<object, object, bool> GetComparer( Type type )
    {
      return _comparers.GetOrAdd( type, _ =>
        {
          if ( !type.IsDefined<DataContractAttribute>( true ) )
            return null;

          var props = type.GetProperties().Where( p => p.CanRead && p.GetIndexParameters().Length == 0 && p.IsDefined<DataMemberAttribute>( true ) ).ToList();

          var epOne = Expression.Parameter( typeof( object ) );
          var epSecond = Expression.Parameter( typeof( object ) );
          var eOne = Expression.Convert( epOne, type );
          var eSecond = Expression.Convert( epSecond, type );
          BinaryExpression eAnd = null;

          foreach ( var pi in props )
          {
            var getMethod = pi.GetGetMethod( true );

            if ( getMethod != null )
            {
              var eq = Expression.Equal( Expression.Convert( Expression.Call( eOne, getMethod ), typeof( object ) ), Expression.Convert( Expression.Call( eSecond, getMethod ), typeof( object ) ), false, miEqIsEqualNullable );
              if ( eAnd == null )
                eAnd = eq;
              else
                eAnd = Expression.AndAlso( eAnd, eq );
            }
          }

          Type funcType = Expression.GetFuncType( typeof( object ), typeof( object ), typeof( bool ) );
          var leEq = Expression.Lambda( funcType, eAnd, epOne, epSecond );
          return leEq.Compile();

        } ) as Func<object, object, bool>;
    }

  }
}
