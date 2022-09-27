using System;

using CoreWCF;

using ST.Utils;
using ST.Utils.Attributes;

namespace ST.Core
{
  /// <summary>
  /// Исключение выбрасывается при попытке выполнить операцию без соответствующих разрешений.
  /// </summary>
  [Serializable]
  public sealed class AccessDeniedException : FaultException<AccessDeniedFault>
  {
    #region .Ctor
    internal AccessDeniedException( [NotNullNotEmpty] string permission, string description = null ) :
      base(new AccessDeniedFault(), string.IsNullOrWhiteSpace(description) ? SR.GetString(ServerContext.Session.Culture, Interface.RI.AccessDeniedError) : description)
    {
    }
    #endregion
  }

  /// <summary>
  /// Исключение выбрасывается при попытке выполнить вход на сервер с использованием недопустимых данных аутентификации для используемого типа соединения с сервером.
  /// </summary>
  [Serializable]
  public sealed class InvalidCredentialsException : FaultException<InvalidCredentialsFault>
  {
    #region .Ctor
    internal InvalidCredentialsException() : base(new InvalidCredentialsFault(), SR.GetString(ServerContext.Session.Culture, Interface.RI.InvalidCredentialsError))
    {
    }
    #endregion
  }

  /// <summary>
  /// Исключение выбрасывается при неудачной попытке выполнить вход на сервер.
  /// </summary>
  [Serializable]
  public abstract class LogonFailedException<T> : FaultException<T>
    where T : LogonFailedFault, new()
  {
    #region .Ctor
    internal LogonFailedException( string reason ) : base(new T(), reason)
    {
    }
    #endregion
  }

  /// <summary>
  /// Исключение выбрасывается при попытке выполнить вход на сервер неизвестным или отключенным пользователем.
  /// </summary>
  [Serializable]
  public sealed class UnknownUserException : LogonFailedException<UnknownUserFault>
  {
    #region .Ctor
    internal UnknownUserException( [NotNull] string login ) : base(SR.GetString(ServerContext.Session.Culture, Interface.RI.LogonFailedError, login))
    {
    }
    #endregion
  }

  /// <summary>
  /// Исключение выбрасывается при попытке выполнить вход на сервер пользователем c истекшим сроком действия учетной записи, или этот срок истек во время работы пользователя
  /// </summary>
  [Serializable]
  public sealed class UserAccountExpiredException : LogonFailedException<UserAccountExpiredFault>
  {
    #region .Ctor
    internal UserAccountExpiredException( [NotNull] string login ) : base(SR.GetString(ServerContext.Session.Culture, Interface.RI.UserAccountExpired, login))
    {
    }
    #endregion
  }

  /// <summary>
  /// Исключение выбрасывается при попытке выполнить вход на сервер, если не загружен модуль, поддерживающий аутентификацию и авторизацию пользователей.
  /// </summary>
  [Serializable]
  public sealed class AuthIsNotAvailableException : FaultException<AuthIsNotAvailableFault>
  {
    #region .Ctor
    internal AuthIsNotAvailableException() : base(new AuthIsNotAvailableFault(), SR.GetString(ServerContext.Session.Culture, Interface.RI.AuthIsNotAvailableError))
    {
    }
    #endregion
  }

  /// <summary>
  /// Исключение выбрасывается при непредвиденной ошибке аутентификации или авторизации пользователя.
  /// </summary>
  [Serializable]
  public sealed class AuthFailException : FaultException<AuthFailFault>
  {
    #region .Ctor
    internal AuthFailException() : base(new AuthFailFault(), SR.GetString(ServerContext.Session.Culture, Interface.RI.AuthFailError))
    {
    }
    #endregion
  }

  /// <summary>
  /// Исключение выбрасывается при отсутствии лицензии во время аутентификации пользователя.
  /// </summary>
  [Serializable]
  public sealed class AuthLicenceException : FaultException<AuthLicenceFault>
  {
    #region .Ctor
    internal AuthLicenceException() : base(new AuthLicenceFault(), SR.GetString(ServerContext.Session.Culture, Interface.RI.AuthLicenceError))
    {
    }
    #endregion
  }

  /// <summary>
  /// Исключение выбрасывается при попытке пользователя войти на сервер удаленно, когда у него отсутствует соответствующее разрешение.
  /// </summary>
  [Serializable]
  public sealed class RemoteAccessRequiredException : LogonFailedException<RemoteAccessRequiredFault>
  {
    #region .Ctor
    internal RemoteAccessRequiredException( [NotNull] string login ) : base(SR.GetString(ServerContext.Session.Culture, Interface.RI.RemoteAccessRequired, login))
    {
    }
    #endregion
  }

