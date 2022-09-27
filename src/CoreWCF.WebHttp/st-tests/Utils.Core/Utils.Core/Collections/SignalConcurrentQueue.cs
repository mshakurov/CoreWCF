using System.Collections.Concurrent;
using System.Threading;

namespace ST.Utils.Collections
{
  /// <summary>
  /// Потокобезопасная очередь с возможностью получать уведомления о добавлении новых элементов.
  /// </summary>
  /// <typeparam name="T">Тип элемента.</typeparam>
  public class SignalConcurrentQueue<T> : ConcurrentQueue<T>
  {
    #region .Fields
    private readonly AutoResetEvent _event = new AutoResetEvent( false );
    #endregion

    #region .Properties
    /// <summary>
    /// Дескриптор ожидания, срабатывающий при добавлении новых элементов.
    /// </summary>
    public WaitHandle WaitHandle
    {
      get { return _event; }
    }
    #endregion

    #region Enqueue
    /// <summary>
    /// Добавляет элемент в очередь и оповещает о добавлении с помощью WaitHandle.
    /// </summary>
    /// <param name="item">Элемент.</param>
    public new void Enqueue( T item )
    {
      base.Enqueue( item );

      _event.Set();
    }
    #endregion

    #region EnqueueRange
    /// <summary>
    /// Добавляет массив элементов в очередь и оповещает один раз после добваления всех элементов
    /// </summary>
    /// <param name="items"></param>
    public void EnqueueRange( T[] items )
    {
      foreach ( T item in items )
        base.Enqueue( item );

      _event.Set();
    } 
    #endregion

    /// <summary>
    /// Sets the state of the event to signaled, allowing one or more waiting threads
    ///     to proceed.
    /// </summary>
    public void EventSignal()
    {
      _event.Set();
    }
  }
}
