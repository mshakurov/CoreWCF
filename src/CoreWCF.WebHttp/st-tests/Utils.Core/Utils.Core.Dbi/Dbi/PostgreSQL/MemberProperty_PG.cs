using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace ST.Utils
{
  public partial class Dbi_PG
  {
    private class MemberEntry
    {
      #region .Fields
      public List<string> Names;
      public MemberProperty Property;
      #endregion
    }

    private class MemberProperty
    {
      #region .Static Fields
      private static readonly NumberFormatInfo _nfi = new NumberFormatInfo
      {
        CurrencyDecimalSeparator = ".",
        CurrencyGroupSeparator = " ",
        NumberDecimalSeparator = ".",
        NumberGroupSeparator = " ",
        PercentDecimalSeparator = ".",
        PercentGroupSeparator = " "
      };

      private static readonly MethodInfo _getType = MemberHelper.GetMethod( (object obj) => obj.GetType() );
      private static readonly MethodInfo _getTypeFromHandle = MemberHelper.GetMethod( () => Type.GetTypeFromHandle( (RuntimeTypeHandle) Type.Missing ) );
      private static readonly MethodInfo _enumParse = MemberHelper.GetMethod( () => Enum.Parse( (Type) null, (string) null, (bool) false ) );
      private static readonly MethodInfo _enumToObject = MemberHelper.GetMethod( () => Enum.ToObject( (Type) null, (object) null ) );
      private static readonly MethodInfo _changeType = MemberHelper.GetMethod( () => Convert.ChangeType( (object) null, (Type) null, (IFormatProvider) null ) );
      private static readonly MethodInfo _intParse = MemberHelper.GetMethod( () => int.Parse( (string) null ) );
      private static readonly MethodInfo _toBoolean = MemberHelper.GetMethod( () => Convert.ToBoolean( (int) 0 ) );
      private static readonly MethodInfo _toBinary = MemberHelper.GetMethod( () => Converter.ToBinary( (string) null ) );
      private static readonly FieldInfo _nfiField = MemberHelper.GetField( () => MemberProperty._nfi );

      private static readonly ConcurrentDictionary<Type, Dictionary<string, List<MemberEntry>>> _in = new ConcurrentDictionary<Type, Dictionary<string, List<MemberEntry>>>();
      private static readonly ConcurrentDictionary<Type, Dictionary<string, List<MemberEntry>>> _out = new ConcurrentDictionary<Type, Dictionary<string, List<MemberEntry>>>();

      private static readonly List<Dbi.BindBaseAttribute> _dynamicBindEmpty = new List<Dbi.BindBaseAttribute>();
      internal static readonly ConcurrentDictionary<ulong, List<Dbi.BindBaseAttribute>> DynamicBinds = new ConcurrentDictionary<ulong, List<Dbi.BindBaseAttribute>>();
      #endregion

      #region .Properties
      public Type MemberType { get; private set; }
      public MemberKind MemberKind { get; private set; }

      public Type ElementType { get; private set; }

      public bool IsComplexType { get; private set; }

      public bool IsOutComplexType { get; private set; }

      public bool IsNullable { get; private set; }

      public Func<object, object> Get { get; private set; }
      public Action<object, object> Set { get; private set; }
      public Action<object, string> SetString { get; private set; }
      #endregion

      #region .Ctor
      private MemberProperty( PropertyInfo pi )
      {
        ElementType = MemberType = pi.PropertyType;

        MemberKind = MemberKind.Value;

        if( MemberType.IsGenericType )
        {
          var gtd = MemberType.GetGenericTypeDefinition();

          if( gtd == typeof( Nullable<> ) )
          {
            IsNullable = true;
            ElementType = MemberType.GetGenericArguments()[0];
          }
          else
            if( gtd == typeof( List<> ) )
            {
              MemberKind = MemberKind.List;

              ElementType = MemberType.GetGenericArguments()[0];
            }
        }
        else
          if( MemberType.IsArray && MemberType.GetArrayRank() == 1 && !MemberType.GetElementType().IsArray )
          {
            MemberKind = MemberKind.Array;

            ElementType = MemberType.GetElementType();
          }

        IsComplexType = Utils.IsComplexType( MemberType );

        IsOutComplexType = Utils.IsOutComplexType( MemberType );

        var dm1 = new DynamicMethod( "", typeof( void ), new Type[] { typeof( object ), typeof( object ) }, typeof( MemberProperty ), true );
        var dm2 = new DynamicMethod( "", typeof( void ), new Type[] { typeof( object ), typeof( string ) }, typeof( MemberProperty ), true );

        var g1 = dm1.GetILGenerator();
        var g2 = dm2.GetILGenerator();

        var setMethod = pi.GetSetMethod( true ) ?? pi.DeclaringType.GetProperty( pi.Name, BindingFlags.Instance | BindingFlags.Public ).GetSetMethod( true );

        if( setMethod != null )
        {
          var label1 = g1.DefineLabel();
          var label2 = g1.DefineLabel();
          var label3 = g1.DefineLabel();

          g1.Emit( OpCodes.Ldarg_0 );
          g1.Emit( OpCodes.Castclass, pi.DeclaringType );

          if( MemberType.IsClass )
          {
            g1.Emit( OpCodes.Ldarg_1 );
            g1.Emit( OpCodes.Castclass, MemberType );
          }
          else
          {
            g1.Emit( OpCodes.Ldarg_1 );
            g1.Emit( OpCodes.Brtrue_S, label2 );

            if( MemberType != ElementType )
            {
              g1.Emit( OpCodes.Ldnull );
              g1.Emit( OpCodes.Br_S, label3 );
            }
            else
            {
              g1.Emit( OpCodes.Ldstr, string.Format( "Unable to assign null value to property {0} of type {1}.", pi.Name, pi.DeclaringType.FullName ) );
              g1.Emit( OpCodes.Newobj, typeof( InvalidCastException ).GetConstructor( new[] { typeof( string ) } ) );
              g1.Emit( OpCodes.Throw );
            }

            g1.MarkLabel( label2 );

            if( ElementType.IsEnum )
            {
              g1.Emit( OpCodes.Ldtoken, ElementType );
              g1.Emit( OpCodes.Call, _getTypeFromHandle );
              g1.Emit( OpCodes.Ldarg_1 );
              g1.Emit( OpCodes.Isinst, typeof( string ) );
              g1.Emit( OpCodes.Brfalse_S, label1 );
              g1.Emit( OpCodes.Ldarg_1 );
              g1.Emit( OpCodes.Ldc_I4_1 );
              g1.Emit( OpCodes.Call, _enumParse );
              g1.Emit( OpCodes.Br_S, label3 );
              g1.MarkLabel( label1 );
              g1.Emit( OpCodes.Ldarg_1 );
              g1.Emit( OpCodes.Call, _enumToObject );
            }
            else
            {
              g1.Emit( OpCodes.Ldarg_1 );
              g1.Emit( OpCodes.Ldarg_1 );
              g1.Emit( OpCodes.Callvirt, _getType );
              g1.Emit( OpCodes.Ldtoken, ElementType );
              g1.Emit( OpCodes.Call, _getTypeFromHandle );
              g1.Emit( OpCodes.Ceq );
              g1.Emit( OpCodes.Brtrue_S, label3 );
              g1.Emit( OpCodes.Ldtoken, ElementType );
              g1.Emit( OpCodes.Call, _getTypeFromHandle );
              g1.Emit( OpCodes.Ldsfld, _nfiField );
              g1.Emit( OpCodes.Call, _changeType );
            }

            g1.MarkLabel( label3 );
            g1.Emit( OpCodes.Unbox_Any, MemberType );
          }

          g1.Emit( OpCodes.Callvirt, setMethod );

          if( !IsComplexType && MemberType != typeof( DataTable ) )
          {
            g2.Emit( OpCodes.Ldarg_0 );
            g2.Emit( OpCodes.Castclass, pi.DeclaringType );

            if( MemberType == typeof( string ) )
              g2.Emit( OpCodes.Ldarg_1 );
            else
              if( ElementType == typeof( bool ) )
              {
                g2.Emit( OpCodes.Ldarg_1 );
                g2.Emit( OpCodes.Call, _intParse );
                g2.Emit( OpCodes.Call, _toBoolean );
              }
            else
              if( ElementType.IsEnum )
              {
                g2.Emit( OpCodes.Ldtoken, ElementType );
                g2.Emit( OpCodes.Call, _getTypeFromHandle );
                g2.Emit( OpCodes.Ldarg_1 );
                g2.Emit( OpCodes.Ldc_I4_1 );
                g2.Emit( OpCodes.Call, _enumParse );
                g2.Emit( OpCodes.Unbox_Any, MemberType );
              }
            else
              if( MemberType == typeof( byte[] ) )
              {
                g2.Emit( OpCodes.Ldarg_1 );
                g2.Emit( OpCodes.Call, _toBinary );
              }
            else
            {
              g2.Emit( OpCodes.Ldarg_1 );
              g2.Emit( OpCodes.Ldtoken, ElementType );
              g2.Emit( OpCodes.Call, _getTypeFromHandle );
              g2.Emit( OpCodes.Ldsfld, _nfiField );
              g2.Emit( OpCodes.Call, _changeType );
              g2.Emit( OpCodes.Unbox_Any, MemberType );
            }

            g2.Emit( OpCodes.Callvirt, setMethod );
          }
        }

        g1.Emit( OpCodes.Ret );
        g2.Emit( OpCodes.Ret );

        Set = (Action<object, object>) dm1.CreateDelegate( typeof( Action<object, object> ) );
        SetString = (Action<object, string>) dm2.CreateDelegate( typeof( Action<object, string> ) );

        var dm3 = new DynamicMethod( "", typeof( object ), new Type[] { typeof( object ) }, typeof( MemberProperty ), true );

        var g3 = dm3.GetILGenerator();

        var getMethod = pi.GetGetMethod( true );

        if( getMethod == null )
          g3.Emit( OpCodes.Ldnull );
        else
        {
          g3.Emit( OpCodes.Ldarg_0 );
          g3.Emit( OpCodes.Castclass, pi.DeclaringType );
          g3.Emit( OpCodes.Callvirt, getMethod );

          if( MemberType.IsValueType )
            g3.Emit( OpCodes.Box, MemberType );
        }

        g3.Emit( OpCodes.Ret );

        Get = (Func<object, object>) dm3.CreateDelegate( typeof( Func<object, object> ) );
      }
      #endregion

      #region GetMembers
      public static List<MemberEntry> GetInMembers( Type type, string name )
      {
        return GetMembers( type, name, _in );
      }

      public static List<MemberEntry> GetOutMembers( Type type, string name )
      {
        return GetMembers( type, name, _out );
      }

      private static List<MemberEntry> GetMembers( Type type, string name, ConcurrentDictionary<Type, Dictionary<string, List<MemberEntry>>> cache )
      {
        var d = cache.GetOrAdd( type, t =>
        {
          var dict = new Dictionary<string, List<MemberEntry>>();

          var members = new List<MemberEntry>();

          dict.Add( "#", members );

          foreach( var pi in t.GetProperties( BindingFlags.Instance | BindingFlags.Public ) )
          {
            if( pi.GetIndexParameters().Length > 0 )
              continue;

            var token = pi.GetUniqueToken();

            var dynamicBind = DynamicBinds.GetValue( token );

            for( var baseType = t.BaseType; baseType != typeof( object ) && baseType != null; baseType = baseType.BaseType )
              baseType.GetProperty( pi.Name, BindingFlags.Instance | BindingFlags.Public ).IfNotNull( p =>
                DynamicBinds.GetValue( p.GetUniqueToken() ).IfNotNull( db => dynamicBind = MemberProperty.DynamicBinds.GetOrAdd( token, db ) ) );

            if( dynamicBind == null )
              dynamicBind = _dynamicBindEmpty;

            if( !(dynamicBind.Exists( b => b.GetType() == typeof( Dbi.BindNoneAttribute ))) &&
                ( ( cache == _in && !dynamicBind.Exists( b => b is Dbi.BindInNoneAttribute ) ) || ( cache == _out && !dynamicBind.Exists( b => b is Dbi.BindOutNoneAttribute ) ) ) &&
                !pi.IsDefined<Dbi.BindNoneAttribute>( false ) &&
                !( cache == _in ? pi.IsDefined<Dbi.BindInNoneAttribute>( false ) : pi.IsDefined<Dbi.BindOutNoneAttribute>( false ) ) )
            {
              var property = new MemberProperty( pi );

              var bindAll = ( dynamicBind.FirstOrDefault( b => b is Dbi.BindAttribute ) ?? pi.GetAttribute<Dbi.BindAttribute>() ) as Dbi.BindAttribute;

              var memberNames = bindAll != null ? bindAll.Names : new List<string> { pi.Name.ToUpper()/*.ToLower? .ToUpper()*/ };

              if( cache == _in )
                memberNames = memberNames.Select( n => "P_"/*"@"*/ + n ).ToList();

              var binds = ( cache == _in ? dynamicBind.Where( b => b is Dbi.BindInExAttribute ) : dynamicBind.Where( b => b is Dbi.BindOutExAttribute ) ).Cast<Dbi.BindExAttribute>().ToArray();

              if( binds.Length == 0 )
                binds = pi.GetCustomAttributes( cache == _in ? typeof( Dbi.BindInExAttribute ) : typeof( Dbi.BindOutExAttribute ), true ) as Dbi.BindExAttribute[];

              foreach( var b in binds )
              {
                if( !dict.ContainsKey( b.SPName ) )
                  dict.Add( b.SPName, new List<MemberEntry>() );

                dict[b.SPName].Add( new MemberEntry { Names = b.Names, Property = property } );
              }

              var bind = ( cache == _in ? dynamicBind.FirstOrDefault( b => b is Dbi.BindInAttribute ) : dynamicBind.FirstOrDefault( b => b is Dbi.BindOutAttribute ) ) as Dbi.BindNonEmptyAttribute;

              if( bind == null )
                bind = Attribute.GetCustomAttribute( pi, cache == _in ? typeof( Dbi.BindInAttribute ) : typeof( Dbi.BindOutAttribute ), true ) as Dbi.BindNonEmptyAttribute;

              memberNames = ( bind == null ? memberNames : bind.Names.Select( n => n != null ? n.Replace( "@", "P_" ) : n ).ToList() );

              members.Add( new MemberEntry { Names = memberNames, Property = property } );
            }
          }

          foreach( var n in dict.Where( me => me.Key != "#" ) )
            foreach( var m in members )
              if( !n.Value.Exists( me => object.ReferenceEquals( me.Property, m.Property ) ) )
                n.Value.Add( m );

          return dict;
        } );

        return d.GetValue( name ) ?? d["#"];
      }
      #endregion
    }

    private enum MemberKind
    {
      #region .Static Fields
      Value,
      Array,
      List
      #endregion
    }
  }
}
