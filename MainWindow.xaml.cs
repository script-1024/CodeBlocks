using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Windowing;
using CodeBlocks.Pages;
using CodeBlocks.Core;
using System.Diagnostics;

namespace CodeBlocks
{
    public sealed partial class MainWindow : Window
    {
        private bool isFileSaved = false;
        private bool isFileOpened = false;
        private readonly MessageDialog dialog = new();
        private readonly App app = Application.Current as App;

        public MainWindow()
        {
            InitializeComponent();
            this.Closed += Window_Closed;

            // 限制窗口最小尺寸
            this.AppWindow.Changed += Window_SizeChanged;

            // 外观
            SystemBackdrop = new MicaBackdrop();
            ExtendsContentIntoTitleBar = true;
            var fe = this.Content as FrameworkElement;
            app.ThemeChanged += () => fe.RequestedTheme = (ElementTheme)app.CurrentThemeId;
            if (fe.RequestedTheme != (ElementTheme)app.CurrentThemeId)
                fe.RequestedTheme = (ElementTheme)app.CurrentThemeId;

            Tab.Loaded += (_, _) => dialog.XamlRoot = Tab.XamlRoot;

            this.SizeChanged += (_, _) => UpdateDragRects();
            Tab.AddTabButtonClick += (_, _) => AddNewTab(typeof(CodingPage));
            AddNewTab(typeof(CodingPage));
        }

        private void Window_SizeChanged(object sender, AppWindowChangedEventArgs args)
        {
            if (args.DidSizeChange)
            {
                bool needResize = false;
                var size = this.AppWindow.Size;
                if (size.Width < 600) { needResize = true; size.Width = 600; }
                if (size.Height < 600) { needResize = true; size.Height = 600; }
                if (needResize) AppWindow.Resize(size);
            }
        }

        public void UpdateDragRects()
        {
            const int iconSize = 54; // 应用图示尺寸，同时也是标题栏的高度

            // 标签页宽度 240, 上方间距(可拖动) 24, 右侧 '添加按钮' 宽度 40
            int split = Tab.TabItems.Count * 240 + iconSize + 40;
            var icon = new Windows.Graphics.RectInt32(0, 0, iconSize, iconSize);
            var tab = new Windows.Graphics.RectInt32(0, 0, split, 24);
            var footer = new Windows.Graphics.RectInt32(split, 0, AppWindow.Size.Width, iconSize);
            AppWindow.TitleBar.SetDragRectangles([icon, tab, footer]);
        }

        public void UpdateDragRects(int split)
        {
            var right = new Windows.Graphics.RectInt32(split, 0, AppWindow.Size.Width, 54);
            AppWindow.TitleBar.SetDragRectangles([right]);
        }

        public void Close(bool forceQuit)
        {
            if (forceQuit) this.Closed -= Window_Closed; // 取消订阅 Closed 事件
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
            foreach (var obj in Tab.TabItems)
            {
                if (obj is TabViewItem item) await TabItem_CloseRequested(item, args);
            }
        }

        private void AddNewTab(Type page, string header = "")
        {
            TabViewItem item = new() { Margin = new(0,12,0,0) };
            item.Header = (string.IsNullOrEmpty(header)) ? "New Tab" : header;
            Frame frame = new();
            Action resize = () =>
            {
                frame.Width = AppWindow.Size.Width - 20;
                frame.Height = AppWindow.Size.Height - 50;
            };

            this.SizeChanged += (_, _) => resize(); resize(); // 立即调整一次大小
            frame.Navigate(page);
            item.Content = frame;

            Tab.TabItems.Add(item);
            Tab.SelectedItem = item;
            int split = (int)(item.ActualOffset.X + item.ActualWidth);
            if (split == 0) split = 240;
            UpdateDragRects();

            item.CloseRequested += async (s, _) => await TabItem_CloseRequested(s);
        }

        private async Task TabItem_CloseRequested(TabViewItem sender, WindowEventArgs args = null)
        {
            var frame = sender.Content as Frame;
            var page = frame.Content as CodingPage;

            if (!page.IsSaved)
            {
                var result = await dialog.ShowAsync("WindowClosing", DialogVariant.SaveGiveupCancel);
                if (result == ContentDialogResult.None) return;
                else if (result == ContentDialogResult.Primary)
                {
                    var isSaved = await page.ExportDatapack();
                    if (!isSaved) return; // 用户取消操作 或 保存文件时出错
                }
            }

            Tab.TabItems.Remove(sender);
            if (Tab.TabItems.Count == 0) { args.Handled = false; Close(true); }
            else UpdateDragRects();
        }
    }
}
