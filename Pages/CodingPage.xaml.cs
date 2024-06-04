using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;
using CodeBlocks.Controls;
using CodeBlocks.Core;

namespace CodeBlocks.Pages
{
    public sealed partial class CodingPage : Page
    {
        private bool canCanvasScroll = false;
        private readonly MessageDialog dialog = new();
        private readonly App app = Application.Current as App;
        private readonly BlockDragger dragger;
        private string GetLocalizedString(string key) => app.Localizer.GetString(key);

        public bool Edited { get; private set; } = false;

        public CodingPage()
        {
            InitializeComponent();
            InitializePage();
            this.Loaded += Page_Loaded;
            dragger = new(BlockCanvas, Scroller, ghostBlock);
            ToolBox.BlockDragger = dragger;
            InitializeBlockDragger();
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
    }
}
