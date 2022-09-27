using System;
using System.Runtime.Serialization;
using ST.Core;

namespace ST.EventLog.Server
{
  /// <summary>
  /// Коммуникационное сообщений, несущее информацию о событии системы.
  /// </summary>
  [Serializable]
  [DataContract( Namespace = Constants.MODULE_NAMESPACE )]
  public abstract class EventBaseMessage : CommunicationMessage
  {
    #region .Properties
    /// <summary>
    /// Событие системы.
    /// </summary>
    [DataMember]
    public int EventId { get; set; }

    /// <summary>
    /// Типа события.
    /// </summary>
    [DataMember]
    public int TypeId { get; set; }

    /// <summary>
    /// Идентификатор группы организаций.
    /// </summary>
    [DataMember]
    public int? OrgGroupId { get; set; }
    #endregion
  }

  /// <summary>
  /// Коммуникационное сообщений, несущее информацию об отправленном событии системы.
  /// </summary>
  [Serializable]
  [DataContract( Namespace = Constants.MODULE_NAMESPACE )]
  public sealed class EventLoggedMessage : EventBaseMessage
  {    
  }

  /// <summary>
  /// Коммуникационное сообщений, несущее информацию о состоянии события системы.
  /// </summary>
  [Serializable]
  [DataContract( Namespace = Constants.MODULE_NAMESPACE )]
  public class EventStateMessage : EventBaseMessage
  {
    #region .Properties
    /// <summary>
    /// Состояние события системы.
    /// </summary>
    [DataMember]
    public EventState State { get; set; }
    #endregion
  }  
}
