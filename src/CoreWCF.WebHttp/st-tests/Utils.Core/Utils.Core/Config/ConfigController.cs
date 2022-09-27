using ST.Utils.Attributes;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ST.Utils.Config
{
  public static class ConfigController
  {
    public const string CONFIG_MODULES_SUBPATH = "Modules";
    public const string CONFIG_APPSERVER_SUBPATH = "Application Server";
    public const string CONFIG_SHELLSERVER_SUBPATH = "";

    public static bool IsFileConfig([NotNull] string rootPath, [NotNull] string subPath) => FileConfig.Exists(subPath);

    public static TItem Get<TItem>([NotNull] string rootPath, [NotNull] string subPath, string name)
      where TItem : ItemConfig
    {
      if (IsFileConfig(rootPath, subPath))
        return BaseConfig.Get<FileConfig, TItem>(FileConfig.GetConfigFilePath(subPath), name);
      else
        return null; // BaseConfig.Get<RegistryConfig, TItem>(rootPath + "\\" + subPath, name);
    }

    /// <summary>
    /// Возвращает значение из конфигурации в обход кэша конфигурации.
    /// </summary>
    /// <typeparam name="TConfigType">Тип конфигуратора (например: RegistryConfig, XmlFileConfig и т.д.)</typeparam>
    /// <typeparam name="TValue">Тип значения.</typeparam>
    /// <param name="rootPath">Корневой путь реестра, по которому располагается значение.</param>
    /// <param name="subPath">Относительный путь раздела реестра, по которому располагается значение.</param>
    /// <param name="name">Название значения.</param>
    /// <returns>Значение.</returns>
    private static TValue GetValue<TConfigType, TValue>([NotNull] string rootPath, [NotNull] string subPath, string name)
      where TConfigType : BaseConfig, new()
    {
      var path =
        IsFileConfig(rootPath, subPath)
        ? FileConfig.GetConfigFilePath(subPath)
        : (rootPath + "\\" + subPath);

      var _config = BaseConfig.Get<TConfigType, ValueItemConfig>(path, name);

      return _config != null ? _config.GetValue<TValue>() : default(TValue); 
    }


    /// <summary>
    /// Возвращает значение из реестра.
    /// </summary>
    /// <typeparam name="TValue">Тип значения.</typeparam>
    /// <param name="rootPath">Корневой путь реестра, по которому располагается значение.</param>
    /// <param name="subPath">Относительный путь раздела реестра, по которому располагается значение.</param>
    /// <param name="name">Название значения.</param>
    /// <returns>Значение.</returns>
    public static TValue GetValue<TValue>([NotNull] string rootPath, [NotNull] string subPath, string name)
    {
      return
        IsFileConfig(rootPath, subPath)
        ? GetValue<FileConfig, TValue>(rootPath, subPath, name)
        : default(TValue); // GetValue<RegistryConfig, TValue>(rootPath, subPath, name);
    }

    /// <summary>
    /// Добавляет/обновляет элемент конфигурации.
    /// </summary>
    /// <param name="item">Элемент.</param>
    /// <param name="rootPath">Корневой путь реестра, по которому располагается значение.</param>
    /// <param name="subPath">Относительный путь раздела реестра, по которому располагается значение.</param>
    /// <param name="name">Название элемента.</param>
    public static void Set(ItemConfig item, string rootPath, string subPath, string name)
    {
      (
        (item.IsFile())
        ? (BaseConfig)BaseConfig.CreateConfig<FileConfig>(FileConfig.GetConfigFilePath(subPath))
        : null //(BaseConfig)BaseConfig.CreateConfig<RegistryConfig>(rootPath + "\\" + subPath)
      )?
        .SetToSource(item, name);
    }


    /// <summary>
    /// Добавляет/обновляет значение в реестре.
    /// </summary>
    /// <typeparam name="T">Тип значения.</typeparam>
    /// <param name="value">Значение.</param>
    /// <param name="rootPath">Корневой путь, по которому располагается значение.</param>
    /// <param name="subPath">Относительный путь раздела, по которому располагается значение.</param>
    /// <param name="name">Название значения.</param>
    public static void SetValue<T>(T value, [NotNull] string rootPath, [NotNull] string subPath, string name)
    {
      if (IsFileConfig(rootPath, subPath))
        FileConfig.SetValue(value, subPath, name);
      //else
      //  RegistryConfig.SetValue(value, rootPath + "\\" + subPath, name);
    }

  }
}
