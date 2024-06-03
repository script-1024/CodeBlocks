using System;
using Windows.UI;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;

namespace CodeBlocks.Core;

public static class ColorHelper
{
    /// <summary>
    /// 取得用于绘制边框的较深色彩
    /// </summary>
    public static Color GetBorderColor(Color color)
    {
        byte a = color.A; byte r = color.R; byte g = color.G; byte b = color.B;

        r = (byte)(r * 0.75);
        g = (byte)(g * 0.75);
        b = (byte)(b * 0.75);

        return Color.FromArgb(a, r, g, b);
    }

    /// <summary>
    /// 将整数转换成颜色
    /// </summary>
    /// <param name="colorInt">要转换的整数值</param>
    /// <param name="allowAlpha">(可选) 是否应接受不透明度</param>
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

    /// <summary>
    /// 将十六进制色彩字符串转换成颜色
    /// </summary>
    /// <param name="colorHex">
    /// 十六进制色彩字符串，必须以 '#' 开头，接受以下格式：
    ///   <list type="table">
    ///     <term>#AARRGGBB</term> <description>完整色彩字符串</description><br/>
    ///     <term>#RRGGBB</term> <description>标准色彩字符串</description><br/>
    ///     <term>#ARGB</term> <description>精简色彩字符串，视为 #AARRGGBB</description><br/>
    ///     <term>#RGB</term> <description>精简色彩字符串，视为 #RRGGBB</description><br/>
    ///     <term>#XY</term> <description>灰度色彩字符串，视为 #XYXYXY</description><br/>
    ///     <term>#X</term> <description>灰度色彩字符串，视为 #XXXXXX</description>
    ///   </list>
    /// </param>
    /// <returns></returns>
    public static Color FromHexString(string colorHex)
    {
        byte a = 0xFF, r = 0xFF, g = 0xFF, b = 0xFF;
        if (!colorHex.StartsWith('#')) return Color.FromArgb(a, r, g, b);

        if (colorHex.Length == 9) // #AARRGGBB
        {
            a = (byte)Convert.ToInt32(colorHex[1..3], 16);
            r = (byte)Convert.ToInt32(colorHex[3..5], 16);
            g = (byte)Convert.ToInt32(colorHex[5..7], 16);
            b = (byte)Convert.ToInt32(colorHex[7..9], 16);
            return Color.FromArgb(a, r, g, b);
        }
        if (colorHex.Length == 7) // #RRGGBB
        {
            r = (byte)Convert.ToInt32(colorHex[1..3], 16);
            g = (byte)Convert.ToInt32(colorHex[3..5], 16);
            b = (byte)Convert.ToInt32(colorHex[5..7], 16);
            return Color.FromArgb(a, r, g, b);
        }
        if (colorHex.Length == 5) // #ARGB --> #AARRGGBB
        {
            a = (byte)Convert.ToInt32(colorHex[1..2], 16); a = (byte)(a << 8 | a);
            r = (byte)Convert.ToInt32(colorHex[2..3], 16); r = (byte)(r << 8 | r);
            g = (byte)Convert.ToInt32(colorHex[3..4], 16); g = (byte)(g << 8 | g);
            b = (byte)Convert.ToInt32(colorHex[4..5], 16); b = (byte)(b << 8 | b);
            return Color.FromArgb(a, r, g, b);
        }
        if (colorHex.Length == 4) // #RGB --> #RRGGBB
        {
            r = (byte)Convert.ToInt32(colorHex[1..2], 16); r = (byte)(r << 8 | r);
            g = (byte)Convert.ToInt32(colorHex[2..3], 16); g = (byte)(g << 8 | g);
            b = (byte)Convert.ToInt32(colorHex[3..4], 16); b = (byte)(b << 8 | b);
            return Color.FromArgb(a, r, g, b);
        }
        if (colorHex.Length == 3) // #XY --> #XYXYXY
        {
            var tmp = Convert.ToInt32(colorHex[1..3], 16);
            r = g = b = (byte)tmp;
            return Color.FromArgb(a, r, g, b);
        }
        if (colorHex.Length == 2) // #X --> #XXXXXX
        {
            var tmp = Convert.ToInt32(colorHex[1..2], 16);
            tmp = tmp << 8 | tmp;
            r = g = b = (byte)tmp;
            return Color.FromArgb(a, r, g, b);
        }

        return Color.FromArgb(a, r, g, b);
    }

    #region "Extensions"

    /// <summary>
    /// 将颜色转换成整数
    /// </summary>
    /// <returns>值为 0xAARRGGBB 的整数</returns>
    public static int ToInt(this Color color) => (color.A << 24) | (color.R << 16) | (color.G << 8) | color.B;

    /// <summary>
    /// 将颜色转换成十六进制色彩字符串
    /// </summary>
    /// <returns>值为 #AARRGGBB 的字符串</returns>
    public static string GetHexString(this Color color) => $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";

    public static SolidColorBrush GetSolidColorBrush(this Color color) => new(color);

    #endregion
}
