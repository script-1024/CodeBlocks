using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Controls;

using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.Foundation;

using CodeBlocks.Core;
using CodeBlocks.Controls;
using System.Xml.Linq;
using System.Diagnostics;

namespace CodeBlocks.Pages
{
    public sealed partial class CodingPage : Page
    {
        private bool canCanvasScroll = false;
        private readonly MessageDialog dialog = new();
        private readonly App app = Application.Current as App;
        private readonly BlockDragger dragger;
        private string GetLocalizedString(string key) => app.Localizer.GetString(key);

        public bool IsSaved { get; private set; } = true;

        public CodingPage()
        {
            InitializeComponent();
            this.Loaded += Page_Loaded;
            dragger = new(BlockCanvas, Scroller, ghostBlock);
            ToolBox.BlockDragger = dragger;
            InitializeBlockDragger();
            InitializePage();
        }

        private void InitializePage()
        {
            UICanvas.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
            UICanvas.ManipulationDelta += UICanvas_ManipulationDelta;
            ZoomIn.PointerPressed += (_, _) => ZoomChange(zoomIn: true);
            ZoomOut.PointerPressed += (_, _) => ZoomChange(zoomIn: false);
        }

        private void InitializeBlockDragger()
        {
            dragger.Parent = this;

            dragger.GetTrashCanPosition = () => new Point()
            {
                X = Canvas.GetLeft(TrashCan) + TrashCan.ActualWidth / 2,
                Y = Canvas.GetTop(TrashCan) + TrashCan.ActualHeight / 2
            };

            dragger.RemoveBlock = CodeBlock_RemoveAsync;
            dragger.FocusChanged += Dragger_FocusChanged;
        }

        #region "Events"

        private void Dragger_FocusChanged()
        {
            IsSaved = false;
            canCanvasScroll = (dragger.FocusBlock is null);
        }
        private void ZoomChange(bool zoomIn = true)
        {
            if ( zoomIn && Scroller.ZoomFactor > Scroller.MaxZoomFactor - 0.125f) return;
            if (!zoomIn && Scroller.ZoomFactor < Scroller.MinZoomFactor + 0.125f) return;

            int sign = (zoomIn) ? 1 : -1;
            float newFactor = Scroller.ZoomFactor + 0.125f * sign;
            var scale = newFactor / Scroller.ZoomFactor;

            // 缩放前窗口中心在画布上的坐标
            double centerX = Scroller.HorizontalOffset + Scroller.ViewportWidth / 2;
            double centerY = Scroller.VerticalOffset + Scroller.ViewportHeight / 2;

            // 缩放后窗口中心在画布上的坐标
            double newCenterX = centerX * scale;
            double newCenterY = centerY * scale;

            // 计算滚动视图的偏移量
            var dx = newCenterX - Scroller.ViewportWidth / 2;
            var dy = newCenterY - Scroller.ViewportHeight / 2;

            Scroller.ChangeView(dx, dy, newFactor);
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            dialog.XamlRoot = this.XamlRoot;

            // 默认 Scroller 水平和垂直置中
            var centerX = (BlockCanvas.ActualWidth - Scroller.ActualWidth) / 2;
            var centerY = (BlockCanvas.ActualHeight - Scroller.ActualHeight) / 2;
            Scroller.ChangeView(centerX, centerY, null, true);

            // 鼠标离开作用区后禁止画布能被拖动
            Scroller.PointerPressed += (_, _) => canCanvasScroll = (dragger.FocusBlock == null);
            Scroller.PointerReleased += (_, _) => canCanvasScroll = false;
        }
        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Canvas.SetLeft(ZoomIn, UICanvas.ActualWidth - 90);
            Canvas.SetTop(ZoomIn, UICanvas.ActualHeight - 230);
            Canvas.SetLeft(ZoomOut, UICanvas.ActualWidth - 90);
            Canvas.SetTop(ZoomOut, UICanvas.ActualHeight - 180);
            Canvas.SetLeft(TrashCan, UICanvas.ActualWidth - 100);
            Canvas.SetTop(TrashCan, UICanvas.ActualHeight - 100);

