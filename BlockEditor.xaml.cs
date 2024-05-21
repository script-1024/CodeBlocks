using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Controls;
using CodeBlocks.Core;
using CodeBlocks.Controls;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Microsoft.UI.Xaml.Input;

namespace CodeBlocks
{
    public sealed partial class BlockEditor : Window
    {
        public static readonly ushort CurrentCBDFormat = 1;
        public static readonly (ushort Min, ushort Max) SupportFileVersion = (1, 1);

        private bool isFileSaved = true;
        private uint invalidDataCount = 0;
        private StorageFile activeFile = null;
        private bool hasSaveDialogShown = false;

        private readonly MessageDialog dialog = new();
        private readonly App app = Application.Current as App;
        private readonly CodeBlockDefinition cbd = new();

        public BlockEditor()
        {
            InitializeComponent();

            // 订阅事件
            this.Closed += Window_Closed;
            RootGrid.Loaded += RootGrid_Loaded;
            DisplayCanvas.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
            DisplayCanvas.ManipulationDelta += DisplayCanvas_ManipulationDelta;
            cbd.FileVersionNotSupport += CBDFileVersionNotSupport;
            cbd.FileReadFailed += CBDFileReadFailed;

            // 限制窗口尺寸
            this.AppWindow.Changed += AppWindow_SizeChanged;
            AppWindow.Resize(new(900, 600));

            // 外观
            this.SystemBackdrop = new MicaBackdrop();
            this.ExtendsContentIntoTitleBar = true;
            var fe = this.Content as FrameworkElement;
            app.OnThemeChanged += () => fe.RequestedTheme = (ElementTheme)App.CurrentTheme;
            if (fe.RequestedTheme != (ElementTheme)App.CurrentTheme)
                fe.RequestedTheme = (ElementTheme)App.CurrentTheme;

            // 读取颜色清单
            ColorButton_ReloadItems();

            // 本地化翻译
            app.OnLanguageChanged += GetLocalized;
            GetLocalized();

            // 设置Tip的目标控件
            IdTip.Target = BlockIDTextBox;
        }
        public BlockEditor(StorageFile file) : this()
        {
            // 记录指定的文件
            activeFile = file;
        }

        #region "Window Close"

        public void Close(bool forceQuit)
        {
            if (forceQuit) this.Closed -= Window_Closed; // 取消订阅 Closed 事件
            app.OnLanguageChanged -= GetLocalized;       // 取消订阅翻译事件
            this.Close(); // 关闭窗口。如果 forceQuit == false，则此函数的表现和无参数 Close() 方法一致
        }
        private async Task<ContentDialogResult> FileNotSavedDialogShowAsync()
        {
            if (isFileSaved) return ContentDialogResult.Secondary;  // 已经保存，直接退出
            else return await dialog.ShowAsync("WindowClosing", DialogVariant.SaveGiveupCancel);
        }
        private async void Window_Closed(object sender, WindowEventArgs args)
        {
            args.Handled = true;
            var dialogResult = await FileNotSavedDialogShowAsync();

            // 保存并推出
            if (dialogResult == ContentDialogResult.Primary)
            {
                if (cbd.FilePath != null) cbd.SaveFile();  // 文件已存在
                else                                       // 文件不存在，弹出保存对话框
                {
                    var taskResult = await ExportFileAsync();
                    if (taskResult.Failed) return;
                }
            }

            // 只要对话框返回结果不是取消 都应该关闭窗口
            if (dialogResult != ContentDialogResult.None) { args.Handled = false; Close(true); }
        }

        #endregion

        #region "Translate"

        private string GetLocalizedString(string key) => app.Localizer.GetString(key);
        private void GetLocalized()
        {
            TitleBar_Name.Text = GetLocalizedString("BlockEditor.Title");
            OpenButton.Content = GetLocalizedString("BlockEditor.Open");
            ExportButton.Content = GetLocalizedString("BlockEditor.Export");

            foreach (var obj in BlockTypeComboBox.Items)
            {
                if (obj is ComboBoxItem item) item.Content = GetLocalizedString($"BlockEditor.BlockType.{item.Tag}");
            }
            // 切换语言需要更新当前选择项
            BlockTypeComboBox.SelectedIndex = -1;

            BlockIDLabel.Text = GetLocalizedString("BlockEditor.BlockData.ID");
            SlotsLabel.Text = GetLocalizedString("BlockEditor.BlockData.Slots.Label");
            LSlotCheckBox.Content = GetLocalizedString("BlockEditor.BlockData.Slots.Left");
            TSlotCheckBox.Content = GetLocalizedString("BlockEditor.BlockData.Slots.Top");
            RSlotCheckBox.Content = GetLocalizedString("BlockEditor.BlockData.Slots.Right");
            BSlotCheckBox.Content = GetLocalizedString("BlockEditor.BlockData.Slots.Bottom");

            TranslationsDictEditorHeader.Text = GetLocalizedString($"BlockEditor.TranslationsDictionary.Header");
        }

