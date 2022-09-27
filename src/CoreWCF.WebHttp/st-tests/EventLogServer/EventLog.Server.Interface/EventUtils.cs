using System;
using System.Runtime.CompilerServices;
using ST.Utils.Attributes;

namespace ST.EventLog.Server
{
  public class EventUtils
  {
    #region .Static Properties
    internal static Func<int, int, Event> EventByUserId { get; private set; } 
    #endregion

    #region Initialize
    [CallsAllowedFrom( "ST.EventLog.Server" )]
    [MethodImpl( MethodImplOptions.NoInlining )]
    public static void Initialize( Func<int, int, Event> func )
    {
      if( EventByUserId == null )
        EventByUserId = func;
    } 
    #endregion
  }
}
