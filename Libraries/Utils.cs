using Microsoft.UI.Xaml;
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
            if (rl >= 0.2) return true;
            return false;
        }

        public static Color GetBorderColor(Color color)
        {
            byte delta = 30;
            byte a = color.A; byte r = color.R; byte g = color.G; byte b = color.B;

            if (IsBrightColor(color))
            {
                if (r >= delta) r -= delta;
                if (g >= delta) g -= delta;
                if (b >= delta) b -= delta;
            }
            else
            {
                if (r <= 255 - delta) r += delta;
                if (g <= 255 - delta) g += delta;
                if (b <= 255 - delta) b += delta;
            }
            return Color.FromArgb(a, r, g, b);
        }
        public static Color ToWindowsUIColor(System.Drawing.Color color)
        {
            return Color.FromArgb(color.A, color.R, color.G, color.B);
        }
    }
}
