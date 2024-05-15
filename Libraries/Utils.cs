using System;
using Windows.UI;
using Microsoft.UI.Xaml;
using System.Linq;
using System.Text;
using CodeBlocks.Controls;

namespace CodeBlocks.Core
{
    public static class Utils
    {
        public static double GetBigger(double a, double b) => (a > b) ? a : b;
        public static double GetSmaller(double a, double b) => (a < b) ? a : b;
        public static bool GetFlag(int bits, int digits) => ((bits >> digits) & 1) == 1;

        public static T[] ConcatArrays<T>(params T[][] pList)
        {
            var result = new T[pList.Sum(x => x.Length)];
            int offset = 0;
            for (int i=0; i<pList.Length; i++)
            {
                pList[i].CopyTo(result, offset);
                offset += pList[i].Length;
            }
            return result;
        }
    }

    public static class Extensions
    {
        public static Visibility ToVisibility(this bool value) => (value) ? Visibility.Visible : Visibility.Collapsed;

        private static byte[] IntegerToBytes(ulong value, int length)
        {
            var bytes = new byte[length];
            for (int i = 0; i < length; i++)
            {
                bytes[i] = (byte)(value & 0xFF);
                value >>= 8;
            }
            return bytes;
        }

        private static long BytesToInteger(byte[] bytes, int index, int length)
        {
            long result = 0;
            for (int i=index, offset=0; i<bytes.Length && offset<length; i++, offset++)
            {
                result |= (long)bytes[i] << (offset * 8);
            }
            return result;
        }

        public static int ToInt(this Color color)
        {
            // A: 0xFF  R: 0xAA  G: 0xBB  B: 0xCC  ->  int: 0xFFAABBCC
            return (color.A << 24) | (color.R << 16) | (color.G << 8) | color.B;
        }

        public static string ToUnicodeString(this byte[] bytes, int bytesIndex = 0, int bytesCount = -1)
        {
            if (bytesCount == 0 || bytes.Length == 0) return string.Empty;
            if (bytesIndex < 0 || bytesIndex >= bytes.Length) throw new ArgumentOutOfRangeException(paramName: nameof(bytesIndex), message: "byte[].ToUnicodeString(): An invalid index was specified.");

            // UTF-16 每两个字节为一字符
            // 若呼叫时不想指定特定长度或设置了非法值，则将 count 设为字串长度
            if (bytesCount < 0) bytesCount = (bytes.Length - bytesIndex) / 2;
            return Encoding.Unicode.GetString(bytes, bytesIndex, bytesCount);
        }

        public static byte[] ToBytes(this string value, int charIndex = 0, int charCount = -1)
        {
            if (value.Length == 0 || charCount == 0) return [];
            if (charIndex < 0 || charIndex >= value.Length) throw new ArgumentOutOfRangeException(paramName: nameof(charIndex), message: "string.ToBytes(): An invalid index was specified.");

            // 若呼叫时不想指定特定长度或设置了非法值，则将 count 设为字串长度
            if (charCount < 0) charCount = value.Length;
            return Encoding.Unicode.GetBytes(value, charIndex, charCount);
        }

        public static byte[] ToBytes(this long value, int length = 8) => IntegerToBytes((ulong)value, length);
        public static byte[] ToBytes(this int value, int length = 4) => IntegerToBytes((ulong)value, length);
        public static byte[] ToBytes(this short value, int length = 2) => IntegerToBytes((ulong)value, length);

        public static byte[] ToBytes(this ulong value, int length = 8) => IntegerToBytes(value, length);
        public static byte[] ToBytes(this uint value, int length = 4) => IntegerToBytes(value, length);
        public static byte[] ToBytes(this ushort value, int length = 2) => IntegerToBytes(value, length);

        public static long ToLong(this byte[] bytes, int index = 0, int length = 8) => BytesToInteger(bytes, index, length);
        public static int ToInt(this byte[] bytes, int index = 0, int length = 4) => (int)BytesToInteger(bytes, index, length);
        public static short ToShort(this byte[] bytes, int index = 0, int length = 2) => (short)BytesToInteger(bytes, index, length);

        public static bool IsEqual(this BlockValueType type, BlockValueType other) => ((type & other) == other);
    }

    public static class ColorHelper
    {
        public static double GetRelativeLuminance(Color color)
        {
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;
            return 0.2126 * r + 0.7152 * g + 0.0722 * b;
        }

        public static bool IsBrightColor(Color color)
        {
            double rl = GetRelativeLuminance(color);
            if (rl >= 0.08) return true;
            return false;
        }

        public static Color GetBorderColor(Color color)
        {
            byte a = color.A; byte r = color.R; byte g = color.G; byte b = color.B;

            r = (byte)(r * 0.75);
            g = (byte)(g * 0.75);
            b = (byte)(b * 0.75);

            return Color.FromArgb(a, r, g, b);
        }

        public static Color FromInt(int colorHex, bool allowAlpha = false)
        {
            byte a = 0xFF, r, g, b;

            // 0xFFAABBCC -> Color.FromArgb(0xFF, 0xAA, 0xBB, 0xCC);
            b = (byte)(colorHex & 0xFF); colorHex >>= 8;
            g = (byte)(colorHex & 0xFF); colorHex >>= 8;
            r = (byte)(colorHex & 0xFF); colorHex >>= 8;
            if (allowAlpha) a = (byte)(colorHex & 0xFF);

            return Color.FromArgb(a, r, g, b);
        }
    }

    public static class TextHelper
    {
        public static double GetWidth(string str)
        {
            double totalWidth = 0;
            foreach (char c in str)
            {
                if (IsFullWidth(c)) totalWidth += 1;
                else totalWidth += 0.5;
            }
            return totalWidth;
        }

        public static bool IsFullWidth(char c)
        {
            return
            /* 中文字符 */ c >= 0x4E00 && c <= 0x9FFF ||
            /* 全角字符 */ c >= 0xFF01 && c <= 0xFF5E ||
            /* 全角空格 */ c == 0x3000;
        }
    }
}