  /// <summary>
  /// Исключение выбрасывается при попытке пользователя войти на сервер не по TC/IP.
  /// </summary>
  [Serializable]
  public sealed class TCPIPOnlyAccessRequiredException : LogonFailedException<TCPIPOnlyAccessRequiredFault>
  {
    #region .Ctor
    internal TCPIPOnlyAccessRequiredException( [NotNull] string login ) : base(SR.GetString(ServerContext.Session.Culture, Interface.RI.TCPIPOnlyAccessRequired, login))
    {
    }
    #endregion
  }

  /// <summary>
  /// Исключение выбрасывается при попытке пользователя войти на сервер по TC/IP.
  /// </summary>
  [Serializable]
  public sealed class TCPIPAccessRequiredException : LogonFailedException<TCPIPAccessRequiredFault>
  {
    #region .Ctor
    internal TCPIPAccessRequiredException( [NotNull] string login )
      : base(SR.GetString(ServerContext.Session.Culture, Interface.RI.TCPIPOnlyAccessRequired, login))
    {
    }
    #endregion
  }

  /// <summary>
  /// Исключение выбрасывается при попытке выполнить операцию без предварительного входа на сервер.
  /// </summary>
  [Serializable]
  public sealed class NotLoggedOnException : FaultException<NotLoggedOnFault>
  {
    #region .Ctor
    internal NotLoggedOnException() : base(new NotLoggedOnFault(), SR.GetString(Interface.RI.NotLoggedOnError))
    {
    }
    #endregion
  }

  /// <summary>
  /// Исключение выбрасывается при попытке выгруженного модуля обратиться к хосту.
  /// </summary>
  [Serializable]
  public class UnloadedModuleException : InvalidOperationException
  {
    #region .Ctor
    internal UnloadedModuleException( BaseModule module ) : base( SR.GetString( Interface.RI.UnloadedModuleError, module.GetDisplayName() ) )
    {
    }
    #endregion
  }

  /// <summary>
  /// Исключение выбрасывается при попытке обратиться к выгруженному модулю.
  /// </summary>
  [Serializable]
  public sealed class UnloadedTargetModuleException : InvalidOperationException
  {
    #region .Ctor
    internal UnloadedTargetModuleException( BaseModule module, BaseModule target ) : base( SR.GetString( Interface.RI.UnloadedTargetModuleError, module.GetDisplayName(), target.GetDisplayName() ) )
    {
    }
    #endregion
  }

  /// <summary>
  /// Исключение выбрасывается при попытке выполнить вход с помощью windows-аутентификации на сервер, который ее не поддерживает.
  /// </summary>
  [Serializable]
  public sealed class WindowsAuthenticationNotSupportedException : Exception
  {
    #region .Ctor
    internal WindowsAuthenticationNotSupportedException() : base( SR.GetString( Interface.RI.WindowsAuthenticationNotSupported ) )
    {
    }
    #endregion
  }

  /// <summary>
  /// Исключение выбрасывается при ошибке аутентификации пользователя при исчерпании количества сессий выделенных на группу организаций.
  /// </summary>
  [Serializable]
  public sealed class OrgGroupLicenceException : FaultException<OrgGroupLicenceFault>
  {
    #region .Ctor
    internal OrgGroupLicenceException() : base(new OrgGroupLicenceFault(), SR.GetString(ServerContext.Session.Culture, Interface.RI.OrgGroupLicenceError))
    {
    }
    #endregion
  }

  /// <summary>
  /// Исключение выбрасывается при ошибке аутентификации пользователя при исчерпании количества сессий.
  /// </summary>
  [Serializable]
  public sealed class SessionCountLicenceException : FaultException<SessionCountLicenceFault>
  {
    #region .Ctor
    internal SessionCountLicenceException()
      : base(new SessionCountLicenceFault(), SR.GetString(ServerContext.Session.Culture, Interface.RI.SessionCountLicenceError))
    {
    }
    #endregion
  }

  /// <summary>
  /// Исключение выбрасывается при попытке выполнить действие, которое не поддерживается в данном контексте.
  /// </summary>
  [Serializable]
  public sealed class ActionSupportedException : FaultException<ActionSupportedFault>
  {
    #region .Ctor
    internal ActionSupportedException()
      : base(new ActionSupportedFault(), SR.GetString(ServerContext.Session.Culture, Interface.RI.ActionSupportedException))
    {
    }
    #endregion
  }

  /// <summary>
  /// Исключение выбрасывается при попытке пользователя войти на сервер, когда уже существует подключение с предоставленными учетными данными.
  /// </summary>
  [Serializable]
  public sealed class UserEnterExistException : LogonFailedException<UserEnterExistFault>
  {
    #region .Ctor
    internal UserEnterExistException()
      : base(SR.GetString(ServerContext.Session.Culture, Interface.RI.UserEnterExist))
    {
    }
    #endregion
  }
}
