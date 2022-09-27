using System;
using ST.Utils;
using ST.Utils.Attributes;

namespace ST.EventLog.Server
{
  /// <summary>
  /// Базовый класс категории события.
  /// </summary>
  [Serializable]
  public abstract class EventCategory
  {
    #region GetCode
    /// <summary>
    /// Возвращает код категории события.
    /// </summary>
    /// <returns>Код категории события.</returns>
    public int GetCode()
    {
      return GetCode( this.GetType() );
    }

    /// <summary>
    /// Возвращает код категории события.
    /// </summary>
    /// <typeparam name="T">Тип категории события.</typeparam>
    /// <returns>Код категории события.</returns>
    public static int GetCode<T>()
      where T : EventCategory
    {
      return GetCode( typeof( T ) );
    }

    /// <summary>
    /// Возвращает код категории события.
    /// </summary>
    /// <param name="type">Тип категории события.</param>
    /// <returns>Код категории события.</returns>
    public static int GetCode( Type type )
    {
      return type.GetUniqueHash();
    }
    #endregion
  }

  /// <summary>
  /// Отсутствующая категория.
  /// </summary>
  [DisplayNameLocalized( Server.RI.CategoryNone )]
  [Serializable]
  public sealed class EventCategoryNone : EventCategory
  {
  }  
}
