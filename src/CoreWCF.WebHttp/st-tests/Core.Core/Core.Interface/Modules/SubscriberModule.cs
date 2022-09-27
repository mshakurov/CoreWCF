using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ST.Utils;
using ST.Utils.Attributes;

namespace ST.Core
{
  /// <summary>
  /// Базовый класс модуля, поддерживающего обработку сообщений.
  /// </summary>
  public abstract class SubscriberModule : BaseModule, ISubscriber
  {
    #region .Fields
    private readonly Dictionary<Type, Dictionary<Delegate, Delegate>> _handlers = new Dictionary<Type, Dictionary<Delegate, Delegate>>();
    private readonly ReaderWriterLockSlim _syncLock = new ReaderWriterLockSlim();
    #endregion

    #region ModifyHandlers
    private void ModifyHandlers( Action modifier )
    {
      _syncLock.EnterWriteLock();

      try
      {
        modifier();
      }
      finally
      {
        _syncLock.ExitWriteLock();
      }
    }
    #endregion

    #region Send
    /// <summary>
    /// Посылает сообщение всем подписчикам.
    /// </summary>
    /// <typeparam name="T">Тип сообщения.</typeparam>
    /// <param name="msg">Экземпляр сообщения.</param>
    [DebuggerStepThrough]
    public void Send<T>( [NotNull] T msg )
      where T : BaseMessage
    {
      ServerInterface.IfIs<IPublisher>( s => s.Send( msg ) );
    }
    #endregion

    #region Subscribe
    /// <summary>
    /// Устанавливает обработчик сообщения.
    /// </summary>
    /// <typeparam name="T">Тип сообщения.</typeparam>
    /// <param name="handler">Обработчик сообщения.</param>
    /// <param name="filter">Фильтр сообщений.</param>
    [DebuggerStepThrough]
    public void Subscribe<T>( [NotNull] Action<T> handler, Func<T, bool> filter = null )
      where T : BaseMessage
    {
      ServerInterface.IfIs<IPublisher>( s => ModifyHandlers( () =>
      {
        var t = typeof( T );

        if( !_handlers.ContainsKey( t ) )
        {
          s.Subscribe<T>();

          _handlers.Add( t, new Dictionary<Delegate, Delegate>() );
        }

        _handlers.GetValue( t ).AddOrUpdate( handler, filter );
      } ) );
    }
    #endregion

    #region Uninitialize
    /// <summary>
    /// Деинициализирует модуль.
    /// </summary>
    protected internal override void Uninitialize()
    {
      ModifyHandlers( _handlers.Clear );

      base.Uninitialize();
    }
    #endregion

    #region Unsubscribe
    /// <summary>
    /// Удаляет обработчик сообщения.
    /// </summary>
    /// <typeparam name="T">Тип сообщения.</typeparam>
    /// <param name="handler">Обработчик сообщения.</param>
    [DebuggerStepThrough]
    public void Unsubscribe<T>( [NotNull] Action<T> handler )
      where T : BaseMessage
    {
      ServerInterface.IfIs<IPublisher>( s => ModifyHandlers( () =>
      {
        var t = typeof( T );

        var dict = _handlers.GetValue( t );

        if( dict != null && dict.Remove( handler ) && dict.Count == 0 )
        {
          _handlers.Remove( t );

          s.Unsubscribe<T>();
        }
      } ) );
    }
    #endregion

    #region ISubscriber
    [DebuggerStepThrough]
    void ISubscriber.OnMessage( [NotNull] BaseMessage msg )
    {
      List<KeyValuePair<Delegate, Delegate>> list;

      _syncLock.EnterReadLock();

      try
      {
        list = _handlers.GetValue( msg.GetType() ).IfNotNull( d => d.ToList() );
      }
      finally
      {
        _syncLock.ExitReadLock();
      }

      if( list != null && list.Count > 0 )
        Parallel.ForEach( list, kvp =>
        {
          if( kvp.Value == null || (bool) kvp.Value.DynamicInvoke( msg ) == true )
            kvp.Key.DynamicInvoke( msg );
        } );
    }
    #endregion
  }
}
