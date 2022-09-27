using System;
using System.Runtime.Serialization;
using ST.Utils;

namespace ST.EventLog.Server
{
  /// <summary>
  /// Фильтр для событий.
  /// </summary>
  [DataContract( Namespace = Constants.MODULE_NAMESPACE )]
  [Serializable]
  public class EventFilter : IEventFilter
  {
    #region .Ctor
    public EventFilter()
    {
      Types = new int[0];
      Levels = new int[0];
      Sources = new int[0];
      Categories = new int[0];
      States = new int[0];
      MonObjIds = new int[0];
    }
    #endregion

    #region IEventFilter
    /// <summary>
    /// Идентификаторы типов.
    /// </summary>
    [DataMember]
    public int[] Types { get; set; }

    /// <summary>
    /// Идентификаторы уровней.
    /// </summary>
    [DataMember]
    public int[] Levels { get; set; }

    /// <summary>
    /// Идентификаторы источников.
    /// </summary>
    [DataMember]
    public int[] Sources { get; set; }

    /// <summary>
    /// Идентификаторы категорий.
    /// </summary>
    [DataMember]
    public int[] Categories { get; set; }

    /// <summary>
    /// Идентификаторы состояний.
    /// </summary>
    [DataMember]
    public int[] States { get; set; }

    /// <summary>
    /// Идентификаторы состояния обработки для текущего пользователя.
    /// </summary>
    [DataMember]
    public int[] StatesProcessed { get; set; }

    /// <summary>
    /// Фильтр по деталям события.
    /// </summary>
    [DataMember]
    public QueryFilter QueryFilter { get; set; }

    /// <summary>
    /// Идентификатор объекта контроля.
    /// </summary>
    [DataMember]
    public int[] MonObjIds { get; set; }
    
    /// <summary>
    /// Гос.№
    /// </summary>
    [DataMember]
    public string Plate { get; set; }

    /// <summary>
    /// Гар.№
    /// </summary>
    [DataMember]
    public string GarageNumber { get; set; }

    /// <summary>
    /// Идентификатор группы ТС
    /// </summary>
    [DataMember]
    public int? MonGroupId { get; set; }
    #endregion
  }
}
