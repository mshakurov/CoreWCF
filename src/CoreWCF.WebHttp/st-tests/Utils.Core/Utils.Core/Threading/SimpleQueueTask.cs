using ST.Utils.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ST.Utils.Threading
{
  public sealed class SimpleQueueTask<TQueueItem> : SimpleTaskBase
  {
    public delegate void ActionProcessingItem( TQueueItem queueItem, CancellationToken token );
    private readonly ActionProcessingItem _actionProcessingItem;

    public delegate void ActionProcessingQueue( SignalConcurrentQueue<TQueueItem> queue, CancellationToken token );
    private readonly ActionProcessingQueue _actionProcessingQueue;

    private readonly SignalConcurrentQueue<TQueueItem> Queue = new SignalConcurrentQueue<TQueueItem>();

    public int QueueLength
    {
      get
      {
        return Queue.Count;
      }
    }

    public bool QueueIsEmpty
    {
      get
      {
        return Queue.IsEmpty;
      }
    }

    public void QueueClear()
    {
      Queue.Clear();
    }

    public void Enqueue( TQueueItem item )
    {
      Queue.Enqueue( item );
    }

    public void EnqueueRange( TQueueItem[] items )
    {
      if ( items == null || items.Length == 0 )
        return;
      Queue.EnqueueRange( items );
    }

    public SimpleQueueTask( ActionProcessingItem actionProcessingItem, TaskCreationOptions options )
      : base( options )
    {
      _actionProcessingItem = actionProcessingItem;
    }

    public SimpleQueueTask( ActionProcessingQueue actionProcessingQueue, TaskCreationOptions options )
      : base( options )
    {
      _actionProcessingQueue = actionProcessingQueue;
    }

    protected override Action<CancellationToken> ActionToExecute
    {
      get
      {
        if ( _actionProcessingItem != null )
          return ExecuteByItems;
        return ExecuteBySygnal;
      }
    }

    private void ExecuteByItems( CancellationToken token )
    {
      var handles = new[] { token.WaitHandle, Queue.WaitHandle };
      while ( true )
      {
        token.ThrowIfCancellationRequested();

        while ( !Queue.IsEmpty )
        {
          TQueueItem msg;

          if ( Queue.TryDequeue( out msg ) )
          {
            token.ThrowIfCancellationRequested();

            _actionProcessingItem( msg, token );
          }
        }

        WaitHandle.WaitAny( handles );
      }
    }

    private void ExecuteBySygnal( CancellationToken token )
    {
      var handles = new[] { token.WaitHandle, Queue.WaitHandle };
      while ( true )
      {
        if ( Queue.IsEmpty )
          WaitHandle.WaitAny( handles );

        token.ThrowIfCancellationRequested();

        _actionProcessingQueue( Queue, token );
      }
    }

    public bool WaitQueueEmpty( int milliseconds, Action<int> actIfNotEmptyBeforeWait, Action<int> actIfNotEmptyAfterWait )
    {
      var len = Queue.Count;

      if ( actIfNotEmptyBeforeWait != null )
        if ( len > 0 )
          actIfNotEmptyBeforeWait( len );

      var sw = Stopwatch.StartNew();
      while ( !Queue.IsEmpty && sw.ElapsedMilliseconds < milliseconds )
        Thread.Sleep( 10 );
      sw.Stop();

      if ( actIfNotEmptyAfterWait != null )
      {
        var len2 = Queue.Count;
        if ( len2 > 0 )
          actIfNotEmptyAfterWait( len2 );
        //else
        //  if ( len > 0 )
        //    actIfNotEmptyAfterWait( 0 );
      }

      return Queue.IsEmpty;
    }

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
}
