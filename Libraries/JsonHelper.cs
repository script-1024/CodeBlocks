using System;
using System.Text.Json;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace CodeBlocks.Core;

public static class JsonHelper
{
    public static JsonElement GetChildElement(this JsonElement root, string path)
    {
        var pathArray = path.Split('.');
        JsonElement target = root;

        foreach (var element in pathArray)
        {
            if (target.TryGetProperty(element, out var item)) target = item;
            else return root; // 如果路径不存在，返回根节点
        }

        return target;
    }

    public static JsonElement GetElementByPath(JsonDocument document, string path)
    {
        // 原始 JsonDocument 物件在离开生命周期后会被释放。
        // 为了安全地使用 JsonElement 对象，这里得克隆一个。
        return document.RootElement.Clone().GetChildElement(path);
    }

    public static Array JsonElementToArray(JsonElement element)
    {
        List<object> list = [];
        foreach (var item in element.EnumerateArray())
        {
            list.Add(item.GetObject());
        }
        return list.ToArray();
    }

    public static Dictionary<string, object> JsonElementToDictionary(JsonElement element)
    {
        Dictionary<string, object> dict = [];
        foreach (var item in element.EnumerateObject())
        {
            string key = item.Name;
            dict.Add(key, item.Value.GetObject());
        }
        return dict;
    }

    public static object JsonElementToObject(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                return element.GetString();

            case JsonValueKind.Number:
                if (element.TryGetInt32(out int intValue)) return intValue;
                else return element.GetDouble();

            case JsonValueKind.True:
            case JsonValueKind.False:
                return element.GetBoolean();

            case JsonValueKind.Object:
                return element.GetDictionary();

            case JsonValueKind.Array:
                return element.GetArray();

            default:
                return element.ToString();
        }
    }

    public static object GetObject(this JsonElement element) => JsonElementToObject(element);

    public static Array GetArray(this JsonElement element) => JsonElementToArray(element);

    public static Dictionary<string, object> GetDictionary(this JsonElement element) => JsonElementToDictionary(element);
}
