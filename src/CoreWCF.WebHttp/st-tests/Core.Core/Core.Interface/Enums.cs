
using System;
using System.Runtime.Serialization;
namespace ST.Core
{
  /// <summary>
  /// Предустановленные приоритеты загрузки модулей.
  /// </summary>
  public enum ModuleLoadingPriority : byte
  {
    #region .Static Fields
    /// <summary>
    /// Низший приоритет.
    /// </summary>
    Lowest = byte.MinValue,

    /// <summary>
    /// Очень низкий приоритет.
    /// </summary>
    VeryLow = 31,

    /// <summary>
    /// Низкий приоритет.
    /// </summary>
    Low = 63,

    /// <summary>
    /// Приоритет ниже обычного.
    /// </summary>
    BelowNormal = 95,

    /// <summary>
    /// Обычный приоритет.
    /// </summary>
    Normal = 127,

    /// <summary>
    /// Приоритет выше обычного.
    /// </summary>
    AboveNormal = 159,

    /// <summary>
    /// Высокий приоритет.
    /// </summary>
    High = 191,

    /// <summary>
    /// Очень высокий приоритет.
    /// </summary>
    VeryHigh = 223,

    /// <summary>
    /// Высший приоритет.
    /// </summary>
    Highest = byte.MaxValue
    #endregion
  }

  /// <summary>
  /// Тип сообщения журнала событий сервера.
  /// </summary>
  public enum ServerLogEntryType
  {
    #region .Static Fields
    /// <summary>
    /// Ошибка.
    /// </summary>
    Error = 1,

    /// <summary>
    /// Предупреждение.
    /// </summary>
    Warning = 2,

    /// <summary>
    /// Информация.
    /// </summary>
    Information = 4
    #endregion
  }

  /// <summary>
  /// Типы объектов в сообщениях.
  /// </summary>
  [Serializable]
  [DataContract( Namespace = Interface.Constants.SERVER_NAMESPACE )]
  public enum MessageObjectType
  {
    /// <summary>
    /// Экземпляр бизнес сущности.
    /// </summary>
    [EnumMember]
    Entity,

    /// <summary>
    /// Экземпляр организации.
    /// </summary>
    [EnumMember]
    Organization,

    /// <summary>
    /// Экземпляр группы организаций.
    /// </summary>
    [EnumMember]
    OrgGroup,

    /// <summary>
    /// Экземпляр часового пояса.
    /// </summary>
    [EnumMember]
    SimpleTimeZone
  }
}
