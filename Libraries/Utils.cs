using System;
using Windows.UI;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CodeBlocks.Core
{
    public enum DialogVariant
    {
        Yes = 1, No = 2, Cancel = 4, Confirm = 8, Save = 16, Giveup = 32,
        YesNo = 3, YesCancel = 5, YesNoCancel = 7, ConfirmCancel = 12, SaveGiveupCancel = 52,
    }

    public class MessageDialog
    {
        private readonly ContentDialog dialog = new();
        public bool IsDialogActivated { get; private set; } = false;
        public XamlRoot XamlRoot { get => dialog.XamlRoot; set => dialog.XamlRoot = value; }
        private string GetLocalizedString(string key) => (Application.Current as App).Localizer.GetString(key);
        public async Task<ContentDialogResult> ShowAsync(string msgId, DialogVariant variant = DialogVariant.Confirm)
        {
            if (IsDialogActivated) return ContentDialogResult.None;
            else IsDialogActivated = true;

            bool hasPrimaryButton = false;
            dialog.Title = GetLocalizedString($"Messages.{msgId}.Title");
            dialog.Content = GetLocalizedString($"Messages.{msgId}.Description");
            dialog.PrimaryButtonText = dialog.SecondaryButtonText = dialog.CloseButtonText = null; // 重置按键文本

            if ((int)(variant & DialogVariant.Yes) > 0)
            {
                dialog.PrimaryButtonText = GetLocalizedString("Messages.Button.Yes");
                hasPrimaryButton = true;
            }
            if ((int)(variant & DialogVariant.Confirm) > 0)
            {
                
                dialog.PrimaryButtonText = GetLocalizedString("Messages.Button.Confirm");
                hasPrimaryButton = true;
            }
            if ((int)(variant & DialogVariant.Save) > 0)
            {
                dialog.PrimaryButtonText = GetLocalizedString("Messages.Button.Save");
                hasPrimaryButton = true;
            }
            if ((int)(variant & DialogVariant.Giveup) > 0)
            {
                dialog.SecondaryButtonText = GetLocalizedString("Messages.Button.Giveup");
            }
            if ((int)(variant & DialogVariant.Cancel) > 0)
            {
                dialog.CloseButtonText = GetLocalizedString("Messages.Button.Cancel");
            }

            if (variant == DialogVariant.No || variant == DialogVariant.YesNo)
            {
                dialog.CloseButtonText = GetLocalizedString("Messages.Button.No");
            }
            else if (variant == DialogVariant.YesNoCancel)
            {
                dialog.SecondaryButtonText = GetLocalizedString("Messages.Button.No");
            }

            if (hasPrimaryButton) dialog.DefaultButton = ContentDialogButton.Primary;
            else dialog.DefaultButton = ContentDialogButton.Close;

            var result = await dialog.ShowAsync();
            IsDialogActivated = false;
            return result;
        }
    }

    public static class Utils
    {
        public static double GetBigger(double a, double b) => (a > b) ? a : b;
        public static double GetSmaller(double a, double b) => (a < b) ? a : b;
        public static bool GetFlag(int bits, int digits) => ((bits >> digits) & 1) == 1;
    }

    public static class Extensions
    {
        public static Visibility ToVisibility(this bool value) => (value) ? Visibility.Visible : Visibility.Collapsed;
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