        #endregion

        #region "Loaded & Changed Event"

        private void ColorButton_ReloadItems()
        {
            var resourceDict = app.Resources.MergedDictionaries.FirstOrDefault(d => d.Source.OriginalString.EndsWith("BlockColor.xaml"));
            if (resourceDict is null) return;

            var gridView = (ColorButton.Flyout as Flyout).Content as GridView;
            gridView.Items.Clear();

            resourceDict = resourceDict.ThemeDictionaries["Default"] as ResourceDictionary;
            foreach (SolidColorBrush brush in resourceDict.Values)
            {
                var rect = new Rectangle();
                var color = brush.Color;
                rect.Fill = new SolidColorBrush(color);
                gridView.Items.Add(rect);
            }
        }
        private void RootGrid_Loaded(object sender, RoutedEventArgs e)
        {
            dialog.XamlRoot = RootGrid.XamlRoot;
            if (activeFile != null)
            {
                if (!cbd.ReadFile(activeFile)) activeFile = null;
            }

            Scroller_BackToCenter();
            LoadBlock();

            TranslationsDictoraryEditor.UpdateDictionary += () => {
                DemoBlock.TranslationsDict = TranslationsDictoraryEditor.GetDictionary(cbd.TranslationsDict);
                DemoBlock.RefreshBlockText();
            };
        }
        private void AppWindow_SizeChanged(object sender, AppWindowChangedEventArgs args)
        {
            if (args.DidSizeChange)
            {
                bool needResize = false;
                var size = this.AppWindow.Size;
                if (size.Width < 900) { needResize = true; size.Width = 900; }
                if (size.Height < 600) { needResize = true; size.Height = 600; }
                if (needResize) AppWindow.Resize(size);
                Scroller_BackToCenter();
            }
        }
        private async void CBDFileVersionNotSupport(ushort fileVer, bool tooOld = true)
        {
            if (tooOld) await dialog.ShowAsync("FileFormatIsTooOld", DialogVariant.Confirm);
            else await dialog.ShowAsync("AppVersionIsTooOldToOpenFile", DialogVariant.Confirm);
        }
        private async void CBDFileReadFailed(string msg = "")
        {
            if (string.IsNullOrEmpty(msg)) await dialog.ShowAsync("ReadFileFailed.UnknownException", DialogVariant.Confirm);
            else await dialog.ShowAsync($"ReadFileFailed.{msg}", DialogVariant.Confirm);
        }

        #endregion

        #region "User Input Event"

        private void DisplayCanvas_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            var newX = Scroller.HorizontalOffset - e.Delta.Translation.X;
            var newY = Scroller.VerticalOffset - e.Delta.Translation.Y;
            Scroller.ChangeView(newX, newY, null, true);
        }
        private async void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker openPicker = new();

