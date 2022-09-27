using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using ST.Utils.Attributes;
using Npgsql;
using Newtonsoft.Json;

namespace ST.Utils
{
  public partial class Dbi_PG
  {
    public class StoredProcedure : IDbiDataProvider, IDisposable
    {
      #region .Static Fields
      private static ConcurrentDictionary<string, NpgsqlParameter[]> _cache = new ConcurrentDictionary<string, NpgsqlParameter[]>();
      #endregion

      #region .Fields
      private readonly Dbi_PG _dbi;

      protected bool _disposed;

      private NpgsqlConnection _conn;

      public DbCommand Command { [DebuggerStepThrough] get; [DebuggerStepThrough] private set; }

      public virtual DbDataReader Reader { [DebuggerStepThrough] get; [DebuggerStepThrough] protected set; }

      public NpgsqlCommand PgsqlCommand
      {
        [DebuggerStepThrough]
        get
        {
          return Command as NpgsqlCommand;
        }
      }

      private string _name;
      #endregion

      #region .Ctor
      private StoredProcedure()
      {
      }

      [DebuggerStepThrough]
      internal StoredProcedure(Dbi_PG dbi, string name, params object[] args)
        : this(dbi, false, name, args)
      {
      }

      [DebuggerStepThrough]
      internal StoredProcedure(Dbi_PG dbi, bool async, string name, params object[] args)
        : this(dbi, async, name, false, args)
      {
      }

      [DebuggerStepThrough]
      internal StoredProcedure([NotNull] Dbi_PG dbi, bool async, [NotNullNotEmpty] string name, bool query, params object[] args)
      {
        _dbi = dbi;

        _name = Utils.GetNormalizedName(name, _dbi.OwnerModuleName, query);

        NpgsqlConnection realConnection = null;

        var transaction = _dbi._transaction == null ? null : _dbi._transaction.GetTransaction();

        try
        {
          if( transaction == null )
        {
          //_conn = new NpgsqlConnection( string.Format( "{0}{1}", _dbi.Connection, async ? ";Async=True" : "" ) );
            _conn = new NpgsqlConnection( _dbi.Connection );

            if( _conn.ConnectionString.IndexOf( CONNECTION_TIMEOUT, StringComparison.OrdinalIgnoreCase ) == -1 )
            _conn.ConnectionString += ";" + CONNECTION_TIMEOUT + "=" + _dbi._connectionTimeout;

          _conn.Open();

          realConnection = _conn;
        }
        else
          realConnection = transaction.Connection;

        Command = new NpgsqlCommand
        {
          CommandText = _name,
          CommandType = query ? CommandType.Text : CommandType.StoredProcedure,
          Connection = realConnection,
          Transaction = transaction,
          CommandTimeout = _dbi.Timeout
        };

          var parameters = _cache.GetOrAdd( string.Format( "{0}@{1}#{2}", Command.Connection.DataSource, Command.Connection.Database, _name ), n =>
          {
         NpgsqlCommandBuilder.DeriveParameters(PgsqlCommand);

            //Command.Parameters.RemoveAt( 0 );

            var arr = Command.Parameters.Cast<NpgsqlParameter>().ForEach( p =>
            {
              //p.TypeName = "";
              //p.UdtTypeName = "";
              ////p.DataTypeName = "";

              p.ParameterName = p.ParameterName/*ToLower? .ToUpper()*/;
            } ).ToArray();

            Command.Parameters.Clear();

            return arr;
          } ).Select( p => ( p as ICloneable ).Clone() as NpgsqlParameter ).ToArray();

          Command.Parameters.AddRange( parameters );

          if( args != null )
        {
          var bound = false;

            foreach( var argument in args.Where( a => a != null ) )
          {
            var arg = argument;

            var argType = arg.GetType();

              if( Utils.IsComplexType( argType ) )
            {
              bound = true;

                foreach( var me in MemberProperty.GetInMembers( argType, _name ).Where( me => !me.Property.IsComplexType ) )
                parameters.FirstOrDefault(p => me.Names.Contains(p.ParameterName.ToUpper())).IfNotNull(p => SetValue(p, ConvertArg(me.Property.Get(arg), p)));
            }
          }

            if( !bound )
              for( int i = 0; i < parameters.Length; i++ )
            {
                if( i < args.Length )
              {
                var arg = ConvertArg(args[i], parameters[i]);

                  SetValue( parameters[i], arg );
              }
                else if( parameters[i].Direction == ParameterDirection.Input || parameters[i].Direction == ParameterDirection.InputOutput )
              {
                //SetValue( parameters[i], null );

                //parameters[i].IsNullable = true;
                //parameters[i].Value = null;

                parameters[i].Direction = ParameterDirection.Output;  // Решение проблемы отсутствия передачи значения для DEFAULT параметра.
                // Npgsql не понимает PostgreSQL DEFAULT и валится при внутренней валидации, с тем что отсутствует значение входного параметра для функции.
                // Изменение направления параметра на выходной блокирует валидацию значения для этого параметра внутри Npgsql. 
                // При этом валидация уровне вызова ф-ии БД все равно будет работать и если параметр с DEFAULT, то ошибки не будет. Если же параметр без DEFAULT, то будет ошибка вызова из БД, т.е. сработает честно.
                // Пример: вызов "[Session].[Get]" без передачи входного параметра.
              }
            }
        }
      }
        catch
        {
          this.Dispose();

          throw;
        }
      }
      #endregion

