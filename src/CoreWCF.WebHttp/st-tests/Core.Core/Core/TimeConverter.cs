using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using ST.Utils;
using ST.Utils.Collections;

namespace ST.Core
{
  internal static class TimeConverter
  {
    #region .Constants
    private const int CACHED_CONVERTERS = 32;
    #endregion

    #region .Static Fields
    private static readonly MethodInfo _convertTimeValue = MemberHelper.GetMethod( () => TimeConverter.ConvertTimeValue( DateTime.MinValue, false ) );
    private static readonly MethodInfo _convertTimeNullableValue = MemberHelper.GetMethod( () => TimeConverter.ConvertTimeValue( null, false ) );
    private static readonly MethodInfo _convertValue = MemberHelper.GetMethod( () => TimeConverter.ConvertValue( null, false ) );

    private static readonly HashSet<Type> _contracts = new HashSet<Type>();

    private static readonly FastCache<MethodInfo, MethodValueDescriptor[]> _methods = new FastCache<MethodInfo, MethodValueDescriptor[]>( false );

    private static readonly FastCache<Type, Action<object, bool>> _converters = new FastCache<Type, Action<object, bool>>( true );

    [ThreadStatic]
    private static CachedConverter[] _cachedConverters;

    [ThreadStatic]
    private static int _converterIndex;
    #endregion

    #region ConvertAfterCall
    public static void ConvertAfterCall( MethodInfo methodInfo, object[] args, ref object returnValue )
    {
      var list = _methods.Get( methodInfo );

      if( list != null )
      {
        ConvertArgs( list, args, false );

        if( list[0].Position == -1 )
          returnValue = ConvertValue( returnValue, true );
      }
    }
    #endregion

    #region ConvertBeforeCall
    public static void ConvertBeforeCall( MethodInfo methodInfo, object[] args )
    {
      ConvertArgs( _methods.Get( methodInfo ), args, true );
    }
    #endregion

    #region ConvertArgs
    private static void ConvertArgs( MethodValueDescriptor[] parameters, object[] args, bool beforeCall )
    {
      if( parameters != null )
        foreach( var p in parameters )
          // if( p.Position != -1 && (beforeCall || p.IsOut) )
          if( p.Position != -1 ) // ^ - необходимо преобразование не только out-параметров, но и in-параметров, чтобы вернуть им значения, которые были до вызова метода.
            args[p.Position] = ConvertValue( args[p.Position], !beforeCall );
    }
    #endregion

    #region ConvertTimeValue
    private static DateTime ConvertTimeValue( DateTime dateTime, bool toLocalTime )
    {
      return dateTime == DateTime.MinValue || dateTime == DateTime.MaxValue ? dateTime : (toLocalTime ? dateTime.ToLocalTime() : dateTime.ToUniversalTime());
    }

    private static DateTime? ConvertTimeValue( DateTime? dateTime, bool toLocalTime )
    {
      return dateTime.HasValue ? ConvertTimeValue( dateTime.Value, toLocalTime ) : dateTime;
    }
    #endregion

    #region ConvertValue
    private static object ConvertValue( object value, bool toLocalTime )
    {
      if( value != null )
      {
        var array = value as Array;

        if( array != null && array.Rank == 1 )
          for( var i = 0; i < array.Length; i++ )
            array.SetValue( ConvertValue( array.GetValue( i ), toLocalTime ), i );
        else
        {
          var type = value.GetType();

          if( type == typeof( DateTime ) )
            value = ConvertTimeValue( (DateTime) value, toLocalTime );
          else
            if( type == typeof( DateTime? ) )
              value = ConvertTimeValue( (DateTime?) value, toLocalTime );
            else
            {
              if( _cachedConverters == null )
                _cachedConverters = new CachedConverter[CACHED_CONVERTERS];

              if( _cachedConverters[_converterIndex].Type != type )
              {
                var usedCount = ulong.MaxValue;

                var i = 0;

                for( ; i < CACHED_CONVERTERS; i++ )
                {
                  var cachedType = _cachedConverters[i].Type;

                  if( cachedType == type )
                  {
                    if( _cachedConverters[_converterIndex = i].UsedCount < ulong.MaxValue )
                      _cachedConverters[i].UsedCount++;

                    break;
                  }
                  else
                    if( cachedType == null )
                    {
                      _cachedConverters[i].Type = type;
                      _cachedConverters[_converterIndex = i].Converter = GetConverter( type );

                      break;
                    }

                  if( _cachedConverters[i].UsedCount < usedCount )
                    usedCount = _cachedConverters[_converterIndex = i].UsedCount;
                }

                if( i == CACHED_CONVERTERS )
                {
                  _cachedConverters[_converterIndex].Type = type;
                  _cachedConverters[_converterIndex].Converter = GetConverter( type );
                  _cachedConverters[_converterIndex].UsedCount = 0;
                }
              }

              if( _cachedConverters[_converterIndex].Converter != null )
                _cachedConverters[_converterIndex].Converter( value, toLocalTime );
            }
        }
      }

      return value;
    }
    #endregion

