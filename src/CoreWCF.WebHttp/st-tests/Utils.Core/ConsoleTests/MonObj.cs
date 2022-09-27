using System;
using System.Collections.Generic;
using System.Linq;

using ST.BusinessEntity;
using ST.BusinessEntity.Server;
using ST.Utils;

namespace ST.Telematics.Server.Entities
{
  /// <summary>
  /// Объект контроля.
  /// </summary>
  public class MonObj : Entity
  {
    #region .Propeties
    /// <summary>
    /// Идентификатор Терминала (основного).
    /// </summary>
    public int? PrimaryTerminalId { get; set; }

    /// <summary>
    /// Терминал изменен.
    /// </summary>
    public DateTime? TerminalLastChange { get; set; }
    #endregion
  }
}
