using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace CodeBlocks.Controls;

public sealed partial class DictionaryEditBox : UserControl
{
    private bool disabledFocusChange = false;
    public delegate void UpdateDictionaryEventHandler();
    public event UpdateDictionaryEventHandler UpdateDictionary;

    public DictionaryEditBox()
    {
        this.InitializeComponent();
        AddNewItem();
        DictionaryView.SelectionChanged += async (s, e) =>
        {
            if (disabledFocusChange) return;
            if (e == null || e.AddedItems == null || e.AddedItems[0] == null) return;

            await Task.Delay(10);  // 稍等一下，确保控件已被载入
            var panel = e.AddedItems[0] as StackPanel;
            if (panel.Children.Count > 0) panel.Children[0].Focus(FocusState.Programmatic);
        };

        this.LostFocus += (_, _) => UpdateDictionary?.Invoke();
    }

    #region "UserInput"

    private void AddButton_Click(object sender, RoutedEventArgs e) => AddNewItem();
    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        disabledFocusChange = true; // 删除控件时暫时不要修改焦点
        if (DictionaryView.SelectedItems.Count != 0)
        {
            foreach (var item in DictionaryView.SelectedItems)
            {
                DictionaryView.Items.Remove(item);
            }
        }
        disabledFocusChange = false;
        if (DictionaryView.Items.Count == 0)
        {
            await Task.Delay(10);
            AddNewItem();
        }
    }
    private async void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        disabledFocusChange = true; // 删除控件时暫时不要修改焦点
        DictionaryView.Items.Clear();
        disabledFocusChange = false;
        await Task.Delay(10);
        AddNewItem();
    }

    #endregion

    private void AddNewItem(string key = "", string val = "")
    {
        var stackPanel = new StackPanel()
        {
            Orientation = Orientation.Horizontal,
            Padding = new(10),
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };

        var keyTxtBox = new TextBox()
        {
            Text = key,
            Margin = new(5, 0, 0, 0),
            CornerRadius = new(0),
            IsSpellCheckEnabled = false
        };

        var valTxtBox = new TextBox()
        {
            Text = val,
            Margin = new(5, 0, 0, 0),
            CornerRadius = new(0),
            IsSpellCheckEnabled = false,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        keyTxtBox.AddHandler(TappedEvent, new TappedEventHandler(OnTapped), true);
        valTxtBox.AddHandler(TappedEvent, new TappedEventHandler(OnTapped), true);

        void OnTapped(object sender, TappedRoutedEventArgs e)
        {
            var item = (sender as TextBox).Parent as StackPanel;
            DictionaryView.SelectedItem = item;
        }

        keyTxtBox.AddHandler(KeyDownEvent, new KeyEventHandler(OnKeyPress), true);
        valTxtBox.AddHandler(KeyDownEvent, new KeyEventHandler(OnKeyPress), true);

        void OnKeyPress(object sender, KeyRoutedEventArgs e)
        {
            var txtbox = sender as TextBox;
            var stPanel = txtbox.Parent as StackPanel;
            int index = DictionaryView.Items.IndexOf(stPanel);
            if (txtbox == keyTxtBox)
            {
                if (e.Key == Windows.System.VirtualKey.Left)
                {
                    if (index > 0) DictionaryView.SelectedIndex = index - 1;
                }
                if (e.Key == Windows.System.VirtualKey.Right)
                {
                    valTxtBox.Focus(FocusState.Keyboard);
                }
                if (e.Key == Windows.System.VirtualKey.Back && keyTxtBox.Text.Length == 0)
                {
                    DeleteButton_Click(null, null);
                    if (index > 0) DictionaryView.SelectedIndex = index - 1;
                }
                if (e.Key == Windows.System.VirtualKey.Enter)
                {
                    valTxtBox.Focus(FocusState.Keyboard);
                }
            }
            if (txtbox == valTxtBox)
            {
                if (e.Key == Windows.System.VirtualKey.Left)
                {
                    keyTxtBox.Focus(FocusState.Keyboard);
                }
                if (e.Key == Windows.System.VirtualKey.Right)
                {
                    if (stPanel != DictionaryView.Items[^1] as StackPanel) DictionaryView.SelectedIndex = index + 1;
                }
                if (e.Key == Windows.System.VirtualKey.Back && valTxtBox.Text.Length == 0)
                {
                    keyTxtBox.Focus(FocusState.Keyboard);
                }
                if (e.Key == Windows.System.VirtualKey.Enter)
                {
                    if (stPanel == DictionaryView.Items[^1] as StackPanel) AddNewItem();
                    DictionaryView.SelectedIndex = index + 1;
                }
            }
        }

        stackPanel.Children.Add(keyTxtBox);
        stackPanel.Children.Add(valTxtBox);
        DictionaryView.Items.Add(stackPanel);
    }

    public Dictionary<string, string> GetDictionary(Dictionary<string, string> dict = null)
    {
        dict ??= new(DictionaryView.Items.Count);
        foreach (var item in DictionaryView.Items)
        {
            var stackPanel = item as StackPanel;
            string key = (stackPanel.Children[0] as TextBox).Text;
            string val = (stackPanel.Children[1] as TextBox).Text;
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(val)) continue;
            if (!dict.ContainsKey(key)) dict.Add(key, val);
        }
        return dict;
    }

    public void LoadDictionary(Dictionary<string, string> dict)
    {
        if (dict is null) return;
        DictionaryView.Items.Clear();
        foreach (string key in dict.Keys)
        {
            AddNewItem(key, dict[key]);
        }
        if (DictionaryView.Items.Count == 0) AddNewItem();
    }
}
