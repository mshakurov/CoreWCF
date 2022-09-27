using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ST.Utils.Threading
{
  public interface ISimpleTaskStop
  {
    void Stop();

    bool Stop( int millisecondsTimeout );
  }
}
