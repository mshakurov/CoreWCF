using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace ST.Utils
{
  public class SysLogWriter : ILogWriter
  {
    #region .Fields
    private string _identity;
    #endregion

    #region .Ctor
    [SuppressMessage( "Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>" )]
    public SysLogWriter( params string[] args )
    {
      var platform = Environment.OSVersion.Platform;

      if( !RuntimeInformation.IsOSPlatform( OSPlatform.Linux ) )
        throw new Exception( "SysLogWriter is only available on Linux-like systems" );

      if( args == null || args.Length != 1 )
        throw new Exception( "Arguments is invalid for use SysLog" );

      _identity = args[0];
    }
    #endregion

    #region DLLImport
    [DllImport( "libc" )]
    private static extern void openlog( IntPtr ident, Option option, Facility facility );

    [DllImport( "libc" )]
    private static extern void syslog( int priority, string message );

    [DllImport( "libc" )]
    private static extern void closelog();
    #endregion

    #region ILogWriter
    void ILogWriter.Write( string message, ushort eventId, int level )
    {
      if( !OperatingSystem.IsLinux() )
        return;

      if( string.IsNullOrWhiteSpace( message ) && string.IsNullOrWhiteSpace( _identity ) )
        return;

      IntPtr ident = Marshal.StringToHGlobalAnsi( _identity );

      openlog( ident, Option.Console | Option.Pid | Option.PrintError, Facility.User );

      foreach( var line in message.Split( '\n', StringSplitOptions.RemoveEmptyEntries ) )
        syslog( (int) Facility.User | level, line.Trim() );
      
      closelog();

      Marshal.FreeHGlobal( ident );
    }

    int ILogWriter.GetLevelId( int serverLevelId )
    {
      return serverLevelId == 1 ? 3 : (serverLevelId == 2 ? 4 : 6 );
    }
    #endregion

    #region .Enums
    [Flags]
    private enum Option
    {
      Pid = 0x01,
      Console = 0x02,
      Delay = 0x04,
      NoDelay = 0x08,
      NoWait = 0x10,
      PrintError = 0x20
    }

    [Flags]
    private enum Facility
    {
      User = 1 << 3, //removed other unused enum values for brevity
    }
    #endregion
  }
}
