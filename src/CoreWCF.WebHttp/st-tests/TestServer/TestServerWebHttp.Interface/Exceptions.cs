using System;
using System.Runtime.Serialization;

using CoreWCF;

namespace ST.BusinessEntity.Server
{
  /// <summary>
  /// Контракт, описывающий ошибку при работе с бизнес-сущностями.
  /// </summary>
  [Serializable]
  [DataContract( Namespace = Constants.MODULE_NAMESPACE )]
  public sealed class EntityFault
  {
    #region .Properties
    [DataMember]
    public string Message { get; set; }
    #endregion
  }

  /// <summary>
  /// Контракт, описывающий ошибку при удалении нескольких экземпляров бизнес-сущностей.
  /// </summary>
  [Serializable]
  [DataContract( Namespace = Constants.MODULE_NAMESPACE )]
  public sealed class EntityRemoveMultipleFault
  {
    #region .Properties
    /// <summary>
    /// Результаты удаления. Если элемент равен True - экземпляр был физически удален, если False - экземплярн не может быть физически удален и помечен как "удаленный" или возникла ошибка.
    /// </summary>
    [DataMember]
    public bool[] Results;

    /// <summary>
    /// Ошибки. Если элемент равен null, то ошибки не было.
    /// </summary>
    [DataMember]
    public EntityFault[] Faults;
    #endregion
  }


  /// <summary>
  /// Контракт, описывающий ошибку при попытке указать объекту неверную группу организаций.
  /// </summary>
  [Serializable]
  [DataContract( Namespace = Constants.MODULE_NAMESPACE )]
  public sealed class WrongOrgGroupFault
  {
    #region .Ctor
    internal WrongOrgGroupFault()
    {
    }
    #endregion
  }

  /// <summary>
  /// Исключение выбрасывается при возникновении ошибки в работе с бизнес-сущностями.
  /// </summary>
  [Serializable]
  public class EntityException : FaultException<EntityFault>
  {
    #region .Ctor
    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="reason">Причина исключения.</param>
    public EntityException( string reason ) : base( new EntityFault(), reason )
    {
    }
    #endregion
  }

  /// <summary>
  /// Исключение выбрасывается при возникновении ошибок в процессе удаления нескольких экземпляров бизнес-сущностей.
  /// </summary>
  [Serializable]
  public class EntityRemoveMultipleException : FaultException<EntityRemoveMultipleFault>
  {
    #region .Ctor
    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="fault">Соответствующий исключению контракт.</param>
    public EntityRemoveMultipleException( EntityRemoveMultipleFault fault ) : base( fault )
    {
    }
    #endregion
  }


}
