using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;
using WinRT.Interop;
using CodeBlocks.Pages;
using CodeBlocks.Core;
using Microsoft.UI;
using Windows.Graphics;
using Microsoft.UI.Windowing;
using static ABI.System.Collections.Generic.IReadOnlyDictionary_Delegates;
using Microsoft.UI.Input;
using Windows.UI.ViewManagement;

namespace CodeBlocks
{
    public sealed partial class MainWindow : Window
    {
        private IntPtr wndHandle;
        private readonly App app = Application.Current as App;
        public NavigationViewItem Navi_Settings;

        private string GetLocalizedString(string key) => (Application.Current as App).Localizer.GetString(key);
        private void GetLocalized()
        {
            
        }
        
        public MainWindow()
        {
            InitializeComponent();
            wndHandle = WindowNative.GetWindowHandle(this);
            app.OnLanguageChanged += GetLocalized;

            // 设置窗口最小尺寸
            WindowProc.SetWndMinSize(wndHandle, 800, 600);

            // 外观
            SystemBackdrop = new MicaBackdrop();
            ExtendsContentIntoTitleBar = true;
            Tab.AddTabButtonClick += (_, _) => AddNewTab();
            AddNewTab();

            this.SizeChanged += (_, _) => UpdateDragRects();
        }

        public void UpdateDragRects()
        {
            int split = Tab.TabItems.Count * 240 + 48;
            RectInt32 left = new(0, 0, split, 24);
            RectInt32 right = new(split, 0, AppWindow.Size.Width, 48);
            AppWindow.TitleBar.SetDragRectangles([left, right]);
        }

        public void UpdateDragRects(int split)
        {
            RectInt32 right = new(split, 0, AppWindow.Size.Width, 48);
            AppWindow.TitleBar.SetDragRectangles([right]);
        }

        private void AddNewTab(string header = "")
        {
            TabViewItem item = new() { Margin = new(0,12,0,0) };
            item.Header = (string.IsNullOrEmpty(header)) ? "New Tab" : header;
            Frame frame = new();
            Action resize = () =>
            {
                frame.Width = AppWindow.Size.Width - 20;
                frame.Height = AppWindow.Size.Height - 50;
            };

            this.SizeChanged += (_, _) => resize();  resize(); // 立即调整一次大小
            frame.Navigate(typeof(CodingPage));
            item.Content = frame;

            Tab.TabItems.Add(item);
            Tab.SelectedItem = item;
            int split = (int)(item.ActualOffset.X + item.ActualWidth);
            if (split == 0) split = 240;
            UpdateDragRects();
        }
    }
}
