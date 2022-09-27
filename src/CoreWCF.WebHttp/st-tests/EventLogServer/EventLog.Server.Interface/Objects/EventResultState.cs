using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ST.EventLog.Server
{
  /// <summary>
  /// Состояние указанного события.
  /// </summary>
  public class EventResultState
  {
    /// <summary>
    /// Идентификатор события.
    /// </summary>
    public int EventId { get; set; }

    /// <summary>
    /// Состояние события.
    /// </summary>
    public EventState EventState { get; set; }
  }
}
