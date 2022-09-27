using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ST.Utils.Attributes;

namespace ST.Utils.Threading
{
  /// <summary>
  /// Только для внутреннего использования.
  /// </summary>
  public abstract class SimpleTaskBase : ISimpleTaskStop
  {
    #region .Fields
    private readonly object _locker = new object();

    private readonly TaskCreationOptions _options;

    private CancellationTokenSource _source;

    private Task _task;

    private DateTime? _startTime;
    #endregion

    #region .Properties
    protected abstract Action<CancellationToken> ActionToExecute { get; }

    public DateTime? StartTime
    {
      get
      {
        lock ( _locker )
          return _startTime;
      }
    }

    public int ElapsedMilliseconds
    {
      get
      {
        lock ( _locker )
          return _startTime.HasValue
            ? (int)( DateTime.UtcNow - _startTime.Value ).TotalMilliseconds
            : 0;
      }
    }
    #endregion

    #region .Ctor
    protected SimpleTaskBase( TaskCreationOptions options )
    {
      _options = options;
    }
    #endregion

    #region Start
    [DebuggerStepThrough]
    protected void Start()
    {
      lock ( _locker )
        if ( _task == null )
        {
          _source = new CancellationTokenSource();

          _task = Task.Factory.StartNew( () =>
            {
              try
              {
                _startTime = DateTime.UtcNow;
                ActionToExecute( _source.Token );
              }
              catch ( OperationCanceledException )
              {
              }
            }, _source.Token, _options, TaskScheduler.Default );

          do
          {
            Thread.Sleep( 0 );
          } while ( _task.Status.In( TaskStatus.Created, TaskStatus.WaitingForActivation, TaskStatus.WaitingForChildrenToComplete, TaskStatus.WaitingToRun ) );
        }
    }
    #endregion

    #region Stop
    /// <summary>
    /// Останавливает задачу. Если задача уже остановлена, то метод ничего не делает.
    /// </summary>
    [DebuggerStepThrough]
    public void Stop()
    {
      Stop( Timeout.Infinite );
    }

    [DebuggerStepThrough]
    public void InternalStop()
    {
       lock (_locker)
           if (_task != null)
           {
               Exec.Try(_source.Cancel);
           }
    }
    /// <summary>
    /// Останавливает задачу. Если задача уже остановлена, то метод ничего не делает.
    /// </summary>
    /// <param name="millisecondsTimeout">Время, отводимое задаче на остановку.</param>
    /// <returns>True - задача была остановлена за указанное время, иначе - False.</returns>
    [DebuggerStepThrough]
    public bool Stop( int millisecondsTimeout )
    {
      lock ( _locker )
        if ( _task != null )
        {
          Exec.Try( _source.Cancel );

          try
          {
            if ( !_task.Wait( millisecondsTimeout ) )
              return false;
          }
          catch
          {
          }

          Exec.Try( _source.Dispose );
          Exec.Try( _task.Dispose );

          _source = null;
          _task = null;
        }

      return true;
    }
    #endregion

    /// <summary>
    /// Ожидание остановки задачи
    /// </summary>
    /// <param name="millisecondsTimeout">Время, отводимое задаче на остановку.</param>
    /// <returns>True - задача была остановлена за указанное время, иначе - False.</returns>
    [DebuggerStepThrough]
    public bool Join( int millisecondsTimeout )
    {
      var sw = Stopwatch.StartNew();

      while ( sw.ElapsedMilliseconds < millisecondsTimeout )
      {
        lock ( _locker )
          if ( _task == null || _task.Wait( 1 ) )
            return true;
      }

      lock ( _locker )
        return _task == null || _task.Wait( 0 );
    }

    public CancellationToken GetCancellationToken()
    {
      lock ( _locker )
        return Exec.Try( () => _task != null && _source != null && !_source.IsCancellationRequested ? _source.Token : CancellationToken.None, ex => CancellationToken.None );
    }
  }

  /// <summary>
  /// Вспомогательный класс для запуска и остановки задач, поддерживающих отмену выполнения.
  /// </summary>
  public sealed class SimpleTask : SimpleTaskBase
  {
    #region .Fields
    private readonly Action<CancellationToken> _action;
    #endregion

    #region .Properties
    protected override Action<CancellationToken> ActionToExecute
    {
      get { return _action; }
    }
    #endregion

    #region .Ctor
    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="action">Действие, которое будет выполняться задачей.</param>
    /// <param name="options">Параметры запуска задачи.</param>
    public SimpleTask( [NotNull] Action<CancellationToken> action, TaskCreationOptions options )
      : base( options )
    {
      _action = action;
    }
    #endregion

    #region Start
    /// <summary>
    /// Запускает задачу. Если задача уже выполняется, то метод ничего не делает.
    /// </summary>
    [DebuggerStepThrough]
    public new void Start()
    {
      base.Start();
    }
    #endregion
  }

  /// <summary>
  /// Вспомогательный класс для запуска и остановки задач с параметром, поддерживающих отмену выполнения.
  /// </summary>
  /// <typeparam name="T">Тип параметра задачи.</typeparam>
  public sealed class SimpleTask<T> : SimpleTaskBase
  {
    #region .Fields
    private readonly Action<CancellationToken, T> _action;

    private T _value;
    #endregion

    #region .Properties
    protected override Action<CancellationToken> ActionToExecute
    {
      get { return token => _action( token, _value ); }
    }
    #endregion

    #region .Ctor
    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="action">Действие, которое будет выполняться задачей.</param>
    /// <param name="options">Параметры запуска задачи.</param>
    public SimpleTask( [NotNull] Action<CancellationToken, T> action, TaskCreationOptions options )
      : base( options )
    {
      _action = action;
    }
    #endregion

    #region Start
    /// <summary>
    /// Запускает задачу. Если задача уже выполняется, то метод ничего не делает.
    /// </summary>
    /// <param name="value">Параметр.</param>
    [DebuggerStepThrough]
    public void Start( T value )
    {
      _value = value;

      base.Start();
    }
    #endregion
  }
}
