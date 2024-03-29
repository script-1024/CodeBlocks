﻿using Microsoft.UI.Xaml;
using Windows.UI;

namespace CodeBlocks.Core
{
    public static class Utils
    {
        public static Visibility ToVisibility(bool value) => (value) ? Visibility.Visible : Visibility.Collapsed;
        public static double GetBigger(double a, double b) => (a > b) ? a : b;
        public static double GetSmaller(double a, double b) => (a < b) ? a : b;
        public static bool GetFlag(int source, int bit) => ((source >> bit) & 1) == 1;
    }

    public static class ColorHelper
    {
        public static double GetRelativeLuminance(Color color)
        {
            double r = color.R / 255;
            double g = color.G / 255;
            double b = color.B / 255;
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
        public static Color ToWindowsUIColor(System.Drawing.Color color)
        {
            return Color.FromArgb(color.A, color.R, color.G, color.B);
        }
    }

    public static class TextHelper
    {
        public static double CalculateStringWidth(string str)
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
                c >= 0x4E00 && c <= 0x9FFF || // 中文字符
                c >= 0xFF01 && c <= 0xFF5E || // 全角字符
                c == 0x3000; // 全角空格
        }
    }
}
