using System;
//using System.Data.SqlClient;
using Npgsql;

namespace ST.Utils
{
  public partial class Dbi_PG
  {
    /// <summary>
    /// Класс для взаимодействия с БД в рамках транзакции.
    /// </summary>
    public sealed class DbiTransaction : IDisposable
    {
      #region .Fields
      private readonly Dbi_PG _dbi;

      private bool _disposed;
      private bool _completed;

      private NpgsqlConnection _connection;
      private NpgsqlTransaction _transaction;

      private bool _success = true;
      #endregion

      #region .Ctor
      internal DbiTransaction( Dbi_PG dbi )
      {
        _dbi = dbi;

        if( _dbi._transaction == null )
          _dbi._transaction = this;
      }
      #endregion

      #region Complete
      /// <summary>
      /// Устанавливает, что все операции внутри транзакции завершились успешно.
      /// </summary>
      public void Complete()
      {
        if( _disposed )
          throw new InvalidOperationException( "Can't execute method Complete on disposed transaction." );

        if( _completed )
          throw new InvalidOperationException( "Method Complete has been called already." );

        _completed = true;
      }
      #endregion

      #region Dispose
      /// <summary>
      /// Завершение транзакции.
      /// </summary>
      public void Dispose()
      {
        if( _disposed )
          return;

        if( !_completed )
          _dbi._transaction._success = false;

        if( _dbi._transaction == this )
        {
          try
          {
            if( _transaction != null )
              if( _success )
                _transaction.Commit();
              else
                _transaction.Rollback();
          }
          catch
          {
            throw;
          }
          finally
          {
            _dbi._transaction = null;

            if( _connection != null )
              _connection.Close();

            if( _transaction != null )
            {
              _transaction.Dispose();
              _transaction = null;
            }

            if( _connection != null )
            {
              _connection.Dispose();
              _connection = null;
            }
          }
        }

        _disposed = true;
      }
      #endregion

      #region GetTransaction
      internal NpgsqlTransaction GetTransaction()
      {
        if( _transaction == null )
        {
          _connection = new NpgsqlConnection( _dbi.Connection );

          try
          {
            _connection.Open();
          }
          catch
          {
            _connection.Close();
            _connection.Dispose();
            _connection = null;

            throw;
          }

          try
          {
            _transaction = _connection.BeginTransaction();
          }
          catch
          {
            _connection.Close();
            _connection.Dispose();
            _connection = null;

            _transaction = null;

            throw;
          }
        }

        return _transaction;
      }
      #endregion
    }
  }
}
