using System;
using System.Runtime.Serialization;

namespace ST.EventLog.Server
{
  /// <summary>
  /// Информация, относящаяся к обработке события.
  /// </summary>
  [DataContract( Namespace = Constants.MODULE_NAMESPACE )]
  [Serializable]
  public class ProcessingInfo
  {
    #region .Properties
    /// <summary>
    /// Идентификатор события.
    /// </summary>
    [DataMember]
    public int EventId { get; set; }

    /// <summary>
    /// Идентификатор пользователя.
    /// </summary>
    [DataMember]
    public int UserId { get; set; }

    /// <summary>
    /// Время.
    /// </summary>
    [DataMember]
    public DateTime? Time { get; set; }

    /// <summary>
    /// Тип доступности события.
    /// </summary>
    [DataMember]
    public EventAccessType AccessType { get; set; }

    /// <summary>
    /// Примечание.
    /// </summary>
    [DataMember]
    public string Description { get; set; }
    #endregion
  }
}
