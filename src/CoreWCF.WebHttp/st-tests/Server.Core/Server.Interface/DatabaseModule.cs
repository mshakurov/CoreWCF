using ST.Core;
using ST.Utils;

namespace ST.Server
{
  /// <summary>
  /// Базовый класс серверной части модуля, работающего с базой данных.
  /// </summary>
  public abstract class DatabaseModule : ServerModule
  {
    #region .Fields
    /// <summary>
    /// Экземпляр класса для работы с базой данных.
    /// </summary>
    protected readonly Dbi DB = new Dbi();
    #endregion

    #region Initialize
    protected override void Initialize()
    {
      base.Initialize();

      OnConfigurationChanged();
    }
    #endregion

    #region OnConfigurationChanged
    protected override void OnConfigurationChanged()
    {
      base.OnConfigurationChanged();

      var dbConfig = GetConfiguration<ModuleDBConfig>();

      if( dbConfig != null && !string.IsNullOrWhiteSpace( dbConfig.Connection ) )
        DB.Connection = dbConfig.Connection;
    }
    #endregion
  }
}
