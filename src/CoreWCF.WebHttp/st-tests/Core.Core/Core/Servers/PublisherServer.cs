using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ST.Utils;
using ST.Utils.Attributes;
using ST.Utils.Collections;
using ST.Utils.Threading;

namespace ST.Core
{
  /// <summary>
  /// Класс сервера, поддерживающего обмен сообщениями.
  /// </summary>
  public class PublisherServer : BaseServer, IPublisher
  {
    #region .Fields
    private readonly Dictionary<BaseModule, Subscription> _subscriptions = new Dictionary<BaseModule, Subscription>( ModuleComparer.Instance );

    private readonly ConcurrentDictionary<Type, HashSet<BaseModule>> _messageTypes = new ConcurrentDictionary<Type, HashSet<BaseModule>>();

    //private static DateTime _lastLaunchTime;
    //private static string _filePath = Path.Combine( (new FileInfo( (new System.Uri( System.Reflection.Assembly.GetExecutingAssembly().CodeBase )).AbsolutePath )).DirectoryName, @"MessageCount.txt" );
    #endregion

    #region .Properties
    /// <summary>
    /// Список уникальных типов сообщений, на которые подписаны модули.
    /// </summary>
    protected Type[] SubscribedMessageTypes
    {
      get { return _messageTypes.Keys.ToArray(); }
    }
    #endregion

    #region .Ctor
    static PublisherServer()
    {
    }
    #endregion

    #region OnMessageReceived
    /// <summary>
    /// Вызывается при поступлении сообщения от модуля.
    /// </summary>
    /// <param name="msg">Экземпляр сообщения.</param>
    protected virtual void OnMessageReceived( BaseMessage msg )
    {
    }
    #endregion

    #region OnMessageSubscribed
    /// <summary>
    /// Вызывается при первой подписке на сообщение.
    /// </summary>
    /// <param name="messageType">Тип сообщения.</param>
    protected virtual void OnMessageSubscribed( Type messageType )
    {
    }
    #endregion

    #region OnMessageUnsubscribed
    /// <summary>
    /// Вызывается, если на сообщение больше не осталось подписок.
    /// </summary>
    /// <param name="messageType">Тип сообщения.</param>
    protected virtual void OnMessageUnsubscribed( Type messageType )
    {
    }
    #endregion

    #region OnModuleInitialized
    protected override void OnModuleInitialized( BaseModule module )
    {
      base.OnModuleInitialized( module );

      _subscriptions.GetValue( module ).IfNotNull( s => s.Start() );
    }
    #endregion

    #region OnModuleInitializing
    protected override void OnModuleInitializing( BaseModule module )
    {
      base.OnModuleInitializing( module );

      if( module is ISubscriber )
        _subscriptions.Add( module, new Subscription( module as ISubscriber, e => WriteToLog( e ) ) );
    }
    #endregion

    #region OnModulesUnloaded
    protected override void OnModulesUnloaded()
    {
      _messageTypes.Clear();

      base.OnModulesUnloaded();
    }
    #endregion

    #region OnModuleUninitialized
    protected override void OnModuleUninitialized( BaseModule module )
    {
      _subscriptions.Remove( module );

      base.OnModuleUninitialized( module );
    }
    #endregion

    #region OnModuleUninitializing
    protected override void OnModuleUninitializing( BaseModule module )
    {
      _subscriptions.GetValue( module ).IfNotNull( s =>
      {
        if( !s.Stop() )
          WriteToLog( new ModuleUnloadException( new ModuleOperationTimeOutException( RI.ModuleMessageQueueTimeOutError ), module ) );
      } );

      base.OnModuleUninitializing( module );
    }
    #endregion

    #region Send
    /// <summary>
    /// Посылает сообщение модулям.
    /// </summary>
    /// <param name="msg">Сообщение.</param>
    /// <param name="filter">Метод отбора модулей, которым сообщение должно быть послано. Если null, то сообщение будет послано всем подписанным модулям.</param>
    protected void Send( [NotNull] BaseMessage msg, Func<BaseModule, bool> filter = null )
    {
      var communicationMessage = msg as CommunicationMessage;

      if( communicationMessage != null )
        communicationMessage.Id = Interlocked.Increment( ref CommunicationMessage.MessageId );

      var context = ThreadStaticContext.Capture();

      _messageTypes.GetValue( msg.GetType() ).IfNotNull( modules => modules.ForEach( m => filter == null || filter( m ), m => 
      {
        _subscriptions.GetValue( m ).IfNotNull( subscription =>
          {
            //var currentTime = DateTime.UtcNow;

            //if( _lastLaunchTime == default( DateTime ) || currentTime.Subtract( _lastLaunchTime ) >= TimeSpan.FromSeconds( 10 ) )
            //{
            //  var typeStatistic = String.Join( ", ", subscription.Queue.GroupBy( mi => mi.Message.GetType(), ( k, gr ) => { return k.ToString() + " - " + gr.Count(); } ) );

            //  var text = string.Format( "ModuleName = {0}, MessageCount = {1} {2}\r\n", m.ToString(), subscription.MessagesCount, typeStatistic );

            //  if( !File.Exists( _filePath ) )
            //    File.WriteAllText( _filePath, text );
            //  else
            //    File.AppendAllText( _filePath, text );

            //  _lastLaunchTime = currentTime;
            //}

            subscription.Send( msg, context );
          } );
      } ) );
    }

