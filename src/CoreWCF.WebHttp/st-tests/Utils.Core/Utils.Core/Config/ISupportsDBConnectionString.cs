using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ST.Utils.Config
{
  public interface ISupportsDBConnectionString
  {
    string Connection { get; set; }
  }
}
