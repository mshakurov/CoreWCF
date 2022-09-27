using CoreWCF;
using CoreWCF.Web;

namespace ST.Core
{
  /// <summary>
  /// Точка входа в сервер.
  /// </summary>
  [WcfService( Interface.Constants.SERVER_ADDRESS, Interface.Constants.SERVER_NAMESPACE )]
  public interface IServerEntryPoint
  {
    /// <summary>
    /// Возвращает текущие параметры WCF-сервера.
    /// JSON URI: "http://localhost:55565/ApplicationServer/IServerEntryPoint/GetInfo"
    /// JSON Result string: {"BasicHttpPort":55564,"HttpPort":55555,"ProductName":"ST Passenger","ServerVersion":"1.0.1.0","TcpPort":55556,"TcpZippedPort":0,"WinTcpPort":55557,"WinTcpZippedPort":0}
    /// </summary>
    /// <returns>Текущие параметры WCF-сервера.</returns>
    [WebGet( UriTemplate = "GetInfo", ResponseFormat = WebMessageFormat.Json )]
    ServerInfo GetInfo();

    /// <summary>
    /// Корректно завершает работу с сервером.
    /// JSON URI: "http://localhost:55565/ApplicationServer/IServerEntryPoint/Logoff"
    /// </summary>
    [OneWay]
    [WebGet( UriTemplate = "Logoff" )]
    void Logoff();

    /// <summary>
    /// Аутентифицирует и авторизует пользователя на сервере.
    /// JSON URI: "http://localhost:55565/ApplicationServer/IServerEntryPoint/Logon"
    /// JSON Parameter string: {"credentials":{"Login":"Администратор","PasswordMD5":"DCE3CACEF358856F604E3DCB63DB2048"},"clientInfo":{"Culture":"ru"}}
    /// JSON Result string: {"Permissions":["-1100638439","-1100638439|-1264522917","-1100638439|-1264522917|281798495","-1100638439|1281181309","-1100638439|1281181309|-526516360","-1100638439|1603161322","-1100638439|1603161322|1569867321"],"SessionId":23362670131}    
    /// </summary>
    /// <param name="credentials">Аутентификационные данные пользователя.</param>
    /// <param name="clientInfo">Параметры клиента.</param>
    /// <param name="createdIP">IP-адрес создания сессии.</param>
    /// <param name="fileHash">Хеш файлов.</param>
    /// <param name="isRepeat">Признак повторного вызова.</param>
    /// <returns>Информация об авторизации пользователя.</returns>
    [WebInvoke( Method = "POST", UriTemplate = "Logon", BodyStyle = WebMessageBodyStyle.WrappedRequest, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json )]
    [FaultContract( typeof( AuthIsNotAvailableFault ) )]
    [FaultContract( typeof( AuthFailFault ) )]
    [FaultContract( typeof( InvalidCredentialsFault ) )]
    [FaultContract( typeof( UnknownUserFault ) )]
    [FaultContract( typeof( RemoteAccessRequiredFault ) )]
    [FaultContract( typeof( TCPIPOnlyAccessRequiredFault ) )]
    [FaultContract( typeof( TCPIPAccessRequiredFault ) )]
    [FaultContract( typeof( UserEnterExistFault ) )]
    AuthorizationToken Logon( UserCredentials credentials, ClientInfo clientInfo, string createdIP = null, string[][] fileHash = null, bool isRepeat = false );

    /// <summary>
    /// Аутентифицирует и авторизует пользователя на сервере.
    /// JSON URI: "http://localhost:55565/ApplicationServer/IServerEntryPoint/LogonAs"
    /// JSON Parameter string: {"userId":"1","clientInfo":{"Culture":"ru"}}
    /// JSON Result string: {"Permissions":["-1100638439","-1100638439|-1264522917","-1100638439|-1264522917|281798495","-1100638439|1281181309","-1100638439|1281181309|-526516360","-1100638439|1603161322","-1100638439|1603161322|1569867321"],"SessionId":23362670131}    
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="clientInfo">Параметры клиента.</param>
    /// <param name="createdIP">IP-адрес создания сессии.</param>
    /// <returns>Информация об авторизации пользователя.</returns>
    [WebInvoke( Method = "POST", UriTemplate = "LogonAs", BodyStyle = WebMessageBodyStyle.WrappedRequest, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json )]
    [FaultContract( typeof( AuthIsNotAvailableFault ) )]
    [FaultContract( typeof( AuthFailFault ) )]
    [FaultContract( typeof( InvalidCredentialsFault ) )]
    [FaultContract( typeof( UnknownUserFault ) )]
    [FaultContract( typeof( RemoteAccessRequiredFault ) )]
    [FaultContract( typeof( TCPIPOnlyAccessRequiredFault ) )]
    [FaultContract( typeof( TCPIPAccessRequiredFault ) )]
    AuthorizationToken LogonAs( int userId, ClientInfo clientInfo, string createdIP = null );

