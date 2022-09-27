using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using ST.Utils.Attributes;

namespace ST.Utils
{
  /// <summary>
  /// Класс для работы с базой данных через интерфейс хранимых процедур.
  /// </summary>
  [SqlExceptionHandler]
  public sealed partial class Dbi : IDbi, IConnectionHolder
  {
    #region .Constants
    private const string CONNECTION_TIMEOUT = "CONNECTION TIMEOUT";
    #endregion

    #region .Fields
    private readonly SqlConnectionStringBuilder _connection = new SqlConnectionStringBuilder();

    private int _connectionTimeout = 15;
    private int _timeout = 180;

    private DbiTransaction _transaction;

    //private string _ownerModuleName;
    #endregion

    #region .Properties
    /// <summary>
    /// Строка соединения с БД.
    /// </summary>
    [NotNullNotEmpty]
    public string Connection
    {
      [DebuggerStepThrough]
      get { return _connection.ConnectionString; }
      [DebuggerStepThrough]
      set
      {
        _connection.ConnectionString = value;
        _connection.DataSource = _connection.DataSource.ToUpper();
        _connection.InitialCatalog = _connection.InitialCatalog.ToUpper();
      }
    }

    ///// <summary>
    ///// Название модуля, создавшего экземпляр DBi
    ///// </summary>
    //public string OwnerModuleName
    //{
    //  get { return _ownerModuleName; }
    //  set { _ownerModuleName = value;  }
    //}

    /// <summary>
    /// Тайм-аут попытки соединения с базой данных (сек.).
    /// </summary>
    [Range( 1, 15 )]
    public int ConnectionTimeout
    {
      [DebuggerStepThrough]
      get { return _connectionTimeout; }
      [DebuggerStepThrough]
      set { _connectionTimeout = value; }
    }

    /// <summary>
    /// Тайм-аут выполнения хранимой процедуры (сек.).
    /// </summary>
    [Range( 1, int.MaxValue )]
    public int Timeout
    {
      [DebuggerStepThrough]
      get { return _timeout; }
      [DebuggerStepThrough]
      set { _timeout = value; }
    }

    /// <summary>
    /// Получение объектов из recordset-результата работы хранимой процедуры.
    /// </summary>
    public IRSResult RS { get; private set; }

    /// <summary>
    /// Получение объектов из xml-результата работы хранимой процедуры.
    /// </summary>
    public IXmlResult Xml { get; private set; }
    #endregion
    
    #region .Static Fields
    private static readonly ConcurrentDictionary<string, Func<Dbi, CommandBehavior?, object[], IDbiDataProvider>> _replaceActions = new ConcurrentDictionary<string, Func<Dbi, CommandBehavior?, object[], IDbiDataProvider>>();

    [ThreadStatic]
    private static Dictionary<string, bool> _blockedReplaceActions;
    #endregion

    #region .Static Properties
    /// <summary>
    /// Тип БД
    /// </summary>
    public static DbiType DbiType
    {
      get
      {
        return DbiType.MSSQL;
      }
    }
    #endregion

    #region .Ctor
    public Dbi()
    {
      RS = new RSResult( this );
      Xml = new XmlResult( this );
    }
    #endregion

    #region CheckConnection
    [DebuggerStepThrough]
    private Exception CheckConnection( [Range( 1, 90 )] int interval )
    {
      var conn = new SqlConnection( Connection );

      if( conn.ConnectionString.IndexOf( CONNECTION_TIMEOUT, StringComparison.OrdinalIgnoreCase ) == -1 )
        conn.ConnectionString += ";" + CONNECTION_TIMEOUT + "=" + _connectionTimeout;

      Exception exc = null;

      for( var i = 0; i < interval / _connectionTimeout + 1; i++ )
        try
        {
          conn.Open();

          using( var comm = new SqlCommand( "select 1", conn ) )
          {
            comm.ExecuteScalar();
          }

          exc = null;

          break;
        }
        catch( Exception e )
        {
          exc = e;
        }
        finally
        {
          conn.Close();
          conn.Dispose();
        }

      return exc;
    }
    #endregion

    #region GetReplaceAction
    [DebuggerStepThrough]
    private static Func<Dbi, CommandBehavior?, object[], IDbiDataProvider> GetReplaceAction( string name )
    {
      if( _blockedReplaceActions != null )
      {
        var isActionBlocked = _blockedReplaceActions.GetValue( name );

        if( isActionBlocked )
          return null;
      }

      return _replaceActions.GetValue( name );
    }
    #endregion

    #region Execute
    /// <summary>
    /// Выполняет хранимую процедуру.
    /// </summary>
    /// <param name="name">Название хранимой процедуры.</param>
    /// <param name="args">Значения параметров хранимой процедуры.</param>
    [DebuggerStepThrough]
    public void Execute( string name, params object[] args )
    {
      var replaceAction = GetReplaceAction( name );

      using( var sp = replaceAction != null ? replaceAction( this, null, args ) : ( IDbiDataProvider ) new StoredProcedure( this, name, args ) )
        sp.Command.ExecuteNonQuery();
    }
    #endregion

    #region ExecuteAsync
    /// <summary>
    /// Выполняет хранимую процедуру в асинхронном режиме.
    /// </summary>
    /// <param name="name">Название хранимой процедуры.</param>
    /// <param name="args">Значения параметров хранимой процедуры.</param>
    [DebuggerStepThrough]
    public IExecuteAsyncResult ExecuteAsync( string name, params object[] args )
    {
      var sp = new StoredProcedure( this, true, name, args );

      return new ExecuteAsyncResult( sp.SqlCommand, sp.SqlCommand.BeginExecuteNonQuery( target =>
      {
        Exec.Try( () =>
        {
          try
          {
            sp.SqlCommand.EndExecuteNonQuery( target );
          }
          finally
          {
            sp.Dispose();
          }
        } );
      }, null ) );
    }
    #endregion

