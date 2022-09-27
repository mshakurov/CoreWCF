using System;
using System.Data.SqlClient;
using System.Globalization;
using System.Runtime.Serialization;

using CoreWCF;

using ST.Utils;
using ST.Utils.Attributes;

namespace ST.Utils.Exceptions
{
  /// <summary>
  /// Контракт, описывающий ошибку при работе с базой данных.
  /// </summary>
  [Serializable]
  [DataContract( Namespace = Constants.DATA_TYPES_NAMESPACE )]
  public class DatabaseFault
  {
  }

  /// <summary>
  /// Базовое исключение при работе с базой данных.
  /// </summary>
  [Serializable]
  public abstract class DatabaseException : Exception, IFaultExceptionProvider
  {
    #region .Properties
    public DbiType DbiType
    {
      get;
      private set;
    }
    #endregion

    #region .Ctor
    public DatabaseException( string message, DbiType dbiType, Exception innerException )
      : base( SR.GetString( message ), innerException )
    {
      DbiType = dbiType;
    }
    #endregion

    #region GetFaultException
    public abstract FaultException GetFaultException( CultureInfo culture );
    #endregion
  }

  /// <summary>
  /// Исключение выбрасывается при ошибке работы с базой данных.
  /// </summary>
  [Serializable]
  public sealed class DatabaseException<T> : FaultException<T>
    where T : DatabaseFault, new()
  {
    #region .Properties
    public override string StackTrace
    {
      get { return null; }
    }
    #endregion

    #region .Ctor
    internal DatabaseException( string reason ) : this( new T(), reason )
    {
    }

    internal DatabaseException( T detail, [NotNullNotEmpty] string reason ) : base( detail, new FaultReason( reason ) )
    {
    }
    #endregion
  }

  /// <summary>
  /// Исключение выбрасывается при невозможности установить соединение с базой данных.
  /// </summary>
  [Serializable]
  internal sealed class DatabaseConnectionException : DatabaseException
  {
    #region .Static Fields
    private static readonly int[] _serverErrors = new[] { 2, 53 };
    private static readonly int[] _dbErrors = new[] { 4060, 18452, 18456 };
    #endregion

    #region .Ctor
    internal DatabaseConnectionException( [NotNull] SqlException exc, [NotNull] string server, [NotNull] string database ) :
      base( RI.DatabaseConnectionFault, DbiType.MSSQL, new Exception( exc.Number.In( _serverErrors ) ? (server == string.Empty ? RI.DatabaseServerEmptyError : SR.GetString( RI.DatabaseServerConnectionError, server )) :
                                                       exc.Number.In( _dbErrors ) ? (database == string.Empty ? SR.GetString( RI.DatabaseEmptyError, server ) : SR.GetString( RI.DatabaseConnectionError, database, server )) :
                                                       SR.GetString( RI.DatabaseConnectionUnknownError, database, server ), exc ) )
    {
    }
    #endregion

    #region GetFaultException
    public override FaultException GetFaultException( CultureInfo culture )
    {
      return new DatabaseException<DatabaseFault>( SR.GetString( culture, RI.DatabaseConnectionFault ) );
    }
    #endregion

    #region IsConnectionError
    internal static bool IsConnectionError( int number )
    {
      return number.In( _serverErrors ) || number.In( _dbErrors );
    }
    #endregion
  }

  /// <summary>
  /// Исключение выбрасывается, если выполняемая операция в базе данных завершилась с ошибкой.
  /// </summary>
  [Serializable]
  public partial class DatabaseGenericException : DatabaseException
  {
    #region .Ctor
    internal DatabaseGenericException( [NotNull] SqlException exc, [NotNullNotEmpty] string server, [NotNullNotEmpty] string database ) :
      base( RI.DatabaseGenericFault, DbiType.MSSQL, new Exception( SR.GetString( RI.DatabaseGenericError, server, database, exc.Procedure, exc.Number ), exc ) )
    {
    }
    #endregion

    #region GetFaultException
    public override FaultException GetFaultException( CultureInfo culture )
    {
      return new DatabaseException<DatabaseFault>( SR.GetString( culture, RI.DatabaseGenericFault ) );
    }
    #endregion
  }

