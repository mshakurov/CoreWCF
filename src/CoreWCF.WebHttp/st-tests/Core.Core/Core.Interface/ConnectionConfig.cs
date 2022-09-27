using System;
using ST.Utils.Config;
using ST.Utils.Wcf;

namespace ST.Core
{
  /// <summary>
  /// Параметры соединения с сервером.
  /// </summary>
  [Serializable]
  public sealed class ConnectionConfig : ItemConfig
  {
    #region .Properties
    /// <summary>
    /// Точка доступа к WCF-серверу.
    /// </summary>
    public WcfServerEndpoint Endpoint { get; set; }

    /// <summary>
    /// Признак того, что используется аутентификация Windows.
    /// </summary>
    public bool UseWindowsAuthentication { get; set; }

    /// <summary>
    /// Признак того, что по возможности необходимо использовать TCP со сжатием.
    /// </summary>
    public bool UseTcpZipped { get; set; }

    /// <summary>
    /// Имя пользователя.
    /// </summary>
    public string UserName { get; set; }
    #endregion

    #region InitializeInstance
    protected override void InitializeInstance()
    {
      base.InitializeInstance();

      Endpoint = new WcfServerEndpoint( "localhost", 55555, WcfProtocolType.Http );
      UseWindowsAuthentication = true;
    }
    #endregion
  }
}