    #region ExecuteList
    /// <summary>
    /// Выполняет хранимую процедуру для каждого объекта списка как единую транзакцию.
    /// </summary>
    /// <param name="name">Название хранимой процедуры.</param>
    /// <param name="objects">Список объектов.</param>
    [DebuggerStepThrough]
    public void ExecuteList( string name, IEnumerable objects )
    {
      if ( objects == null )
        return;

      var replaceAction = GetReplaceAction( name );

      using ( var transaction = GetTransaction() )
      {
        foreach ( var obj in objects )
          using( var sp = replaceAction != null ? replaceAction( this, null, new object[] { obj } ) : ( IDbiDataProvider ) new StoredProcedure( this, name, obj ) )
            sp.Command.ExecuteNonQuery();

        transaction.Complete();
      }
    }
    #endregion

    #region GenerateObject
    [DebuggerStepThrough]
    private static object GenerateObject( DbDataReader reader, GeneratingData data, List<ReaderData> items, bool nullAsDefault = false )
    {
      var obj = data.Ctor();

      foreach ( var item in items )
      {
        var value = reader[item.ReaderIndex];

        object defValue = item.MemberEntry.Property.ElementType.IsValueType && nullAsDefault && value == DBNull.Value && !item.MemberEntry.Property.IsNullable
          ? Activator.CreateInstance( item.MemberEntry.Property.ElementType )
          : null;

        item.MemberEntry.Property.Set( obj, value == DBNull.Value ? defValue : value );
      }

      return obj;
    }

    [DebuggerStepThrough]
    private static object GenerateObject( XmlReader node, string elementName, GeneratingData data )
    {
      object obj = null;

      if ( node.Read() && node.Name.ToUpper() == elementName.ToUpper() )
        if ( data.PrimitiveCreator != null )
          obj = data.PrimitiveCreator( node.ReadElementContentAsString() );
        else
          if ( data.Members.Count > 0 || ( data.ComplexMembers != null && data.ComplexMembers.Count > 0 ) )
          {
            obj = data.Ctor();

            while ( node.MoveToNextAttribute() )
            {
              var name = node.Name.ToUpper();

              var me = data.Members.Find( m => m.Names.Contains( name ) );

              if ( me != null )
              {
                var value = node.Value;

                me.Property.SetString( obj, string.IsNullOrEmpty( value ) ? null : value );
              }
            }

            node.MoveToElement();

            while ( node.Read() && node.NodeType != XmlNodeType.EndElement )
            {
              var name = node.Name.ToUpper();

              var me = data.ComplexMembers.Find( m => m.Names.Contains( name ) );

              if ( me != null )
              {
                if ( me.Property.MemberKind == MemberKind.Value )
                  me.Property.Set( obj, GenerateObject( node.ReadSubtree(), node.Name, new GeneratingData( me.Property.MemberType, data.Name, true ) ) );
                else
                {
                  var listType = ListType.Get( me.Property.ElementType );

                  var items = listType.GenericType.CreateFast();

                  var innerData = new GeneratingData( me.Property.ElementType, data.Name, true );

                  var subnodes = node.ReadSubtree();

                  subnodes.Read();

                  Action<string> arrayCreator = elementTypeName =>
                  {
                    while ( subnodes.Read() == true && subnodes.NodeType != XmlNodeType.EndElement )
                      listType.Add( items, GenerateObject( subnodes.ReadSubtree(), elementTypeName, innerData ) );
                  };

                  if ( innerData.PrimitiveCreator != null )
                    arrayCreator( "Item" );
                  else
                    arrayCreator( me.Property.ElementType.Name );

                  if ( me.Property.MemberKind == MemberKind.Array )
                    items = listType.ToArray( items );

                  me.Property.Set( obj, items );

                  subnodes.Close();
                }
              }
              else
                node.Skip();
            }
          }

      node.Close();

      return obj;
    }
    #endregion

    #region GetList
    private List<T> GetList<T>( Func<string, object[], T> get, string name, IEnumerable objects )
    {
      if ( objects == null )
        return null;

      var result = new List<T>();

      using ( var transaction = GetTransaction() )
      {
        foreach ( var obj in objects )
          result.Add( get( name, new object[] { obj } ) );

        transaction.Complete();
      }

      return result;
    }
    #endregion

    #region GetRecordset
    [DebuggerStepThrough]
    private DataTable GetRecordset( CommandBehavior behavior, string name, params object[] args )
    {
      var replaceAction = GetReplaceAction( name );

      using( var sp = replaceAction != null ? replaceAction( this, behavior, args ) : ( IDbiDataProvider ) new StoredProcedure.Recordset( this, behavior, name, args ) )
      {
        var dt = new DataTable();

        for ( int i = 0; i < sp.Reader.FieldCount; i++ )
          dt.Columns.Add( sp.Reader.GetName( i ) ).DataType = sp.Reader.GetFieldType( i );

        var values = new object[sp.Reader.FieldCount];

        while ( sp.Reader.Read() )
        {
          sp.Reader.GetValues( values );

          dt.Rows.Add( values );
        }

        return dt;
      }
    }
    #endregion

    #region GetRow
    /// <summary>
    /// Выполняет хранимую процедуру и возвращает результат ее работы в виде единственной записи.
    /// </summary>
    /// <param name="name">Название хранимой процедуры.</param>
    /// <param name="args">Значения параметров хранимой процедуры.</param>
    /// <returns>Запись с данными.</returns>
    [DebuggerStepThrough]
    public DataRow GetRow( string name, params object[] args )
    {
      var dt = GetRecordset( CommandBehavior.SingleRow, name, args );

      return dt.Rows.Count > 0 ? dt.Rows[0] : null;
    }
    #endregion

