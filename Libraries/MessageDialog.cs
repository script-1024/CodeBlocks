using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

namespace CodeBlocks.Core;

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
    
    private void SetDialogButtons(DialogVariant variant)
    {
        bool hasPrimaryButton = false;

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
    }

    public async Task<ContentDialogResult> ShowAsync(string msgId, DialogVariant variant = DialogVariant.Confirm)
    {
        // 若对话框处于激活状态 等待其退出
        while (IsDialogActivated) await Task.Delay(200);
        IsDialogActivated = true;
        
        dialog.Title = GetLocalizedString($"Messages.{msgId}.Title");
        dialog.Content = GetLocalizedString($"Messages.{msgId}.Description");
        dialog.PrimaryButtonText = dialog.SecondaryButtonText = dialog.CloseButtonText = null; // 重置按键文本

        SetDialogButtons(variant);

        var result = await dialog.ShowAsync();
        IsDialogActivated = false;
        return result;
    }
}
