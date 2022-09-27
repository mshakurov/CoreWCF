using System;
using System.Runtime.Serialization;
using ST.Utils.Attributes;

namespace ST.EventLog.Server
{
  /// <summary>
  /// Тип доступности события.
  /// </summary>
  [DataContract( Namespace = Constants.MODULE_NAMESPACE ), Serializable]
  public enum EventAccessType : byte
  {
    #region .Static Fields
    /// <summary>
    /// Доступно на просмотр.
    /// </summary>
    [EnumMember, DisplayNameLocalized( RI.View )]
    View = 1,

    /// <summary>
    /// Требует обработки.
    /// </summary>
    [EnumMember, DisplayNameLocalized( RI.Simple )]
    Simple = 4,

    /// <summary>
    /// Требует групповой обработки.
    /// </summary>
    [EnumMember, DisplayNameLocalized( RI.Complex )]
    Complex = 8
    #endregion
  }

  /// <summary>
  /// Уровень события.
  /// </summary>
  [DataContract( Namespace = Constants.MODULE_NAMESPACE ), Serializable]
  public enum EventLevel : byte
  {
    #region .Static Fields
    /// <summary>
    /// Информация.
    /// </summary>
    [EnumMember, DisplayNameLocalized( RI.Information )]
    Information = 1,

    /// <summary>
    /// Предупреждение.
    /// </summary>
    [EnumMember, DisplayNameLocalized( RI.Warning )]
    Warning = 4,

    /// <summary>
    /// Ошибка.
    /// </summary>
    [EnumMember, DisplayNameLocalized( RI.Error )]
    Error = 8,

    /// <summary>
    /// Тревога.
    /// </summary>
    [EnumMember, DisplayNameLocalized( RI.Alert )]
    Alert = 16
    #endregion
  }

  /// <summary>
  /// Реакция на событие.
  /// </summary>
  [DataContract( Namespace = Constants.MODULE_NAMESPACE ), Serializable]
  public enum EventReaction : byte
  {
    #region .Static Fields
    /// <summary>
    /// Доступно на просмотр.
    /// </summary>
    [EnumMember, DisplayNameLocalized( RI.View )]
    None = 1,

    /// <summary>
    /// Требуется реакция.
    /// </summary>
    [EnumMember, DisplayNameLocalized( RI.Simple )]
    Simple = 4,

    /// <summary>
    /// Требуется групповая реакция.
    /// </summary>
    [EnumMember, DisplayNameLocalized( RI.Complex )]
    Complex = 8,

    /// <summary>
    /// Требуется смешанная реакция.
    /// </summary>
    [EnumMember, DisplayNameLocalized( RI.Mixed )]
    Mixed = 16
    #endregion
  }

  /// <summary>
  /// Состояние события.
  /// </summary>
  [DataContract( Namespace = Constants.MODULE_NAMESPACE ), Serializable]
  public enum EventState : byte
  {
    #region .Static Fields
    /// <summary>
    /// Обработка не требуется.
    /// </summary>
    [EnumMember, DisplayNameLocalized( RI.ProcessNotRequired )]
    ProcessNotRequired = 1,

    /// <summary>
    /// Не обработано.
    /// </summary>
    [EnumMember, DisplayNameLocalized( RI.NotProcessed )]
    NotProcessed = 4,

    /// <summary>
    /// Частично обработано.
    /// </summary>
    [EnumMember, DisplayNameLocalized( RI.PartiallyProcessed )]
    PartiallyProcessed = 8,

    /// <summary>
    /// Обработано пользователем.
    /// </summary>
    [EnumMember, DisplayNameLocalized( RI.Processed )]
    Processed = 16,

    /// <summary>
    /// Требуется обработка.
    /// </summary>
    [EnumMember, DisplayNameLocalized( RI.ProcessRequired )]
    ProcessRequired = 32
    #endregion
  }
}