    #region GetRowList
    /// <summary>
    /// Выполняет хранимую процедуру для каждого объекта списка как единую транзакцию и возвращает
    /// список записей, содержащих результаты ее работы.
    /// </summary>
    /// <param name="name">Название хранимой процедуры.</param>
    /// <param name="objects">Значения параметров хранимой процедуры.</param>
    /// <returns>Список записей с данными.</returns>
    [DebuggerStepThrough]
    public List<DataRow> GetRowList( string name, IEnumerable objects )
    {
      return GetList<DataRow>( GetRow, name, objects );
    }
    #endregion

    #region GetScalar
    /// <summary>
    /// Выполняет хранимую процедуру и возвращает скалярный результат ее работы.
    /// </summary>
    /// <param name="name">Название хранимой процедуры.</param>
    /// <param name="args">Значения параметров хранимой процедуры.</param>
    /// <returns>Скалярное значение.</returns>
    [DebuggerStepThrough]
    public object GetScalar( string name, params object[] args )
    {
      return GetScalar<object>( name, args );
    }

    /// <summary>
    /// Выполняет хранимую процедуру и возвращает скалярный результат ее работы.
    /// </summary>
    /// <typeparam name="T">Тип скалярного значения.</typeparam>
    /// <param name="name">Название хранимой процедуры.</param>
    /// <param name="args">Значения параметров хранимой процедуры.</param>
    /// <returns>Скалярное значение.</returns>
    [DebuggerStepThrough]
    public T GetScalar<T>( string name, params object[] args )
    {
      var replaceAction = GetReplaceAction( name );

      using( var sp = replaceAction != null ? replaceAction( this, null, args ) : ( IDbiDataProvider ) new StoredProcedure( this, name, args ) )
      {
        var value = sp.Command.ExecuteScalar();

        return value == DBNull.Value ? default( T ) : (T)value;
      }
    }
    #endregion

    #region GetScalarList
    /// <summary>
    /// Выполняет хранимую процедуру для каждого объекта списка как единую транзакцию и возвращает
    /// список скалярных значений результатов ее работы.
    /// </summary>
    /// <typeparam name="T">Тип скалярного значения.</typeparam>
    /// <param name="name">Название хранимой процедуры.</param>
    /// <param name="objects">Список объектов.</param>
    /// <returns>Список скалярных значений.</returns>
    [DebuggerStepThrough]
    public List<T> GetScalarList<T>( string name, IEnumerable objects )
    {
      return GetList<T>( GetScalar<T>, name, objects );
    }
    #endregion

    #region GetTable
    /// <summary>
    /// Выполняет хранимую процедуру и возвращает результат ее работы в виде таблицы.
    /// </summary>
    /// <param name="name">Название хранимой процедуры.</param>
    /// <param name="args">Значения параметров хранимой процедуры.</param>
    /// <returns>Таблица с данными.</returns>
    [DebuggerStepThrough]
    public DataTable GetTable( string name, params object[] args )
    {
      return GetRecordset( CommandBehavior.SingleResult, name, args );
    }
    #endregion

    #region GetTableList
    /// <summary>
    /// Выполняет хранимую процедуру для каждого объекта списка как единую транзакцию и возвращает
    /// список таблиц, содержащих результаты ее работы.
    /// </summary>
    /// <param name="name">Название хранимой процедуры.</param>
    /// <param name="objects">Значения параметров хранимой процедуры.</param>
    /// <returns>Список таблиц с данными.</returns>
    [DebuggerStepThrough]
    public List<DataTable> GetTableList( string name, IEnumerable objects )
    {
      return GetList<DataTable>( GetTable, name, objects );
    }
    #endregion

    #region GetTransaction
    /// <summary>
    /// Создает область новой транзакции.
    /// </summary>
    /// <returns>Объект транзакции.</returns>
    public DbiTransaction GetTransaction()
    {
      return new DbiTransaction( this );
    }
    #endregion

    #region GetXml
    /// <summary>
    /// Выполняет хранимую процедуру и возвращает строку, содержащую xml-результат ее работы.
    /// </summary>
    /// <param name="name">Название хранимой процедуры.</param>
    /// <param name="args">Значения параметров хранимой процедуры.</param>
    /// <returns>Строка, содержащая xml.</returns>
    [DebuggerStepThrough]
    public string GetXml( string name, params object[] args )
    {
      var replaceAction = GetReplaceAction( name );

      using( var sp = replaceAction != null ? replaceAction( this, null, args ) : ( IDbiDataProvider ) new StoredProcedure.Xml( this, name, args ) )
      {
        if( !( sp is StoredProcedure.Xml ) )
          throw new NotSupportedException( "StoredProcedure.Xml" );

        var xmlSp = ( sp as StoredProcedure.Xml );

        var sb = new StringBuilder( @"<?xml version=""1.0"" encoding=""utf-16""?>", 16384 );

        while( xmlSp.Reader.Read() )
          sb.Append( xmlSp.Reader.ReadOuterXml() );

        return sb.ToString();
      }
    }
    #endregion

    #region GetXmlList
    /// <summary>
    /// Выполняет хранимую процедуру для каждого объекта списка как единую транзакцию и возвращает
    /// список строк, содержащих xml-результаты ее работы.
    /// </summary>
    /// <param name="name">Название хранимой процедуры.</param>
    /// <param name="objects">Значения параметров хранимой процедуры.</param>
    /// <returns>Список строк, содержащих xml.</returns>
    [DebuggerStepThrough]
    public List<string> GetXmlList( string name, IEnumerable objects )
    {
      return GetList<string>( GetXml, name, objects );
    }
    #endregion

