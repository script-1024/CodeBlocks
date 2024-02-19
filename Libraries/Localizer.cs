using System.IO;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace CodeBlocks.Core
{
    public class Localizer
    {
        private Dictionary<object, object> rootDict;

        public Localizer()
        {
            string id = App.LanguageIdentifiers[App.CurrentLanguage];
            string filePath = $"{App.AppPath}\\Languages\\{id}.yml";
            string yamlContent = File.ReadAllText(filePath);
            var deserializer = new DeserializerBuilder().Build();
            rootDict = deserializer.Deserialize(new StringReader(yamlContent)) as Dictionary<object, object>;
        }

        public string GetString(string category, string key)
        {
            return GetString(rootDict, category, key);
        }

        private static string GetString(object source, string category, string key)
        {
            var rootDict = source as Dictionary<object, object>;
            if (!rootDict.ContainsKey(category)) return $"{category}.{key}";
            var dict = rootDict[category] as Dictionary<object, object>;
            if (!dict.ContainsKey(key)) return $"{category}.{key}";
            return dict[key].ToString();
        }

        public static void ReloadLanguageProfiles()
        {
            List<string> list = new();
            App.LanguageIdentifiers.Clear();
            foreach (var filePath in Directory.GetFiles($"{App.AppPath}\\Languages\\", "*.yml"))
            {
                string yamlContent = File.ReadAllText(filePath);
                var deserializer = new DeserializerBuilder().Build();
                var rootDict = deserializer.Deserialize(new StringReader(yamlContent)) as Dictionary<object, object>;
                if (rootDict.ContainsKey("Profile"))
                {
                    var dict = rootDict["Profile"] as Dictionary<object, object>;
                    var id = GetString(rootDict, "Profile", "Identifier");
                    var name = GetString(rootDict, "Profile", "DisplayName");
                    var fileName = Path.GetFileNameWithoutExtension(filePath);
                    if (fileName != id) continue; // 跳过格式错误的语言档案
                    App.LanguageIdentifiers.Add(name, id);
                    list.Add(name);
                }
            }

            App.SupportedLangList = list.ToArray();
        }
    }
}
