using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

using ST.Utils.Attributes;

namespace ST.Utils
{
  /// <summary>
  /// Вспомогательный класс для работы со сборками и типами.
  /// </summary>
  public static class AssemblyHelper
  {
    #region GetFiles
    private static string[] GetFiles( string fileMask )
    {
      return Directory.GetFiles(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), fileMask);
    }
    private static string[] GetFiles( string[] fileNames )
    {
      return Directory.GetFiles(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "*.*").Join(fileNames, f => Path.GetFileName(f), f => f, ( filePath, _ ) => filePath, StringComparer.InvariantCultureIgnoreCase).ToArray();
    }
    #endregion

    #region GetSubtypes
    /// <summary>
    /// Возвращает список унаследованных типов (неабстрактных), найденных в указаной сборке.
    /// </summary>
    /// <param name="assembly">Сборка.</param>
    /// <param name="includeInternals">Признак включения в поиск внутренних типов.</param>
    /// <param name="parentTypes">Список родительских типов.</param>
    /// <returns>Список унаследованных типов.</returns>
    [DebuggerStepThrough]
    public static List<Type> GetSubtypes( [NotNull] Assembly assembly, bool includeInternals, [CollectionLength(1)] params Type[] parentTypes )
    {
      var types = new List<Type>();

      Type[] atypes = null;

      try
      {
        atypes = includeInternals ? assembly.GetTypes() : assembly.GetExportedTypes();
      }
      catch (ReflectionTypeLoadException exc)
      {
        atypes = exc.Types;
      }

      if (atypes != null)
        types.AddRange((from t in atypes
                        where t != null && !t.IsInterface && !t.IsAbstract && parentTypes.Any(p => t.IsInheritedFrom(p))
                        select t));

      return types;
    }