    #region HasConnection
    /// <summary>
    /// Проверяет наличие соединения с БД.
    /// </summary>
    /// <param name="interval">Интервал, в течение которого будет происходить проверка соединения. Проверка соединения будет происходить через тайм-аут, задаваемый свойством ConnectionTimeout.</param>
    /// <returns>True - соединение установлено, False - за указанный интервал не удалось установить соединение.</returns>
    [DebuggerStepThrough]
    public bool HasConnection( int interval = 60 )
    {
      return CheckConnection( interval ) == null;
    }
    #endregion

    #region SetBind
    /// <summary>
    /// Указывает, что свойство не будет участвовать в сопоставлениях.
    /// Эквивалентно использованию Dbi.BindNoneAttribute.
    /// </summary>
    /// <typeparam name="T">Тип.</typeparam>
    /// <param name="e">Лямбда-выражение, содержащее обращение к свойству.</param>
    [DebuggerStepThrough]
    public static void SetBind<T>( Expression<Func<T, object>> e )
    {
      SetBind<T>( e, new Dbi.BindNoneAttribute() );
    }

    /// <summary>
    /// Динамически задает для свойства сопоставление с названиями параметров хранимых процедур
    /// и полями из результатов работы хранимых процедур. Эквивалентно использованию Dbi.BindAttribute.
    /// </summary>
    /// <typeparam name="T">Тип.</typeparam>
    /// <param name="e">Лямбда-выражение, содержащее обращение к свойству.</param>
    /// <param name="names">Сопоставляемые названия.</param>
    [DebuggerStepThrough]
    public static void SetBind<T>( Expression<Func<T, object>> e, params string[] names )
    {
      SetBind<T>( e, new Dbi.BindAttribute( names ) );
    }

    /// <summary>
    /// Динамически задает для свойства сопоставление с названиями параметров хранимых процедур
    /// и полями из результатов работы хранимых процедур или указывает, что свойство не будет участвовать в сопоставлениях.
    /// </summary>
    /// <typeparam name="T">Тип.</typeparam>
    /// <param name="e">Лямбда-выражение, содержащее обращение к свойству.</param>
    /// <param name="binding">Массив атрибутов сопоставления.</param>
    [DebuggerStepThrough]
    public static void SetBind<T>( Expression<Func<T, object>> e, params BindBaseAttribute[] binding )
    {
      var p = MemberHelper.GetProperty( e );

      if( p == null )
        throw new ArgumentException( "Lambda expression is not a property accessor.", "e" );

      MemberProperty.DynamicBinds[p.GetUniqueToken()] = binding.ToList();

      // Аналогично вызываем задание биндингов для PostgreSQL:
      Dbi_PG.SetBind<T>( e, binding );
    }
    #endregion

    #region RegisterAction
    /// <summary>
    /// Регистрирует действия для спецобработки выбранной хранимой процедуры в определенной БД.
    /// </summary>
    /// <param name="dbiType">Тип БД</param>
    /// <param name="spName">Название хранимой процедуры.</param>
    /// <param name="replaceAction">Действие, замещающее вызов хранимой процедуры и возвращающее иной интерфейс для получения данных из базы данных.</param>
    [DebuggerStepThrough]
    public static void RegisterAction<T>( DbiType dbiType, string spName, Func<T, CommandBehavior?, object[], IDbiDataProvider> replaceAction ) where T : IDbi
    {
      if( dbiType == DbiType )
      {
        if( typeof( T ) != typeof( Dbi ) )
          throw new ArgumentException( String.Format( "'{0}' is incorrect Dbi type for selected DbiType enum value '{1}'. Should be '{2}'.", typeof( T ), dbiType.ToString(), typeof( Dbi ) ) );

        if( !String.IsNullOrEmpty( spName ) && replaceAction != null )
        {
          Func<Dbi, CommandBehavior?, object[], IDbiDataProvider> replaceActionWrapper = ( Dbi dbi, System.Data.CommandBehavior? behavior, object[] args ) =>
          {
            // Wrapper необходим для общего механизма предотвращения зацикливания экшенов при переиспользовании.

            if( _blockedReplaceActions == null )
              _blockedReplaceActions = new Dictionary<string, bool>();
            else
            {
              var isActionBlocked = _blockedReplaceActions.GetValue( spName );

              if( isActionBlocked )
                return null;
            }

            try
            {
              _blockedReplaceActions[spName] = true;

              return ( replaceAction as Func<Dbi, CommandBehavior?, object[], IDbiDataProvider> )( dbi, behavior, args );
            }
            finally
            {
              _blockedReplaceActions[spName] = false;
            }
          };

          _replaceActions[spName] = replaceActionWrapper;
        }
      }

      // Аналогично вызываем регистрацию спецобработок для PostgreSQL:
      Dbi_PG.RegisterAction<T>( dbiType, spName, replaceAction );
    }
    #endregion

    #region ThrowIfNoConnection
    /// <summary>
    /// Проверяет наличие соединения с БД и выбрасывает исключение в случае невозможности установить соединение.
    /// </summary>
    /// <param name="interval">Интервал, в течение которого будет происходить проверка соединения. Проверка соединения будет происходить через тайм-аут, задаваемый свойством ConnectionTimeout.</param>
    [DebuggerStepThrough]
    public void ThrowIfNoConnection( int interval = 60 )
    {
      var exc = CheckConnection( interval );

      if ( exc != null )
        throw exc;
    }
    #endregion

    #region ToString
    /// <summary>
    /// Возвращает строку соединения с базой данных.
    /// </summary>
    /// <returns>Строка соединения с базой данных.</returns>
    public override string ToString()
    {
      return Connection;
    }
    #endregion

    #region IConnectionHolder
    SqlConnectionStringBuilder IConnectionHolder.Connection
    {
      [DebuggerStepThrough]
      get { return _connection; }
    }
    #endregion