  [Serializable]
  public abstract partial class DatabaseCustomException : DatabaseGenericException
  {
    public abstract int MSSQLCode
    {
      get;
    }

    public abstract string PostgreSQLCode
    {
      get;
    }

    #region .Ctor
    internal DatabaseCustomException( [NotNull] SqlException exc, [NotNullNotEmpty] string server, [NotNullNotEmpty] string database )
      : base( exc, server, database )
    {
    }
    #endregion
  }

  [Serializable]
  public partial class RaiseException : DatabaseCustomException
  {
    public override int MSSQLCode
    {
      get
      {
        return 50000;
      }
    }

    public override string PostgreSQLCode
    {
      get
      {
        return "P0001";   // P0001 - код raise_exception в PostgreSQL
      }
    }

    #region .Ctor
    internal RaiseException( [NotNull] SqlException exc, [NotNullNotEmpty] string server, [NotNullNotEmpty] string database )
      : base( exc, server, database )
    {
    }
    #endregion
  }

  [Serializable]
  public partial class UniqueIndexViolationException_2601 : DatabaseCustomException
  {
    public override int MSSQLCode
    {
      get 
      {
        return 2601;      // Cannot insert duplicate key row in object '%.*ls' with unique index '%.*ls'. The duplicate key value is %ls.
      }
    }

    public override string PostgreSQLCode
    {
      get 
      {
        return "23505";   // 23505 - код unique_violation в PostgreSQL
      }
    }

    #region .Ctor
    internal UniqueIndexViolationException_2601( [NotNull] SqlException exc, [NotNullNotEmpty] string server, [NotNullNotEmpty] string database )
      : base( exc, server, database )
    {
    }
    #endregion
  }

  [Serializable]
  public partial class UniqueConstraintViolationException_2627 : DatabaseCustomException
  {
    public override int MSSQLCode
    {
      get
      {
        return 2627;      // Violation of %ls constraint '%.*ls'. Cannot insert duplicate key in object '%.*ls'. The duplicate key value is %ls.
      }
    }

    public override string PostgreSQLCode
    {
      get
      {
        return "23505";   // 23505 - код unique_violation в PostgreSQL
      }
    }

    #region .Ctor
    internal UniqueConstraintViolationException_2627( [NotNull] SqlException exc, [NotNullNotEmpty] string server, [NotNullNotEmpty] string database )
      : base( exc, server, database )
    {
    }
    #endregion
  }

  [Serializable]
  public partial class InvalidAsKeyColumnInIndexException_1919 : DatabaseCustomException
  {
    public override int MSSQLCode
    {
      get
      {
        return 1919;      // Column '%.*ls' in table '%.*ls' is of a type that is invalid for use as a key column in an index.
      }
    }

    public override string PostgreSQLCode
    {
      get
      {
        return "";        // no analog
      }
    }

    #region .Ctor
    internal InvalidAsKeyColumnInIndexException_1919( [NotNull] SqlException exc, [NotNullNotEmpty] string server, [NotNullNotEmpty] string database )
      : base( exc, server, database )
    {
    }
    #endregion
  }

  [Serializable]
  public partial class ForeignKeyOrReferenceConstraintViolationException_547 : DatabaseCustomException
  {
    public override int MSSQLCode
    {
      get
      {
        return 547;      // The %ls statement conflicted with the %ls constraint "%.*ls". The conflict occurred in database "%.*ls", table "%.*ls"%ls%.*ls%ls.
      }
    }

    public override string PostgreSQLCode
    {
      get
      {
        return "23503";  // Код "foreign_key_violation" в PostgreSQL
        // "23503" - "foreign_key_violation"
        // "23514"	check_violation
      }
    }

    #region .Ctor
    internal ForeignKeyOrReferenceConstraintViolationException_547( [NotNull] SqlException exc, [NotNullNotEmpty] string server, [NotNullNotEmpty] string database )
      : base( exc, server, database )
    {
    }
    #endregion
  }

  [Serializable]
  public partial class ColumnNotNullViolationException_515 : DatabaseCustomException
  {
    public override int MSSQLCode
    {
      get
      {
        return 515;      // Cannot insert the value NULL into column '%.*ls', table '%.*ls'; column does not allow nulls. %ls fails.
      }
    }

