using ST.Utils.Collections;

namespace ST.Utils.Threading
{
  /// <summary>
  /// Задача обработки очереди объектов порциями. При запуске задачи, объекты считываются в очередь обработки, при успешной обработке, объекты удаляются из базы. Инофрмирование об ошибках производится порциями.
  /// </summary>
  /// <typeparam name="TQueueItem">Класс объекта обработки</typeparam>
  public sealed class StoredQueueTask<TQueueItem> : ISimpleTaskStop
    where TQueueItem : class, new()
  {
    private readonly SimpleTask _taks;

    private readonly SignalConcurrentQueue<TQueueItem> _queue = new SignalConcurrentQueue<TQueueItem>();
    private readonly SignalConcurrentQueue<TQueueItem> _queueInactive = new SignalConcurrentQueue<TQueueItem>();

    private int _queueActive = 0;

    private readonly string LoadItemsProcedureName;
    private readonly string RemoveItemsProcedureName;

    private readonly Dbi DB;

    private readonly Func<TQueueItem, int> ItemKeySelector;

    private readonly Func<TQueueItem, bool> FilterItem;

    private readonly int portionSize;

    private readonly Action<TQueueItem[], CancellationToken, List<Exception>> ProcessItems;

    /// <summary>
    /// Инофрмирование об ошибках.
    /// 1-й параметр - список ошибок. 2-й -ошибки прекратились, 3-й очередь очищена
    /// </summary>
    private readonly Action<Exception[], bool, bool> OnExceptions;

    /// <summary>
    /// Конструктор задачи обработки очереди объектов порциями. При запуске задачи, объекты считываются в очередь обработки, при успешной обработке, объекты удаляются из базы. Инофрмирование об ошибках производится порциями.
    /// </summary>
    /// <param name="loadItemsProcedureName">Процедура загрузки сохраненных объектов, подлежащих отправке</param>
    /// <param name="removeItemsProcedureName">Процедура удаления обработанного объекта</param>
    /// <param name="db">БД</param>
    /// <param name="itemKeySelector">Функция значения ключа объекта</param>
    /// <param name="itemsInPortion">Максимальное число объетков в порции</param>
    /// <param name="filterItem">Функция фильтрации обрабатываемых объектов. Если объект не выбран, то он будет удален из очереди и из базы</param>
    /// <param name="processItems">Процедура обработки объекта</param>
    /// <param name="onExceptions">Процедура обработки порции ошибок обработки. Инофрмирование об ошибках. 1-й параметр - список ошибок. 2-й -ошибки прекратились, 3-й очередь очищена</param>
    public StoredQueueTask( string loadItemsProcedureName, string removeItemsProcedureName, Dbi db, Func<TQueueItem, int> itemKeySelector, int itemsInPortion, Func<TQueueItem, bool> filterItem, Action<TQueueItem[], CancellationToken, List<Exception>> processItems, Action<Exception[], bool, bool> onExceptions )
    {
      LoadItemsProcedureName = loadItemsProcedureName;
      RemoveItemsProcedureName = removeItemsProcedureName;

      DB = db;

      ItemKeySelector = itemKeySelector;

      portionSize = itemsInPortion;

      FilterItem = filterItem;

      ProcessItems = processItems;

      OnExceptions = onExceptions;

      _taks = new SimpleTask( TaskExecuter, TaskCreationOptions.LongRunning );
    }

    private void LoadItems()
    {
      var messages = DB.RS.ListDef<TQueueItem>( LoadItemsProcedureName );

      foreach ( var message in messages )
        _queue.Enqueue( message );
    }

    public int QueueLength
    {
      get
      {
        return _queue.Count;
      }
    }

    public void Enqueue( TQueueItem item )
    {
      if ( _queueActive == 1 )
      {
        // переносим те, что накоплены пока были неактивны
        if ( _queueInactive.Count > 0 )
          RestoreInactiveItems();

        _queue.Enqueue( item );
      }
      else
        _queueInactive.Enqueue( item );
    }

    /// <summary>
    /// переносим те, что накоплены пока были неактивны 
    /// </summary>
    private void RestoreInactiveItems()
    {
      TQueueItem temp;
      while ( _queueInactive.Count > 0 )
        if ( _queueInactive.TryDequeue( out temp ) )
          _queue.Enqueue( temp );
    }

    public int QueueClear()
    {
      int[] ids = new int[0];

      Interlocked.Exchange( ref _queueActive, 0 );
      try
      {
        ids = _queue.ToArray().Select( m => ItemKeySelector( m ) ).ToArray();

        _queue.Clear();

        //удаляем их из базы, порциями не более 100
        if ( ids.Length > 0 )
          removeFromDb( ids );

        RestoreInactiveItems();
      }
      finally
      {
        Interlocked.Exchange( ref _queueActive, 1 );
      }

      return ids.Length;
    }

    private void removeFromDb( int[] ids )
    {
      ids.Select( ( id, index ) => new { id, grp = index / 100 } ).GroupBy( it => it.grp, ( k, itcoll ) => itcoll.Select( it => it.id ).ToArray() ).ToArray().ForEach( grp =>
        DB.Execute( RemoveItemsProcedureName, CollectionHelper.GetIdentifiersTable( grp ) ) );
    }

    public void Start()
    {
      LoadItems();

      Interlocked.Exchange( ref _queueActive, 1 );

      if ( _queueInactive.Count > 0 )
        RestoreInactiveItems();

      _taks.Start();
    }

    public void Stop()
    {
      _taks.Stop();
    }

    public bool Stop( int milliseconds = Timeout.Infinite )
    {
      return _taks.Stop( milliseconds );
    }

    private void TaskExecuter( CancellationToken token )
    {
      var handles = new[] { token.WaitHandle, _queue.WaitHandle };

      const int _cMaxSecondsLogInterval = 60;
      var exceptionList = new List<Exception>( 1000 );
      var lastExceptionsLog = DateTime.UtcNow;

      Action<bool, List<Exception>, bool> logExceptions = ( isEnd, exceptionsAdd, isQueueFree ) =>
      {
        var isBegin = exceptionList.Count == 0;

        if ( exceptionsAdd != null )
          exceptionList.AddRange( exceptionsAdd );

        if ( exceptionList.Count > 0 )
          if ( isEnd || isBegin || ( DateTime.UtcNow - lastExceptionsLog ).TotalSeconds >= _cMaxSecondsLogInterval )
          {
            lastExceptionsLog = DateTime.UtcNow;

            if ( !isBegin )
              exceptionList.RemoveAt( 0 );

            if ( exceptionList.Count > 0 )
            {
              OnExceptions( exceptionList.ToArray(), isEnd, isQueueFree );
            }

            if ( !isBegin || isEnd )
              exceptionList.Clear();
          }
      };

      try
      {
        var sendExceptList = new List<Exception>();
        var repeatedErrorCount = 0;

        while ( true )
        {
          sendExceptList.Clear();

          try
          {
            var queueCopy = _queue.ToArray();

            if ( queueCopy.Length != 0 )
            {
              int countValid = 0, countMax = Math.Min( queueCopy.Length, portionSize );
              var now = DateTime.UtcNow;

              //набираем из очереди, пока не будет countValid валидных
              var portion = queueCopy.Select( m => new { M = m, Invalid = !FilterItem( m ) } )
                .TakeWhile( m =>
                {
                  //пока нет валиных, инвалидные можно сразу исключать из очереди
                  if ( m.Invalid )
                  {
                    if ( countValid == 0 )
                    {
                      TQueueItem msg;
                      _queue.TryDequeue( out msg );
                    }
                  }
                  else
                    ++countValid;
                  return countValid <= countMax;
                } ).ToArray();

              //берем первые инвалидные и удаляем их из базы
              portion.TakeWhile( m => m.Invalid ).Select( m => ItemKeySelector( m.M ) ).ToArray()
                .IfNotNull( idsRemove =>
                {
                  if ( idsRemove.Length > 0 )
                    removeFromDb( idsRemove );
                } );

              token.ThrowIfCancellationRequested();

              //убираем из списка первые инвалидные, они уже везде удалены
              portion = portion.SkipWhile( m => m.Invalid ).ToArray();

              var validPortion = portion.Where( m => !m.Invalid ).Select( m => m.M ).ToArray();

              if ( validPortion.Length > 0 )
                ProcessItems( validPortion, token, sendExceptList );

              if ( sendExceptList.Count == 0 )
              {
                repeatedErrorCount = 0;

                //если успешно обработали валидные, то удаляем все, включая инвалидные
                DB.Execute( RemoveItemsProcedureName, CollectionHelper.GetIdentifiersTable( portion.Select( m => ItemKeySelector( m.M ) ).ToArray() ) );

                TQueueItem item;

                for ( int i = 0; i < portion.Length; i++ )
                  _queue.TryDequeue( out item );

                token.ThrowIfCancellationRequested();

                logExceptions( true, null, validPortion.Length == 0 );
              }
              else
              {
                repeatedErrorCount++;

                token.ThrowIfCancellationRequested();

                logExceptions( false, sendExceptList, validPortion.Length == 0 );
              }
            }
          }
          catch ( OperationCanceledException )
          {
            return;
          }
          catch ( Exception ex )
          {
            sendExceptList.Add( ex );

            logExceptions( false, sendExceptList, false );
          }

          if ( _queue.IsEmpty )
            WaitHandle.WaitAny( handles, _cMaxSecondsLogInterval * 1000 );
          else
            // если возникла ошибка обработки более 10 раз подряд, то зависнем на 5 сек
            if ( sendExceptList.Count > 0 && repeatedErrorCount > 10 )
              token.WaitHandle.WaitOne( 5000 );

          token.ThrowIfCancellationRequested();
        }
      }
      finally
      {
        logExceptions( false, null, false );
      }
    }
  }
}