    /// <summary>
    /// Получение объектов из recordset-результата работы хранимой процедуры.
    /// </summary>
    [SqlExceptionHandler]
    public sealed class RSResult : IRSResult, IConnectionHolder
    {
      #region .Fields
      private readonly Dbi _dbi;
      #endregion

      #region .Ctor
      internal RSResult( Dbi dbi )
      {
        _dbi = dbi;
      }
      #endregion

      #region GeneratePartialObject
      private static PartialObject<T> GeneratePartialObject<T>( DbDataReader reader, GeneratingData data, List<ReaderData> items, List<int> notBounded, bool nullAsDefault = false )
        where T : class, new()
      {
        var obj = new PartialObject<T> { Target = GenerateObject( reader, data, items, nullAsDefault ) as T };

        foreach ( var i in notBounded )
          obj.UnboundFields.Add( new KeyValuePair<string, object>( reader.GetName( i ), reader[i] ) );

        return obj;
      }
      #endregion

      #region List
      /// <summary>
      /// Возвращает список воссозданных объектов из recordset-результата работы хранимой процедуры.
      /// </summary>
      /// <typeparam name="T">Тип объекта.</typeparam>
      /// <param name="name">Название хранимой процедуры.</param>
      /// <param name="args">Значения параметров хранимой процедуры.</param>
      /// <returns>Список воссозданных объектов типа T.</returns>
      [DebuggerStepThrough]
      public List<T> List<T>( string name, params object[] args )
        where T : class, new()
      {
        var list = new List<T>();

        var data = new GeneratingData( typeof( T ), name, false );

        var replaceAction = GetReplaceAction( name );

        using( var sp = replaceAction != null ? replaceAction( _dbi, CommandBehavior.SingleResult, args ) : ( IDbiDataProvider ) new StoredProcedure.Recordset( _dbi, CommandBehavior.SingleResult, name, args ) )
        {
          var items = ReaderData.Get( sp.Reader, data.Members );

          while ( sp.Reader.Read() )
            list.Add( GenerateObject( sp.Reader, data, items ) as T );
        }

        return list;
      }
      #endregion

      #region ListDef
      /// <summary>
      /// Возвращает список воссозданных объектов из recordset-результата работы хранимой процедуры. Nullable свойства объекта будут заполнены default значениями для DBNull
      /// </summary>
      /// <typeparam name="T">Тип объекта.</typeparam>
      /// <param name="name">Название хранимой процедуры.</param>
      /// <param name="args">Значения параметров хранимой процедуры.</param>
      /// <returns>Список воссозданных объектов типа T.</returns>
      [DebuggerStepThrough]
      public List<T> ListDef<T>( string name, params object[] args )
        where T : class, new()
      {
        var list = new List<T>();

        var data = new GeneratingData( typeof( T ), name, false );

        var replaceAction = GetReplaceAction( name );

        using( var sp = replaceAction != null ? replaceAction( _dbi, CommandBehavior.SingleResult, args ) : ( IDbiDataProvider ) new StoredProcedure.Recordset( _dbi, CommandBehavior.SingleResult, name, args ) )
        {
          var items = ReaderData.Get( sp.Reader, data.Members );

          while ( sp.Reader.Read() )
            list.Add( (T)GenerateObject( sp.Reader, data, items, true ) );
        }

        return list;
      }
      #endregion

      #region ListDefOfBase
      /// <summary>
      /// Возвращает список воссозданных объектов из recordset-результата работы хранимой процедуры. Nullable свойства объекта будут заполнены default значениями для DBNull
      /// </summary>
      /// <typeparam name="TBase">Родительский (базовы) тип объекта.</typeparam>
      /// <param name="name">Название хранимой процедуры.</param>
      /// <param name="derivedTypeGetter">Функция получения наследованного (реального) типа считанного и БД объекта. На вход получает восстановленный объект родительского типа TBase, и должна вернуть тип реального объекта.</param>
      /// <param name="derivedTypeIsAlone">True - если результирующий набор записей содержит объекты разного типа, и тогда функция derivedTypeGetter будет вызвана единожды. False - функция derivedTypeGetter будет вызвана для каждой записи результирующего набора.</param>
      /// <param name="args">Значения параметров хранимой процедуры.</param>
      /// <returns>Список воссозданных объектов типа T.</returns>
      [DebuggerStepThrough]
      public IEnumerable<TBase> EnumerateOfBaseDef<TBase>( string name, Func<TBase, Type> derivedTypeGetter, bool derivedTypeIsAlone, params object[] args )
        where TBase : class, new()
      {
        var replaceAction = GetReplaceAction( name );

        using( var sp = replaceAction != null ? replaceAction( _dbi, CommandBehavior.SingleResult, args ) : ( IDbiDataProvider ) new StoredProcedure.Recordset( _dbi, CommandBehavior.SingleResult, name, args ) )
        {
          var dictGenDataDerived = new Dictionary<Type, GeneratingData>();

          Type typeDerived = null;
          GeneratingData genDataDerived = default( GeneratingData );

          while ( sp.Reader.Read() )
          {
            var genDataBased = dictGenDataDerived.GetOrAdd( typeof( TBase ), type => new GeneratingData( type, name, false ) );

            TBase baseObject = GenerateObject( sp.Reader, genDataBased, ReaderData.Get( sp.Reader, genDataBased.Members ), true ) as TBase;

            if ( !derivedTypeIsAlone || typeDerived == null )
            {
              typeDerived = derivedTypeGetter( baseObject );

              genDataDerived = dictGenDataDerived.GetOrAdd( typeDerived, type => new GeneratingData( type, name, false ) );
            }

            yield return GenerateObject( sp.Reader, genDataDerived, ReaderData.Get( sp.Reader, genDataDerived.Members ), true ) as TBase;
          }
        }
      }
      #endregion

