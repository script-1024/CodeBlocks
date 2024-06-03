using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;

namespace CodeBlocks.Core;
public sealed class CodeBlockDefinition
{
    #region "Properties"

    // 只读属性
    public string FilePath { get; private set; }
    public ushort Version { get; private set; } = BlockEditor.CurrentCBDFormat;

    // 可写属性
    public string McfCode { get; set; } = "";
    public string Identifier { get; set; } = "";
    public byte Variant { get; set; } = 0b_0000;
    public int ColorInt { get; set; } = 0xFFFFFF;
    public BlockType BlockType { get; set; } = BlockType.Undefined;
    public Dictionary<string, string> TranslationsDict { get; set; } = new();
    public Dictionary<string, byte> SlotsTypeDict { get; set; } = new();

    #endregion

    public delegate void FileVersionNotSupportHandler(ushort fileVer, bool tooOld = true);
    public delegate void FileReadFailedHandler(string msg = "");
    public event FileVersionNotSupportHandler FileVersionNotSupport;
    public event FileReadFailedHandler FileReadFailed;

    #region "Medthod"

    private bool Failed(string msg = "")
    {
        FilePath = null;
        FileReadFailed?.Invoke(msg);
        return false; // 用于在方法中回传 false
    }

    public bool ReadFile(StorageFile file)
    {
        // 文件不可用
        if (!file.IsAvailable) return Failed("FileNotAvailable");

        FilePath = file.Path;

        byte[] data;
        try { data = File.ReadAllBytes(FilePath); }
        catch { return Failed("InternalException"); }

        return ReadData(data);
    }

    public async Task<bool> ReadFileAsync(string path)
    {
        // 文件不可用
        if (!File.Exists(path)) return Failed("FileNotAvailable");

        FilePath = path;

        byte[] data;
        try { data = await File.ReadAllBytesAsync(path); }
        catch { return Failed("InternalException"); }

        return ReadData(data);
    }

    public bool ReadData(byte[] data)
    {
        int index = 0;

        // 文件头
        if (data.Length < 16) return Failed("FileNotSupport");
        if (data[index++] != 0xCB || data[index++] != 0xDF) return Failed("FileNotSupport");

        // 检查版本号
        Version = ((ushort)data.ToShort(index)); index += 2;
        if (Version < BlockEditor.SupportFileVersion.Min) FileVersionNotSupport?.Invoke(Version, tooOld: true);
        if (Version > BlockEditor.SupportFileVersion.Max) FileVersionNotSupport?.Invoke(Version, tooOld: false);

        // 方块外观数据
        BlockType = (BlockType)data[index++];
        Variant = data[index++];
        byte countSlots = data[index++];

        // 校验
        if (data[index++] != FileOperations.GetCheckDigit(data, 0x00, 0x07)) return Failed("FileVerificationFailed");

        // 方块资料数据
        ColorInt = data.ToInt(index, +3, isBigEndian: true); index += 3;
        byte countTranslations = data[index++];
        byte lengthIdentifier = data[index++];
        int lengthCode = data.ToInt(index, 2); index += 2;

        // 校验
        if (data[index++] != FileOperations.GetCheckDigit(data, 0x08, 0x0F)) return Failed("FileVerificationFailed");

        // 读取文本  UTF-16字符每个占2字节
        Identifier = data.ToUnicodeString(index, lengthIdentifier * 2); index += lengthIdentifier * 2;
        if (Identifier.Length != lengthIdentifier) return Failed("FileDataIncorrectOrMissing");
        McfCode = data.ToUnicodeString(index, lengthCode * 2); index += lengthCode * 2;
        if (McfCode.Length != lengthCode) return Failed("FileDataIncorrectOrMissing");

        // 读取字典数据
        index = FileOperations.GetDictionaryFromData(data, index, countSlots, SlotsTypeDict);
        if (SlotsTypeDict.Count != countSlots) return Failed("FileDataIncorrectOrMissing");
        index = FileOperations.GetDictionaryFromData(data, index, countTranslations, TranslationsDict);
        if (TranslationsDict.Count != countTranslations) return Failed("FileDataIncorrectOrMissing");

        return true;
    }

    public void SaveFile()
    {
        WriteToFile(FilePath);
    }

