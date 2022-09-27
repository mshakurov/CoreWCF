using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ST.Utils
{
  /// <summary>
  /// Вспомогательный класс для работы с окружением.
  /// </summary>
  public static class EnvironmentHelper
  {
    #region GetApplicationCultures
    /// <summary>
    /// Возвращает список культур, используемых приложением.
    /// </summary>
    /// <returns>Список культур.</returns>
    public static List<CultureInfo> GetApplicationCultures()
    {
      var cultures = new List<CultureInfo>();

      var baseDir = AppDomain.CurrentDomain.BaseDirectory;

#if DEBUG
#else
      foreach ( var d in Directory.GetDirectories( baseDir ) )
        Exec.Try( () => cultures.Add( CultureInfo.GetCultureInfo( d.Replace( baseDir, "" ) ) ) );
#endif

      if ( !cultures.Contains( SR.DefaultCulture ) )
        cultures.Add( SR.DefaultCulture );

      cultures.Sort( (ci1, ci2) => ci1.DisplayName.CompareTo( ci2.DisplayName ) );

      return cultures;
    }
    #endregion

    #region IsCommandLineArgumentDefined
    /// <summary>
    /// Определяет, указан ли в командной строке запуска приложения (через символ / или -) аргумент.
    /// </summary>
    /// <param name="arg">Проверяемый аргумент.</param>
    /// <returns>True - аргумент указан, иначе - False.</returns>
    public static bool IsCommandLineArgumentDefined( string arg )
    {
      return Environment.GetCommandLineArgs().Any( a => Regex.Match( a, @"[/|-]" + arg, RegexOptions.IgnoreCase | RegexOptions.Singleline ).Success );
    }
    #endregion
  }
}