      #region ListPartial
      /// <summary>
      /// Возвращает список частично воссозданных объектов из recordset-результата работы хранимой процедуры.
      /// </summary>
      /// <typeparam name="T">Тип объекта.</typeparam>
      /// <param name="name">Название хранимой процедуры.</param>
      /// <param name="args">Значения параметров хранимой процедуры.</param>
      /// <returns>Список частично воссозданных объектов типа T.</returns>
      [DebuggerStepThrough]
      public List<PartialObject<T>> ListPartial<T>( string name, params object[] args )
        where T : class, new()
      {
        var list = new List<PartialObject<T>>();

        var data = new GeneratingData( typeof( T ), name, false );

        var replaceAction = GetReplaceAction( name );

        using( var sp = replaceAction != null ? replaceAction( _dbi, CommandBehavior.SingleResult, args ) : ( IDbiDataProvider ) new StoredProcedure.Recordset( _dbi, CommandBehavior.SingleResult, name, args ) )
        {
          var notBounded = new List<int>();
          var items = ReaderData.Get( sp.Reader, data.Members, notBounded );

          while ( sp.Reader.Read() )
            list.Add( GeneratePartialObject<T>( sp.Reader, data, items, notBounded ) );
        }

        return list;
      }
      #endregion


      #region ListPartialDef
      /// <summary>
      /// Возвращает список частично воссозданных объектов из recordset-результата работы хранимой процедуры. Nullable свойства объекта будут заполнены default значениями для DBNull
      /// </summary>
      /// <typeparam name="T">Тип объекта.</typeparam>
      /// <param name="name">Название хранимой процедуры.</param>
      /// <param name="args">Значения параметров хранимой процедуры.</param>
      /// <returns>Список частично воссозданных объектов типа T.</returns>
      [DebuggerStepThrough]
      public List<PartialObject<T>> ListPartialDef<T>( string name, params object[] args )
        where T : class, new()
      {
        var list = new List<PartialObject<T>>();

        var data = new GeneratingData( typeof( T ), name, false );

        var replaceAction = GetReplaceAction( name );

        using( var sp = replaceAction != null ? replaceAction( _dbi, CommandBehavior.SingleResult, args ) : ( IDbiDataProvider ) new StoredProcedure.Recordset( _dbi, CommandBehavior.SingleResult, name, args ) )
        {
          var notBounded = new List<int>();
          var items = ReaderData.Get( sp.Reader, data.Members, notBounded );

          while ( sp.Reader.Read() )
            list.Add( GeneratePartialObject<T>( sp.Reader, data, items, notBounded, true ) );
        }

        return list;
      }
      #endregion

      #region ListScalar
      /// <summary>
      /// Возвращает список скалярных значений из recordset-результата работы хранимой процедуры.
      /// </summary>
      /// <typeparam name="T">Тип скалярного значения.</typeparam>
      /// <param name="name">Название хранимой процедуры.</param>
      /// <param name="args">Значения параметров хранимой процедуры.</param>
      /// <returns>Список скалярных значений типа T.</returns>
      [DebuggerStepThrough]
      public List<T> ListScalar<T>( string name, params object[] args )
      {
        var list = new List<T>();

        var replaceAction = GetReplaceAction( name );

        using( var sp = replaceAction != null ? replaceAction( _dbi, CommandBehavior.SingleResult, args ) : ( IDbiDataProvider ) new StoredProcedure.Recordset( _dbi, CommandBehavior.SingleResult, name, args ) )
          while ( sp.Reader.Read() )
          {
            var value = sp.Reader[0];

            list.Add( value == DBNull.Value ? default( T ) : (T)value );
          }

        return list;
      }
      #endregion

      #region Single
      /// <summary>
      /// Воссоздает объект из recordset-результата работы хранимой процедуры.
      /// При воссоздании объекта используется только первая запись recordset'а.
      /// </summary>
      /// <typeparam name="T">Тип объекта.</typeparam>
      /// <param name="name">Название хранимой процедуры.</param>
      /// <param name="args">Значения параметров хранимой процедуры.</param>
      /// <returns>Объект типа T.</returns>
      [DebuggerStepThrough]
      public T Single<T>( string name, params object[] args )
        where T : class, new()
      {
        var data = new GeneratingData( typeof( T ), name, false );

        var replaceAction = GetReplaceAction( name );

        using( var sp = replaceAction != null ? replaceAction( _dbi, CommandBehavior.SingleRow, args ) : ( IDbiDataProvider ) new StoredProcedure.Recordset( _dbi, CommandBehavior.SingleRow, name, args ) )
          if ( sp.Reader.Read() )
            return GenerateObject( sp.Reader, data, ReaderData.Get( sp.Reader, data.Members ) ) as T;

        return null;
      }
      #endregion

      #region SingleOfBase
      /// <summary>
      /// Воссоздает объект из recordset-результата работы хранимой процедуры.
      /// При воссоздании объекта используется только первая запись recordset'а.
      /// Конечный тип воссоздаваемого объекта определяется после вызова функции-делегата derivedTypeGetter, в которую передается воссозданный объект родительского типа.
      /// </summary>
      /// <typeparam name="TBase">Родительский (базовы) тип объекта.</typeparam>
      /// <param name="name">Название хранимой процедуры.</param>
      /// <param name="derivedTypeGetter">Функция получения наследованного (реального) типа считанного и БД объекта. На вход получает восстановленный объект родительского типа TBase, и должна вернуть тип реального объекта</param>
      /// <param name="args">Значения параметров хранимой процедуры.</param>
      /// <returns>Объект наследованного от TBase типа.</returns>
      [DebuggerStepThrough]
      public TBase SingleOfBaseDef<TBase>( string name, Func<TBase, Type> derivedTypeGetter, params object[] args )
        where TBase : class, new()
      {
        //var data = new GeneratingData( typeof( T ), name, false );

        var replaceAction = GetReplaceAction( name );

        using( var sp = replaceAction != null ? replaceAction( _dbi, CommandBehavior.SingleRow, args ) : ( IDbiDataProvider ) new StoredProcedure.Recordset( _dbi, CommandBehavior.SingleRow, name, args ) )
          if ( sp.Reader.Read() )
          {
            var genDataBased = new GeneratingData( typeof( TBase ), name, false );

            TBase baseObject = GenerateObject( sp.Reader, genDataBased, ReaderData.Get( sp.Reader, genDataBased.Members ), true ) as TBase;

            var typeDerived = derivedTypeGetter( baseObject );

            var genDataDerived = new GeneratingData( typeDerived, name, false );

            return GenerateObject( sp.Reader, genDataDerived, ReaderData.Get( sp.Reader, genDataDerived.Members ), true ) as TBase;
          }

        return null;
      }
      #endregion

