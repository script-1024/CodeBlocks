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
    private string activatedMsgId = "";
    private readonly ContentDialog dialog = new();

    public bool IsDialogActivated { get; private set; } = false;
    public XamlRoot XamlRoot { get => dialog.XamlRoot; set => dialog.XamlRoot = value; }

    private string GetLocalizedString(string key) => (Application.Current as App).Localizer.GetString(key);
    
    private void SetDialogButtons(DialogVariant variant)
    {
        // 重置对话框按钮文本
        dialog.PrimaryButtonText = dialog.SecondaryButtonText = dialog.CloseButtonText = null;

        // 主要按钮
        if (variant.HasFlag(DialogVariant.Yes))
            dialog.PrimaryButtonText = GetLocalizedString("Messages.Button.Yes");

        if (variant.HasFlag(DialogVariant.Confirm))
            dialog.PrimaryButtonText = GetLocalizedString("Messages.Button.Confirm");

        if (variant.HasFlag(DialogVariant.Save))
            dialog.PrimaryButtonText = GetLocalizedString("Messages.Button.Save");

        // 次要按钮
        if (variant.HasFlag(DialogVariant.Giveup))
            dialog.SecondaryButtonText = GetLocalizedString("Messages.Button.Giveup");

        // 关闭按钮
        if (variant.HasFlag(DialogVariant.Cancel))
            dialog.CloseButtonText = GetLocalizedString("Messages.Button.Cancel");

        // 'No' 的位置不固定，需要特别判断
        if (variant == DialogVariant.No || variant == DialogVariant.YesNo) dialog.CloseButtonText = GetLocalizedString("Messages.Button.No");
        else if (variant == DialogVariant.YesNoCancel) dialog.SecondaryButtonText = GetLocalizedString("Messages.Button.No");

        // 若未指定主要按钮，则将预设按键设为关闭按钮
        dialog.DefaultButton = (dialog.PrimaryButtonText is null) ? ContentDialogButton.Close : ContentDialogButton.Primary;
    }

    public async Task<ContentDialogResult> ShowAsync(string msgId, DialogVariant variant = DialogVariant.Confirm)
    {
        if (XamlRoot is null) return ContentDialogResult.None;

        // 阻止重复呼叫
        if (IsDialogActivated && activatedMsgId == msgId) return ContentDialogResult.None;

        // 若对话框处于激活状态 等待其退出
        while (IsDialogActivated) await Task.Delay(200);

        activatedMsgId = msgId;
        IsDialogActivated = true;
        
        dialog.Title = GetLocalizedString($"Messages.{msgId}.Title");
        dialog.Content = GetLocalizedString($"Messages.{msgId}.Description");

        SetDialogButtons(variant);

        var result = await dialog.ShowAsync();
        IsDialogActivated = false;
        activatedMsgId = "";
        return result;
    }

    public async Task<ContentDialogResult> ShowAsync(string msgId, object content, DialogVariant variant = DialogVariant.Confirm)
    {
        if (XamlRoot is null) return ContentDialogResult.None;

        // 阻止重复呼叫
        if (IsDialogActivated && activatedMsgId == msgId) return ContentDialogResult.None;

        // 若对话框处于激活状态 等待其退出
        while (IsDialogActivated) await Task.Delay(200);

        activatedMsgId = msgId;
        IsDialogActivated = true;

        dialog.Title = GetLocalizedString($"Messages.{msgId}.Title");
        dialog.Content = content;

        SetDialogButtons(variant);

        var result = await dialog.ShowAsync();
        IsDialogActivated = false;
        activatedMsgId = "";
        return result;
    }
}
