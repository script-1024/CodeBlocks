using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Windowing;
using Windows.UI;
using Windows.Storage;
using CodeBlocks.Core;

namespace CodeBlocks
{
    public sealed partial class BlockEditor : Window
    {
        private bool isFileSaved = false;
        private bool isFileOpened = false;
        private StorageFile activeFile = null;
        private readonly MessageDialog dialog = new();
        private readonly App app = Application.Current as App;

        private string GetLocalizedString(string key) => app.Localizer.GetString(key);

        public BlockEditor()
        {
            InitializeComponent();
            this.Closed += Window_Closed;
            RootGrid.Loaded += RootGrid_Loaded;

            // 限制窗口尺寸
            this.AppWindow.Changed += Window_SizeChanged;
            AppWindow.Resize(new(750, 750));

            // 外观
            this.SystemBackdrop = new MicaBackdrop();
            this.ExtendsContentIntoTitleBar = true;
            var fe = this.Content as FrameworkElement;
            app.OnThemeChanged += () => fe.RequestedTheme = (ElementTheme)App.CurrentTheme;
            if (fe.RequestedTheme != (ElementTheme)App.CurrentTheme)
                fe.RequestedTheme = (ElementTheme)App.CurrentTheme;

            // 本地化翻译
            app.OnLanguageChanged += GetLocalize;
            GetLocalize();
        }

        private void GetLocalize()
        {
           TitleBar_Name.Text = GetLocalizedString("BlockEditor.Title");
        }

        public BlockEditor(StorageFile file) : this()
        {
            // 读取档案
            activeFile = file;
        }

        private void RootGrid_Loaded(object sender, RoutedEventArgs e)
        {
            dialog.XamlRoot = RootGrid.XamlRoot;
            if (activeFile != null)
            {
                if (IsValidFile(activeFile))
                {
                    isFileOpened = true;
                }
                else
                {
                    activeFile = null;
                    _ = dialog.ShowAsync("InvalidFile");
                }
            }

            DisplayNewBlock();
        }

        private void Window_SizeChanged(object sender, AppWindowChangedEventArgs args)
        {
            if (args.DidSizeChange)
            {
                bool needResize = false;
                var size = this.AppWindow.Size;
                if (size.Width < 750) { needResize = true; size.Width = 750; }
                if (size.Height < 750) { needResize = true; size.Height = 750; }
                if (needResize) AppWindow.Resize(size);

                ResetBlockPosition();
            }
        }

        public void Close(bool forceQuit)
        {
            if (forceQuit) this.Closed -= Window_Closed; // 取消订阅 Closed 事件
            app.OnLanguageChanged -= GetLocalize; // 取消订阅翻译事件
            this.Close(); // 关闭窗口。如果 forceQuit == false，则此函数的表现和无参数 Close() 方法一致
        }

        private async Task<ContentDialogResult> FileNotSavedDialogShowAsync()
        {
            if (!isFileOpened || isFileSaved) return ContentDialogResult.Primary;
            else return await dialog.ShowAsync("WindowClosing", DialogVariant.SaveGiveupCancel);
        }

        private async void Window_Closed(object sender, WindowEventArgs args)
        {
            args.Handled = true;
            var result = await FileNotSavedDialogShowAsync();
            if (result != ContentDialogResult.None) { args.Handled = false; Close(true); }
        }

        private bool IsValidFile(StorageFile file)
        {
            // 检查文件是否可用、格式是否正确
            return false; // 佔位符
        }

        private void DisplayNewBlock()
        {
            if (activeFile == null)
            {
                DemoBlock.BlockColor = Color.FromArgb(0xFF, 0xFF, 0xC8, 0x00);
                DemoBlock.MetaData = new() { Type = BlockType.ProcessBlock, Variant = 10 };
                DemoBlock.TranslationKey = "Blocks.Demo";
                Canvas.SetLeft(DemoBlock.BlockDescription, 24);
                Canvas.SetTop(DemoBlock.BlockDescription, 16);
                ResetBlockPosition();
            }
        }

        private void ResetBlockPosition()
        {
            var blockX = (DisplayCanvas.ActualWidth - DemoBlock.Size.Width) / 2;
            var blockY = (DisplayCanvas.ActualHeight - DemoBlock.Size.Height) / 2;
            Canvas.SetLeft(DemoBlock, blockX);
            Canvas.SetTop(DemoBlock, blockY);
        }
    }
}