      #region SinglePartial
      /// <summary>
      /// Воссоздает частично объект из recordset-результата работы хранимой процедуры.
      /// При воссоздании объекта используется только первая запись recordset'а.
      /// </summary>
      /// <typeparam name="T">Тип объекта.</typeparam>
      /// <param name="name">Название хранимой процедуры.</param>
      /// <param name="args">Значения параметров хранимой процедуры.</param>
      /// <returns>Частично воссозданный объект типа T.</returns>
      [DebuggerStepThrough]
      public PartialObject<T> SinglePartial<T>( string name, params object[] args )
        where T : class, new()
      {
        var data = new GeneratingData( typeof( T ), name, false );

        var replaceAction = GetReplaceAction( name );

        using( var sp = replaceAction != null ? replaceAction( _dbi, CommandBehavior.SingleRow, args ) : ( IDbiDataProvider ) new StoredProcedure.Recordset( _dbi, CommandBehavior.SingleRow, name, args ) )
          if ( sp.Reader.Read() )
          {
            var notBounded = new List<int>();
            var items = ReaderData.Get( sp.Reader, data.Members, notBounded );

            return GeneratePartialObject<T>( sp.Reader, data, items, notBounded );
          }

        return null;
      }
      #endregion

      #region IConnectionHolder
      SqlConnectionStringBuilder IConnectionHolder.Connection
      {
        [DebuggerStepThrough]
        get { return ( _dbi as IConnectionHolder ).Connection; }
      }
      #endregion

      /// <summary>
      /// Класс, содержащий информацию о частично воссозданном объекте.
      /// </summary>
      /// <typeparam name="T">Тип объекта.</typeparam>
      public sealed class PartialObject<T>
      {
        #region .Fields
        private T _target;

        private readonly List<KeyValuePair<string, object>> _unboundFields = new List<KeyValuePair<string, object>>();
        #endregion

        #region .Properties
        /// <summary>
        /// Частично воссозданный объект.
        /// </summary>
        public T Target
        {
          get { return _target; }
          internal set { _target = value; }
        }

        /// <summary>
        /// Значения полей, которые не удалось восстановить в объекте.
        /// </summary>
        public List<KeyValuePair<string, object>> UnboundFields
        {
          get { return _unboundFields; }
        }
        #endregion
      }
    }

    /// <summary>
    /// Получение объектов из xml-результата работы хранимой процедуры.
    /// </summary>
    [SqlExceptionHandler]
    public sealed class XmlResult : IXmlResult, IConnectionHolder
    {
      #region .Fields
      private readonly Dbi _dbi;
      #endregion

      #region .Ctor
      internal XmlResult( Dbi dbi )
      {
        _dbi = dbi;
      }
      #endregion

      #region List
      /// <summary>
      /// Воссоздает список объектов из xml-результата работы хранимой процедуры.
      /// </summary>
      /// <typeparam name="T">Тип объекта.</typeparam>
      /// <param name="name">Название хранимой процедуры.</param>
      /// <param name="args">Значения параметров хранимой процедуры.</param>
      /// <returns>Список объектов типа T.</returns>
      [DebuggerStepThrough]
      public List<T> List<T>( string name, params object[] args )
        where T : class, new()
      {
        var list = new List<T>();

        var data = new GeneratingData( typeof( T ), name, true );

        var replaceAction = GetReplaceAction( name );

        using( var sp = replaceAction != null ? replaceAction( _dbi, CommandBehavior.SingleResult, args ) : ( IDbiDataProvider ) new StoredProcedure.Xml( _dbi, name, args ) )
        {
          if( !( sp is StoredProcedure.Xml ) )
            throw new NotSupportedException( "StoredProcedure.Xml" );

          var xmlSp = ( sp as StoredProcedure.Xml );

          while( xmlSp.Reader.Read() && xmlSp.Reader.NodeType != XmlNodeType.EndElement )
            list.Add( ( T ) GenerateObject( xmlSp.Reader.ReadSubtree(), typeof( T ).Name, data ) );
        }

        return list;
      }
      #endregion

      #region Single
      /// <summary>
      /// Воссоздает объект из xml-результата работы хранимой процедуры.
      /// </summary>
      /// <typeparam name="T">Тип объекта.</typeparam>
      /// <param name="name">Название хранимой процедуры.</param>
      /// <param name="args">Значения параметров хранимой процедуры.</param>
      /// <returns>Объект типа T.</returns>
      [DebuggerStepThrough]
      public T Single<T>( string name, params object[] args )
        where T : class, new()
      {
        var replaceAction = GetReplaceAction( name );

        using( var sp = replaceAction != null ? replaceAction( _dbi, CommandBehavior.SingleRow, args ) : ( IDbiDataProvider ) new StoredProcedure.Xml( _dbi, name, args ) )
        {
          if( !( sp is StoredProcedure.Xml ) )
            throw new NotSupportedException( "StoredProcedure.Xml" );

          var xmlSp = ( sp as StoredProcedure.Xml );

          return ( T ) GenerateObject( xmlSp.Reader, typeof( T ).Name, new GeneratingData( typeof( T ), name, true ) );
        }
      }
      #endregion

