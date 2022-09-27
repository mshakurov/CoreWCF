using System;
using System.ComponentModel;
using System.Drawing.Design;
using ST.Core;
//using ST.UI.Editors;
using ST.Utils.Attributes;

namespace ST.EventLog.Server
{
  /// <summary>Конфигурация сервера протоколирования.</summary>
  [Serializable]
  internal sealed class EventLogConfig : ModuleDBConfig_PG
  {
    #region .Properties
    //[Editor( typeof( ConnectionEditor ), typeof( UITypeEditor ) )]
    public override string Connection { get; set; }

    public override string DefaultConnection
    {
      get { return "Data Source=(local);Initial Catalog=CP_ST-EventLog;Integrated Security=True"; }
    }

    /// <summary>
    /// Дублировать события в Windows EventLog
    /// </summary>
    [DisplayNameLocalized( RI.LogToWindowsEventLog )]
    public bool LogToWindowsEventLog { get; set; }
    #endregion
  }
}
