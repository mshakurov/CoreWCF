using ST.Utils.Attributes;
using ST.Utils.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ST.Utils.Threading
{
  public static class SimpleTaskManager
  {
    #region class SimpleTasKey
    private class SimpleTasKey2 : IEquatable<SimpleTasKey2>
    {
      public object AllTasksHolder;
      public string TaskName;

      public SimpleTasKey2( object allTasksHolder, string taskName )
      {
        AllTasksHolder = allTasksHolder;
        TaskName = taskName;
      }

      public override bool Equals( object obj )
      {
        return this.Equals( obj as SimpleTasKey2 );
      }

      public bool Equals( SimpleTasKey2 other )
      {
        return other != null && object.ReferenceEquals( this.AllTasksHolder, other.AllTasksHolder ) && this.TaskName.IsEqualCI( other.TaskName );
      }

      public override int GetHashCode()
      {
        return ( ( ( (long)this.AllTasksHolder.GetHashCode() ) << 32 ) + (long)this.TaskName.GetHashCode() ).GetHashCode();
      }

      #region == operators
      public static bool operator ==( SimpleTasKey2 one, SimpleTasKey2 other )
      {
        return
          object.ReferenceEquals( one, other )
          ||
          (object)one != null
          &&
          one.Equals( other );
      }

      public static bool operator !=( SimpleTasKey2 one, SimpleTasKey2 other )
      {
        return !( one == other );
      }
      #endregion
    }
    #endregion

    private static ConcurrentDict<SimpleTasKey2, ST.Utils.Threading.SimpleTaskBase> _taskDict = new ConcurrentDict<SimpleTasKey2, ST.Utils.Threading.SimpleTaskBase>();

    private class TaskInfo<TParam>
    {
      public SimpleTasKey2 Key;
      public ST.Utils.Threading.SimpleTaskBase Task;
      public Action<CancellationToken, TParam> Action;
      public TParam Param;
      public Action<CancellationToken, Exception, TParam> OnException;
      public Action<CancellationToken, TParam> OnFinished;
    }

    /// <summary>
    /// Стартрует таск с заданными именем и группой, если еще не был запущен предыдущий таск с таким именем в группе.
    /// </summary>
    /// <typeparam name="TParam">Тип данных параметра, передаемого в действие таска</typeparam>
    /// <param name="holder">Группа (пространство) задач</param>
    /// <param name="taskName">Имя таска</param>
    /// <param name="action">Действие таска</param>
    /// <param name="parameter">Параметр, передаемый в действие таска</param>
    /// <returns>Был ли стартован таск</returns>
    public static bool StartSingleton<TParam>( [NotNull] [MethodCallParameterValueNotInheritedFrom( typeof( string ) )] object holder, string taskName, Action<CancellationToken, TParam> action, TParam parameter )
    {
      return StartSingleton( holder, taskName, action, parameter, null, null, null );
    }

    /// <summary>
    /// Стартрует таск с заданными именем и группой. Условия запуска таска:
    /// <para>Если <paramref name="timeout"/> не задан:</para>
    /// <para>- Если запущен предыдущий таск с таким именем в группе:</para>
    /// <para>- - Новый таск не будет запущен.</para>
    /// <para>- Иначе новый таск будет запущен.</para>
    /// <para>Иначе (если <paramref name="timeout"/> задан):</para>
    /// <para>- Если запущен предыдущий таск с таким именем в группе:</para>
    /// <para>- - Если предыдущий таск работает дольше <paramref name="timeout"/>:</para>
    /// <para>- - - Предыдущий таск будет остановлен (вызван Stop), и</para>
    /// <para>- - - Новый таск будет запущен.</para>
    /// <para>- - Иначе (предыдущий таск работает меньше <paramref name="timeout"/>):</para>
    /// <para>- - - Новый таск не будет запущен.</para>
    /// <para>- Иначе (предыдущий таск не запущен):</para>
    /// <para>- - Новый таск будет запущен.</para>
    /// </summary>
    /// <typeparam name="TParam">Тип данных параметра, передаемого в действие таска</typeparam>
    /// <param name="holder">Группа (пространство) задач</param>
    /// <param name="taskName">Имя таска</param>
    /// <param name="action">Действие таска</param>
    /// <param name="parameter">Параметр, передаемый в действие таска</param>
    /// <param name="timeout">Время работы предыдущего таска в миллисекундах (см. описание метода)</param>
    /// <returns>Был ли стартован таск</returns>
    public static bool StartSingleton<TParam>( [NotNull] [MethodCallParameterValueNotInheritedFrom( typeof( string ) )] object holder, string taskName, Action<CancellationToken, TParam> action, TParam parameter, int timeout )
    {
      return StartSingleton( holder, taskName, action, parameter, timeout, null, null );
    }

    /// <summary>
    /// Стартрует таск с заданными именем и группой, если еще не был запущен предыдущий таск с таким именем в группе.
    /// </summary>
    /// <typeparam name="TParam">Тип данных параметра, передаемого в действие таска</typeparam>
    /// <param name="holder">Группа (пространство) задач</param>
    /// <param name="taskName">Имя таска</param>
    /// <param name="action">Действие таска</param>
    /// <param name="parameter">Параметр, передаемый в действие таска</param>
    /// <param name="onException">Действие при возникновении исключения</param>
    /// <returns>Был ли стартован таск</returns>
    public static bool StartSingleton<TParam>( [NotNull] [MethodCallParameterValueNotInheritedFrom( typeof( string ) )] object holder, string taskName, Action<CancellationToken, TParam> action, TParam parameter, Action<CancellationToken, Exception, TParam> onException )
    {
      return StartSingleton( holder, taskName, action, parameter, null, onException, null );
    }

    /// <summary>
    /// Стартрует таск с заданными именем и группой. Условия запуска таска:
    /// <para>Если <paramref name="timeout"/> не задан:</para>
    /// <para>- Если запущен предыдущий таск с таким именем в группе:</para>
    /// <para>- - Новый таск не будет запущен.</para>
    /// <para>- Иначе новый таск будет запущен.</para>
    /// <para>Иначе (если <paramref name="timeout"/> задан):</para>
    /// <para>- Если запущен предыдущий таск с таким именем в группе:</para>
    /// <para>- - Если предыдущий таск работает дольше <paramref name="timeout"/>:</para>
    /// <para>- - - Предыдущий таск будет остановлен (вызван Stop), и</para>
    /// <para>- - - Новый таск будет запущен.</para>
    /// <para>- - Иначе (предыдущий таск работает меньше <paramref name="timeout"/>):</para>
    /// <para>- - - Новый таск не будет запущен.</para>
    /// <para>- Иначе (предыдущий таск не запущен):</para>
    /// <para>- - Новый таск будет запущен.</para>
    /// </summary>
    /// <typeparam name="TParam">Тип данных параметра, передаемого в действие таска</typeparam>
    /// <param name="holder">Группа (пространство) задач</param>
    /// <param name="taskName">Имя таска</param>
    /// <param name="action">Действие таска</param>
    /// <param name="parameter">Параметр, передаемый в действие таска</param>
    /// <param name="timeout">Время работы предыдущего таска в миллисекундах (см. описание метода)</param>
    /// <param name="onException">Действие при возникновении исключения</param>
    /// <param name="onFinished">Действие по завершению таска</param>
    /// <returns>Был ли стартован таск</returns>
    public static bool StartSingleton<TParam>( [NotNull] [MethodCallParameterValueNotInheritedFrom( typeof( string ) )] object holder, string taskName, Action<CancellationToken, TParam> action, TParam parameter, int? timeout = null, Action<CancellationToken, Exception, TParam> onException = null, Action<CancellationToken, TParam> onFinished = null )
    {
      bool added = false;

      Func<SimpleTasKey2, ST.Utils.Threading.SimpleTask<TaskInfo<TParam>>> createTask = key => new ST.Utils.Threading.SimpleTask<TaskInfo<TParam>>( startSinglInternal, TaskCreationOptions.LongRunning );

      var _key = new SimpleTasKey2( holder, taskName );

      var task = _taskDict.AddOrUpdate( _key,
        key =>
        {
          added = true;

          return createTask( key );
        },
        ( key, exTask ) =>
        {
          added = false;

          if ( timeout.HasValue )
            if ( exTask.ElapsedMilliseconds > timeout.Value )
            {
              SimpleTaskManager.Stop( holder, taskName, 0 );

              added = true;

              return createTask( key );
            }

          return exTask;
        } ) as ST.Utils.Threading.SimpleTask<TaskInfo<TParam>>;

      if ( added )
      {
        task.Start( new TaskInfo<TParam>() { Action = action, Key = _key, Task = task, Param = parameter, OnException = onException, OnFinished = onFinished } );

        return true;
      }

      return false;
    }

    private static void startSinglInternal<TValue>( CancellationToken token, TaskInfo<TValue> taskInfo )
    {
      try
      {
        try
        {
          taskInfo.Action( token, taskInfo.Param );
        }
        finally
        {
          if ( taskInfo.OnFinished != null )
            Exec.Try( () => taskInfo.OnFinished( token, taskInfo.Param ) );

          //Удаляем из словаря только если не было замены или исключения по таймауту
          _taskDict.TryRemoveIf( taskInfo.Key, ( _, taskCurrent ) => object.ReferenceEquals( taskCurrent, taskInfo.Task ) );
        }
      }
      catch ( OperationCanceledException )
      {
        System.Diagnostics.Debug.WriteLine( "Task Canceled: {0} {1}", taskInfo.Key.AllTasksHolder, taskInfo.Key.TaskName );

        //Удаляем из словаря только если не было замены или исключения по таймауту
        _taskDict.TryRemoveIf( taskInfo.Key, ( _, taskCurrent ) => object.ReferenceEquals( taskCurrent, taskInfo.Task ) );
      }
      catch ( Exception ex )
      {
        System.Diagnostics.Debug.WriteLine( "### Exception ({0} {1}): {2}", taskInfo.Key.AllTasksHolder, taskInfo.Key.TaskName, ex );

        if ( taskInfo.OnException != null )
          taskInfo.OnException( token, ex, taskInfo.Param );

        //Удаляем из словаря только если не было замены или исключения по таймауту
        _taskDict.TryRemoveIf( taskInfo.Key, ( _, taskCurrent ) => object.ReferenceEquals( taskCurrent, taskInfo.Task ) );
      }
    }

    #region class Sequencer
    private class Sequencer
    {
      private object locker = new object();
      private ulong _value;
      public Sequencer()
      {
        _value = ulong.MaxValue;
      }
      public ulong Next()
      {
        lock ( locker )
        {
          if ( _value == ulong.MaxValue )
            _value = 0;
          else
            _value++;
          return _value;
        }
      }
    }
    #endregion
    private static Sequencer execTaskSequence = new Sequencer();

    /// <summary>
    /// Выполняет действие в скрытом таске с ожиданием завершения и с передачей ему CancellationToken
    /// </summary>
    /// <typeparam name="TParam">Тип данных параметра, передаемого в действие</typeparam>
    /// <param name="holder">Группа (пространство) задач</param>
    /// <param name="action">Действие</param>
    /// <param name="onException">Действие при возникновении исключения</param>
    /// <param name="parameter">Параметр, передаемый в действие таска</param>
    /// <param name="wait">Ожидать ли завершения действия</param>
    /// <param name="millisecondsWait">Максимальное время, отводимое на исполнение действия. По истечении времени таску будет вызван Stop/></param>
    public static void ExecuteAction<TParam>( [NotNull] [MethodCallParameterValueNotInheritedFrom( typeof( string ) )] object holder, Action<CancellationToken, TParam> action, Action<CancellationToken, Exception, TParam> onException, TParam parameter, bool wait, int millisecondsWait )
    {
      string taskName = string.Format( "ExecutionTask_{0}", execTaskSequence.Next() );

      if ( !StartSingleton( holder, taskName, action, parameter, onException ) )
        return;

      if ( !wait )
        return;

      ST.Utils.Threading.SimpleTaskBase task;
      if ( !_taskDict.TryGetValue( new SimpleTasKey2( holder, taskName ), out task ) )
        return;

      if ( !task.Join( millisecondsWait ) )
        Stop( holder, taskName, 0 );
    }

    public static bool Stop( [NotNull] [MethodCallParameterValueNotInheritedFrom( typeof( string ) )] object holder, string taskName, int millisecondsTimeout )
    {
      ST.Utils.Threading.SimpleTaskBase task;
      var key = new SimpleTasKey2( holder, taskName );
      if ( !_taskDict.TryGetValue( key, out task ) )
        return false;
      var stopped = task.Stop( millisecondsTimeout );
      _taskDict.TryRemoveIf( key, ( _, taskCurrent ) => object.ReferenceEquals( taskCurrent, task ) );
      return stopped;
    }

    public static void StopParallel( [NotNull] [MethodCallParameterValueNotInheritedFrom( typeof( string ) )] object holder, int millisecondsTimeoutForEach )
    {
      _taskDict.KeysToList().Where( key => object.ReferenceEquals( key.AllTasksHolder, holder ) ).AsParallel().ForAll( key => Stop( key.AllTasksHolder, key.TaskName, millisecondsTimeoutForEach ) );
    }

    public static bool Join( [NotNull] [MethodCallParameterValueNotInheritedFrom( typeof( string ) )] object holder, string taskName, int millisecondsTimeout )
    {
      ST.Utils.Threading.SimpleTaskBase task;
      if ( !_taskDict.TryGetValue( new SimpleTasKey2( holder, taskName ), out task ) )
        return true;

      return task.Join( millisecondsTimeout );
    }

    public static class Tester
    {
      private static System.Diagnostics.Stopwatch wrsw = new System.Diagnostics.Stopwatch();
      public static void wl( string format, params object[] arg )
      {
        wrsw.Stop();
        Console.Write( "{0:HH:mm:ss.fff} | {1:000} | {2:00000} | ", DateTime.Now, Thread.CurrentThread.ManagedThreadId, wrsw.ElapsedMilliseconds );
        Console.WriteLine( format, arg );
        wrsw.Restart();
      }

      public static void Test1()
      {
        Func<object, string, bool> start = ( ns, nm ) =>
        {
          wl( "> Starting {0} {1}", ns, nm );
          var res = StartSingleton( ns, nm,
            ( token, tp ) =>
            {
              var sw = System.Diagnostics.Stopwatch.StartNew();
              try
              {
                wl( ">>> {0}", tp );
                while ( sw.ElapsedMilliseconds <= 1000 )
                {
                  token.ThrowIfCancellationRequested();
                  Thread.Sleep( 20 );
                }
                wl( "--- < end cycle {0}", tp );
              }
              finally
              {
                sw.Stop();
                wl( "<<< {0} | Elapsed: {1}{2}", tp, sw.ElapsedMilliseconds, token.IsCancellationRequested ? "@@@ CANCELED @@@" : "" );
              }
            },
            new Tuple<object, string>( ns, nm ),
            1000,
            ( token, exc, param ) =>
            {
              wl( "Exception {0} {1}: {2}", ns, nm, exc );
            },
            ( token, param ) =>
            {
              wl( "finished {0} {1}", ns, nm );
            }
            );
          wl( "< Starting {0} {1} : started: {2}", ns, nm, res );
          return res;
        };

        Func<object, string, int, bool> stop = ( ns, nm, millis ) =>
        {
          wl( "> Stopping {0} {1}", ns, nm );
          var res = Stop( ns, nm, millis );
          wl( "< Stopping {0} {1} : stopped: {2}", ns, nm, res );
          return res;
        };

        var ns1 = new object();

        wl( "Begin" );
        wrsw.Start();
        start( ns1, "1" );
        start( ns1, "1" );
        Thread.Sleep( 300 );
        start( ns1, "1" );

        start( ns1, "2" );
        start( ns1, "2" );
        Thread.Sleep( 300 );
        start( ns1, "2" );

        wl( "> StopParallel ns1" );
        StopParallel( ns1, 100 );
        wl( "< StopParallel ns1" );

        start( ns1, "11" );
        start( ns1, "22" );
        stop( ns1, "11", 100 );
        start( ns1, "11" );
        Thread.Sleep( 200 );
        stop( ns1, "11", 0 );
        start( ns1, "11" );
        stop( ns1, "11", 0 );
        start( ns1, "11" );

        wl( "End" );
      }

      public static void Start2()
      {
        SimpleTaskManager.StartSingleton( "1", "3",
          ( token, param ) =>
          {
          }, new { F1 = 1, F2 = 2, F3 = 3 } );
      }
    }
  }
}
