using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace ST.Utils
{
  public class EventLogWriter : ILogWriter
  {
    #region .Fields
    private EventLog _log;
    private object _logLock = new object();
    #endregion

    #region .Ctor
    [SuppressMessage( "Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>" )]
    public EventLogWriter( params string[] args )
    {
      if( !RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) )
        throw new Exception( "EventLogWriter is only available on Windows-like systems" );

      if( args == null || args.Length != 2 )
        throw new Exception( "Arguments is invalid for use EventLog" );

      lock( _logLock )
      {
        if( _log != null )
        {
          _log.Close();

          _log.Dispose();
        }

        EventLogUtils.EnsureSourceAndName( args[0], args[1] );

        _log = EventLogUtils.Create( args[0] );
      }
    }
    #endregion

    #region ILogWriter
    [SuppressMessage( "Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>" )]
    void ILogWriter.Write( string message, ushort eventId, int type )
    {
      lock( _logLock )
        if( _log != null )
          Exec.Try( () => _log.WriteEntry( message.TrimLength( Constants.EVENTLOG_MAXEVENTMESSAGEBYTES ), (EventLogEntryType) type, eventId ) );
    }

    int ILogWriter.GetLevelId( int serverLevelId )
    {
      return serverLevelId;
    }
    #endregion
  }
}