    /// <summary>
    /// Возвращает список унаследованных типов (неабстрактных), найденных в сборках текущего домена.
    /// </summary>
    /// <param name="includeInternals">Признак включения в поиск внутренних типов.</param>
    /// <param name="parentTypes">Список родительских типов.</param>
    /// <param name="assemblyAttributes">Список атрибутов, которыми должны быть помечены анализируемые сборки.</param>
    /// <returns>Список унаследованных типов.</returns>
    [DebuggerStepThrough]
    public static List<Type> GetSubtypes( bool includeInternals, [CollectionLength(1)] Type[] parentTypes, params Type[] assemblyAttributes )
    {
      return GetSubtypes(AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic).ToArray(), includeInternals, parentTypes, assemblyAttributes);
    }

    /// <summary>
    /// Возвращает список унаследованных типов (неабстрактных), найденных в сборках текущего каталога (в текущий домен загружаются только сборки с искомыми типами).
    /// </summary>
    /// <param name="includeInternals">Признак включения в поиск внутренних типов.</param>
    /// <param name="fileNames">Имена файлов сборок (без путей).</param>
    /// <param name="parentTypes">Список родительских типов.</param>
    /// <param name="assemblyAttributes">Список атрибутов, которыми должны быть помечены анализируемые сборки.</param>
    /// <returns>Список унаследованных типов.</returns>
    [DebuggerStepThrough]
    public static List<Type> GetSubtypes( bool includeInternals, string[] fileNames, [CollectionLength(1)] Type[] parentTypes, params Type[] assemblyAttributes )
    {
      return GetSubtypes(LoadAssemblies(fileNames, assemblyAttributes), includeInternals, parentTypes, assemblyAttributes);
    }

    /// <summary>
    /// Возвращает список унаследованных типов (неабстрактных), найденных в сборках.
    /// </summary>
    /// <param name="assemblies">Сбоки</param>
    /// <param name="includeInternals">Признак включения в поиск внутренних типов.</param>
    /// <param name="parentTypes">Список родительских типов.</param>
    /// <param name="assemblyAttributes">Список атрибутов, которыми должны быть помечены анализируемые сборки.</param>
    /// <returns>Список унаследованных типов.</returns>
    [DebuggerStepThrough]
    public static List<Type> GetSubtypes( Assembly[] assemblies, bool includeInternals, [CollectionLength(1)] Type[] parentTypes, params Type[] assemblyAttributes )
    {
      var types = new List<Type>();

      foreach (var assembly in assemblies)
        if (IsAttributesDefined(assembly, assemblyAttributes))
          types.AddRange(GetSubtypes(assembly, includeInternals, parentTypes));

      return types;
    }
    #endregion

    #region LoadAssembliesFromFiles
    /// <summary>
    /// Загружает в текущий домен сборки, удовлетворяющие указанным параметрам.
    /// </summary>
    /// <param name="files">Перечень полных путей файлов.</param>
    /// <param name="assemblyAttributes">Список атрибутов, которыми должны быть помечены анализируемые сборки.</param>
    public static Assembly[] LoadAssembliesFromFiles( string[] files, params Type[] assemblyAttributes )
    {
      var assemblies = new List<Assembly>();

      try
      {
        files.ForEachTry(filePath =>
        {
          var assembly = Assembly.LoadFrom(filePath);

          if (IsAttributesDefined(assembly, assemblyAttributes))
            assemblies.Add(assembly);
        });
      }
      catch { }

      return assemblies.ToArray();
    }
    #endregion

    #region LoadAssemblies
    /// <summary>
    /// Загружает в текущий домен сборки, удовлетворяющие указанным параметрам.
    /// </summary>
    /// <param name="fileMask">Маска обрабатываемых файлов.</param>
    /// <param name="assemblyAttributes">Список атрибутов, которыми должны быть помечены анализируемые сборки.</param>
    public static Assembly[] LoadAssemblies( [NotNullNotEmpty] string fileMask, params Type[] assemblyAttributes )
    {
      return LoadAssembliesFromFiles(GetFiles(fileMask), assemblyAttributes);
    }
    #endregion

    #region LoadAssemblies
    /// <summary>
    /// Загружает в текущий домен сборки, удовлетворяющие указанным параметрам.
    /// </summary>
    /// <param name="fileNames">Перечень имен файлов (без пути).</param>
    /// <param name="assemblyAttributes">Список атрибутов, которыми должны быть помечены анализируемые сборки.</param>
    public static Assembly[] LoadAssemblies( string[] fileNames, params Type[] assemblyAttributes )
    {
      return LoadAssembliesFromFiles(GetFiles(fileNames), assemblyAttributes);
    }
    #endregion

    #region IsAttributesDefined
    private static bool IsAttributesDefined( Assembly assembly, Type[] assemblyAttributes )
    {
      return assemblyAttributes == null || assemblyAttributes.Length == 0 || assemblyAttributes.All(a => assembly.IsDefined(a, false));
    }
    #endregion

    private class LoaderHelper
    {
      #region GetAssemblies
      [DebuggerStepThrough]
      public List<string> GetAssemblies( string[] files, Type[] assemblyAttributes )
      {
        return GetRequiredAssemblies(files, assemblyAttributes).Select(a => a.Location).ToList();
      }
      #endregion

      #region GetRequiredAssemblies
      [DebuggerStepThrough]
      private List<Assembly> GetRequiredAssemblies( string[] files, Type[] assemblyAttributes )
      {
        var assemblies = new List<Assembly>();

        files.ForEachTry(filePath =>
       {
         var assembly = Assembly.LoadFrom(filePath);

         if (IsAttributesDefined(assembly, assemblyAttributes))
           assemblies.Add(assembly);
       });

        return assemblies;
      }
      #endregion

      #region GetTypes
      [DebuggerStepThrough]
      public List<Type> GetTypes( bool includeInternals, string[] files, Type[] parentTypes, Type[] assemblyAttributes )
      {
        return GetRequiredAssemblies(files, assemblyAttributes).SelectMany(a => GetSubtypes(a, includeInternals, parentTypes)).ToList();
      }
      #endregion
    }
  }
}
