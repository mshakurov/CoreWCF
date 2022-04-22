using System;
using System.Runtime.Serialization;

namespace ST.BusinessEntity.Server
{
  /// <summary>
  /// Идентификаторы системных типов данных.
  /// </summary>
  [DataContract( Namespace = Constants.MODULE_NAMESPACE ), Serializable]
  public enum SystemTypeId
  {
    #region .Static Fields
    /// <summary>
    /// Неизвестный тип данных.
    /// </summary>
    Unknown,
    /// <summary>
    /// Большое целое число.
    /// </summary>
    [EnumMember]
    BigInt,
    /// <summary>
    /// Бинарные данные.
    /// </summary>
    [EnumMember]
    Binary,
    /// <summary>
    /// Флаг.
    /// </summary>
    [EnumMember]
    Bool,
    /// <summary>
    /// Дата.
    /// </summary>
    [EnumMember]
    Date,
    /// <summary>
    /// Дата и время.
    /// </summary>
    [EnumMember]
    DateTime,
    /// <summary>
    /// Перечисление.
    /// </summary>
    [EnumMember]
    Enum,
    /// <summary>
    /// Изображение.
    /// </summary>
    [EnumMember]
    Image,
    /// <summary>
    /// Целое число.
    /// </summary>
    [EnumMember]
    Int,
    /// <summary>
    /// Десятичное число.
    /// </summary>
    [EnumMember]
    Numeric,
    /// <summary>
    /// Среднее целое число.
    /// </summary>
    [EnumMember]
    SmallInt,
    /// <summary>
    /// Строка.
    /// </summary>
    [EnumMember]
    String,
    /// <summary>
    /// Строка.
    /// </summary>
    [EnumMember]
    Text,
    /// <summary>
    /// Время.
    /// </summary>
    [EnumMember]
    Time,
    /// <summary>
    /// Малое целое число.
    /// </summary>
    [EnumMember]
    TinyInt,
    /// <summary>
    /// Расширяемый язык разметки.
    /// </summary>
    [EnumMember]
    Xml
    #endregion
  }

  /// <summary>
  /// Параметры выборки экземпляров бизнес-сущностей.
  /// </summary>
  [DataContract( Namespace = Constants.MODULE_NAMESPACE ), Serializable]
  [Flags]
  public enum EntityQueryOption
  {
    #region .Static Fields
    /// <summary>
    /// Параметры выборки по умолчанию: Deleted - не установлен, Binary - не установлен.
    /// </summary>
    [EnumMember]
    Default = 0,

    /// <summary>
    /// Включать в выборку "удаленные" экземпляры бизнес-сущностей.
    /// </summary>
    [EnumMember]
    Deleted = 1,

    /// <summary>
    /// Включать в выборку значения атрибутов, содержащих бинарные данные.
    /// Если данный флаг не указан, то значения бинарных атрибутов отличных от null будут содержать пустой массив байт (byte[0]).
    /// </summary>
    [EnumMember]
    Binary = 2,

    /// <summary>
    /// Возвращать права доступа на тип или экземпляр бизнес сущности.
    /// </summary>
    [EnumMember]
    IncludePermissions = 4,

    /// <summary>
    /// Включить в выборку экземпляры: без учета унаследованных типов.
    /// </summary>
    [EnumMember]
    WithoutChildTypes = 8
    #endregion
  }

  /// <summary>
  /// Параметр результата выборки экземпляра бизнес-сущности.
  /// </summary>
  [DataContract( Namespace = Constants.MODULE_NAMESPACE ), Serializable]
  public enum EntityResult
  {
    #region .Static Fields
    /// <summary>
    /// Результат по умолчанию: Exception
    /// </summary>
    [EnumMember]
    Default = 0,

    /// <summary>
    /// Возвращать Null вместо исключения, если нет прав доступа на экземпляр бизнес-сущности.
    /// </summary>
    [EnumMember]
    NullInsteadEx = 1,

    /// <summary>
    /// Возвращать LockedEntity вместо исключения, если нет прав доступа на экземпляр бизнес-сущности.
    /// </summary>
    [EnumMember]
    LockedInsteadEx = 2,

    /// <summary>
    /// Генерировать исключение, если нет прав доступа на экземпляр бизнес-сущности.
    /// </summary>
    [EnumMember]
    Exception = 3
    #endregion
  }

  /// <summary>
  /// Параметр результата выборки типа бизнес-сущности.
  /// </summary>
  [DataContract( Namespace = Constants.MODULE_NAMESPACE ), Serializable]
  public enum EntityTypeResult
  {
    #region .Static Fields
    /// <summary>
    /// Результат по умолчанию: Exception
    /// </summary>
    [EnumMember]
    Default = 0,

    /// <summary>
    /// Возвращать Null вместо исключения, если нет прав доступа на тип бизнес-сущности.
    /// </summary>
    [EnumMember]
    NullInsteadEx = 1,

    /// <summary>
    /// Генерировать исключение, если нет прав доступа на тип бизнес-сущности.
    /// </summary>
    [EnumMember]
    Exception = 2
    #endregion
  }

  /// <summary>
  /// Тип разграничения прав доступа к объектам бизнес-сущностей.
  /// </summary>
  [DataContract( Namespace = Constants.MODULE_NAMESPACE ), Serializable]
  
  public enum PermissionType
  {
    #region .Static Fields
    /// <summary>
    /// Неизвестный тип разграничения прав доступа к объектам бизнес-сущностей.
    /// </summary>
    [EnumMember]
    
    Unknown = 0,

    /// <summary>
    /// Разграничение прав пользователя на экземпляры бизнес-сущностей.
    /// </summary>
    [EnumMember]
    
    UserEntity = 1,

    /// <summary>
    /// Разграничение прав пользователя на типы бизнес-сущностей.
    /// </summary>
    [EnumMember]
    
    UserEntityType = 2,

    /// <summary>
    /// Разграничение прав группы пользователей на экземпляры бизнес-сущностей.
    /// </summary>
    [EnumMember]
    
    GroupEntity = 3,

    /// <summary>
    /// Разграничение прав группы пользователей на типы бизнес-сущностей.
    /// </summary>
    [EnumMember]
    
    GroupEntityType = 4
    #endregion
  }

  /// <summary>
  /// Тип разграничения прав доступа к объектам бизнес-сущностей.
  /// </summary>
  [DataContract( Namespace = Constants.MODULE_NAMESPACE ), Serializable]
  
  public enum PermissionForm
  {
    #region .Static Fields
    /// <summary>
    /// Разграничение прав пользователя на типы бизнес-сущностей.
    /// </summary>
    [EnumMember]
    
    EntityType = 1,

    /// <summary>
    /// Разграничение прав пользователя на экземпляры бизнес-сущностей.
    /// </summary>
    [EnumMember]
    
    Entity = 2
    #endregion
  }
}
