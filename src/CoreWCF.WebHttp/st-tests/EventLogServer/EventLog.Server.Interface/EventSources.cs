using System;
using ST.Utils;
using ST.Utils.Attributes;

namespace ST.EventLog.Server
{
  /// <summary>
  /// Базовый класс источник события.
  /// </summary>
  [Serializable]
  public abstract class EventSource
  {
    #region GetCode
    /// <summary>
    /// Возвращает код источника события.
    /// </summary>
    /// <returns>Код источника события.</returns>
    public int GetCode()
    {
      return GetCode( this.GetType() );
    }

    /// <summary>
    /// Возвращает код источника события.
    /// </summary>
    /// <returns>Код источника события.</returns>
    public static int GetCode<T>()
      where T : EventSource
    {
      return GetCode( typeof( T ) );
    }

    /// <summary>
    /// Возвращает код источника события.
    /// </summary>
    /// <param name="type">Тип источника события.</param>
    /// <returns>Код источника события.</returns>
    public static int GetCode( Type type )
    {
      return type.GetUniqueHash();
    }
    #endregion
  }

  /// <summary>
  /// Отсутствующий источник.
  /// </summary>
  [DisplayNameLocalized( Server.RI.SourceNone )]
  [Serializable]
  public sealed class EventSourceNone : EventSource
  {    
  }  
}
