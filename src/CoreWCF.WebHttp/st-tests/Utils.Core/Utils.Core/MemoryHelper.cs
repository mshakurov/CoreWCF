using System;
using System.Diagnostics;

namespace ST.Utils
{
  /// <summary>
  /// Вспомогательный класс для управления памятью.
  /// </summary>
  public static class MemoryHelper
  {
    #region Collect
    /// <summary>
    /// Выполняет сбор мусора.
    /// </summary>
    [DebuggerStepThrough]
    public static void Collect()
    {
      GC.Collect();
      GC.WaitForPendingFinalizers();
      GC.Collect();
    }
    #endregion

    #region Free
    /// <summary>
    /// Освобождает неиспользуемую процессом память.
    /// </summary>
    [DebuggerStepThrough]
    public static void Free()
    {
      Collect();

      if (OperatingSystem.IsWindows())
        Press();
    }

    [System.Runtime.Versioning.SupportedOSPlatform("freebsd")]
    [System.Runtime.Versioning.SupportedOSPlatform("maccatalyst")]
    [System.Runtime.Versioning.SupportedOSPlatform("macOS")]
    [System.Runtime.Versioning.SupportedOSPlatform("OSX")]
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static void Press()
    {
      //Предупреждение CA1416  Этот сайт вызова доступен на всех платформах. "Process.MaxWorkingSet.set" поддерживается только в 'freebsd', 'maccatalyst', 'macOS/OSX', 'windows'.Utils.Core  C:\_Projects_\_STL\MTOP\st - idea\Development\Source\Utils.Core\Utils.Core\MemoryHelper.cs  40  Активные
      Exec.Try(() => Process.GetCurrentProcess().MaxWorkingSet = Process.GetCurrentProcess().MaxWorkingSet);
    }
    #endregion
  }
}
