using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ST.EventLog.Server
{
  /// <summary>
  /// Интерфейс фильтрации событий.
  /// </summary>
  public interface IEventFilterProvider
  {
    /// <summary>
    /// Возвращает отфильтрованный список событий.
    /// </summary>
    /// <returns>Список пользователей.</returns>
    Func<Event[], int, Event[]> GetFilterFunc();

    /// <summary>
    /// Возвращает отфильтрованный список пользователей.
    /// </summary>
    /// <returns>Список пользователей.</returns>
    Func<EventBase, int[], int[]> GetFilterUsersFunc();
  }
}
