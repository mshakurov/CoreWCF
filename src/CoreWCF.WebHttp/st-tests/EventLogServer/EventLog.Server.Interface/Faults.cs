using System;
using System.Runtime.Serialization;

namespace ST.EventLog.Server
{
  /// <summary>
  /// Контракт, описывающий невозможность выполнения пользователем операции без соответствующих разрешений.
  /// </summary>
  [Serializable]
  [DataContract( Namespace = Constants.MODULE_NAMESPACE )]
  public sealed class UserPermissionEditFault
  {
    #region .Ctor
    internal UserPermissionEditFault()
    {
    }
    #endregion
  }
}
