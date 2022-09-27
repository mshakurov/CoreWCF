using CoreWCF.Channels;

using ST.Utils;

using System.Runtime.Serialization;
using System.Xml;

namespace ST.Core
{
  /// <summary>
  /// Параметры WCF-сервера.
  /// </summary>
  [Serializable]
  [DataContract( Namespace = Interface.Constants.SERVER_NAMESPACE )]
  public sealed class ServerInfo
  {
    #region .Properties
    /// <summary>
    /// Номер Https-порта.
    /// </summary>
    [DataMember]
    public int HttpPort { get; internal set; }

    /// <summary>
    /// Номер Tcp-порта.
    /// </summary>
    [DataMember]
    public int TcpPort { get; internal set; }

    /// <summary>
    /// Номер Tcp-порта, используемого при Windows аутентификации.
    /// </summary>
    [DataMember]
    public int WinTcpPort { get; internal set; }

    /// <summary>
    /// Номер Tcp-порта, применяющего сжатие для обмена информацией.
    /// </summary>
    [DataMember]
    public int TcpZippedPort { get; internal set; }

    /// <summary>
    /// Номер Tcp-порта, используемого при Windows аутентификации и применяющего сжатие для обмена информацией.
    /// </summary>
    [DataMember]
    public int WinTcpZippedPort { get; internal set; }

    /// <summary>
    /// Номер Http-порта, используемого для обмена информацией.
    /// </summary>
    [DataMember]
    public int BasicHttpPort { get; internal set; }

    /// <summary>
    /// Номер Json-порта, используемого для обмена информацией.
    /// </summary>
    [DataMember]
    public int JsonPort { get; internal set; }

    /// <summary>
    /// Номер Http-порта c security на уровне транспорта, используемого для обмена информацией.
    /// </summary>
    [DataMember]
    public int SecHttpPort { get; internal set; }

    /// <summary>
    /// Номер Http-порта c security на уровне транспорта и Windows аутентификацией, используемого для обмена информацией.
    /// </summary>
    [DataMember]
    public int WinSecHttpPort { get; internal set; }

    /// <summary>
    /// Номер Json-порта c security на уровне транспорта, используемого для обмена информацией.
    /// </summary>
    [DataMember]
    public int SecJsonPort { get; internal set; }

    /// <summary>
    /// Номер Http-порта, используемого для обмена информацией без аутентификации.
    /// </summary>
    [DataMember]
    public int OpenHttpPort { get; internal set; }

    /// <summary>
    /// Номер Json-порта, применяющего сжатие для обмена информацией.
    /// </summary>
    [DataMember]
    public int JsonZippedPort { get; internal set; }

    /// <summary>
    /// Номер Json-порта, используемого для обмена информацией без аутентификации.
    /// </summary>
    [DataMember]
    public int OpenJsonPort { get; internal set; }

    /// <summary>
    /// Номер Http-порта c Windows аутентификацией, используемого для обмена информацией.
    /// </summary>
    [DataMember]
    public int WinHttpPort { get; internal set; }

    /// <summary>
    /// Версия сервера.
    /// </summary>
    [DataMember]
    public string ServerVersion { get; internal set; }

    /// <summary>
    /// Название продукта.
    /// </summary>
    [DataMember]
    public string ProductName { get; internal set; }

    /// <summary>
    /// SHA-256 хэширование паролей.
    /// </summary>
    [DataMember]
    public bool UseSHA256Hash { get; internal set; }
    #endregion
  }

  /// <summary>
  /// Данные для аутентификации пользователя.
  /// </summary>
  [Serializable]
  [DataContract( Namespace = Interface.Constants.SERVER_NAMESPACE )]
  public sealed class UserCredentials
  {
    #region .Properties
    /// <summary>
    /// Логин пользователя. Если null или пустая строка, то используется windows-аутентификация (свойство PasswordMD5 в этом случае игнорируется).
    /// </summary>
    [DataMember]
    public string Login { get; set; }

    /// <summary>
    /// Пароль, захешированный с помощью MD5 или SHA256. Не может быть равным null или пустой строкой, если свойство Login не равно null и не является пустой строкой.
    /// </summary>
    [DataMember]
    public string PasswordMD5 { get; set; }
    #endregion
  }

  /// <summary>
  /// Параметры клиента.
  /// </summary>
  [Serializable]
  [DataContract( Namespace = Interface.Constants.SERVER_NAMESPACE )]
  public sealed class ClientInfo
  {
    #region .Properties
    /// <summary>
    /// Название культуры, используемой клиентом.
    /// </summary>
    [DataMember]
    public string Culture { get; set; }
    #endregion
  }

