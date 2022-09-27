using ST.Utils.Attributes;
using System;

namespace ST.Utils.Licence
{
  /// <summary>
  /// Элемент, описывающий модуль
  /// </summary>
  [Serializable]
  [NotifyPropertyChanged]
  public class ModuleItem
  {
    #region .Properties
    [DisplayNameLocalized(RI.AssemblyFileName)]
    public string AssemblyFileName { get; set; } = string.Empty;
    #endregion

    #region .Ctor
    public ModuleItem()
    {
    }
    #endregion

    #region Equals
    public override bool Equals(object obj)
    {
      var sd = obj as ModuleItem;

      return sd != null && sd.AssemblyFileName == AssemblyFileName;
    }
    #endregion

    #region GetHashCode
    public override int GetHashCode()
    {
      return AssemblyFileName.GetHashCode();
    }
    #endregion

    #region ToString
    public override string ToString()
    {
      return AssemblyFileName;
    }
    #endregion
  }

  /// <summary>
  /// Элемент, описывающий максимальное количество терминалов
  /// </summary>
  [Serializable]
  [NotifyPropertyChanged]
  public class TerminalModuleItem : ModuleItem, ITerminalModuleItem
  {
    #region .Properties
    [DisplayNameLocalized("Максимально терминалов")]
    public int? MaxTerminals { get; set; }
    #endregion

    #region ToString
    public override string ToString()
    {
      return string.Format("{0}, Максимально терминалов: {1}", AssemblyFileName, MaxTerminals);
    }
    #endregion
  }

  /// <summary>
  /// Элемент, описывающий максимальное количество сессий
  /// </summary>
  [Serializable]
  [NotifyPropertyChanged]
  public class SessionModuleItem : ModuleItem, ISessionModuleItem
  {
    #region .Properties
    [DisplayNameLocalized("Максимально сессий")]
    public int? MaxSessions { get; set; }
    #endregion

    #region ToString
    public override string ToString()
    {
      return string.Format("{0}, Максимально сессий: {1}", AssemblyFileName, MaxSessions);
    }
    #endregion
  }
}