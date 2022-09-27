using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using Microsoft.SqlServer.Types;
using Npgsql;

namespace ST.Utils
{
  public partial class Dbi
  {
    private static class Utils
    {
      #region GetNormalizedName
      [DebuggerStepThrough]
      public static string GetNormalizedName( string name )
      {
        return name.ToUpper().Replace( "[", "" ).Replace( "]", "" ).Replace( "DBO.", "" );
      }
      #endregion

      #region IsComplexType
      [DebuggerStepThrough]
      public static bool IsComplexType( Type t )
      {
        //return t.IsClass && !t.IsValueType && !t.IsPrimitive && t != typeof( string ) && t != typeof( byte[] ) && t != typeof( DataTable ) && t != typeof( DBNull ) && !t.Name.StartsWith( "SqlGeo", StringComparison.Ordinal );
        return t.IsClass && !t.IsValueType && !t.IsPrimitive && t != typeof( DBNull ) && t != typeof( string ) && t != typeof( byte[] ) && t != typeof( DataTable ) &&
               t.Name != "SqlGeometry" && t.Name != "SqlGeography";
      }
      #endregion
    }
  }
}
