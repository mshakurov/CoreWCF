using System;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using ST.Utils.Attributes;
using Microsoft.SqlServer.Types;

namespace ST.Utils
{
  public partial class Dbi
  {
    public class StoredProcedure : IDbiDataProvider, IDisposable
    {
      #region .Static Fields
      private static ConcurrentDictionary<string, SqlParameter[]> _cache = new ConcurrentDictionary<string, SqlParameter[]>();
      #endregion

      #region .Fields
      private readonly Dbi _dbi;

      protected bool _disposed;

      private SqlConnection _conn;

      public DbCommand Command { get; private set; }

      public virtual DbDataReader Reader { get; protected set; }

      public SqlCommand SqlCommand
      {
        get
        {
          return Command as SqlCommand;
        }
      }

      private string _name;
      #endregion

      #region .Ctor
      private StoredProcedure()
      {
      }

      [DebuggerStepThrough]
      internal StoredProcedure( Dbi dbi, string name, params object[] args ) : this( dbi, false, name, args )
      {
      }

      [DebuggerStepThrough]
      internal StoredProcedure( Dbi dbi, bool async, string name, params object[] args ) : this( dbi, async, name, false, args )
      {
      }

      [DebuggerStepThrough]
      internal StoredProcedure( [NotNull] Dbi dbi, bool async, [NotNullNotEmpty] string name, bool query, params object[] args )
      {
        _dbi = dbi;

        _name = Utils.GetNormalizedName( name );

        SqlConnection realConnection = null;

        var transaction = _dbi._transaction == null ? null : _dbi._transaction.GetTransaction();

        try
        {
          if( transaction == null )
          {
            _conn = new SqlConnection( string.Format( "{0}{1}", _dbi.Connection, async ? ";Async=True" : "" ) );

            if( _conn.ConnectionString.IndexOf( CONNECTION_TIMEOUT, StringComparison.OrdinalIgnoreCase ) == -1 )
              _conn.ConnectionString += ";" + CONNECTION_TIMEOUT + "=" + _dbi._connectionTimeout;

            _conn.Open();

            realConnection = _conn;
          }
          else
            realConnection = transaction.Connection;

          Command = new SqlCommand
          {
            CommandText = _name,
            CommandType = query ? CommandType.Text : CommandType.StoredProcedure,
            Connection = realConnection,
            Transaction = transaction,
            CommandTimeout = _dbi.Timeout
          };

          var parameters = _cache.GetOrAdd( string.Format( "{0}@{1}#{2}", Command.Connection.DataSource, Command.Connection.Database, _name ), n =>
          {
            SqlCommandBuilder.DeriveParameters( SqlCommand );

            Command.Parameters.RemoveAt( 0 );

            var arr = Command.Parameters.Cast<SqlParameter>().ForEach( p =>
            {
              p.TypeName = "";
              p.UdtTypeName = "";
              p.ParameterName = p.ParameterName.ToUpper();
            } ).ToArray();

            Command.Parameters.Clear();

            return arr;
          } ).Select( p => ( p as ICloneable ).Clone() as SqlParameter ).ToArray();

          Command.Parameters.AddRange( parameters );

          if( args != null )
          {
            var bound = false;

            foreach( var arg in args.Where( a => a != null ) )
            {
              var argType = arg.GetType();

              if( Utils.IsComplexType( argType ) )
              {
                bound = true;

                foreach( var me in MemberProperty.GetInMembers( argType, _name ).Where( me => !me.Property.IsComplexType ) )
                  parameters.FirstOrDefault( p => me.Names.Contains( p.ParameterName ) ).IfNotNull( p => SetValue( p, me.Property.Get( arg ) ) );
              }
            }

            if( !bound )
              for( int i = 0; i < args.Length && i < parameters.Length; i++ )
                SetValue( parameters[i], args[i] );
          }

        }
        catch
        {
          this.Dispose();

          throw;
        }
      }
      #endregion

      #region Dispose
      public virtual void Dispose()
      {
        if( _disposed )
          return;

        if( _conn != null )
          _conn.Close();

        if( Reader != null )
        {
          Reader.Close();
          Reader.Dispose();
          Reader = null;
        }

        if( Command != null )
        {
          Command.Connection = null;
          Command.Parameters.Clear();
          Command.Dispose();
        }

        if( _conn != null )
        {
          _conn.Dispose();
          _conn = null;
        }

        _disposed = true;
      }
      #endregion

      #region SetValue
      private void SetValue( SqlParameter parameter, object value )
      {
        parameter.UdtTypeName = value.GetType().Name == "SqlGeometry" ? "geometry" :
                                value.GetType().Name == "SqlGeography" ? "geography" :
                                "";

        parameter.Value = value ?? DBNull.Value;
      }
      #endregion

      public sealed class Xml : StoredProcedure
      {
        #region .Properties
        public new XmlReader Reader { [DebuggerStepThrough] get; [DebuggerStepThrough] private set; }
        #endregion

        #region .Ctor
        [DebuggerStepThrough]
        public Xml( Dbi dbi, string name, params object[] args ) : base( dbi, name, args )
        {
          try
          {
            Reader = SqlCommand.ExecuteXmlReader();
          }
          catch
          {
            this.Dispose();

            throw;
          }
        }
        #endregion

        #region Dispose
        public override void Dispose()
        {
          if( !_disposed && Reader != null )
          {
            Reader.Close();
            Reader = null;
          }

          base.Dispose();
        }
        #endregion
      }

      public sealed class Recordset : StoredProcedure
      {
        #region .Properties
        public override DbDataReader Reader { [DebuggerStepThrough] get; [DebuggerStepThrough] protected set; }
        #endregion

        #region .Ctor
        [DebuggerStepThrough]
        public Recordset( Dbi dbi, CommandBehavior behavior, string name, params object[] args ) : base( dbi, name, args )
        {
          try
          {
            Reader = Command.ExecuteReader( behavior );
          }
          catch
          {
            this.Dispose();

            throw;
          }
        }
        #endregion

        #region Dispose
        public override void Dispose()
        {
          if( !_disposed && Reader != null )
          {
            Reader.Close();
            Reader = null;
          }

          base.Dispose();
        }
        #endregion
      }

      public sealed class Query : StoredProcedure
      {
        #region .Properties
        public override DbDataReader Reader { [DebuggerStepThrough] get; [DebuggerStepThrough] protected set; }
        #endregion

        #region .Ctor
        [DebuggerStepThrough]
        public Query( Dbi dbi, CommandBehavior behavior, string queryText, params object[] args )
          : base( dbi, false, queryText, true, args )
        {
          try
          {
            Reader = Command.ExecuteReader( behavior );
          }
          catch
          {
            this.Dispose();

            throw;
          }
        }
        #endregion

        #region Dispose
        public override void Dispose( )
        {
          if( !_disposed && Reader != null )
          {
            Reader.Close();
            Reader = null;
          }

          base.Dispose();
        }
        #endregion
      }
    }
  }
}
