using System;
//using System.Data.SqlClient;
using System.Globalization;
using ST.Utils.Attributes;
using Npgsql;
using CoreWCF;

namespace ST.Utils.Exceptions
{
  /// <summary>
  /// Исключение выбрасывается при невозможности установить соединение с базой данных.
  /// </summary>
  [Serializable]
  internal sealed class PgDatabaseConnectionException : DatabaseException
  {
    #region .Static Fields
    private static readonly string[] _serverErrors = new[] { "08000", "08003", "08006", "08001", "08004", "08P01" };
    private static readonly string[] _dbErrors = new[] { "3D000", "28P01", "28000" };
    #endregion

    #region .Ctor
    internal PgDatabaseConnectionException( [NotNull] Exception exc, [NotNull] string server, [NotNull] string database ) :
      base( RI.DatabaseConnectionFault, DbiType.PostgreSQL, new Exception( ( exc is PostgresException ) && ( exc as PostgresException ).SqlState.In( _serverErrors ) ? ( server == string.Empty ? RI.DatabaseServerEmptyError : SR.GetString( RI.DatabaseServerConnectionError, server ) ) :
                                                       ( exc is PostgresException ) && ( exc as PostgresException ).SqlState.In( _dbErrors ) ? ( database == string.Empty ? SR.GetString( RI.DatabaseEmptyError, server ) : SR.GetString( RI.DatabaseConnectionError, database, server ) ) :
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
    internal static bool IsConnectionError( string errorCode )
    {
      return errorCode.In( _serverErrors ) || errorCode.In( _dbErrors );
    }
    #endregion
  }

  /// <summary>
  /// Исключение выбрасывается, если выполняемая операция в базе данных завершилась с ошибкой.
  /// </summary>
  //[Serializable]
  public partial class DatabaseGenericException/*PgDatabaseGenericException*/ : DatabaseException
  {
    #region .Ctor
    internal DatabaseGenericException( [NotNull] NpgsqlException exc, [NotNullNotEmpty] string server, [NotNullNotEmpty] string database ) :
      base( RI.DatabaseGenericFault, DbiType.PostgreSQL, new Exception( SR.GetString( RI.DatabaseGenericError, server, database, ( exc is PostgresException ? ( exc as PostgresException ).Where/*Procedure*/ : "" ), ( exc is PostgresException ? ( exc as PostgresException ).SqlState/*Number*/ : "" ) ), exc ) )
    {
    }
    #endregion
  }

  public abstract partial class DatabaseCustomException : DatabaseGenericException
  {
    internal DatabaseCustomException( [NotNull] NpgsqlException exc, [NotNullNotEmpty] string server, [NotNullNotEmpty] string database )
      : base( exc, server, database )
    {
    }
  }

  public partial class RaiseException : DatabaseCustomException
  {
    internal RaiseException( [NotNull] NpgsqlException exc, [NotNullNotEmpty] string server, [NotNullNotEmpty] string database )
      : base( exc, server, database )
    {
    }
  }

  public partial class UniqueIndexViolationException_2601 : DatabaseCustomException
  {
    internal UniqueIndexViolationException_2601( [NotNull] NpgsqlException exc, [NotNullNotEmpty] string server, [NotNullNotEmpty] string database )
      : base( exc, server, database )
    {
    }
  }

  public partial class UniqueConstraintViolationException_2627 : DatabaseCustomException
  {
    internal UniqueConstraintViolationException_2627( [NotNull] NpgsqlException exc, [NotNullNotEmpty] string server, [NotNullNotEmpty] string database )
      : base( exc, server, database )
    {
    }
  }

  public partial class InvalidAsKeyColumnInIndexException_1919 : DatabaseCustomException
  {
    internal InvalidAsKeyColumnInIndexException_1919( [NotNull] NpgsqlException exc, [NotNullNotEmpty] string server, [NotNullNotEmpty] string database )
      : base( exc, server, database )
    {
    }
  }

  public partial class ForeignKeyOrReferenceConstraintViolationException_547 : DatabaseCustomException
  {
    internal ForeignKeyOrReferenceConstraintViolationException_547( [NotNull] NpgsqlException exc, [NotNullNotEmpty] string server, [NotNullNotEmpty] string database )
      : base( exc, server, database )
    {
    }
  }

  public partial class ColumnNotNullViolationException_515 : DatabaseCustomException
  {
    internal ColumnNotNullViolationException_515( [NotNull] NpgsqlException exc, [NotNullNotEmpty] string server, [NotNullNotEmpty] string database )
      : base( exc, server, database )
    {
    }
  }

  public partial class TypeConversionFailedException_245 : DatabaseCustomException
  {
    internal TypeConversionFailedException_245( [NotNull] NpgsqlException exc, [NotNullNotEmpty] string server, [NotNullNotEmpty] string database )
      : base( exc, server, database )
    {
    }
  }

  public partial class ImplicitConversionFailedException_206 : DatabaseCustomException
  {
    internal ImplicitConversionFailedException_206( [NotNull] NpgsqlException exc, [NotNullNotEmpty] string server, [NotNullNotEmpty] string database )
      : base( exc, server, database )
    {
    }
  }

  public partial class ImplicitConversionFailedException_257 : DatabaseCustomException
  {
    internal ImplicitConversionFailedException_257( [NotNull] NpgsqlException exc, [NotNullNotEmpty] string server, [NotNullNotEmpty] string database )
      : base( exc, server, database )
    {
    }
  }

  public partial class CannotConvertException_8114 : DatabaseCustomException
  {
    internal CannotConvertException_8114( [NotNull] NpgsqlException exc, [NotNullNotEmpty] string server, [NotNullNotEmpty] string database )
      : base( exc, server, database )
    {
    }
  }

  public partial class StringTruncationException_8152 : DatabaseCustomException
  {
    internal StringTruncationException_8152( [NotNull] NpgsqlException exc, [NotNullNotEmpty] string server, [NotNullNotEmpty] string database )
      : base( exc, server, database )
    {
    }
  }
}
