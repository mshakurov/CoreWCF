using ST.Utils.Config;

using System.Threading;

namespace ST.Core
{
  internal static class Constants
  {
    #region .Constants
    public const string SHELL_REGISTRY_HIVE = @"HKEY_CURRENT_USER";
    public const string SHELL_REGISTRY_ROOT_PATH = SHELL_REGISTRY_HIVE + @"\Software\SpaceTeam\SpaceTeamLab";
    public const string SHELL_REGISTRY_SUB_PATH = ConfigController.CONFIG_SHELLSERVER_SUBPATH;
    public const string SHELL_MODULES_REGISTRY_PATH = SHELL_REGISTRY_SUB_PATH + @"\" + ConfigController.CONFIG_MODULES_SUBPATH;

    public const string SERVER_REGISTRY_HIVE = @"HKEY_LOCAL_MACHINE";
    public const string SERVER_REGISTRY_ROOT_PATH = SERVER_REGISTRY_HIVE + @"\Software\SpaceTeam\SpaceTeamLab";
    public const string SERVER_REGISTRY_SUB_PATH = ConfigController.CONFIG_APPSERVER_SUBPATH;
    public const string SERVER_MODULES_REGISTRY_PATH = SERVER_REGISTRY_SUB_PATH + @"\" + ConfigController.CONFIG_MODULES_SUBPATH;

    public const string CERTIFICATE_PARAMETER = "HttpsCertificate";

    public const string MODULE_DISABLED_PARAMETER = "#Disabled#";

    public const string LOG_NAME = "ST";
    public const long LOG_SIZE = 2048;

    public const string PARAMETER_CONNECTION = "Connection";

    public const string DEFAULT_NAMESPACE = "http://tempuri.org";

    public const int COMMUNICATION_TIME = 1000;
    public const int COMMUNICATION_GET_TIMEOUT_MIN = 100;
    public const int COMMUNICATION_GET_TIMEOUT_MAX = 1000;
    public const int COMMUNICATION_MESSAGE_EXPIRE_TIME = 60000;

    public const string SESSION_HEADER_NAME = "AuthID";
    public const int SESSION_CHECK_TIME = 10000;

    public const int COMMON_THREAD_STOP_TIMEOUT = 1000;

    public const string XMLSCHEMA_INSTANCE_NAMESPACE = "http://www.w3.org/2001/XMLSchema-instance";
    public const string SOAP_ENVELOPE_NAMESPACE = "http://www.w3.org/2003/05/soap-envelope";
    public const string WS_ADDRESSING_NAMESPACE = "http://www.w3.org/2005/08/addressing";

    public const string REPLY_TO_HEADER = "ReplyTo";
    public const string MESSAGE_ID_HEADER = "MessageID";

#if DEBUG
    public const int OPERATION_TIMEOUT = Timeout.Infinite;
#else
    public const int OPERATION_TIMEOUT = 90000;
#endif

#if DEBUG
    public const int UNLOAD_OPERATION_TIMEOUT = 90000;
#else
    public const int UNLOAD_OPERATION_TIMEOUT = 10000;
#endif
    #endregion
  }
}
