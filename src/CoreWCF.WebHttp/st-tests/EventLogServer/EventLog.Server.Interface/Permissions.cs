using System;
using System.ComponentModel;
using ST.Core;

namespace ST.EventLog.Permissions
{
  [Serializable]
  [DisplayName( Server.RI.Log )]
  public class EventLog : Permission
  {
  }

  [Serializable]
  [DisplayName( Server.RI.EventView )]
  public class EventView : EventLog
  {
  }

  [Serializable]
  [DisplayName( Server.RI.EventViewAll )]
  public class EventViewAll : EventView
  {
  }

  [Serializable]
  [DisplayName( Server.RI.IgnoreEventWrite )]
  public class IgnoreEventWrite : EventLog
  {
  }

  [Serializable]
  [DisplayName( Server.RI.Management )]
  public class Management : EventLog
  {
  }
}
