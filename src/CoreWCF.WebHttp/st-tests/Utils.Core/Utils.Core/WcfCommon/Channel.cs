using System.ComponentModel;
using System.Diagnostics;


namespace ST.Utils.Wcf
{
  /// <summary>
  /// Тип протокола обмена данными с WCF-сервисом.
  /// </summary>
  [Serializable]
  public enum WcfProtocolType : int
  {
    #region .Static Fields
    /// <summary>
    /// HTTPS.
    /// </summary>
    [Description( "https" )]
    Http = 0,

    /// <summary>
    /// TCP.
    /// </summary>
    [Description( "net.tcp" )]
    Tcp = 1,

    /// <summary>
    /// TCP и Windows аутентификация.
    /// </summary>
    [Description( "net.tcp" )]
    WinTcp = 2,

    /// <summary>
    /// TCP со сжатием. Десериализация сообщений при чтении.
    /// </summary>
    [Description( "net.tcp" )]
    TcpZippedIn = 3,

    /// <summary>
    /// TCP со сжатием и Windows аутентификация. Десериализация сообщений при чтении.
    /// </summary>
    [Description( "net.tcp" )]
    WinTcpZippedIn = 4,

    /// <summary>
    /// TCP со сжатием. Сериализация сообщений при записи.
    /// </summary>
    [Description( "net.tcp" )]
    TcpZippedOut = 5,

    /// <summary>
    /// TCP со сжатием и Windows аутентификация. Сериализация сообщений при записи.
    /// </summary>
    [Description( "net.tcp" )]
    WinTcpZippedOut = 6,

    /// <summary>
    /// TCP со сжатием. Сериализация сообщений при записи и их десериализация при чтении.
    /// </summary>
    [Description( "net.tcp" )]
    TcpZipped = 7,

    /// <summary>
    /// TCP со сжатием и Windows аутентификация. Сериализация сообщений при записи и их десериализация при чтении.
    /// </summary>
    [Description( "net.tcp" )]
    WinTcpZipped = 8,

    /// <summary>
    /// HTTP.
    /// </summary>
    [Description( "http" )]
    BasicHttp = 9,

    /// <summary>
    /// Json.
    /// </summary>
    [Description( "http" )]
    Json = 10,

    /// <summary>
    /// Именованный канал.
    /// </summary>
    [Description( "net.pipe" )]
    NamedPipe = 11,

    /// <summary>
    /// HTTP c security на уровне транспорта.
    /// </summary>
    [Description( "https" )]
    SecHttp = 12,

    /// <summary>
    /// Json c security на уровне транспорта..
    /// </summary>
    [Description( "https" )]
    SecJson = 13,

    /// <summary>
    /// HTTP c security на уровне транспорта и Windows аутентификация.
    /// </summary>
    [Description( "https" )]
    WinSecHttp = 14,

    /// <summary>
    /// HTTP без аутентификации.
    /// </summary>
    [Description( "http" )]
    OpenHttp = 15,

    /// <summary>
    /// Json со сжатием.
    /// </summary>
    [Description( "http" )]
    JsonZipped = 16,

    /// <summary>
    /// Json без аутентификации.
    /// </summary>
    [Description( "http" )]
    OpenJson = 17,

    /// <summary>
    /// HTTP и Windows аутентификация.
    /// </summary>
    [Description( "http" )]
    WinHttp = 18
    #endregion
  }

  /// <summary>
  /// Точка доступа к WCF-серверу.
  /// </summary>
  [Serializable]
  public sealed class WcfServerEndpoint
  {
    #region .Properties
    /// <summary>
    /// Адрес.
    /// </summary>
    public string Address { get; private set; }

    /// <summary>
    /// Порт.
    /// </summary>
    public int Port { get; private set; }

    /// <summary>
    /// Тип протокола.
    /// </summary>
    public WcfProtocolType ProtocolType { get; private set; }
    #endregion

    #region .Ctor
    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="address">Адрес.</param>
    /// <param name="port">Порт.</param>
    /// <param name="protocolType">Тип протокола.</param>
    [DebuggerStepThrough]
    public WcfServerEndpoint( string address, int port, WcfProtocolType protocolType )
    {
      Address = address;
      Port = port;
      ProtocolType = protocolType;
    }
    #endregion

    #region Equals
    public override bool Equals( object obj )
    {
      return (obj is WcfServerEndpoint se) && Address.IsEqualCI( se.Address ) && Port == se.Port && ProtocolType == se.ProtocolType;
    }
    #endregion

    #region GetHashCode
    public override int GetHashCode()
    {
      return ToString().GetHashCode();
    }
    #endregion

    #region ToString
    public override string ToString()
    {
      return string.Format( "{0}://{1}:{2}", ProtocolType.GetDescription(), Address.TrimEnd( '/' ), Port );
    }
    #endregion
  }

}