      #region ConvertArg
      private static object ConvertArg(object arg, NpgsqlParameter par)
      {
        if (arg == null)
        {
          return arg;
        }

        var argType = arg.GetType();

        if (argType == typeof(UInt64))
        {
          arg = Convert.ToInt64(arg); // Решение проблемы отсутствия UInt64 в PostgreSQL. Ближайший - bigint (Int64).
          // В принципе, UInt64 (почти) нигде и не используется, но почему-то sessionId в контрактах везде ulong, хотя в исходной базе MSSQL это bigint.
          // Например, при вызове "[Session].[Get]" в IAuthModule.GetDBSession.
          // При этом адаптер MSSQL автоматом тихо конвертит, а Npgsql валит InvalidCastException (Can't write CLR type System.UInt64 with handler type Int64Handler).
        }
        else if (argType == typeof(DataTable))
        {
          var dt = arg as DataTable;

          if (dt.Columns.Count == 1)
          {
            // Для PostgreSQL функций обрабатываем эту ситуацию по-другому - не как таблицу с одной колонкой, а как массив.
            // Пример: idsTable в вызове: DB.RS.List<FlatDataPermission>( schemeName + ".[GetDataPermissions]", idsTable )
            // где idsTable: var idsTable = CollectionHelper.GetIdentifiersTable<int>( ownerIds );

            var items = new List<object>();

            for (var i = 0; i < dt.Rows.Count; i++)
            {
              var row = dt.Rows[i];

              items.Add(row[0]);
            }

            arg = items.ToArray();
          }
          else
          {
            // Для PostgreSQL функций обрабатываем многоколоночные таблицы по-другому - передаем список объектов, соответствующих многоколоночной таблице в виде аргумента типа JSON на вход ф-ии PostgreSQL.

            List<object> items = new List<object>();

            for (var i = 0; i < dt.Rows.Count; i++)
            {
              var row = dt.Rows[i];

              var item = new Dictionary<string, object>();

              for (var j = 0; j < dt.Columns.Count; j++)
              {
                var column = dt.Columns[j];

                if (column != null && !String.IsNullOrEmpty(column.ColumnName))
                  item[column.ColumnName.ToLower()] = row[column];
              }

              items.Add(item);
            }

            arg = JsonConvert.SerializeObject(items);
          }
        }

