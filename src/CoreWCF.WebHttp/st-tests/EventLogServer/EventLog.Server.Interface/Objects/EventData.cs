using System;
using System.Runtime.Serialization;

namespace ST.EventLog.Server
{
  /// <summary>
  /// Информация о событие.
  /// </summary>
  [DataContract( Namespace = Constants.MODULE_NAMESPACE )]
  [Serializable]
  public class EventData
  {
    #region .Properties
    /// <summary>
    /// Событие.
    /// </summary>
    [DataMember]
    public Event Event { get; set; }

    /// <summary>
    /// Информация по досутпности пользователя к типу события.
    /// </summary>
    [DataMember]
    public EventTypeUser Info { get; set; }
    #endregion
  }
}
