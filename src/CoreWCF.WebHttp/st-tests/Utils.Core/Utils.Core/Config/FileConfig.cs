using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using ST.Utils.Attributes;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;

namespace ST.Utils.Config
{
  internal sealed class FileConfig : BaseConfig
  {
    private static string _rootDirectory = null;

    private const string _serialize_type_xml = "xml";
    private const string _serialize_type_json = "json";
    private const string _serialize_type = _serialize_type_json;

    public const string XMLCONFIG_SUBDIR = "config";

    public static string RootDirectory
    {
      get
      {
        return _rootDirectory ?? (_rootDirectory = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), XMLCONFIG_SUBDIR));
      }
    }

    private static Type[] _extraTypes;
    public static Type[] ExtraTypes => _extraTypes ?? (_extraTypes = AssemblyHelper.GetSubtypes(false, new[] { typeof(ItemConfig) }, Type.EmptyTypes).ToArray());

    protected override void Initialize()
    {
      base.Initialize();

      if (string.IsNullOrWhiteSpace(Path))
        throw new ArgumentException("Path property was not set properly.");
    }

    private static XmlFileMainConfigItem ReadConfig(string path)
    {
      if (!File.Exists(path))
        throw new FileNotFoundException(SR.GetString(RI.XmlFileConfigFileNotFound, path), path);

      using (Stream reader = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
      {
        if (_serialize_type.IsEqualCI(_serialize_type_json))
        {
          using (var text = new StreamReader(reader))
            return text.ReadToEnd().DeserializeJson<XmlFileMainConfigItem>();
        }
        else
        if (_serialize_type.IsEqualCI(_serialize_type_xml))
        {
          var config = (XmlFileMainConfigItem)(new XmlSerializer(typeof(XmlFileMainConfigItem)).Deserialize(reader));

          return config;
        }
        else
          throw new NotSupportedException();
      }
    }

    private static void Save(string path, XmlFileMainConfigItem config)
    {
      Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
      bool exists = File.Exists(path);
      try
      {
        using (Stream writer = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
        {
          if (_serialize_type.IsEqualCI(_serialize_type_json))
          {
            using (var text = new StreamWriter(writer))
              text.Write(config.SerializeJson());
          }
          else
          if (_serialize_type.IsEqualCI(_serialize_type_xml))
          {
            new XmlSerializer(typeof(XmlFileMainConfigItem)/*, ExtraTypes*/).Serialize(writer, config);
          }
        }
      }
      catch
      {
        if (!exists)
          try { File.Delete(path); } catch { }
        throw;
      }
    }

    internal static bool Exists(string subPath)
    {
      return Directory.Exists(RootDirectory) && File.Exists(GetConfigFilePath(subPath));
    }

    internal static void SetValue<T>(T value, string subPath, string name)
    {
      BaseConfig.Set<FileConfig>(new ValueItemConfig { Value = value }, GetConfigFilePath(subPath), name);
    }

    public static string GetConfigFilePath(string subPath) => System.IO.Path.Combine(RootDirectory, System.IO.Path.ChangeExtension(subPath, $".config.{(_serialize_type == _serialize_type_json ? "json" : _serialize_type == _serialize_type_xml ? "xml" : "/*=-=#~`")}"));

    private XmlFileMainConfigItem ReadConfig()
    {
      var config = ReadConfig(this.Path);

      if (config.ConfigItems != null)
        config.ConfigItems.ForEach(ci => base.Set(ci.Config.AsFile(), ci.Name));

      if (config.NamedParameters != null)
        config.NamedParameters.ForEach(ci => base.Set(ci.AsFile(), ci.Name));

      return config;
    }

    public override void Load()
    {
      base.Load();

      ReadConfig();
    }

    public override void Save()
    {
      Save(this.Path,
          new XmlFileMainConfigItem()
          {
            ConfigItems = base.GetList<ItemConfig>().Where(kv => !(kv.Value is NamedValueItemConfig)).Select(kv => new NamedItemConfig { Name = kv.Key, Config = kv.Value }).ToList(),
            NamedParameters = base.GetList<NamedValueItemConfig>().Select(kv => kv.Value).ToList()
          }
        );
    }

    internal override TConfigItem GetFromSource<TConfigItem>(string name = null)
    {
      var config = ReadConfig();

      name = name.GetEmptyOrTrimmed();

      return
        (config?.ConfigItems?.FirstOrDefault(nvc => nvc.Name == name)?.Config as TConfigItem)
        ??
        (config?.NamedParameters?.FirstOrDefault(nvc => nvc.Name == name) as TConfigItem);
    }

    internal override void SetToSource([NotNull] ItemConfig item, string name = null)
    {
      if (item != null || !string.IsNullOrEmpty(name))
      {
        var config = File.Exists(Path)
          ? ReadConfig(this.Path)
          : new XmlFileMainConfigItem();

        name = name.GetEmptyOrTrimmed();

        if (item is ValueItemConfig valueItemConfig)
        {
          var confItem = config.NamedParameters.FirstOrDefault(it => it.Name == name);

          if (valueItemConfig.Value != null)
          {
            config.NamedParameters.Remove(confItem);
            config.NamedParameters.Add(new NamedValueItemConfig
            {
              Name = name,
              Value = valueItemConfig.Value
            });
            Save(this.Path, config);
          }
          else
          {
            if (confItem != null)
            {
              config.NamedParameters.Remove(confItem);
              Save(this.Path, config);
            }
          }
        }
        else
        {
          var confItem = config.ConfigItems.FirstOrDefault(it => it.Name == name);

          config.ConfigItems.Remove(confItem);

          if (item != null)
          {
            var named = new NamedItemConfig
            {
              Name = name,
              Config = item
            };

            config.ConfigItems.Add(named);
          }

          Save(this.Path, config);
        }

      }
    }




    [Serializable]
    public class NamedItemConfig : ItemConfig
    {
      public string Name { get; set; }

      public ItemConfig Config { get; set; }
    }

    [Serializable]
    public class NamedItemListConfig
    {
      public List<NamedItemConfig> Items { get; set; } = new List<NamedItemConfig>();
    }

    [Serializable]
    public class NamedValueItemConfig : ValueItemConfig
    {
      public string Name { get; set; }
    }

    /// <summary>
    /// Класс для хранения всех параметров как один конфигурационный параметр
    /// </summary>
    [Serializable]
    public class NamedValueItemListConfig
    {
      public List<NamedValueItemConfig> Parameters { get; set; } = new List<NamedValueItemConfig>();
    }

    [Serializable]
    public class XmlFileMainConfigItem
    {
      public List<NamedItemConfig> ConfigItems { get; set; } = new List<NamedItemConfig>();

      public List<NamedValueItemConfig> NamedParameters { get; set; } = new List<NamedValueItemConfig>();
    }

    public class SettingsContractResolver : DefaultContractResolver
    {
      //protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
      //{
      //  var props1 =  base.CreateProperties(type, memberSerialization);
      //  var props2 = type.GetProperties().Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0).Select(p => CreateProperty(p, memberSerialization))
      //    .Concat(type.GetFields().Where(f => !f.IsInitOnly).Select(f => CreateProperty(f, memberSerialization)))
      //    .ToList();
      //  return props2;
      //}

      protected override List<MemberInfo> GetSerializableMembers(Type objectType)
      {
        var mems1 = base.GetSerializableMembers(objectType);
        var mems2 = objectType.GetProperties().Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0).OfType<MemberInfo>()
          .Concat(objectType.GetFields().Where(f => !f.IsInitOnly))
          .ToList();
        return mems2;
      }
    }
  }
}