    /// <summary>
    /// Посылает сообщение указанному модулю.
    /// </summary>
    /// <param name="msg">Сообщение.</param>
    /// <param name="module">Модуль.</param>
    protected void Send( [NotNull] BaseMessage msg, [NotNull] BaseModule module )
    {
      Send( msg, m => ModuleComparer.Instance.Equals( m, module ) );
    }
    #endregion

    #region IPublisher
    void IPublisher.Send( [NotNull] BaseMessage msg )
    {
      if( ServerState.In( ServerState.Starting, ServerState.Started ) )
      {
        Send( msg, m => !ModuleComparer.Instance.Equals( m, CallerModule ) );

        OnMessageReceived( msg );
      }
    }

    void IPublisher.Subscribe<T>()
    {
      Func<Type, HashSet<BaseModule>, HashSet<BaseModule>> subscribe = ( type, modules ) =>
      {
        lock( modules )
        {
          modules.Add( CallerModule );

          if( modules.Count == 1 )
            OnMessageSubscribed( type );

          return modules;
        }
      };

      if( ServerState.In( ServerState.Starting, ServerState.Started ) && _subscriptions.ContainsKey( CallerModule ) )
        _messageTypes.AddOrUpdate( typeof( T ), type => subscribe( type, new HashSet<BaseModule>( ModuleComparer.Instance ) ), ( type, modules ) => subscribe( type, modules ) );
    }

    void IPublisher.Unsubscribe<T>()
    {
      if( ServerState != ServerState.Stopped && _subscriptions.ContainsKey( CallerModule ) )
        _messageTypes.AddOrUpdate( typeof( T ), type => new HashSet<BaseModule>( ModuleComparer.Instance ), ( type, modules ) =>
        {
          lock( modules )
          {
            modules.Remove( CallerModule );

            if( modules.Count == 0 )
              OnMessageUnsubscribed( type );

            return modules;
          }
        } );
    }
    #endregion

    private sealed class Subscription
    {
      #region .Fields
      private readonly ISubscriber _subscriber;

      private readonly Action<Exception> _errorHandler;

      private readonly SimpleTask _task;

      private readonly SignalConcurrentQueue<MessageItem> _messages = new SignalConcurrentQueue<MessageItem>();
      #endregion

      #region .Properties
      public int MessagesCount { get { return _messages.Count; } }

      public ConcurrentQueue<MessageItem> Queue { get { return _messages; } }
      #endregion

      #region .Ctor
      public Subscription( [NotNull] ISubscriber subscriber, [NotNull] Action<Exception> errorHandler )
      {
        _subscriber = subscriber;

        _errorHandler = errorHandler;

        _task = new SimpleTask( MessagingTask, TaskCreationOptions.LongRunning );
      }
      #endregion

      #region MessagingTask
      [DebuggerStepThrough]
      private void MessagingTask( CancellationToken token )
      {
        var handles = new[] { token.WaitHandle, _messages.WaitHandle };

        while( true )
        {
          WaitHandle.WaitAny( handles );

          token.ThrowIfCancellationRequested();

          while( _messages.Count > 0 )
          {
            MessageItem item;

            if( _messages.TryDequeue( out item ) )
            {
              token.ThrowIfCancellationRequested();

              Exec.Try( () => item.Context.Execute( () => _subscriber.OnMessage( item.Message ) ), exc => _errorHandler( new ModuleMessageHandlingException( exc, _subscriber as BaseModule, item.Message.GetType() ) ) );
            }
          }
        }
      }
      #endregion

      #region Send
      public void Send( [NotNull] BaseMessage msg, [NotNull] ThreadStaticContext context )
      {
        _messages.Enqueue( new MessageItem { Message = msg.DeepClone(), Context = context } );
      }
      #endregion

      #region Start
      public void Start()
      {
        _task.Start();
      }
      #endregion

      #region Stop
      public bool Stop()
      {
        _messages.Clear();

        return _task.Stop( Constants.UNLOAD_OPERATION_TIMEOUT );
      }
      #endregion

      [StructLayout( LayoutKind.Auto )]
      public struct MessageItem
      {
        #region .Fields
        public BaseMessage Message;
        public ThreadStaticContext Context;
        #endregion
      }
    }

    /// <summary>
    /// Идентификаторы событий для журнала событий (1000 - 1999).
    /// </summary>
    protected internal static new class LogEventId
    {
      #region .Constants
      /// <summary>
      /// Ошибка, возникшая при обработке модулем сообщения.
      /// </summary>
      public const ushort ModuleMessageHandlingError = 1000;
      #endregion
    }
  }
}
