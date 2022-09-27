using System;
using System.Collections;
using System.Collections.Generic;

namespace ST.Utils
{
  /// <summary>
  /// Получение объектов из recordset-результата работы хранимой процедуры.
  /// </summary>
  public interface IRSResult
  {
    T Single<T>( string name, params object[] args ) where T : class, new();

    TBase SingleOfBaseDef<TBase>( string name, Func<TBase, Type> derivedTypeGetter, params object[] args ) where TBase : class, new();   // Похоже не используется в текущее время.

    Dbi.RSResult.PartialObject<T> SinglePartial<T>( string name, params object[] args ) where T : class, new();

    List<T> List<T>( string name, params object[] args ) where T : class, new();

    List<T> ListDef<T>( string name, params object[] args ) where T : class, new();

    List<Dbi.RSResult.PartialObject<T>> ListPartial<T>( string name, params object[] args ) where T : class, new();

    List<Dbi.RSResult.PartialObject<T>> ListPartialDef<T>( string name, params object[] args ) where T : class, new();  // Похоже не используется в текущее время.

    List<T> ListScalar<T>( string name, params object[] args );
  }
}
