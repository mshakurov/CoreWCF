using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ST.Utils.Config
{
  public static class JsonSerializeHelper
  {
    public readonly static JsonSerializerSettings _jsonSerializerSettings =
      new JsonSerializerSettings()
      {
        // !!! Не включать !!!
        PreserveReferencesHandling = PreserveReferencesHandling.Objects,
        // !!! Не включать !!!
        /*ContractResolver = new SettingsContractResolver()
        {
          DefaultMembersSearchFlags = BindingFlags.Public | BindingFlags.Instance,
        },*/
        ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
        TypeNameHandling = TypeNameHandling.All,
        StringEscapeHandling = StringEscapeHandling.EscapeHtml,
        ObjectCreationHandling = ObjectCreationHandling.Replace,
        MissingMemberHandling = MissingMemberHandling.Ignore,
        NullValueHandling = NullValueHandling.Include,
        ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
        TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
        CheckAdditionalContent = true,
        Formatting = Newtonsoft.Json.Formatting.Indented,
        DefaultValueHandling = DefaultValueHandling.Include
      };

    public static string SerializeJson( this object obj )=>JsonConvert.SerializeObject(obj, _jsonSerializerSettings );

    public static TObject DeserializeJson<TObject>( this string json ) => JsonConvert.DeserializeObject<TObject>(json, _jsonSerializerSettings);

    public static object DeepCloneByJson( this object obj ) => JsonConvert.DeserializeObject(JsonConvert.SerializeObject(obj, _jsonSerializerSettings), _jsonSerializerSettings);

    public static TObject DeepCloneByJson<TObject>( this TObject obj ) => JsonConvert.DeserializeObject<TObject>(JsonConvert.SerializeObject(obj, _jsonSerializerSettings), _jsonSerializerSettings);

  }
}
