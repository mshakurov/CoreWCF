using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using ST.Utils;
using ST.Utils.Collections;

namespace ST.Utils
{
  public static class ObjectTimeConverter
  {
    #region .Fields
    private static readonly ConcurrentDictionary<Type, Func<object, bool, object>> _converters = new ConcurrentDictionary<Type, Func<object, bool, object>>();

    private static MethodInfo mToLocalTime = MemberHelper.GetMethod<DateTime>( dt => dt.ToLocalTime() ); //typeof( DateTime ).GetMethod( "ToLocalTime" );
    private static MethodInfo mToUniversalTime = MemberHelper.GetMethod<DateTime>( dt => dt.ToUniversalTime() ); //typeof( DateTime ).GetMethod( "ToUniversalTime" );
    private static PropertyInfo piValue = typeof( Nullable<DateTime> ).GetProperty( "Value" );
    #endregion

    #region PropertyInfo
    private static PropertyInfo[] GetProps( Type objectType )
    {
      return objectType.GetProperties( BindingFlags.Instance | BindingFlags.Public ).Where( pi => pi.CanRead && pi.CanWrite && (pi.PropertyType == typeof( DateTime ) || pi.PropertyType == typeof( Nullable<DateTime> ) /*|| pi.PropertyType == typeof( TimeSpan ) || pi.PropertyType == typeof( Nullable<TimeSpan> ) */) && pi.GetIndexParameters().Length == 0 ).ToArray();
    }
    #endregion

    #region Converter
    private static Func<object, bool, object> GetConverter( Type objectType )
    {
      var props = GetProps( objectType );
      if( props.Length == 0 )
        return ( obj, toLocalTime ) => obj;
      var epObj = Expression.Parameter( typeof( object ) );
      var eObj = Expression.Convert( epObj, objectType );
      var epToLT = Expression.Parameter( typeof( bool ) );
      var eNull = Expression.Constant( null );
      var eMinValue = Expression.Constant( DateTime.MinValue, typeof( DateTime ) );
      var eMaxValue = Expression.Constant( DateTime.MaxValue, typeof( DateTime ) );

      var eBlockList = new List<Expression>( props.Length );
      foreach( var pi in props )
      {
        var getMethod = pi.GetGetMethod( true );
        var setMethod = pi.GetSetMethod( true ) ?? pi.DeclaringType.GetProperty( pi.Name, BindingFlags.Instance | BindingFlags.Public ).GetSetMethod( true );

        if( getMethod != null && setMethod != null )
        {
          var eValue = Expression.Convert( Expression.Call( eObj, getMethod ), pi.PropertyType );
          if( pi.PropertyType == typeof( Nullable<DateTime> ) )
          {
            var eValueValue = Expression.MakeMemberAccess( eValue, piValue );
            var eConv = Expression.IfThen(
              Expression.AndAlso( Expression.NotEqual( eValue, eNull ),
                Expression.AndAlso( Expression.NotEqual( eValueValue, eMinValue ), Expression.NotEqual( eValueValue, eMaxValue ) ) ),
                Expression.IfThenElse( epToLT,
                  Expression.Call( eObj, setMethod, Expression.Convert( Expression.Call( eValueValue, mToLocalTime ), pi.PropertyType ) ),
                  Expression.Call( eObj, setMethod, Expression.Convert( Expression.Call( eValueValue, mToUniversalTime ), pi.PropertyType ) )
                  )
              );
            eBlockList.Add( eConv );
          }
          else
          {
            var eConv = Expression.IfThen(
                Expression.AndAlso( Expression.NotEqual( eValue, eMinValue ), Expression.NotEqual( eValue, eMaxValue ) ),
                Expression.IfThenElse( epToLT,
                  Expression.Call( eObj, setMethod, Expression.Convert( Expression.Call( eValue, mToLocalTime ), pi.PropertyType ) ),
                  Expression.Call( eObj, setMethod, Expression.Convert( Expression.Call( eValue, mToUniversalTime ), pi.PropertyType ) )
                  )
              );
            eBlockList.Add( eConv );
          }

        }
      }
      eBlockList.Add( Expression.Convert( eObj, typeof( object ) ) );

      var eBlock = Expression.Block( eBlockList );
      Type funcType = Expression.GetFuncType( typeof( object ), typeof( bool ), typeof( object ) );
      var leEq = Expression.Lambda( funcType, eBlock, epObj, epToLT );
      return leEq.Compile() as Func<object, bool, object>;
    }

    private static Func<object, bool, object> CacheConverter( Type objectType )
    {
      return _converters.GetOrAdd( objectType, tp => GetConverter( tp ) );
    }
    #endregion

    /// <summary>
    /// Конвертирует время всех get|set Property с типом DteTime
    /// </summary>
    /// <param name="obj">Объект, чьи свойства конвертируются</param>
    /// <param name="toLocalTime">True - В локальное время, инчае в UTC</param>
    /// <returns>Объект, поданный на входе</returns>
    public static TSrc ConvertTimeValues<TSrc>( this TSrc obj, bool toLocalTime )
    {
      if( obj != null )
        return (TSrc) (CacheConverter( obj.GetType() )( obj, toLocalTime ));
      return obj;
    }

    public static IEnumerable<TSrc> ConvertTimeValuesForAll<TSrc>( this IEnumerable<TSrc> objects, bool toLocalTime )
    {
      if ( objects == null )
        return objects;
      foreach( var obj in objects )
        obj.ConvertTimeValues( toLocalTime );
      return objects;
    }
  }
}
