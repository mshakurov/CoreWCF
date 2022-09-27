using System;

namespace ST.Core
{
  /// <summary>
  /// Интерфейс модуля, поддерживающего аутентификацию и авторизацию пользователей.
  /// </summary>
  [NotServiceInterface]
  public interface IAuthModule
  {
    /// <summary>
    /// Аутентифицирует пользователя. В это время сессия еще не создана и использовать свойство ServerContext.Session некорректно.
    /// </summary>
    /// <param name="login">Логин пользователя.</param>
    /// <param name="passwordMD5">Пароль, захешированный с помощью MD5. Если null, то производится windows-аутентификация.</param>
    /// <returns>Объект, содержащий результат аутентификации. Null означает, что аутентификация не выполнена.</returns>
    object Authenticate( string login, string passwordMD5 );

    /// <summary>
    /// Аутентифицирует пользователя. В это время сессия еще не создана и использовать свойство ServerContext.Session некорректно.
    /// </summary>
    /// <param name="ip">IP-адресс.</param>
    /// <returns>Объект, содержащий результат аутентификации. Null означает, что аутентификация не выполнена.</returns>
    object AuthenticateByIP( string ip );

    /// <summary>
    /// Аутентифицирует пользователя. В это время сессия еще не создана и использовать свойство ServerContext.Session некорректно.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <returns>Объект, содержащий результат аутентификации. Null означает, что аутентификация не выполнена.</returns>
    object AuthenticateAs( int userId );

    /// <summary>
    /// Авторизует пользователя. В это время сессия еще не создана и использовать свойство ServerContext.Session некорректно.
    /// </summary>
    /// <param name="authenticationResult">Результат аутентификации пользователя.</param>
    /// <returns>Разрешения доступа пользователя. Null означает, что авторизация не выполнена.</returns>
    PermissionList Authorize( object authenticationResult );

    /// <summary>
    /// Вызывается после создания сессии сервером. В это время свойство ServerContext.Session содержит созданную сессию.
    /// </summary>
    /// <param name="authenticationResult">Результат аутентификации пользователя.</param>
    /// <param name="sessionDestroyer">Метод, с помощью которого модуль может удалить сессию. В процессе удаления будет вызван метод OnSessionDeleted. Не следует вызывать данный метод из метода OnSessionDeleted.</param>
    void OnSessionCreated( object authenticationResult, Action sessionDestroyer );

    /// <summary>
    /// Вызывается после удаления сессии сервером. В это время свойство ServerContext.Session содержит удаленную сессию.
    /// </summary>
    void OnSessionDeleted();

    /// <summary>
    /// Возвращает признак о том, необходимо ли сообщить пользователю о скором истечении периода действия учетной записи.
    /// </summary>
    /// <param name="expireDate">Дата окончания срока действия учетной записи.</param>
    /// <returns>true, если необходимо отправить сообщение.</returns>
    bool GetUserAccountExpirationWarningNecessary( DateTime? expireDate );

    /// <summary>
    /// Добавляет сессию.
    /// </summary>
    /// <param name="sessionId">Идентификатор сессии.</param>
    /// <param name="login">Логин.</param>
    /// <param name="culture">Культура.</param>
    /// <param name="messageTypes">Подписки.</param>
    /// <param name="createdIP">IP-адрес создания сессии.</param>
    /// <returns>Сессия.</returns>
    DBSession AddDBSession( ulong sessionId, string login, string culture, string[] messageTypes, string createdIP );

    /// <summary>
    /// Восстанавливает сохраненные сессии с БД.
    /// </summary>
    /// <returns>Cессии.</returns>
    DBSession[] RestoreDBSessions();

    /// <summary>
    /// Очищает данные по сессиям.
    /// </summary>
    void ClearDBSessions();

    /// <summary>
    /// Удаляет сессию.
    /// </summary>
    /// <param name="sessionId">Идентификатор сессии.</param>
    void RemoveDBSessions( ulong sessionId );

    /// <summary>
    /// Получает сессию.
    /// </summary>
    /// <param name="sessionId">Идентификатор сессии.</param>
    DBSession GetDBSession( ulong sessionId );

    /// <summary>
    /// Устанавливает подписки в сессию.
    /// </summary>
    /// <param name="sessionId">Идентификатор сессии.</param>
    /// <param name="messageTypes">Подписки.</param>
    /// <returns>Сессия.</returns>
    string[] SetMessageTypes( ulong sessionId, string[] messageTypes );

    /// <summary>
    /// Обновляет трафик пользователя.
    /// </summary>
    void OnUserTrafficUpdate();

    /// <summary>
    /// Получает учетную запись пользователя.
    /// </summary>
    /// <param name="orgGroupId">Идентификатор ГО.</param>
    UserCredentials GetUserCredentials( int orgGroupId );

    /// <summary>
    /// Получает список альтернативных ГО.
    /// </summary>
    /// <param name="login">Пользователь.</param>
    AlternativeOrgGroup[] GetAlternativeOrgGroupList( string login );

    /// <summary>
    /// Возвращает признак того: что пользователь явзяется ведщий.
    /// </summary>
    /// <param name="login">Пользователь.</param>
    bool IsParentUser( string login );

    /// <summary>
    /// Возвращает признак того: что разрешен вход только windows-пользователям.
    /// </summary>
    bool IsWindowsUserOnly();
  }
}