            ToolBox.Height = UICanvas.ActualHeight;
            Scroller.Width = UICanvas.ActualWidth;
            if (UICanvas.ActualHeight > 12) Scroller.Height = UICanvas.ActualHeight - 12;
        }
        private void UICanvas_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (!canCanvasScroll) return;
            var newX = Scroller.HorizontalOffset - e.Delta.Translation.X;
            var newY = Scroller.VerticalOffset - e.Delta.Translation.Y;
            Scroller.ChangeView(newX, newY, null, true);
        }

        #endregion

        private async void CodeBlock_RemoveAsync(CodeBlock thisBlock)
        {
            canCanvasScroll = false;
            int count = thisBlock.GetRelatedBlockCount();
            if (ghostBlock.Visibility == Visibility.Visible) dragger.ResetGhostBlock();
            if (count > 0)
            {
                var result = await dialog.ShowAsync("RemovingMultipleBlocks", DialogVariant.YesCancel);
                if (result == ContentDialogResult.None) { thisBlock.SetPosition(-100, -100, true); return; }
            }
            await thisBlock.RemoveAsync(BlockCanvas);
            canCanvasScroll = true;
        }

        public async Task<bool> ExportDatapack()
        {
            var mainPanel = new StackPanel();

            var panel_packInfo = new StackPanel()
            {
                Margin = new(4),
                Orientation = Orientation.Horizontal
            };

            var label_packName = new TextBlock()
            {
                Text = "数据包名称",
                VerticalAlignment = VerticalAlignment.Center
            };

            var txtbox_packName = new TextBox()
            {
                Margin = new(12, 0, 0, 0),
                Width = 150
            };

            var label_packformat = new TextBlock()
            {
                Text = "版本",
                Margin = new(12, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var txtbox_packformat = new TextBox()
            {
                Margin = new(12, 0, 0, 0),
                Width = 75
            };

            panel_packInfo.Children.Add(label_packName);
            panel_packInfo.Children.Add(txtbox_packName);
            panel_packInfo.Children.Add(label_packformat);
            panel_packInfo.Children.Add(txtbox_packformat);
            mainPanel.Children.Add(panel_packInfo);

            var panel_description = new StackPanel()
            {
                Margin = new(-8, 8, 0, 0),
                Orientation = Orientation.Horizontal
            };

            var label_description = new TextBlock()
            {
                Text = "数据包简介",
                Margin = new(12, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var txtbox_description = new TextBox()
            {
                Margin = new(12, 0, 0, 0),
                Width = 280
            };

            panel_description.Children.Add(label_description);
            panel_description.Children.Add(txtbox_description);
            mainPanel.Children.Add(panel_description);

            var result = await dialog.ShowAsync("ExportDatapack", mainPanel, DialogVariant.ConfirmCancel);
            if (result == ContentDialogResult.Primary)
            {
                IsSaved = (bool) await ExportFileAsync(txtbox_packName.Text, txtbox_packformat.Text, txtbox_description.Text);
                return IsSaved;
            }
            else return false;
        }

        private async Task<bool?> ExportFileAsync(string name, string format, string description)
        {
            FileSavePicker savePicker = new();

            // 取得当前窗口句柄，将选择器的拥有者设为此窗口
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(app.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

            // 选择器的预设路径
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

            // 文件类型
            string fileDescription = GetLocalizedString("Misc.MCFFile");
            savePicker.FileTypeChoices.Add(fileDescription, [".mcf"]);

            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                // 写入文件
                File.WriteAllText(file.Path, ""); // 建立新文件，或重置文件内容
                File_WriteLine(file.Path, $"#> rmdir {name}");
                File_WriteLine(file.Path, $"#> init {name} {format} {description}");
                foreach (var entryBlock in dragger.FunctionEntry)
                {
                    File_WriteLine(file.Path, entryBlock.GetCode());

                    var block = entryBlock.BottomBlock;
                    while (block != null)
                    {
                        File_WriteLine(file.Path, block.GetCode());
                        block = block.BottomBlock;
                    }
                }
                File_WriteLine(file.Path, $"#> close");

                // 成功
                return IsSaved = true;
            }

            // 操作被用户取消
            return false;
        }

        private void File_WriteLine(string path, string line)
        {
            line += Environment.NewLine;
            File.AppendAllText(path, line);
        }
    }
}
