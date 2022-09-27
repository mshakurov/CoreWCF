//GENERATED AUTOMATICALLY BY 'ResIdGenerator' TOOL.
//DO NOT MODIFY THE CONTENTS OF THIS FILE WITH THE CODE EDITOR!

namespace ST.Core.Interface
{
  internal static partial class RI
  {
    #region .Constants
    /// <summary>
    /// Недостаточно разрешений доступа для выполнения операции.
    /// </summary>
    internal const string AccessDeniedError = "AccessDeniedError|ST.Core.Interface@ST.Core.Resources";

    /// <summary>
    /// Сервер не поддерживает этот метод
    /// </summary>
    internal const string ActionSupportedException = "ActionSupportedException|ST.Core.Interface@ST.Core.Resources";

    /// <summary>
    /// Непредвиденная ошибка при попытке входа в систему.
    /// </summary>
    internal const string AuthFailError = "AuthFailError|ST.Core.Interface@ST.Core.Resources";

    /// <summary>
    /// Сервер не позволят выполнить вход в систему, т.к. отсутствует необходимый для этого компонент.
    /// </summary>
    internal const string AuthIsNotAvailableError = "AuthIsNotAvailableError|ST.Core.Interface@ST.Core.Resources";

    /// <summary>
    /// Не удается получить ключ лицензирования.
    /// </summary>
    internal const string AuthLicenceError = "AuthLicenceError|ST.Core.Interface@ST.Core.Resources";

    /// <summary>
    /// Соединение с БД
    /// </summary>
    internal const string CategoryDBConnection = "CategoryDBConnection|ST.Core.Interface@ST.Core.Resources";

    /// <summary>
    /// Параметры соединения
    /// </summary>
    internal const string DisplayNameDBConnection = "DisplayNameDBConnection|ST.Core.Interface@ST.Core.Resources";

    /// <summary>
    /// Параметры соединения PostgreSQL
    /// </summary>
    internal const string DisplayNameDBConnection_PG = "DisplayNameDBConnection_PG|ST.Core.Interface@ST.Core.Resources";

    /// <summary>
    /// Отсутствует разрешение доступа
    /// </summary>
    internal const string InsufficientPermissions = "InsufficientPermissions|ST.Core.Interface@ST.Core.Resources";

    /// <summary>
    /// Недопустимые параметры аутентификации.
    /// </summary>
    internal const string InvalidCredentialsError = "InvalidCredentialsError|ST.Core.Interface@ST.Core.Resources";

    /// <summary>
    /// Пользователя с логином '{0}' и указанным паролем не существует, либо пользователь отключен.
    /// </summary>
    internal const string LogonFailedError = "LogonFailedError|ST.Core.Interface@ST.Core.Resources";

    /// <summary>
    /// Включает смешанный режим БД - сначала PostgreSQL, а если ошибка то MSSQL (только для отладки).
    /// </summary>
    internal const string MixedDbMode = "MixedDbMode|ST.Core.Interface@ST.Core.Resources";

    /// <summary>
    /// Для выполнения операции требуется войти в систему.
    /// </summary>
    internal const string NotLoggedOnError = "NotLoggedOnError|ST.Core.Interface@ST.Core.Resources";

    /// <summary>
    /// Количество подключений превышает выделенное на группу организаций.
    /// </summary>
    internal const string OrgGroupLicenceError = "OrgGroupLicenceError|ST.Core.Interface@ST.Core.Resources";

    /// <summary>
    /// Для входа на сервер под логином '{0}' требуется разрешение удаленного доступа.
    /// </summary>
    internal const string RemoteAccessRequired = "RemoteAccessRequired|ST.Core.Interface@ST.Core.Resources";

    /// <summary>
    /// Последний доступ: {0:dd.MM.yyyy HH:mm:ss}({1}) ({0} - время(DateTime), {1} - ip-адрес(string))
    /// </summary>
    internal const string SessionAccessed = "SessionAccessed|ST.Core.Interface@ST.Core.Resources";

    /// <summary>
    /// Превышено максимальное количество подключений для сервера приложений.
    /// </summary>
    internal const string SessionCountLicenceError = "SessionCountLicenceError|ST.Core.Interface@ST.Core.Resources";

    /// <summary>
    /// Создана: {0:dd.MM.yyyy HH:mm:ss}({1}) ({0} - время(DateTime), {1} - ip-адрес(string))
    /// </summary>
    internal const string SessionCreated = "SessionCreated|ST.Core.Interface@ST.Core.Resources";

    /// <summary>
    /// Данные о сессии
    /// </summary>
    internal const string SessionHeader = "SessionHeader|ST.Core.Interface@ST.Core.Resources";

    /// <summary>
    /// Идентификатор: {0} ({0} - идентификатор(ulong))
    /// </summary>
    internal const string SessionId = "SessionId|ST.Core.Interface@ST.Core.Resources";

    /// <summary>
    /// Логин: {0} ({0} - логин(string))
    /// </summary>
    internal const string SessionLogin = "SessionLogin|ST.Core.Interface@ST.Core.Resources";

    /// <summary>
    /// Вход на сервер под логином '{0}' с использованием данной точки доступа запрещён.
    /// </summary>
    internal const string TCPIPOnlyAccessRequired = "TCPIPOnlyAccessRequired|ST.Core.Interface@ST.Core.Resources";

    /// <summary>
    /// Модуль '{0}' был выгружен и его попытка обращения к ядру была предотвращена.
    /// </summary>
    internal const string UnloadedModuleError = "UnloadedModuleError|ST.Core.Interface@ST.Core.Resources";

    /// <summary>
    /// Модуль '{0}' попытался обратиться к выгруженному модулю '{1}' и данная попытка была предотвращена.
    /// </summary>
    internal const string UnloadedTargetModuleError = "UnloadedTargetModuleError|ST.Core.Interface@ST.Core.Resources";

    /// <summary>
    /// Для пользователя с логином '{0}' истек срок действия учетной записи.
    /// </summary>
    internal const string UserAccountExpired = "UserAccountExpired|ST.Core.Interface@ST.Core.Resources";

    /// <summary>
    /// С предъявленными учетными данными уже осуществлён вход на другом устройстве.
    /// </summary>
    internal const string UserEnterExist = "UserEnterExist|ST.Core.Interface@ST.Core.Resources";

    /// <summary>
    /// Сервер не поддерживает подключение windows-пользователей.
    /// </summary>
    internal const string WindowsAuthenticationNotSupported = "WindowsAuthenticationNotSupported|ST.Core.Interface@ST.Core.Resources";
    #endregion
  }
}