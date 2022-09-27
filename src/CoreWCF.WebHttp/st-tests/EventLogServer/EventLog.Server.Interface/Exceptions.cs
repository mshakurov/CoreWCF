using System;

using CoreWCF;

using ST.Utils;

namespace ST.EventLog.Server
{
  /// <summary>
  /// Исключение выбрасывается при попытке пользователем выполнить операцию без соответствующих разрешений.
  /// </summary>
  [Serializable]
  public sealed class UserPermissionEditException : FaultException<UserPermissionEditFault>
  {
    #region .Ctor
    public UserPermissionEditException() : base( new UserPermissionEditFault(), SR.GetString( /*ServerContext.Session.Culture*/ System.Threading.Thread.CurrentThread.CurrentCulture, RI.UserPermissionEditError ) )
    {
    }
    #endregion
  }
}
