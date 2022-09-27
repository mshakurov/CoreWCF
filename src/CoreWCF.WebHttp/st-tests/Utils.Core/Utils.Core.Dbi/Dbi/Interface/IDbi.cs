using System;
using System.Data;
using System.Collections;
using System.Collections.Generic;

namespace ST.Utils
{
  /// <summary>
  /// Интерфейс для работы с базой данных через вызовы хранимых процедур.
  /// </summary>
  public interface IDbi
  {
    void Execute( string name, params object[] args );

    string Connection { get; set; }

    IRSResult RS { get; }

    IXmlResult Xml { get; }

    object GetScalar( string name, params object[] args );

    T GetScalar<T>( string name, params object[] args );

    List<T> GetScalarList<T>( string name, IEnumerable objects );  // Похоже не используется в текущее время.

    DataTable GetTable( string name, params object[] args );

    List<DataTable> GetTableList( string name, IEnumerable objects ); // Похоже не используется в текущее время.

    void ThrowIfNoConnection( int interval = 60 );
  }
}