            // 取得当前窗口句柄，将选择器的拥有者设为此窗口
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hwnd);

            // 选择器的预设路径
            openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

            // 文件类型
            string fileDescription = GetLocalizedString("Misc.CBDFile");
            openPicker.FileTypeFilter.Add(".cbd");

            StorageFile file = await openPicker.PickSingleFileAsync();
            if (file != null && cbd.ReadFile(file))
            {
                activeFile = file;
                LoadBlock();
            }
        }
        private async void ExportButton_Click(object sender, RoutedEventArgs e) => await ExportFileAsync();
        private void BlockTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BlockTypeComboBox.SelectedIndex < 0) { BlockTypeComboBox.SelectedIndex = (int)DemoBlock.MetaData.Type; return; }
            var selection = BlockTypeComboBox.SelectedItem as ComboBoxItem;
            if (selection is null) return;

            BlockType type;
            switch (selection.Tag?.ToString())
            {
                case "Event":
                    type = BlockType.Event;
                    LSlotCheckBox.IsEnabled = false;
                    LSlotCheckBox.IsChecked = false;
                    TSlotCheckBox.IsEnabled = false;
                    TSlotCheckBox.IsChecked = false;
                    break;
                case "Control":
                    type = BlockType.Control;
                    LSlotCheckBox.IsEnabled = true;
                    TSlotCheckBox.IsEnabled = true;
                    break;
                case "Action":
                    type = BlockType.Action;
                    LSlotCheckBox.IsEnabled = true;
                    TSlotCheckBox.IsEnabled = true;
                    break;
                default:
                    return;
            }

            DemoBlock.SetData(BlockProperties.Type, type);
            UpdateBlockVariant();
            cbd.BlockType = type;
            isFileSaved = false;
        }
        private void ColorGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var rect = (Rectangle)e.ClickedItem;
            var color = ((SolidColorBrush)rect.Fill).Color;

            CurrentColor.Background = new SolidColorBrush(color);
            DemoBlock.BlockColor = color;
            cbd.ColorHex = color.ToInt();
            isFileSaved = false;

            // Delay required to circumvent GridView bug: https://github.com/microsoft/microsoft-ui-xaml/issues/6350
            Task.Delay(10).ContinueWith(_ => ColorButton.Flyout.Hide(), TaskScheduler.FromCurrentSynchronizationContext());
        }
        private void CheckBox_Click(object sender, RoutedEventArgs e) => UpdateBlockVariant();

        private void BlockIDTextBox_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
        {
            int invalidCharCount = args.NewText.Count(c => !IsValidNamingCharacter(c));
            if (invalidCharCount > 0) { args.Cancel = true; SetTipState(IdTip, isOpen: true, 0x2, "非法字符"); }
            else { args.Cancel = false; SetTipState(IdTip, isOpen: false, 0x2); }
        }

        private void BlockIDTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            isFileSaved = false;
            string id = BlockIDTextBox.Text;
            int checkResult = CheckIsNamespaceValid(id);
            if (checkResult > 1)
            {
                string title = "无效的名称空间";
                string subtitle = "";

                if (checkResult == 2)
                {
                    title = "方块ID不可为空";
                    DemoBlock.TranslationKey = "Blocks.Demo";
                }
                else if (checkResult == 3) subtitle = "\':\' 只可出现一次";
                else if (checkResult == 4) subtitle = "格式错误";

                SetTipState(IdTip, isOpen: true, 0x1, title, subtitle);
                return;
            }
            else
            {
                SetTipState(IdTip, isOpen: false, 0x1);
                if (checkResult == 0) id = $"custom:{id}";
            }

            DemoBlock.Identifier = id;
            cbd.Identifier = id;
        }

        #endregion

        #region "Methods"

        private async Task<TaskResult> ExportFileAsync()
        {
            if (hasSaveDialogShown) return new TaskResult(failed: true, "ExistsSaveDialog");

            if (invalidDataCount != 0)
            {
                if (!EditorTip.IsOpen)
                {
                    EditorTip.Title = "当前存在无效内容，暫时无法保存文件";
                    EditorTip.IsOpen = true;
                }
                return new TaskResult(failed: true, "ExistsInvalidData");
            }

            FileSavePicker savePicker = new();

            // 取得当前窗口句柄，将选择器的拥有者设为此窗口
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

            // 选择器的预设路径
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

            // 文件类型
            string fileDescription = GetLocalizedString("Misc.CBDFile");
            savePicker.FileTypeChoices.Add(fileDescription, [".cbd"]);

            // 保存数据
            cbd.Variant = (byte)DemoBlock.MetaData.Variant;
            if (cbd.BlockType == BlockType.Undefined) cbd.BlockType = BlockType.Action;
            TranslationsDictoraryEditor.GetDictionary(cbd.TranslationsDict);

            hasSaveDialogShown = true;
            StorageFile file = await savePicker.PickSaveFileAsync();
            hasSaveDialogShown = false;
            if (file != null)
            {
                // 暫停其他程序对文件的更新
                CachedFileManager.DeferUpdates(file);

                // 写入文件
                cbd.WriteToFile(file.Path);

                // 检查状态
                var state = await CachedFileManager.CompleteUpdatesAsync(file);
                if (state != FileUpdateStatus.Complete && state != FileUpdateStatus.CompleteAndRenamed)
                {
                    // Failed
                    await dialog.ShowAsync("Messages.FailedToSaveFile", DialogVariant.Confirm);
                    return new TaskResult(failed: true, "FailedToSaveFile");
                }
                else isFileSaved = true;
                return new TaskResult(failed: false);
            }

            isFileSaved = false;
            return new TaskResult(failed: true, "NullFile");
        }

        private void Scroller_BackToCenter()
        {
            var centerX = (DisplayCanvas.ActualWidth - Scroller.ActualWidth) / 2;
            var centerY = (DisplayCanvas.ActualHeight - Scroller.ActualHeight) / 2;
            Scroller.ChangeView(centerX, centerY, null, true);
        }

        private void LoadBlock()
        {
            if (activeFile == null)
            {
                DemoBlock.BlockColor = ColorHelper.FromInt(0xFFC800);
                DemoBlock.MetaData = new() { Type = BlockType.Action, Variant = 10 };
                DemoBlock.TranslationKey = "Blocks.Demo";
            }
            else
            {
                DemoBlock.BlockColor = ColorHelper.FromInt(cbd.ColorHex);
                DemoBlock.MetaData = new() { Type = cbd.BlockType, Variant = cbd.Variant };
                TranslationsDictoraryEditor.LoadDictionary(cbd.TranslationsDict);
                DemoBlock.TranslationsDict = cbd.TranslationsDict;
                DemoBlock.RefreshBlockText();

                BlockIDTextBox.Text = cbd.Identifier;
                BlockTypeComboBox.SelectedIndex = (int)cbd.BlockType;
                CurrentColor.Background = new SolidColorBrush(DemoBlock.BlockColor);

                int variant = cbd.Variant;
                LSlotCheckBox.IsChecked = variant.HasSpecificBits(0b_0001);
                TSlotCheckBox.IsChecked = variant.HasSpecificBits(0b_0010);
                RSlotCheckBox.IsChecked = variant.HasSpecificBits(0b_0100);
                BSlotCheckBox.IsChecked = variant.HasSpecificBits(0b_1000);
                isFileSaved = true;
            }

            Canvas.SetLeft(DemoBlock.BlockDescription, 24);
            Canvas.SetTop(DemoBlock.BlockDescription, 16);
            var blockX = (DisplayCanvas.ActualWidth - DemoBlock.Size.Width) / 2;
            var blockY = (DisplayCanvas.ActualHeight - DemoBlock.Size.Height) / 2;
            Canvas.SetLeft(DemoBlock, blockX);
            Canvas.SetTop(DemoBlock, blockY);
        }

        private void UpdateBlockVariant()
        {
            byte variant = 0b_0000;
            if (LSlotCheckBox.IsChecked == true) variant |= 0b_0001;
            if (TSlotCheckBox.IsChecked == true) variant |= 0b_0010;
            if (RSlotCheckBox.IsChecked == true) variant |= 0b_0100;
            if (BSlotCheckBox.IsChecked == true) variant |= 0b_1000;

            DemoBlock.SetData(BlockProperties.Variant, variant);
            cbd.Variant = variant;
        }

        private bool IsValidNamingCharacter(char c)
        {
            return
                (c >= 'a' && c <= 'z') ||
                (c >= '0' && c <= '9') ||
                (c == '.' || c == ':' || c == '-' || c == '_');
        }

        private void SetTipState(TeachingTip target, bool isOpen, uint errorCode, string title = "", string subTitle = "")
        {
            if (isOpen)
            {
                target.IsOpen = true;
                target.Title = title;
                target.Subtitle = subTitle;
                invalidDataCount |= errorCode;
            }
            else
            {
                invalidDataCount &= (~errorCode);
                if (invalidDataCount == 0) target.IsOpen = false;
            }
        }

        public static int CheckIsNamespaceValid(string str)
        {
            // return value:
            // 0  --> Success (0 colon)
            // 1  --> Success (1 colon)
            // 2  --> Empty string
            // 3  --> Exists more than one colon
            // 4  --> Invalid format

            if (string.IsNullOrEmpty(str)) return 2;

            int colonCount = 0, lastPeriodIndex = -1;
            for (int i=0; i<str.Length; i++)
            {
                if (str[i] == '.') lastPeriodIndex = i;
                else if (str[i] == ':')
                {
                    colonCount++;
                    if (colonCount > 1) return 3;
                    if (i == 0 || i == str.Length - 1) return 4;
                    if (lastPeriodIndex != -1 && lastPeriodIndex < i) return 4;
                }
            }

            return colonCount;
        }

        #endregion
    }

    public struct TaskResult
    {
        public bool Failed = false;
        public string Reason = string.Empty;
        public TaskResult(bool failed = false, string reason= "")
        {
            Failed = failed;
            if (Failed) Reason = reason;
        }
    }
}
