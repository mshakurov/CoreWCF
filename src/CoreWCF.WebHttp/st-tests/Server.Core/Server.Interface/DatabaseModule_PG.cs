using ST.Core;
using ST.Utils;

namespace ST.Server
{
  /// <summary>
  /// Базовый класс серверной части модуля, работающего с базой данных MSSQL или PostgreSQL, если передан коннекшен для PostgreSQL.
  /// </summary>
  public abstract class DatabaseModule_PG : DatabaseModule
  {
    #region .Fields
    /// <summary>
    /// Экземпляр класса для работы с базой данных PostgreSQL.
    /// </summary>    
    private readonly Dbi_PG _db_pg = new Dbi_PG();

    private bool _usePG = false;
    #endregion

    #region .Properties
    protected new IDbi DB
    {
      get
      {
        return _usePG ? ( IDbi ) _db_pg : ( IDbi ) base.DB;
      }
    }

    public abstract string DbModuleName
    {
      get;
    }
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

      var dbConfig = GetConfiguration<ModuleDBConfig_PG>();

      if( dbConfig != null )
      {
        this._usePG = !string.IsNullOrWhiteSpace( dbConfig.Connection_PG );

        // PostgreSQL connection string example: "Server=localhost;Port=5432;Database=CP_ST-Security;User Id=pguser;Password=pgpassword"

        if( this._usePG )
        {
          _db_pg.Connection = dbConfig.Connection_PG;

          _db_pg.OwnerModuleName = DbModuleName;

          // Включает смешанный режим БД - сначала PostgreSQL, а если ошибка то MSSQL (только для отладки).
          //if( dbConfig.MixedDbMode )
          //{
          //  _db_pg.ReserveDbi = base.DB;
          //  _db_pg.MixedDbMode = dbConfig.MixedDbMode;
          //}

          /////
          // NOTE: For tests only!
          ////_db_pg.ReserveDbi = base.DB;
          /////
        }
      }
    }
    #endregion
  }
}
