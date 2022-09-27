using System;
using System.Collections;
using System.Collections.Generic;

namespace ST.Utils
{
  /// <summary>
  /// Получение объектов из xml-результата работы хранимой процедуры.
  /// </summary>
  public interface IXmlResult
  {
    List<T> List<T>( string name, params object[] args ) where T : class, new();

    T Single<T>( string name, params object[] args ) where T : class, new();
  }
}
