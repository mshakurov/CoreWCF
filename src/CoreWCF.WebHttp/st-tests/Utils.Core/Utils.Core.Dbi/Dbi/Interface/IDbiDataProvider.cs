using System;
using System.Data;
using System.Data.Common;
using System.Collections;
using System.Collections.Generic;

namespace ST.Utils
{
  /// <summary>
  /// Интерфейс для получения данных из базы данных.
  /// </summary>
  public interface IDbiDataProvider : IDisposable
  {
    DbCommand Command { get; }

    DbDataReader Reader { get; }
  }
}
