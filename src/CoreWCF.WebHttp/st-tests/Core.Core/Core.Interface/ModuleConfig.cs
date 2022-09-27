using System;
using System.ComponentModel;
using ST.Utils.Attributes;
using ST.Utils.Config;

namespace ST.Core
{
  /// <summary>
  /// Базовый класс конфигурации модуля.
  /// </summary>
  [Serializable]
  public class ModuleConfig : ItemConfig
  {
  }

  /// <summary>
  /// Конфигурация модуля, содержащая строку соединения.
  /// </summary>
  [Serializable]
  public abstract class ModuleDBConfig : ModuleConfig, ISupportsDBConnectionString
  {
    #region .Properties
    /// <summary>
    /// Строка соединения.
    /// </summary>
    [CategoryLocalized( Interface.RI.CategoryDBConnection )]
    [DisplayNameLocalized( Interface.RI.DisplayNameDBConnection )]
    public virtual string Connection { get; set; }

    /// <summary>
    /// Строка соединения с базой данных по умолчанию.
    /// </summary>
    [Browsable( false )]
    public abstract string DefaultConnection { get; }
    #endregion

    #region InitializeInstance
    protected override void InitializeInstance()
    {
      base.InitializeInstance();

      Connection = DefaultConnection;
    }
    #endregion

    #region ToString
    /// <summary>
    /// Возвращает строку соединения.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
      return Connection;
    }
    #endregion
  }

  /// <summary>
  /// Конфигурация модуля, содержащая строку соединения.
  /// </summary>
  [Serializable]
  public abstract class ModuleDBConfig_PG : ModuleDBConfig, ISupportsDBConnectionString
  {
    #region .Properties
    /// <summary>
    /// Строка соединения PostgreSQL.
    /// </summary>
    [CategoryLocalized( Interface.RI.CategoryDBConnection )]
    [DisplayNameLocalized( Interface.RI.DisplayNameDBConnection_PG )]
    public virtual string Connection_PG { get; set; }

    ///// <summary>
    ///// Включает смешанный режим БД - сначала PostgreSQL, а если ошибка то MSSQL (только для отладки).
    ///// </summary>
    //[CategoryLocalized( Interface.RI.CategoryDBConnection )]
    //[DisplayNameLocalized( Interface.RI.MixedDbMode )]
    //public bool MixedDbMode { get; set; }

    /// <summary>
    /// Строка соединения с базой данных PostgreSql по умолчанию.
    /// </summary>
    [Browsable( false )]
    public virtual string DefaultConnection_PG
    {
      get
      {
        // PostgreSQL connection string example: "Server=localhost;Port=5432;Database=st_idea;User Id=pguser;Password=pgpassword"

        //return String.Format( "Server={0};Port={1};Database={2};User Id={3};Password={4}", "localhost", 5432, "st_idea", "pguser", "pgpassword" );

        // Строка соединения с базой данных PostgreSql по умолчанию временно отключена до будущих решений по этому вопросу, чтобы по-умолчанию соединялось с MSSSQL.
        return "";
      }
    }
    #endregion

    #region InitializeInstance
    protected override void InitializeInstance( )
    {
      base.InitializeInstance();

      Connection_PG = DefaultConnection_PG;
    }
    #endregion

    #region ToString
    /// <summary>
    /// Возвращает строку соединения.
    /// </summary>
    /// <returns></returns>
    public override string ToString( )
    {
      return !String.IsNullOrEmpty( Connection_PG ) ? Connection_PG : Connection;
    }
    #endregion
  }
}
