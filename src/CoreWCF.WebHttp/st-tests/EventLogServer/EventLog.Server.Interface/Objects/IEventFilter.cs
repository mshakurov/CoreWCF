using System.ServiceModel;
using ST.Utils;

namespace ST.EventLog.Server
{
  /// <summary>
  /// Интерфейс фильтра событий.
  /// </summary>
  public interface IEventFilter
  {
    /// <summary>
    /// Идентификаторы типов.
    /// </summary>
    int[] Types { get; set; }

    /// <summary>
    /// Идентификаторы уровней.
    /// </summary>
    int[] Levels { get; set; }

    /// <summary>
    /// Идентификаторы источников.
    /// </summary>
    int[] Sources { get; set; }

    /// <summary>
    /// Идентификаторы категорий.
    /// </summary>
    int[] Categories { get; set; }

    /// <summary>
    /// Идентификаторы состояний.
    /// </summary>
    int[] States { get; set; }

    /// <summary>
    /// Идентификаторы состояния обработки для текущего пользователя.
    /// </summary>
    int[] StatesProcessed { get; set; }

    /// <summary>
    /// Фильтр по деталям события.
    /// </summary>
    QueryFilter QueryFilter { get; set; }
  }
}