      #region IConnectionHolder
      SqlConnectionStringBuilder IConnectionHolder.Connection
      {
        [DebuggerStepThrough]
        get { return ( _dbi as IConnectionHolder ).Connection; }
      }
      #endregion
    }

    [StructLayout( LayoutKind.Auto )]
    private struct GeneratingData
    {
      #region .Fields
      public string Name;
      public Func<object> Ctor;
      public List<MemberEntry> Members;
      public List<MemberEntry> ComplexMembers;
      public Func<string, object> PrimitiveCreator;
      #endregion

      #region .Ctor
      [DebuggerStepThrough]
      public GeneratingData( Type type, string name, bool complexTypeMembers )
      {
        Name = Utils.GetNormalizedName( name );

        if ( type.IsValueType || type == typeof( string ) )
        {
          Ctor = null;
          Members = null;
          ComplexMembers = null;

          if ( type == typeof( string ) )
            PrimitiveCreator = str => str;
          else
            PrimitiveCreator = str => Convert.ChangeType( str, type, NumberFormatInfo.InvariantInfo );
        }
        else
        {
          Ctor = Runtime.GetCreator( type );
          Members = MemberProperty.GetOutMembers( type, Name ).Where( me => !me.Property.IsComplexType ).ToList();
          ComplexMembers = complexTypeMembers ? MemberProperty.GetOutMembers( type, Name ).Where( me => me.Property.IsComplexType ).ToList() : null;
          PrimitiveCreator = null;
        }
      }
      #endregion
    }

    [StructLayout( LayoutKind.Auto )]
    private struct ReaderData
    {
      #region .Fields
      public int ReaderIndex;
      public MemberEntry MemberEntry;
      #endregion

      #region Get
      [DebuggerStepThrough]
      static public List<ReaderData> Get( DbDataReader reader, List<MemberEntry> members )
      {
        return Get( reader, members, null );
      }

      [DebuggerStepThrough]
      static public List<ReaderData> Get( DbDataReader reader, List<MemberEntry> members, List<int> notBounded )
      {
        var list = new List<ReaderData>();

        for ( int i = 0; i < reader.FieldCount; i++ )
        {
          var name = reader.GetName( i ).ToUpper();

          var me = members.Find( m => m.Names.Contains( name ) );

          if ( me != null )
            list.Add( new ReaderData { ReaderIndex = i, MemberEntry = me } );
          else
            if ( notBounded != null )
              notBounded.Add( i );
        }

        return list;
      }
      #endregion
    }

    private class ListType
    {
      #region .Static Fields
      private static readonly ConcurrentDictionary<Type, ListType> _cache = new ConcurrentDictionary<Type, ListType>();
      #endregion

      #region .Fields
      public Type GenericType { get; private set; }

      public Action<object, object> Add { get; private set; }
      public Func<object, object> ToArray { get; private set; }
      #endregion

      #region Get
      public static ListType Get( Type elementType )
      {
        return _cache.GetOrAdd( elementType, et =>
        {
          var listType = new ListType { GenericType = typeof( List<> ).MakeGenericType( elementType ) };

          var dm = new DynamicMethod( "", typeof( void ), new Type[] { typeof( object ), typeof( object ) }, typeof( ListType ), true );

          var g = dm.GetILGenerator();

          g.Emit( OpCodes.Ldarg_0 );
          g.Emit( OpCodes.Castclass, listType.GenericType );
          g.Emit( OpCodes.Ldarg_1 );
          g.Emit( elementType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, elementType );
          g.Emit( OpCodes.Call, listType.GenericType.GetMethod( "Add", new Type[] { elementType } ) );
          g.Emit( OpCodes.Ret );

          listType.Add = (Action<object, object>)dm.CreateDelegate( typeof( Action<object, object> ) );

          dm = new DynamicMethod( "", typeof( object ), new Type[] { typeof( object ) }, typeof( ListType ), true );

          g = dm.GetILGenerator();

          g.Emit( OpCodes.Ldarg_0 );
          g.Emit( OpCodes.Castclass, listType.GenericType );
          g.Emit( OpCodes.Call, listType.GenericType.GetMethod( "ToArray" ) );
          g.Emit( OpCodes.Ret );

          listType.ToArray = (Func<object, object>)dm.CreateDelegate( typeof( Func<object, object> ) );

          return listType;
        } );
      }
      #endregion
    }

    private class ExecuteAsyncResult : IExecuteAsyncResult
    {
      private SqlCommand Command;

      public IAsyncResult AsyncResult { get; private set; }

      internal ExecuteAsyncResult( SqlCommand command, IAsyncResult asyncResult )
      {
        Command = command;
        AsyncResult = asyncResult;
      }

      public void CancelCommandAndCloseConnection()
      {
        Exec.Try( Command.Cancel );

        Exec.Try( Command.Connection.Close );
      }

      public bool IsCompleted
      {
        get
        {
          return AsyncResult.IsCompleted || AsyncResult.CompletedSynchronously;
        }
      }
    }
  }

  internal interface IConnectionHolder
  {
    SqlConnectionStringBuilder Connection { get; }
  }

  public interface IExecuteAsyncResult
  {
    IAsyncResult AsyncResult { get; }

    /// <summary>
    /// Вовзращает результат выражения IAsyncResult.IsCompleted || IAsyncResult.CompletedSynchronously
    /// </summary>
    bool IsCompleted { get; }

    void CancelCommandAndCloseConnection();
  }

}