  /// <summary>
  /// Информация об авторизации пользователя.
  /// </summary>
  [Serializable]
  [DataContract( Namespace = Interface.Constants.SERVER_NAMESPACE )]
  public sealed class AuthorizationToken
  {
    #region .Properties
    /// <summary>
    /// Идентификатор сессии.
    /// </summary>
    [DataMember]
    public ulong SessionId { get; set; }

    /// <summary>
    /// Разрешения доступа пользователя.
    /// </summary>
    [DataMember]
    public string[] Permissions { get; set; }

    /// <summary>
    /// Идентификатор организации.
    /// </summary>
    [DataMember]
    public int? OrganizationId { get; set; }

    /// <summary>
    /// Срок действия учетной записи
    /// </summary>
    [DataMember]
    public DateTime? UserAccountExpireDate { get; set; }

    /// <summary>
    /// Идентификатор ноды.
    /// </summary>
    [DataMember]
    public int? NodeId { get; set; }

    /// <summary>
    /// Адрес ноды
    /// </summary>
    [DataMember]
    public string NodeUrl { get; set; }

    /// <summary>
    /// Признак необходимости обновления
    /// </summary>
    [DataMember]
    public bool NeedUpdate { get; set; }

    /// <summary>
    /// Логин
    /// </summary>
    [DataMember]
    public string Login { get; set; }

    /// <summary>
    /// Альтернативные ГО
    /// </summary>
    [DataMember]
    public AlternativeOrgGroup[] AlternativeOrgGroups { get; set; }
    #endregion
  }

  /// <summary>
  /// Альтернативное ГО.
  /// </summary>
  [Serializable]
  [DataContract( Namespace = Interface.Constants.SERVER_NAMESPACE )]
  public sealed class AlternativeOrgGroup
  {
    #region .Properties
    /// <summary>
    /// Идентификатор ГО.
    /// </summary>
    [DataMember]
    public int Id { get; set; }

    /// <summary>
    /// Наименование ГО.
    /// </summary>
    [DataMember]
    public string Name { get; set; }
    #endregion
  }

  /// <summary>
  /// Расширение SOAP  заголовока.
  /// </summary>
  public class CustomHeader : MessageHeader
  {
    #region .Static Fields
    /// <summary>
    /// Код атрибута "Идентификатор сессии".
    /// </summary>
    public static readonly string SessionIdCode = MemberHelper.GetProperty(( CustomHeader e ) => e.SessionId).Name;
    #endregion

    #region .Properties
    /// <summary>
    /// Идентификатор сессии.
    /// </summary>
    public ulong SessionId { get; set; }

    public override string Name
    {
      get { return (Interface.Constants.SERVER_ADDRESS); }
    }

    public override string Namespace
    {
      get { return (ST.Utils.Constants.BASE_NAMESPACE); }
    }
    #endregion

    #region .Ctor
    public CustomHeader( ulong sessionId )
    {
      SessionId = sessionId;
    }
    #endregion

    #region .Methods
    protected override void OnWriteHeaderContents( System.Xml.XmlDictionaryWriter writer, MessageVersion messageVersion )
    {
      writer.WriteElementString(SessionIdCode, SessionId.ToString());
    }

    public static CustomHeader ReadHeader( Message request )
    {
      var nameSpace = ST.Utils.Constants.BASE_NAMESPACE;

      var headerPosition = request.Headers.FindHeader(Interface.Constants.SERVER_ADDRESS, nameSpace);

      headerPosition = headerPosition == -1 ? request.Headers.FindHeader(Interface.Constants.SERVER_ADDRESS, nameSpace = nameSpace + @"/") : headerPosition;

      if (headerPosition == -1)
        return null;

      XmlDictionaryReader reader = request.Headers.GetReaderAtHeader(headerPosition);

      if (reader.ReadToDescendant(SessionIdCode, nameSpace))
      {
        var sessionId = reader.ReadElementString();
        return string.IsNullOrEmpty(sessionId) ? null : new CustomHeader(Convert.ToUInt64(sessionId));
      }
      else
      {
        return null;
      }
    }
    #endregion
  }

  /// <summary>
  /// Информация о поддерживаемой системой культуре.
  /// </summary>
  [Serializable]
  [DataContract( Namespace = Interface.Constants.SERVER_NAMESPACE )]
  public class ServerCultureInfo
  {
    #region .Properties
    /// <summary>
    /// Код культуры.
    /// </summary>
    [DataMember]
    public string Code { get; set; }

    /// <summary>
    /// Название культуры.
    /// </summary>
    [DataMember]
    public string Name { get; set; }
    #endregion
  }
}