    public void WriteToFile(string path)
    {
        // 首行文件头数据
        var data = new List<byte>(2) { 0xCB, 0xDF };                    // CB DF       -> 00..01  文件头
        data.AddRange(Version.ToBytes());                               // 01 00       -> 02..03  版本号
        data.Add((byte)BlockType);                                      // 02          -> 04      方块类型
        data.Add(Variant);                                              // 0A          -> 05      方块变体
        data.Add((byte)SlotsTypeDict.Count);                            // 00          -> 06      右侧插槽个数
        data.Add(FileOperations.GetCheckDigit([.. data], 0x00, 0x07));  // Check Digit -> 07      校验码
        data.AddRange(ColorInt.ToBytes(3).Reverse());                   // FF FF FF    -> 08..0A  方块颜色
        data.Add((byte)TranslationsDict.Count);                         // 03          -> 0B      翻译字典元素个数
        data.Add((byte)Identifier.Length);                              // 00          -> 0C      方块ID字数 (~255个字符)
        data.AddRange(McfCode.Length.ToBytes(2));                       // 00 00       -> 0D..0E  代码字数 (~65535个字符)
        data.Add(FileOperations.GetCheckDigit([.. data], 0x08, 0x0F));  // Check Digit -> 0F      校验码

        // 方块标识符
        data.AddRange(Identifier.ToBytes());

        // MCF代码
        data.AddRange(McfCode.ToBytes());

        // 插槽定义
        FileOperations.AppendDictionaryToData(data, SlotsTypeDict);

        // 本地化翻译字典键值对
        FileOperations.AppendDictionaryToData(data, TranslationsDict);

        // 写入数据
        File.WriteAllBytes(path, [.. data]);
    }

    #endregion
}

internal static class FileOperations
{
    public static byte GetCheckDigit(byte[] data, int startIndex = 0, int endIndex = 7)
    {
        byte result = 0, offset = 0;
        if (startIndex >= endIndex || startIndex < 0 || endIndex > data.Length) throw new ArgumentOutOfRangeException(null, "Specified index out of range");
        for (int i = startIndex; i < data.Length && offset < endIndex - startIndex; i++, offset++)
        {
            result ^= (byte)(data[i] ^ offset);
        }
        return result;
    }

    public static void AppendDictionaryToData(List<byte> data, Dictionary<string, string> dict)
    {
        foreach (string key in dict.Keys)
        {
            string value = dict[key];
            data.AddRange(key.Length.ToBytes(2));    // 键字数
            data.AddRange(value.Length.ToBytes(2));  // 值字数
            data.AddRange(key.ToBytes());            // 键：UTF-16 文本
            data.AddRange(value.ToBytes());          // 值：UTF-16 文本
        }
    }

    public static void AppendDictionaryToData(List<byte> data, Dictionary<string, byte> dict)
    {
        foreach (string key in dict.Keys)
        {
            byte value = dict[key];
            data.AddRange(key.Length.ToBytes(2));    // 键字数
            data.AddRange(key.ToBytes());            // 键：UTF-16 文本
            data.Add(value);                         // 值
        }
    }

    public static int GetDictionaryFromData(byte[] data, int bytesIndex, int dictCount, Dictionary<string, string> dict)
    {
        dict.Clear();
        for (int c = 0; c < dictCount; c++)
        {
            int keyLength = data.ToShort(bytesIndex); bytesIndex += 2;
            int valLength = data.ToShort(bytesIndex); bytesIndex += 2;
            var key = data.ToUnicodeString(bytesIndex, keyLength * 2); bytesIndex += keyLength * 2;
            var val = data.ToUnicodeString(bytesIndex, valLength * 2); bytesIndex += valLength * 2;
            dict.Add(key, val);
        }
        return bytesIndex;
    }

    public static int GetDictionaryFromData(byte[] data, int bytesIndex, int dictCount, Dictionary<string, byte> dict)
    {
        dict.Clear();
        for (int c = 0; c < dictCount; c++)
        {
            int keyLength = data.ToShort(bytesIndex); bytesIndex += 2;
            var key = data.ToUnicodeString(bytesIndex, keyLength * 2); bytesIndex += keyLength * 2;
            var val = data[bytesIndex++];
            dict.Add(key, val);
        }
        return bytesIndex;
    }
}