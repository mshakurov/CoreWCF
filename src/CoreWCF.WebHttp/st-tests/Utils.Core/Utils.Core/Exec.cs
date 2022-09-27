using System;
using System.Diagnostics;
using ST.Utils.Attributes;

namespace ST.Utils
{
  /// <summary>
  /// Вспомогательный класс для выполнения действий.
  /// </summary>
  public static class Exec
  {
    #region IfIs
    /// <summary>
    /// Выполняет действие, если объект приводится к указанному типу.
    /// </summary>
    /// <typeparam name="T">Требуемый тип.</typeparam>
    /// <param name="obj">Объект.</param>
    /// <param name="action">Действие.</param>
    [DebuggerStepThrough]
    public static void IfIs<T>( this object obj, [NotNull] Action<T> action )
      where T : class
    {
      ( obj as T ).IfNotNull( action );
    }

    /// <summary>
    /// Выполняет действие, если объект приводится к указанному типу. Или другое действие, если не приводит.
    /// </summary>
    /// <typeparam name="T">Требуемый тип.</typeparam>
    /// <param name="obj">Объект.</param>
    /// <param name="action">Действие, если объект приводится к указанному типу.</param>
    /// <param name="elseAction">Действие, если объект не приводится к указанному типу.</param>
    [DebuggerStepThrough]
    public static void IfIs<T>( this object obj, [NotNull] Action<T> action, Action elseAction )
      where T : class
    {
      ( obj as T ).IfNotNull( action, elseAction );
    }

    /// <summary>
    /// Выполняет функцию, если объект приводится к указанному типу.
    /// </summary>
    /// <typeparam name="T">Требуемый тип.</typeparam>
    /// <typeparam name="TResult">Тип возвращаемого функцией значения.</typeparam>
    /// <param name="obj">Объект.</param>
    /// <param name="func">Функция.</param>
    /// <param name="defaultValue">Функция, возвращающая значение, если аргумент равен null. Если данная функция не указана, то возвращается default( TResult ).</param>
    /// <returns>Значение, возвращаемое функцией.</returns>
    [DebuggerStepThrough]
    public static TResult IfIs<T, TResult>( this object obj, [NotNull] Func<T, TResult> func, Func<TResult> defaultValue = null )
      where T : class
    {
      return ( obj as T ).IfNotNull( func, defaultValue );
    }
    #endregion

    #region IfNotNull
    /// <summary>
    /// Выполняет действие, если указанный аргумент не равен null.
    /// </summary>
    /// <typeparam name="T">Тип аргумента.</typeparam>
    /// <param name="arg">Аргумент.</param>
    /// <param name="action">Действие.</param>
    [DebuggerStepThrough]
    public static void IfNotNull<T>( this T arg, [NotNull] Action<T> action )
      where T : class
    {
      if ( arg != null )
        action( arg );
    }
    /// <summary>
    /// Выполняет действие, если указанный аргумент не равен null. Иначе, другое действие.
    /// </summary>
    /// <typeparam name="T">Тип аргумента.</typeparam>
    /// <param name="arg">Аргумент.</param>
    /// <param name="action">Действие, если указанный аргумент не равен null.</param>
    /// <param name="elseAction">Действие, если указанный аргумент равен null.</param>
    [DebuggerStepThrough]
    public static void IfNotNull<T>( this T arg, [NotNull] Action<T> action, Action elseAction )
      where T : class
    {
      if ( arg != null )
        action( arg );
      else
        elseAction();
    }

    /// <summary>
    /// Выполняет функцию, если указанный аргумент не равен null.
    /// </summary>
    /// <typeparam name="T">Тип аргумента.</typeparam>
    /// <typeparam name="TResult">Тип возвращаемого функцией значения.</typeparam>
    /// <param name="arg">Аргумент.</param>
    /// <param name="func">Функция.</param>
    /// <param name="defaultValue">Функция, возвращающая значение, если аргумент равен null. Если данная функция не указана, то возвращается default( TResult ).</param>
    /// <returns>Значение, возвращаемое функцией.</returns>
    [DebuggerStepThrough]
    public static TResult IfNotNull<T, TResult>( this T arg, [NotNull] Func<T, TResult> func, Func<TResult> defaultValue = null )
      where T : class
    {
      return arg != null ? func( arg ) :
             defaultValue != null ? defaultValue() :
             default( TResult );
    }
    #endregion

