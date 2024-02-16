using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;
using WinRT.Interop;
using CodeBlocks.Pages;
using CodeBlocks.Core;

namespace CodeBlocks
{
    public sealed partial class MainWindow : Window
    {
        private IntPtr wndHandle;

        public MainWindow()
        {
            InitializeComponent();
            wndHandle = WindowNative.GetWindowHandle(this);

            // 设置窗口最小尺寸
            WindowProc.SetWndMinSize(wndHandle, 800, 600);

            // 外观
            SystemBackdrop = new MicaBackdrop();
            ExtendsContentIntoTitleBar = true;
            Navi.Loaded += (s, e) =>
            {
                Navi.SelectedItem = Navi.MenuItems[0];
                var settingsItem = Navi.SettingsItem as NavigationViewItem;
                settingsItem.Content = "設定";
                Navigate(typeof(HomePage));
            };
        }

        private void Navigate(Type naviPageType)
        {
            if (naviPageType == null) return;
            Type prePageType = ContentFrame.CurrentSourcePageType;
            if (naviPageType != prePageType) ContentFrame.Navigate(naviPageType);
        }

        private void Navi_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                Navigate(typeof(SettingsPage));
            }
            else if (args.SelectedItemContainer != null)
            {
                string pageName = "CodeBlocks.Pages." + args.SelectedItemContainer.Tag.ToString();
                Navigate(Type.GetType(pageName));
            }
        }
    }
}