    public override string PostgreSQLCode 
    {
      get
      {
        return "23502";  // "23502	not_null_violation" в PostgreSQL
      }
    }

    #region .Ctor
    internal ColumnNotNullViolationException_515( [NotNull] SqlException exc, [NotNullNotEmpty] string server, [NotNullNotEmpty] string database )
      : base( exc, server, database )
    {
    }
    #endregion
  }

  [Serializable]
  public partial class TypeConversionFailedException_245 : DatabaseCustomException
  {
    public override int MSSQLCode
    {
      get
      {
        return 245;      // Conversion failed when converting the %ls value '%.*ls' to data type %ls.
      }
    }

    public override string PostgreSQLCode
    {
      get
      {
        return "42804";  // "42804	datatype_mismatch" в PostgreSQL
        // "42804	datatype_mismatch" в PostgreSQL
        // 42P18	indeterminate_datatype
        // 42809	wrong_object_type
      }
    }

    #region .Ctor
    internal TypeConversionFailedException_245( [NotNull] SqlException exc, [NotNullNotEmpty] string server, [NotNullNotEmpty] string database )
      : base( exc, server, database )
    {
    }
    #endregion
  }

  [Serializable]
  public partial class ImplicitConversionFailedException_206 : DatabaseCustomException
  {
    public override int MSSQLCode
    {
      get
      {
        return 206;      // Operand type clash: %ls is incompatible with %ls
      }
    }

    public override string PostgreSQLCode
    {
      get
      {
        return "42804";  // "42804	datatype_mismatch" в PostgreSQL
        // "42804	datatype_mismatch" в PostgreSQL
        // 42P18	indeterminate_datatype
        // 42809	wrong_object_type
      }
    }

    #region .Ctor
    internal ImplicitConversionFailedException_206( [NotNull] SqlException exc, [NotNullNotEmpty] string server, [NotNullNotEmpty] string database )
      : base( exc, server, database )
    {
    }
    #endregion
  }

  [Serializable]
  public partial class ImplicitConversionFailedException_257 : DatabaseCustomException
  {
    public override int MSSQLCode
    {
      get
      {
        return 257;      // Implicit conversion from data type %ls to %ls is not allowed. Use the CONVERT function to run this query.
      }
    }

    public override string PostgreSQLCode
    {
      get
      {
        return "42804";  // "42804	datatype_mismatch" в PostgreSQL
        // "42804	datatype_mismatch" в PostgreSQL
        // 42P18	indeterminate_datatype
        // 42809	wrong_object_type
      }
    }

    #region .Ctor
    internal ImplicitConversionFailedException_257( [NotNull] SqlException exc, [NotNullNotEmpty] string server, [NotNullNotEmpty] string database )
      : base( exc, server, database )
    {
    }
    #endregion
  }

  [Serializable]
  public partial class CannotConvertException_8114 : DatabaseCustomException
  {
    public override int MSSQLCode
    {
      get
      {
        return 8114;      // Error converting data type %ls to %ls.
      }
    }

    public override string PostgreSQLCode
    {
      get
      {
        return "42809";  // 42809	wrong_object_type
        // "42804	datatype_mismatch" в PostgreSQL
        // 42P18	indeterminate_datatype
        // 42809	wrong_object_type
      }
    }

    #region .Ctor
    internal CannotConvertException_8114( [NotNull] SqlException exc, [NotNullNotEmpty] string server, [NotNullNotEmpty] string database )
      : base( exc, server, database )
    {
    }
    #endregion
  }

  [Serializable]
  public partial class StringTruncationException_8152 : DatabaseCustomException
  {
    public override int MSSQLCode
    {
      get
      {
        return 8152;      // String or binary data would be truncated.
      }
    }

    public override string PostgreSQLCode
    {
      get
      {
        return "22001";   // "22001	string_data_right_truncation" в PostgreSQL
      }
    }

    #region .Ctor
    internal StringTruncationException_8152( [NotNull] SqlException exc, [NotNullNotEmpty] string server, [NotNullNotEmpty] string database )
      : base( exc, server, database )
    {
    }
    #endregion
  }
}
