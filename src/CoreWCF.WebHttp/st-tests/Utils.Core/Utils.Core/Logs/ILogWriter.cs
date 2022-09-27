using System.Diagnostics;

namespace ST.Utils
{
  public interface ILogWriter
  {
    void Write( string message, ushort eventId, int type );

    int GetLevelId ( int serverLevelId  );
  }
}