    #region Try
    /// <summary>
    /// Выполняет код внутри конструкции try-catch.
    /// Блок catch ничего не делает.
    /// </summary>
    /// <param name="tryBlock">Код, выполняемый в блоке try.</param>
    /// <param name="rethrow">Признак того, что исключение необходимо перевыбросить выше.</param>
    [DebuggerStepThrough]
    public static void Try( Action tryBlock, bool rethrow = false )
    {
      try
      {
        tryBlock();
      }
      catch ( Exception exc )
      {
        if ( rethrow || exc.IsCritical() )
          throw;
      }
    }

    /// <summary>
    /// Выполняет код внутри конструкции try-catch.
    /// </summary>
    /// <param name="tryBlock">Код, выполняемый в блоке try.</param>
    /// <param name="catchBlock">Код, выполняемый в блоке catch.</param>
    [DebuggerStepThrough]
    public static void Try( Action tryBlock, Action<Exception> catchBlock )
    {
      try
      {
        tryBlock();
      }
      catch ( Exception exc )
      {
        if ( exc.IsCritical() )
          throw;

        catchBlock( exc );
      }
    }

    /// <summary>
    /// Выполняет код внутри конструкции try-catch.
    /// </summary>
    /// <typeparam name="T">Тип возвращаемого значения.</typeparam>
    /// <param name="tryBlock">Код, выполняемый в блоке try.</param>
    /// <param name="catchBlock">Код, выполняемый в блоке catch.</param>
    /// <returns>Значение.</returns>
    [DebuggerStepThrough]
    public static T Try<T>( Func<T> tryBlock, Func<Exception, T> catchBlock )
    {
      try
      {
        return tryBlock();
      }
      catch ( Exception exc )
      {
        if ( exc.IsCritical() )
          throw;

        return catchBlock( exc );
      }
    }

    /// <summary>
    /// Выполняет код внутри конструкции try-catch-finally.
    /// Блок catch ничего не делает.
    /// </summary>
    /// <param name="tryBlock">Код, выполняемый в блоке try.</param>
    /// <param name="finallyBlock">Код, выполняемый в блоке finally.</param>
    /// <param name="rethrow">Признак того, что исключение необходимо перевыбросить выше.</param>
    [DebuggerStepThrough]
    public static void Try( Action tryBlock, Action finallyBlock, bool rethrow = true )
    {
      try
      {
        tryBlock();
      }
      catch ( Exception exc )
      {
        if ( rethrow || exc.IsCritical() )
          throw;
      }
      finally
      {
        finallyBlock();
      }
    }

    /// <summary>
    /// Выполняет код внутри конструкции try-catch-finally.
    /// Блок catch ничего не делает.
    /// </summary>
    /// <param name="tryBlock">Код, выполняемый в блоке try.</param>
    /// <param name="finallyBlock">Код, выполняемый в блоке finally.</param>
    /// <param name="catchBlock">Код, выполняемый в блоке catch.</param>
    [DebuggerStepThrough]
    public static void Try( Action tryBlock, Action finallyBlock, Action<Exception> catchBlock )
    {
      try
      {
        tryBlock();
      }
      catch ( Exception exc )
      {
        if ( exc.IsCritical() )
          throw;

        catchBlock( exc );
      }
      finally
      {
        finallyBlock();
      }
    }

    /// <summary>
    /// Выполняет код внутри конструкции try-catch-finally.
    /// </summary>
    /// <typeparam name="T">Тип возвращаемого значения.</typeparam>
    /// <param name="tryBlock">Код, выполняемый в блоке try.</param>
    /// <param name="finallyBlock">Код, выполняемый в блоке finally.</param>
    /// <param name="rethrow">Признак того, что исключение необходимо перевыбросить выше.</param>
    [DebuggerStepThrough]
    public static T Try<T>( Func<T> tryBlock, Action finallyBlock, bool rethrow = true )
    {
      try
      {
        return tryBlock();
      }
      catch ( Exception exc )
      {
        if ( rethrow || exc.IsCritical() )
          throw;
        else
          return default( T );
      }
      finally
      {
        finallyBlock();
      }
    }
    #endregion

