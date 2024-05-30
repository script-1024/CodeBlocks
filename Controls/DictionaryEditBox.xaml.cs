using System;
using Windows.System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Controls;

namespace CodeBlocks.Controls;

public sealed partial class DictionaryEditBox : UserControl
{
    public delegate void DictionaryUpdatedEventHandler();
    public event DictionaryUpdatedEventHandler DictionaryUpdated;

    // 隐藏基底类型的 Content 属性: DictionaryEditBox 不是容器也不支持承载内容控件
    public new Dictionary<string, string> Content { get; private set; } = new();

    private readonly App app = Application.Current as App;
    private string GetLocalizedString(string key) => app.Localizer.GetString(key);

    public DictionaryEditBox()
    {
        this.InitializeComponent();
        app.OnLanguageChanged += GetLocalized;
        GetLocalized();

        AddNewItem();
        Tip.Target = DictionaryView;
    }

    private void GetLocalized()
    {
        foreach (var obj in CommandBar.PrimaryCommands)
        {
            if (obj is AppBarButton btn) btn.Label = GetLocalizedString($"BlockEditor.TranslationsDictionary.Buttons.{btn.Tag}");
        }
        LangLabel.Text = GetLocalizedString("BlockEditor.TranslationsDictionary.ColumnTitles.ID");
        TextLabel.Text = GetLocalizedString("BlockEditor.TranslationsDictionary.ColumnTitles.Text");
        Tip.Title = GetLocalizedString("BlockEditor.Tips.ExistInvalidKeys");
    }

    #region "User Input Event"

    private void AddButton_Click(object sender, RoutedEventArgs e) => AddNewItem();
    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (DictionaryView.SelectedItems.Count != 0)
        {
            foreach (var item in DictionaryView.SelectedItems)
            {
                DictionaryView.Items.Remove(item);
            }
        }
        if (DictionaryView.Items.Count == 0)
        {
            await Task.Delay(10);
            AddNewItem();
        }

        // 字典更新事件
        BeforeDictionaryUpdate();
    }
    private async void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        DictionaryView.Items.Clear();
        await Task.Delay(10);
        Tip.IsOpen = false;
        AddNewItem();

        // 字典更新事件
        BeforeDictionaryUpdate();
    }

    #endregion

    private void FocusTo(int gridIndex, int txtboxIndex = 0)
    {
        if (gridIndex < 0) gridIndex = 0;
        if (gridIndex >= DictionaryView.Items.Count) gridIndex = DictionaryView.Items.Count - 1;
        var grid = DictionaryView.Items[gridIndex] as Grid;
        grid.Children[txtboxIndex].Focus(FocusState.Keyboard);
        DictionaryView.SelectedItem = grid;
    }

    private void AddNewItem(string key = "", string val = "")
    {
        var grid = new Grid() { Padding = new(10) };
        var col_0 = new ColumnDefinition() { Width = new(80) };
        var col_1 = new ColumnDefinition();
        grid.ColumnDefinitions.Add(col_0);
        grid.ColumnDefinitions.Add(col_1);

        var keyTxtBox = new TextBox()
        {
            Text = key,
            Margin = new(0, 0, 0, 0),
            IsSpellCheckEnabled = false,
            TextWrapping = TextWrapping.Wrap /* 设置这个属性以隐藏 “X” 清除按键 */
        };

        var valTxtBox = new TextBox()
        {
            Text = val,
            Margin = new(10, 0, 0, 0),
            IsSpellCheckEnabled = false
        };

        keyTxtBox.AddHandler(TappedEvent, new TappedEventHandler(OnTapped), true);
        valTxtBox.AddHandler(TappedEvent, new TappedEventHandler(OnTapped), true);

        void OnTapped(object sender, TappedRoutedEventArgs e)
        {
            var item = (sender as TextBox).Parent as Grid;
            DictionaryView.SelectedItem = item;
        }

        keyTxtBox.AddHandler(KeyDownEvent, new KeyEventHandler(OnKeyDown), true);
        valTxtBox.AddHandler(KeyDownEvent, new KeyEventHandler(OnKeyDown), true);

        void OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            var txtbox = sender as TextBox;
            var parentGrid = txtbox.Parent as Grid;
            int index = DictionaryView.Items.IndexOf(parentGrid);
            int cursorPosition = txtbox.SelectionStart;
            int textLength = txtbox.Text.Length;
            if (txtbox == keyTxtBox)
                switch (e.Key)
                {
                    case VirtualKey.Left:
                        if (cursorPosition == 0 && index > 0) FocusTo(index - 1, 1);
                        return;

                    case VirtualKey.Right:
                        if (cursorPosition == textLength) valTxtBox.Focus(FocusState.Keyboard);
                        return;

                    case VirtualKey.Up:
                        FocusTo(index - 1, 0);
                        return;

                    case VirtualKey.Down:
                        FocusTo(index + 1, 0);
                        return;

                    case VirtualKey.Back:
                        if (cursorPosition != 0 || index == 0) return;
                        DeleteButton_Click(null, null);
                        FocusTo(index - 1, 1);
                        return;

                    case VirtualKey.Enter:
                        valTxtBox.Focus(FocusState.Keyboard);
                        return;

                    default:
                        return;
                }

            if (txtbox == valTxtBox)
                switch (e.Key)
                {
                    case VirtualKey.Left:
                        if (cursorPosition == 0) keyTxtBox.Focus(FocusState.Keyboard);
                        return;

                    case VirtualKey.Right:
                        if (cursorPosition == textLength && parentGrid != DictionaryView.Items[^1] as Grid) FocusTo(index + 1, 0);
                        return;

                    case VirtualKey.Up:
                        FocusTo(index - 1, 1);
                        return;

                    case VirtualKey.Down:
                        FocusTo(index + 1, 1);
                        return;

                    case VirtualKey.Back:
                        if (cursorPosition == 0) keyTxtBox.Focus(FocusState.Keyboard);
                        return;

                    case VirtualKey.Enter:
                        if (parentGrid != DictionaryView.Items[^1] as Grid) { FocusTo(index + 1, 0); return; }
                        AddNewItem();
                        return;

                    default:
                        return;
                }
        }

        grid.Children.Add(keyTxtBox);
        grid.Children.Add(valTxtBox);

        Grid.SetColumn(keyTxtBox, 0);
        Grid.SetColumn(valTxtBox, 1);

        DictionaryView.Items.Add(grid);
        DictionaryView.SelectedItem = grid;
        keyTxtBox.Loaded += (_, _) => keyTxtBox.Focus(FocusState.Programmatic); // 将焦点设置到新添加的第一个文字框上
        valTxtBox.LostFocus += (_, _) => BeforeDictionaryUpdate();              // 值方块失去焦点时更新字典
    }

    public void BeforeDictionaryUpdate()
    {
        bool existInvalidKeys = false;

        this.Content.Clear();
        foreach (var item in DictionaryView.Items)
        {
            var grid = item as Grid;
            string key = (grid.Children[0] as TextBox).Text;
            string val = (grid.Children[1] as TextBox).Text;
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(val)) continue;
            if (! Content.TryAdd(key, val)) existInvalidKeys = true;
        }

        Tip.IsOpen = existInvalidKeys;

        // 通知订阅者更新字典
        DictionaryUpdated?.Invoke();
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