        return arg;
      }
      #endregion

      #region Dispose
      public virtual void Dispose()
      {
        if (_disposed)
          return;

        if( _conn != null )
          _conn.Close();

        if( Reader != null )
        {
          Reader.Close();
          Reader.Dispose();
          Reader = null;
        }

        if (Command != null)
        {
          Command.Connection = null;
          Command.Parameters.Clear();
          Command.Dispose();
        }

        if (_conn != null)
        {
          _conn.Dispose();
          _conn = null;
        }

        _disposed = true;
      }
      #endregion

      #region SetValue
      private void SetValue(NpgsqlParameter parameter, object value)
      {
        //parameter.UdtTypeName = value is SqlGeometry ? "geometry" :
        //                        value is SqlGeography ? "geography" :
        //                        "";

        if (value is Enum)
        {
          // Решение проблем с передачей перечислений в PostgreSql: Can't write CLR type "EnumType" with handler type TextHandler
          if (parameter.DbType.In(DbType.String, DbType.StringFixedLength, DbType.AnsiString, DbType.AnsiStringFixedLength))
            value = value.ToString();
          else
          {
            var enumType = Enum.GetUnderlyingType(value.GetType());
            if (parameter.NpgsqlDbType == NpgsqlTypes.NpgsqlDbType.Bigint)
              value = (long)Convert.ChangeType(Convert.ChangeType(value, enumType), typeof(long));
            else
            if (parameter.NpgsqlDbType == NpgsqlTypes.NpgsqlDbType.Integer)
              value = (int)Convert.ChangeType(Convert.ChangeType(value, enumType), typeof(int));
            else
            if (parameter.NpgsqlDbType == NpgsqlTypes.NpgsqlDbType.Smallint)
              value = (short)Convert.ChangeType(Convert.ChangeType(value, enumType), typeof(short));
            else
              value = (int)value;
          }
        }

        parameter.Value = value ?? DBNull.Value;
      }
      #endregion

      public sealed class Xml : StoredProcedure
      {
        #region .Consts
        private const string EMPTY_XML_STRING = @"<?xml version=""1.0"" encoding=""utf-16""?>";

        private const string ROOT_XML_NODE = "ROOT_XML_NODE";
        private const string ROOT_XML_STRING = "<" + ROOT_XML_NODE + ">{0}</" + ROOT_XML_NODE + ">";
        #endregion

        #region .Fields
        public StringReader _stringReader;
        #endregion

        #region .Properties
        public new XmlReader Reader { [DebuggerStepThrough] get; [DebuggerStepThrough] private set; }
        #endregion

        #region .Ctor
        [DebuggerStepThrough]
        public Xml(Dbi_PG dbi, string name, params object[] args)
          : base(dbi, name, args)
        {
          try
          {
          var value = Command.ExecuteScalar();

            _stringReader = new StringReader( value == DBNull.Value ? EMPTY_XML_STRING : String.Format( ROOT_XML_STRING, value ) );

            Reader = new XmlTextReader( _stringReader );
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
          if (!_disposed)
          {
            if (_stringReader != null)
            {
              _stringReader.Close();
              _stringReader.Dispose();
              _stringReader = null;
            }

            if (Reader != null)
            {
              Reader.Close();
              Reader.Dispose();
              Reader = null;
            }
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
        public Recordset(Dbi_PG dbi, CommandBehavior behavior, string name, params object[] args)
          : base(dbi, name, args)
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
          if (!_disposed && Reader != null)
          {
            Reader.Close();
            Reader.Dispose();
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
        public Query(Dbi_PG dbi, CommandBehavior behavior, string name, params object[] args)
          : base(dbi, false, name, true, args)
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
          if (!_disposed && Reader != null)
          {
            Reader.Close();
            Reader.Dispose();
            Reader = null;
          }

          base.Dispose();
        }
        #endregion
      }
    }
  }
}
