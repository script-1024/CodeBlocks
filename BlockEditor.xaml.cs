﻿using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Windowing;
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

            // 外观
            SystemBackdrop = new MicaBackdrop();
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
        }

        private void Window_SizeChanged(object sender, AppWindowChangedEventArgs args)
        {
            if (args.DidSizeChange)
            {
                bool needResize = false;
                var size = this.AppWindow.Size;
                if (size.Width < 800) { needResize = true; size.Width = 800; }
                if (size.Height < 600) { needResize = true; size.Height = 600; }
                if (needResize) AppWindow.Resize(size);
            }
        }

        public void Close(bool forceQuit)
        {
            if (forceQuit) this.Closed -= Window_Closed; // 取消订阅 Closed 事件
            this.Close(); // 关闭窗口。如果 forceQuit == false，则此函数的表现和无参数 Close() 方法一致
        }

        private async Task<ContentDialogResult> AskUserToCloseWindowAsync()
        {
            if (!isFileOpened || isFileSaved) return ContentDialogResult.Primary;
            else return await dialog.ShowAsync("ClosingWindow", DialogVariant.SaveGiveupCancel);
        }

        private async void Window_Closed(object sender, WindowEventArgs args)
        {
            args.Handled = true;
            var result = await AskUserToCloseWindowAsync();
            if (result != ContentDialogResult.None) { args.Handled = false; Close(true); }
        }

        private bool IsValidFile(StorageFile file)
        {
            return false;
        }
    }
}