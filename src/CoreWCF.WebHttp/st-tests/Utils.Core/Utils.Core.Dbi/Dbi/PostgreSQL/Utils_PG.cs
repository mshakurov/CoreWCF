using System;
using System.Data;
using System.Diagnostics;
using Microsoft.SqlServer.Types;

namespace ST.Utils
{
  public partial class Dbi_PG
  {
    private static class Utils
    {
      #region GetNormalizedName
      [DebuggerStepThrough]
      public static string GetNormalizedName( string name, string moduleName, bool query = false )
      {
        string res = name.Replace( "[", "" ).Replace( "]", "" );

        //if( !query && !res.StartsWith( "public." ) )
        if( !query )
        {
          //if( !String.IsNullOrEmpty( moduleName ) )
          //  moduleName = moduleName.ToLower();

          res = moduleName + "_" + res;

          res = res.ToLower();
        }

        //var parts = res.Split( '.' );

        //if( parts.Length > 1 )
        //{
        //  res = "\"" + parts[0] + "\"." + parts[1];
        //}

        return res;
      }
      #endregion

      #region IsComplexType
      [DebuggerStepThrough]
      public static bool IsComplexType( Type t )
      {
        //return t.IsClass && !t.IsValueType && !t.IsPrimitive && t != typeof( string ) && t != typeof( byte[] ) && t != typeof( DataTable ) && t != typeof( DBNull ) && !t.Name.StartsWith( "SqlGeo", StringComparison.Ordinal );
        return t.IsClass && !t.IsValueType && !t.IsPrimitive && t != typeof( DBNull ) && t != typeof( string ) && t != typeof( byte[] ) && t != typeof( int[] ) && t != typeof( short[] ) && t != typeof( object[] ) && t != typeof( DataTable ) &&
               t.Name != "SqlGeometry" && t.Name != "SqlGeography";
      }
      #endregion

      #region IsOutComplexType
      [DebuggerStepThrough]
      public static bool IsOutComplexType( Type t )
      {
        //return t.IsClass && !t.IsValueType && !t.IsPrimitive && t != typeof( string ) && t != typeof( byte[] ) && t != typeof( DataTable ) && t != typeof( DBNull ) && !t.Name.StartsWith( "SqlGeo", StringComparison.Ordinal );
        return t.IsClass && !t.IsValueType && !t.IsPrimitive && t != typeof( DBNull ) && t != typeof( string ) && t != typeof( byte[] ) && t != typeof( DataTable ) &&
                t.Name != "SqlGeometry" && t.Name != "SqlGeography";
      }
      #endregion
    }
  }
}
