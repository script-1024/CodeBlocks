using System;
using System.Linq;
using System.Text;
using Microsoft.UI.Xaml;

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

        public static bool TryGetValue<T>(this T[] array, int index, out T value)
        {
            if (array is null || index < 0 || index >= array.Length) { value = default; return false; }
            value = array[index]; return true;
        }

        public static bool TrySetValue<T>(this T[] array, int index, T value)
        {
            if (array is null || index < 0 || index >= array.Length) return false;
            array[index] = value; return true;
        }

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

        private static long BytesToInteger(byte[] bytes, int index, int length, bool isBigEndian = false)
        {
            long result = 0;
            if (isBigEndian)
            {
                for (int i = index, offset = length - 1; i < bytes.Length && offset >= 0; i++, offset--)
                {
                    result |= (long)bytes[i] << (offset * 8);
                }
            }
            else
            {
                for (int i = index, offset = 0; i < bytes.Length && offset < length; i++, offset++)
                {
                    result |= (long)bytes[i] << (offset * 8);
                }
            }
            return result;
        }

        public static string ToUnicodeString(this byte[] bytes, int bytesIndex = 0, int bytesCount = -1)
        {
            if (bytesCount == 0 || bytes.Length == 0) return string.Empty;
            if (bytesIndex < 0 || bytesIndex >= bytes.Length) throw new ArgumentOutOfRangeException(paramName: nameof(bytesIndex), message: "byte[].ToUnicodeString(): An invalid index was specified.");

            // 若呼叫时不想指定特定长度或设置了非法值，则将 count 设为字串长度
            if (bytesCount < 0) bytesCount = bytes.Length - bytesIndex;
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

        public static byte[] ToUTF8Bytes(this string value, int charIndex = 0, int charCount = -1)
        {
            if (value.Length == 0 || charCount == 0) return [];
            if (charIndex < 0 || charIndex >= value.Length) throw new ArgumentOutOfRangeException(paramName: nameof(charIndex), message: "string.ToUTF8Bytes(): An invalid index was specified.");

            // 若呼叫时不想指定特定长度或设置了非法值，则将 count 设为字串长度
            if (charCount < 0) charCount = value.Length;
            return Encoding.UTF8.GetBytes(value, charIndex, charCount);
        }

        public static byte[] ToBytes(this long value, int length = 8) => IntegerToBytes((ulong)value, length);
        public static byte[] ToBytes(this int value, int length = 4) => IntegerToBytes((ulong)value, length);
        public static byte[] ToBytes(this short value, int length = 2) => IntegerToBytes((ulong)value, length);

        public static byte[] ToBytes(this ulong value, int length = 8) => IntegerToBytes(value, length);
        public static byte[] ToBytes(this uint value, int length = 4) => IntegerToBytes(value, length);
        public static byte[] ToBytes(this ushort value, int length = 2) => IntegerToBytes(value, length);

        public static long ToLong(this byte[] bytes, int index = 0, int length = 8, bool isBigEndian = false) => BytesToInteger(bytes, index, length, isBigEndian);
        public static int ToInt(this byte[] bytes, int index = 0, int length = 4, bool isBigEndian = false) => (int)BytesToInteger(bytes, index, length, isBigEndian);
        public static short ToShort(this byte[] bytes, int index = 0, int length = 2, bool isBigEndian = false) => (short)BytesToInteger(bytes, index, length, isBigEndian);

        public static bool HasFlag<T1, T2>(this T1 self, T2 other) where T1 : struct  where T2 : struct
        {
            long selfValue = Convert.ToInt64(self);
            long otherValue = Convert.ToInt64(other);
            return (selfValue & otherValue) == otherValue;
        }
    }
}
