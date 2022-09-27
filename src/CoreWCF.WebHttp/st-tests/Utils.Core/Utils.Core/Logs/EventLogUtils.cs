using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace ST.Utils
{
  public static class EventLogUtils
  {
    [SuppressMessage( "Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>" )]
    public static void EnsureSourceAndName( string sourceName, string logName )
    {
      Action<Func<bool>> wait = condition => { while( condition() ) Thread.Sleep( 100 ); };

      Exec.Try( () =>
      {
        if( EventLog.SourceExists( sourceName ) && EventLog.LogNameFromSourceName( sourceName, "." ) != logName )
        {
          Exec.Try( () =>
          {
            EventLog.DeleteEventSource( sourceName );

            wait( () => EventLog.SourceExists( sourceName ) );
          } );

          if( EventLog.Exists( logName ) )
            Exec.Try( () =>
            {
              EventLog.Delete( logName );

              wait( () => EventLog.Exists( logName ) );
            } );
        }

        if( !EventLog.SourceExists( sourceName ) )
          Exec.Try( () =>
          {
            EventLog.CreateEventSource( sourceName, logName );

            wait( () => !EventLog.SourceExists( sourceName ) );
          } );
      } );

      Exec.Try( () =>
      {
        if( !EventLog.SourceExists( sourceName ) )
        {
          EventLog.CreateEventSource( sourceName, "Application" );

          wait( () => !EventLog.SourceExists( sourceName ) );
        }
      } );
    }

    [SuppressMessage( "Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>" )]
    public static EventLog Create( string sourceName )
    {
      return new EventLog( EventLog.LogNameFromSourceName( sourceName, "." ), ".", sourceName );
    }
  }
}
