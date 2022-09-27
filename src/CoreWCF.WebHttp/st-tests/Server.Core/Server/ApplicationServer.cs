using System;
using System.Diagnostics;
using ST.Core;
using ST.Utils;
using ST.Utils.Attributes;

namespace ST.Server
{
  /// <summary>
  /// Сервер приложений.
  /// </summary>
  public class ApplicationServer : WcfServer, IApplicationServer
  {
    #region .Constants
    public const string LOG_SOURCE_NAME = "STApplicationServer";
    #endregion

    #region .Ctor
    /// <summary>
    /// Конструктор.
    /// </summary>
    public ApplicationServer()
    {
      SetServerLog( LOG_SOURCE_NAME );
    }
    #endregion

    #region OnModuleInitialized
    protected override void OnModulesLoading()
    {
      base.OnModulesLoading();

      StartServerConfigWatcher();
    }
    #endregion

    #region OnModulesUnloading
    protected override void OnModulesUnloading()
    {
      StopServerConfigWatcher();

      base.OnModulesUnloading();
    }
    #endregion

    #region GetConfig
    /// <summary>
    /// Возвращает конфигурацию сервера приложений.
    /// </summary>
    /// <returns>Конфигурация сервера.</returns>
    public static new ApplicationServerConfig GetConfig()
    {
      return WcfServer.GetConfig<ApplicationServerConfig>();
    }
    #endregion

    #region OnServerStateChanged
    protected override void OnServerStateChanged()
    {
      base.OnServerStateChanged();

      if ( ServerState == ServerState.Stopped )
        WriteToLog( SR.GetString( RI.ServerStopped, BaseServer.PID ), LogEventId.ServerStopped, EventLogEntryType.Information );
      else
        if ( ServerState == ServerState.Started )
        {
          var modules = string.Empty;

          foreach ( var m in Modules )
            modules += m.GetDisplayName() + Environment.NewLine;

          WriteToLog( SR.GetString( RI.ServerStarted, BaseServer.PID ) + Environment.NewLine + Environment.NewLine + ( modules == string.Empty ? SR.GetString( RI.NoLoadedModules ) : SR.GetString( RI.LoadedModules, modules ) ),
                      LogEventId.ServerStarted, EventLogEntryType.Information );
        }
    }
    #endregion

    #region SetConfig
    /// <summary>
    /// Устанавливает конфигурацию сервера.
    /// </summary>
    /// <param name="config">Конфигурация сервера.</param>
    public static void SetConfig( [NotNull] ApplicationServerConfig config )
    {
      WcfServer.SetConfig( config );
    }
    #endregion

    /// <summary>
    /// Идентификаторы событий для журнала событий (3000 - 3999).
    /// </summary>
    protected static new class LogEventId
    {
      #region .Constants
      /// <summary>
      /// Сервер приложений запущен.
      /// </summary>
      public const ushort ServerStarted = 3000;

      /// <summary>
      /// Сервер приложений остановлен.
      /// </summary>
      public const ushort ServerStopped = 3100;
      #endregion
    }
  }

  /// <summary>
  /// Конфигурация сервера приложений.
  /// </summary>
  [Serializable]
  public class ApplicationServerConfig : WcfServerConfig
  {
  }
}