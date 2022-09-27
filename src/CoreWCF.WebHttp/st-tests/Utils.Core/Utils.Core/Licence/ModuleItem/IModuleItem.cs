namespace ST.Utils.Licence
{
  /// <summary>
  /// Интерфейс, описывающий модуль: который использует максимальное количество сессий
  /// </summary>
  public interface ISessionModuleItem
  {
    #region .Properties
    /// <summary>
    /// Максимальное количество сессий
    /// </summary>
    int? MaxSessions { get; set; }
    #endregion
  }

  /// <summary>
  /// Интерфейс, описывающий модуль: который использует максимальное количество терминалов
  /// </summary>
  public interface ITerminalModuleItem
  {
    #region .Properties
    /// <summary>
    /// Максимальное количество терминалов
    /// </summary>
    int? MaxTerminals { get; set; }
    #endregion
  }
}