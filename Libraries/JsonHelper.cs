using System;
using System.Text.Json;
using System.Collections.Generic;

namespace CodeBlocks.Core;

public static class JsonHelper
{
    /// <summary>
    /// 依据相对路径取得子元素
    /// </summary>
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

    /// <summary>
    /// 取得指定路径的 JsonElement。此方法会克隆 JsonDocument 的根节点
    /// </summary>
    /// <param name="document"></param>
    /// <param name="path"></param>
    /// <returns></returns>
    public static JsonElement GetElementByPath(JsonDocument document, string path)
    {
        // 原始 JsonDocument 物件在离开生命周期后会被释放。
        // 为了安全地使用 JsonElement 对象，这里得克隆一个。
        return document.RootElement.Clone().GetChildElement(path);
    }

    /// <summary>
    /// 将 JsonElement 转换为物件数组
    /// </summary>
    public static object[] JsonElementToArray(JsonElement element)
    {
        List<object> list = [];
        foreach (var item in element.EnumerateArray())
        {
            list.Add(item.GetObject());
        }
        return [.. list];
    }

    /// <summary>
    /// 将 JsonElement 转换为字典
    /// </summary>
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

    /// <summary>
    /// 将 JsonElement 转换为 C# 物件
    /// </summary>
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

    #region "Extensions"

    public static object GetObject(this JsonElement element) => JsonElementToObject(element);

    public static Array GetArray(this JsonElement element) => JsonElementToArray(element);

    public static Dictionary<string, object> GetDictionary(this JsonElement element) => JsonElementToDictionary(element);
    
    #endregion
}
