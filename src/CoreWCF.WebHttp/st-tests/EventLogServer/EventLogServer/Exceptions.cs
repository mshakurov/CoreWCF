using System;
using System.IO;
using System.ServiceModel;
using ST.Utils;
using ST.Utils.Attributes;

namespace ST.Core
{
  internal abstract class ServerActionException : Exception
  {
    #region .Properties
    public override string StackTrace
    {
      get { return DoNotUseInnerStackTrace ? base.StackTrace : InnerException.StackTrace; }
    }

    protected bool DoNotUseInnerStackTrace { get; set; }

    internal ushort LogEventId { get; private set; }
    #endregion

    #region .Ctor
    internal ServerActionException( [NotNullNotEmpty] string message, [NotNull] Exception exc, ushort eventId ) : base( SR.GetString( message ), exc )
    {
      LogEventId = eventId;
    }
    #endregion
  }


}