    /// <summary>
    /// Аутентифицирует и авторизует пользователя на сервере.
    /// JSON URI: "http://localhost:55565/ApplicationServer/IServerEntryPoint/TrustedLogOn"
    /// JSON Parameter string: {"credentials":{"Login":"Администратор","PasswordMD5":"DCE3CACEF358856F604E3DCB63DB2048"},"clientInfo":{"Culture":"ru"},"createdIP":"127.0.0.1"}
    /// JSON Result string: {"Permissions":["-1100638439","-1100638439|-1264522917","-1100638439|-1264522917|281798495","-1100638439|1281181309","-1100638439|1281181309|-526516360","-1100638439|1603161322","-1100638439|1603161322|1569867321"],"SessionId":23362670131}    
    /// </summary>
    /// <param name="credentials">Аутентификационные данные пользователя.</param>
    /// <param name="clientInfo">Параметры клиента.</param>
    /// <param name="createdIP">IP-адрес создания сессии.</param>
    /// <returns>Информация об авторизации пользователя.</returns>
    [WebInvoke( Method = "POST", UriTemplate = "TrustedLogOn", BodyStyle = WebMessageBodyStyle.WrappedRequest, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json )]
    [FaultContract( typeof( AuthIsNotAvailableFault ) )]
    [FaultContract( typeof( AuthFailFault ) )]
    [FaultContract( typeof( InvalidCredentialsFault ) )]
    [FaultContract( typeof( UnknownUserFault ) )]
    [FaultContract( typeof( RemoteAccessRequiredFault ) )]
    [FaultContract( typeof( ActionSupportedFault ) )]
    [FaultContract( typeof( TCPIPOnlyAccessRequiredFault ) )]
    [FaultContract( typeof( TCPIPAccessRequiredFault ) )]
    AuthorizationToken TrustedLogOn( UserCredentials credentials, ClientInfo clientInfo, string createdIP );

    /// <summary>
    /// Аутентифицирует и авторизует пользователя на сервере.
    /// JSON URI: "http://localhost:55565/ApplicationServer/IServerEntryPoint/TrustedLogonAlternative"
    /// JSON Parameter string: {"credentials":{"Login":"Администратор","PasswordMD5":"DCE3CACEF358856F604E3DCB63DB2048"},"clientInfo":{"Culture":"ru"},"createdIP":"127.0.0.1"}
    /// JSON Result string: {"Permissions":["-1100638439","-1100638439|-1264522917","-1100638439|-1264522917|281798495","-1100638439|1281181309","-1100638439|1281181309|-526516360","-1100638439|1603161322","-1100638439|1603161322|1569867321"],"SessionId":23362670131}    
    /// </summary>
    /// <param name="orgGroupId">Идентификатор ГО.</param>
    /// <returns>Информация об авторизации пользователя.</returns>
    [WebInvoke( Method = "POST", UriTemplate = "TrustedLogonAlternative", BodyStyle = WebMessageBodyStyle.WrappedRequest, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json )]
    [FaultContract( typeof( AuthIsNotAvailableFault ) )]
    [FaultContract( typeof( AuthFailFault ) )]
    [FaultContract( typeof( InvalidCredentialsFault ) )]
    [FaultContract( typeof( UnknownUserFault ) )]
    [FaultContract( typeof( RemoteAccessRequiredFault ) )]
    [FaultContract( typeof( ActionSupportedFault ) )]
    [FaultContract( typeof( TCPIPOnlyAccessRequiredFault ) )]
    [FaultContract( typeof( TCPIPAccessRequiredFault ) )]
    AuthorizationToken TrustedLogonAlternative( int orgGroupId );

    /// <summary>
    /// Возвращает массив поддерживаемых системой культур.
    /// </summary>
    /// <returns>Массив поддерживаемых системой культур.</returns>
    [WebGet( UriTemplate = "GetSupportedCultures", ResponseFormat = WebMessageFormat.Json )]
    ServerCultureInfo[] GetSupportedCultures();

    /// <summary>
    /// Возвращает файл с обновлением Shell.
    /// </summary>
    /// <returns>Массив байт.</returns>
    [WebGet( UriTemplate = "GetUpdateFile", ResponseFormat = WebMessageFormat.Json )]
    byte[] GetUpdateFile();

    /// <summary>
    /// Проверяет наличие обновления клиента
    /// </summary>
    /// <param name="fileHash">хэш файлов клиента</param>
    /// <returns>True - если хэш фалов на сервере отличается от хэа файлов клиента</returns>
    [WebInvoke( Method = "POST", UriTemplate = "CheckUpdate", BodyStyle = WebMessageBodyStyle.WrappedRequest, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json )]
    bool CheckShellUpdate( string[][] fileHash );
  }
}
