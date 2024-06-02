using Microsoft.UI.Xaml.Media;
using System;
using Windows.UI;

namespace CodeBlocks.Core;

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

    public static Color FromInt(int colorInt, bool allowAlpha = false)
    {
        byte a = 0xFF, r, g, b;

        // 0xFFAABBCC -> Color.FromArgb(0xFF, 0xAA, 0xBB, 0xCC);
        b = (byte)(colorInt & 0xFF); colorInt >>= 8;
        g = (byte)(colorInt & 0xFF); colorInt >>= 8;
        r = (byte)(colorInt & 0xFF); colorInt >>= 8;
        if (allowAlpha) a = (byte)(colorInt & 0xFF);

        return Color.FromArgb(a, r, g, b);
    }

    public static Color FromHexString(string colorHex)
    {
        byte a = 0xFF, r = 0xFF, g = 0xFF, b = 0xFF;
        if (!colorHex.StartsWith('#')) return Color.FromArgb(a, r, g, b);

        if (colorHex.Length == 9) // #FFAABBCC
        {
            a = (byte)Convert.ToInt32(colorHex[1..3], 16);
            r = (byte)Convert.ToInt32(colorHex[3..5], 16);
            g = (byte)Convert.ToInt32(colorHex[5..7], 16);
            b = (byte)Convert.ToInt32(colorHex[7..9], 16);
            return Color.FromArgb(a, r, g, b);
        }
        if (colorHex.Length == 7) // #AABBCC
        {
            r = (byte)Convert.ToInt32(colorHex[1..3], 16);
            g = (byte)Convert.ToInt32(colorHex[3..5], 16);
            b = (byte)Convert.ToInt32(colorHex[5..7], 16);
            return Color.FromArgb(a, r, g, b);
        }
        if (colorHex.Length == 5) // #FABC --> #FFAABBCC
        {
            a = (byte)Convert.ToInt32(colorHex[1..2], 16); a = (byte)(a << 8 | a);
            r = (byte)Convert.ToInt32(colorHex[2..3], 16); r = (byte)(r << 8 | r);
            g = (byte)Convert.ToInt32(colorHex[3..4], 16); g = (byte)(g << 8 | g);
            b = (byte)Convert.ToInt32(colorHex[4..5], 16); b = (byte)(b << 8 | b);
            return Color.FromArgb(a, r, g, b);
        }
        if (colorHex.Length == 4) // #ABC --> #AABBCC
        {
            r = (byte)Convert.ToInt32(colorHex[1..2], 16); r = (byte)(r << 8 | r);
            g = (byte)Convert.ToInt32(colorHex[2..3], 16); g = (byte)(g << 8 | g);
            b = (byte)Convert.ToInt32(colorHex[3..4], 16); b = (byte)(b << 8 | b);
            return Color.FromArgb(a, r, g, b);
        }
        if (colorHex.Length == 2) // #A --> #AAAAAA
        {
            var tmp = Convert.ToInt32(colorHex[1..2], 16);
            tmp = tmp << 8 | tmp;
            r = g = b = (byte)tmp;
            return Color.FromArgb(a, r, g, b);
        }

        return Color.FromArgb(a, r, g, b);
    }

    #region "Extensions"

    public static int ToInt(this Color color) => (color.A << 24) | (color.R << 16) | (color.G << 8) | color.B;

    public static string ToHexString(this Color color) => $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";

    public static SolidColorBrush ToSolidColorBrush(this Color color) => new(color);

    #endregion
}