    #region GetConverter
    private static Action<object, bool> GetConverter( Type type )
    {
      if( !IsClass( type ) || type.IsArray )
        return null;

      return _converters.Get( type, t =>
      {
        if( t.IsDefined<DataContractAttribute>() )
        {
          DynamicMethod dm = null;
          ILGenerator g = null;
          Label label = default( Label );

          foreach( var p in GetProperties( t ) )
            if( IsConvertable( p.PropertyType, p ) )
            {
              var getMethod = p.GetGetMethod( true );
              var setMethod = p.GetSetMethod( true ) ?? p.DeclaringType.GetProperty( p.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic ).GetSetMethod( true );

              if( getMethod != null && setMethod != null )
              {
                if( dm == null )
                {
                  g = (dm = new DynamicMethod( "", MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard, null, new[] { typeof( object ), typeof( bool ) }, typeof( TimeConverter ), true )).GetILGenerator();

                  g.DeclareLocal( t );

                  label = g.DefineLabel();

                  g.Emit( OpCodes.Ldarg_0 );
                  g.Emit( OpCodes.Isinst, type );
                  g.Emit( OpCodes.Stloc_0 );
                  g.Emit( OpCodes.Ldloc_0 );
                  g.Emit( OpCodes.Brfalse, label );
                }

                if( IsDateTime( p.PropertyType ) )
                {
                  g.Emit( OpCodes.Ldloc_0 );
                  g.Emit( OpCodes.Dup );
                  g.Emit( OpCodes.Callvirt, getMethod );
                  g.Emit( OpCodes.Ldarg_1 );
                  g.Emit( OpCodes.Call, p.PropertyType == typeof( DateTime ) ? _convertTimeValue : _convertTimeNullableValue );
                  g.Emit( OpCodes.Callvirt, setMethod );
                }
                else
                {
                  g.Emit( OpCodes.Ldloc_0 );
                  g.Emit( OpCodes.Callvirt, getMethod );
                  g.Emit( OpCodes.Ldarg_1 );
                  g.Emit( OpCodes.Call, _convertValue );
                  g.Emit( OpCodes.Pop );
                }
              }
            }

          if( dm != null )
          {
            g.MarkLabel( label );
            g.Emit( OpCodes.Ret );

            return dm.CreateDelegate( typeof( Action<object, bool> ) ) as Action<object, bool>;
          }
        }

        return null;
      } );
    }
    #endregion

    #region GetProperties
    private static IEnumerable<PropertyInfo> GetProperties( Type type )
    {
      return type.GetProperties( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic ).Where( pi => pi.IsDefined<DataMemberAttribute>() );
    }
    #endregion

    #region IsClass
    private static bool IsClass( Type type )
    {
      return !type.IsValueType && type != typeof( string ) && type != typeof( object );
    }
    #endregion

    #region IsConvertable
    private static bool IsConvertable( Type type, ICustomAttributeProvider attributes, HashSet<Type> types = null )
    {
      if( type.IsArray || type.IsByRef )
        type = type.GetElementType() ?? type;

      if( types == null )
        types = new HashSet<Type>();

      types.Add( type );

      try
      {
        if( !attributes.IsDefined( typeof( SkipTimeConvertationAttribute ), true ) )
        {
          if( IsDateTime( type ) )
            return true;

          if( IsClass( type ) )
          {
            if( attributes.IsDefined( typeof( HasInheritedDateTimesAttribute ), true ) )
              return true;

            foreach( var p in GetProperties( type ) )
              if( !types.Contains( p.PropertyType ) && IsConvertable( p.PropertyType, p, types ) )
                return true;
          }
        }

        return false;
      }
      finally
      {
        types.Remove( type );
      }
    }
    #endregion

    #region IsDateTime
    private static bool IsDateTime( Type type )
    {
      return type == typeof( DateTime ) || type == typeof( DateTime? );
    }
    #endregion

    #region PrepareToConvertation
    public static void PrepareToConvertation( Type type )
    {
      if( _contracts.AddSafe( type ) )
        foreach( var mi in type.GetMethods() )
        {
          var list = new List<MethodValueDescriptor>();

          if( mi.ReturnType != typeof( void ) && IsConvertable( mi.ReturnType, mi.ReturnTypeCustomAttributes ) )
            list.Add( new MethodValueDescriptor { IsOut = true, Position = -1 } );

          foreach( var p in mi.GetParameters() )
            if( IsConvertable( p.ParameterType, p ) )
              list.Add( new MethodValueDescriptor { IsOut = p.IsOut, Position = p.Position } );

          if( list.Count > 0 )
            _methods.Set( mi, list.ToArray() );
        }
    }
    #endregion

    [StructLayout( LayoutKind.Auto )]
    private struct MethodValueDescriptor
    {
      #region .Fields
      public int Position;
      public bool IsOut;
      #endregion
    }

    [StructLayout( LayoutKind.Auto )]
    private struct CachedConverter
    {
      #region .Fields
      public Type Type;
      public Action<object, bool> Converter;
      public ulong UsedCount;
      #endregion
    }
  }
}
