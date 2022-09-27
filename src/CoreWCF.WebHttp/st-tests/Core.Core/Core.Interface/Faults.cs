using System;
using System.Runtime.Serialization;

namespace ST.Core
{
  /// <summary>
  /// Контракт, описывающий недопустимые параметры аутентификации.
  /// </summary>
  [Serializable]
  [DataContract( Namespace = Interface.Constants.SERVER_NAMESPACE )]
  public sealed class InvalidCredentialsFault
  {
    #region .Ctor
    internal InvalidCredentialsFault()
    {
    }
    #endregion
  }

  /// <summary>
  /// Контракт, описывающий неудачную попытку входа на сервер.
  /// </summary>
  [Serializable]
  [DataContract( Namespace = Interface.Constants.SERVER_NAMESPACE )]
  [KnownType( typeof( UnknownUserFault ) ), KnownType( typeof( RemoteAccessRequiredFault ) ), KnownType( typeof( UserAccountExpiredFault ) ),
    KnownType( typeof( TCPIPOnlyAccessRequiredFault ) ), KnownType( typeof( TCPIPOnlyAccessRequiredFault ) )]
  public class LogonFailedFault
  {
    #region .Ctor
    internal LogonFailedFault()
    {
    }
    #endregion
  }

  /// <summary>
  /// Контракт, описывающий попытку входа на сервер неизвестным или отключенным пользователем.
  /// </summary>
  [Serializable]
  [DataContract( Namespace = Interface.Constants.SERVER_NAMESPACE )]
  public sealed class UnknownUserFault : LogonFailedFault
  {
  }

  /// <summary>
  /// Контракт, описывающий попытку входа на сервер пользователем, у которого истек срок действия учетной записи.
  /// </summary>
  [Serializable]
  [DataContract( Namespace = Interface.Constants.SERVER_NAMESPACE )]
  public sealed class UserAccountExpiredFault : LogonFailedFault
  {
  }

  /// <summary>
  /// Контракт, описывающий попытку входа на сервер, аутентификация и авторизация пользователей на котором невозможна из-за отсутствия необходимых для этого модулей.
  /// </summary>
  [Serializable]
  [DataContract( Namespace = Interface.Constants.SERVER_NAMESPACE )]
  public sealed class AuthIsNotAvailableFault
  {
    #region .Ctor
    internal AuthIsNotAvailableFault()
    {
    }
    #endregion
  }

  /// <summary>
  /// Контракт, описывающий непредвиденную ошибку аутентификации или авторизации пользователя.
  /// </summary>
  [Serializable]
  [DataContract( Namespace = Interface.Constants.SERVER_NAMESPACE )]
  public sealed class AuthFailFault
  {
    #region .Ctor
    internal AuthFailFault()
    {
    }
    #endregion
  }

  /// <summary>
  /// Контракт, описывающий ошибку аутентификации или авторизации пользователя при отсутствии лицензии.
  /// </summary>
  [Serializable]
  [DataContract( Namespace = Interface.Constants.SERVER_NAMESPACE )]
  public sealed class AuthLicenceFault
  {
    #region .Ctor
    internal AuthLicenceFault()
    {
    }
    #endregion
  }

  /// <summary>
  /// Контракт, описывающий отсутствие разрешений доступа для удаленного входа на сервер.
  /// </summary>
  [Serializable]
  [DataContract( Namespace = Interface.Constants.SERVER_NAMESPACE )]
  public sealed class RemoteAccessRequiredFault : LogonFailedFault
  {
  }

  /// <summary>
  /// Контракт, описывающий требование входа на сервер только по tcp/ip.
  /// </summary>
  [Serializable]
  [DataContract( Namespace = Interface.Constants.SERVER_NAMESPACE )]
  public sealed class TCPIPOnlyAccessRequiredFault : LogonFailedFault
  {
  }

  /// <summary>
  /// Контракт, описывающий требование входа на сервер по tcp/ip.
  /// </summary>
  [Serializable]
  [DataContract( Namespace = Interface.Constants.SERVER_NAMESPACE )]
  public sealed class TCPIPAccessRequiredFault : LogonFailedFault
  {
  }

  /// <summary>
  /// Контракт, описывающий невозможность выполнения операции без предварительного входа на сервер.
  /// </summary>
  [Serializable]
  [DataContract( Namespace = Interface.Constants.SERVER_NAMESPACE )]
  public sealed class NotLoggedOnFault
  {
    #region .Ctor
    internal NotLoggedOnFault()
    {
    }
    #endregion
  }

  /// <summary>
  /// Контракт, описывающий невозможность выполнения операции без соответствующих разрешений.
  /// </summary>
  [Serializable]
  [DataContract( Namespace = Interface.Constants.SERVER_NAMESPACE )]
  public sealed class AccessDeniedFault
  {
    #region .Ctor
    internal AccessDeniedFault()
    {
    }
    #endregion
  }

  /// <summary>
  /// Контракт, описывающий ошибку аутентификации пользователя при исчерпании количества сессий выделенных на группу организаций.
  /// </summary>
  [Serializable]
  [DataContract( Namespace = Interface.Constants.SERVER_NAMESPACE )]
  public sealed class OrgGroupLicenceFault
  {
    #region .Ctor
    internal OrgGroupLicenceFault()
    {
    }
    #endregion
  }

  /// <summary>
  /// Контракт, описывающий ошибку аутентификации пользователя при исчерпании количества сессий.
  /// </summary>
  [Serializable]
  [DataContract( Namespace = Interface.Constants.SERVER_NAMESPACE )]
  public sealed class SessionCountLicenceFault
  {
    #region .Ctor
    internal SessionCountLicenceFault()
    {
    }
    #endregion
  }

  /// <summary>
  /// Контракт, описывающий ошибку поддерживаемого вызова метода.
  /// </summary>
  [Serializable]
  [DataContract( Namespace = Interface.Constants.SERVER_NAMESPACE )]
  public sealed class ActionSupportedFault
  {
    #region .Ctor
    internal ActionSupportedFault()
    {
    }
    #endregion
  }

  /// <summary>
  /// Контракт, описывающий наличие входа на сервер с предъявленными учетными данными с другого устройства.
  /// </summary>
  [Serializable]
  [DataContract( Namespace = Interface.Constants.SERVER_NAMESPACE )]
  public sealed class UserEnterExistFault : LogonFailedFault
  {
  }
}
