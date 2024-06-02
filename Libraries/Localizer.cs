using System.IO;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace CodeBlocks.Core
{
    public class Localizer
    {
        public readonly object Content;

        public Localizer(string id)
        {
            string filePath = $"{App.Path}/Languages/{id}.yml";
            string yamlContent = File.ReadAllText(filePath);
            var deserializer = new DeserializerBuilder().Build();
            Content = deserializer.Deserialize(new StringReader(yamlContent));
        }

        public string GetString(string key) => GetString(Content, key);
        private static string GetString(object source, string key)
        {
            if (source is null || string.IsNullOrEmpty(key)) return string.Empty;

            var path = key.Split('.');
            var dict = source as Dictionary<object, object>;
            foreach (string item in path)
            {
                if (!dict.ContainsKey(item)) break;
                if (dict[item] is Dictionary<object, object> nextDict) dict = nextDict;
                else return dict[item].ToString();
            }
            return key;
        }

        public static void ReloadLanguageFiles()
        {
            List<string> list = [];
            App.RegisteredLanguages.Clear();
            foreach (var filePath in Directory.GetFiles($"{App.Path}/Languages/", "*.yml"))
            {
                string yamlContent = File.ReadAllText(filePath);
                var deserializer = new DeserializerBuilder().Build();
                var dict = deserializer.Deserialize(new StringReader(yamlContent)) as Dictionary<object, object>;
                if (dict.ContainsKey("Profile"))
                {
                    var id = GetString(dict, "Profile.Identifier");
                    var name = GetString(dict, "Profile.DisplayName");
                    var fileName = Path.GetFileNameWithoutExtension(filePath);
                    if (fileName != id) continue; // 忽略格式错误的语言档案
                    App.RegisteredLanguages.Add(name, id);
                    App.RegisteredLanguages.Add(id, name);
                    list.Add(name);
                }
            }

            App.SupportedLanguagesByName = [.. list];
        }
    }
}