    #region TryIfIs
    /// <summary>
    /// Выполняет действие, если объект приводится к указанному типу. Исключения, возникающие при выполнении действия игнорируются.
    /// </summary>
    /// <typeparam name="T">Требуемый тип.</typeparam>
    /// <param name="obj">Объект.</param>
    /// <param name="action">Действие.</param>
    [DebuggerStepThrough]
    public static void TryIfIs<T>( this object obj, [NotNull] Action<T> action )
      where T : class
    {
      ( obj as T ).TryIfNotNull( action );
    }

    /// <summary>
    /// Выполняет функцию, если объект приводится к указанному типу. Исключения, возникающие при выполнении функции игнорируются.
    /// </summary>
    /// <typeparam name="T">Требуемый тип.</typeparam>
    /// <typeparam name="TResult">Тип возвращаемого функцией значения.</typeparam>
    /// <param name="obj">Объект.</param>
    /// <param name="func">Функция.</param>
    /// <param name="defaultValue">Функция, возвращающая значение, если аргумент равен null или возникло исключение. Если данная функция не указана, то возвращается default( TResult ).</param>
    /// <returns>Значение, возвращаемое функцией.</returns>
    [DebuggerStepThrough]
    public static TResult TryIfIs<T, TResult>( this object obj, [NotNull] Func<T, TResult> func, Func<TResult> defaultValue = null )
      where T : class
    {
      return ( obj as T ).TryIfNotNull( func, defaultValue );
    }
    #endregion

    #region TryIfNotNull
    /// <summary>
    /// Выполняет действие, если указанный аргумент не равен null. Исключения, возникающие при выполнении действия игнорируются.
    /// </summary>
    /// <typeparam name="T">Тип аргумента.</typeparam>
    /// <param name="arg">Аргумент.</param>
    /// <param name="action">Действие.</param>
    [DebuggerStepThrough]
    public static void TryIfNotNull<T>( this T arg, [NotNull] Action<T> action )
      where T : class
    {
      if ( arg != null )
        try
        {
          action( arg );
        }
        catch
        {
        }
    }

    /// <summary>
    /// Выполняет функцию, если указанный аргумент не равен null. Исключения, возникающие при выполнении функции игнорируются.
    /// </summary>
    /// <typeparam name="T">Тип аргумента.</typeparam>
    /// <typeparam name="TResult">Тип возвращаемого функцией значения.</typeparam>
    /// <param name="arg">Аргумент.</param>
    /// <param name="func">Функция.</param>
    /// <param name="defaultValue">Функция, возвращающая значение, если аргумент равен null или возникло исключение. Если данная функция не указана, то возвращается default( TResult ).</param>
    /// <returns>Значение, возвращаемое функцией.</returns>
    [DebuggerStepThrough]
    public static TResult TryIfNotNull<T, TResult>( this T arg, [NotNull] Func<T, TResult> func, Func<TResult> defaultValue = null )
      where T : class
    {
      if ( arg != null )
        try
        {
          return func( arg );
        }
        catch
        {
        }

      return defaultValue != null ? defaultValue() : default( TResult );
    }
    #endregion

    #region Mesure
    public static void Mesure( Action tryBlock, Action<TimeSpan, DateTime> finishedBlock )
    {
      var sw = System.Diagnostics.Stopwatch.StartNew();
      var start = DateTime.Now;
      try
      {
        tryBlock();
      }
      finally
      {
        sw.Stop();

        Exec.Try( () => finishedBlock( sw.Elapsed, start ), false );
      }
    }

    public static TResult Mesure<TResult>( Func<TResult> tryBlock, Action<TimeSpan, DateTime, TResult> finishedBlock )
    {
      var sw = System.Diagnostics.Stopwatch.StartNew();
      var start = DateTime.Now;

      TResult result = default( TResult );

      try
      {
        result = tryBlock();
      }
      finally
      {
        sw.Stop();

        Exec.Try( () => finishedBlock( sw.Elapsed, start, result ), false );
      }

      return result;
    }
    #endregion
  }
}
