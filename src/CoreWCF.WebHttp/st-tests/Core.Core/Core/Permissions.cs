using System;
using System.ComponentModel;

namespace ST.Core.Permissions
{
  /// <summary>
  /// Базовое разрешение доступа для работы с сервером приложений.
  /// </summary>
  [Serializable]
  [DisplayName( RI.ApplicationServer )]
  public class ApplicationServer : Permission { }

  /// <summary>
  /// Разрешение доступа дает возможность подключаться к серверу приложений из-за пределов локальной сети.
  /// </summary>
  [Serializable]
  [DisplayName( RI.RemoteAccess )]
  [Description( RI.RemoteAccessDescription )]
  public class RemoteAccess : ApplicationServer { }

  /// <summary>
  /// Разрешение доступа дает возможность одновременно входить на сервер под одним логином нескольким клиентам.
  /// </summary>
  [Serializable]
  [DisplayName( RI.MultipleLogon )]
  [Description( RI.MultipleLogonDescription )]
  public class MultipleLogon : ApplicationServer { }

  /// <summary>
  /// Разрешение доступа дает возможность подключаться к серверу приложений только в пределах локальной сети.
  /// </summary>
  [Serializable]
  [DisplayName( RI.TCPIPOnlyAccess )]
  [Description( RI.TCPIPOnlyAccessDescription )]
  public class TCPIPOnlyAccess : ApplicationServer { }

  /// <summary>
  /// Разрешение доступа дает возможность подключаться к серверу приложений в пределах локальной сети.
  /// </summary>
  [Serializable]
  [DisplayName( RI.TCPIPAccess )]
  [Description( RI.TCPIPAccessDescription )]
  public class TCPIPAccess : ApplicationServer { }

  /// <summary>
  /// Коды специальных разрешений доступа.
  /// </summary>
  public static class Special
  {
    #region .Constants
#if DEBUG
    /// <summary>
    /// Код разрешения доступа "отладочный вход в систему".
    /// </summary>
    public const string DEBUG_LOGON = "DebugLogon";
#endif
    #endregion
  }
}
