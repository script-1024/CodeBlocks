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
            InitializeBlockDragger();
        }

        private void InitializePage()
        {
            UICanvas.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
            UICanvas.ManipulationDelta += UICanvas_ManipulationDelta;

            #region "Test Only"

            var hButton = new Button() { Content = "New Hat Block" };
            var pButton = new Button() { Content = "New Process Block" };
            var vButton = new Button() { Content = "New Value Block" };
            UICanvas.Children.Add(hButton);
            UICanvas.Children.Add(pButton);
            UICanvas.Children.Add(vButton);
            Canvas.SetLeft(hButton, 10); Canvas.SetTop(hButton, 10);
            Canvas.SetLeft(pButton, 10); Canvas.SetTop(pButton, 50);
            Canvas.SetLeft(vButton, 10); Canvas.SetTop(vButton, 90);

            hButton.Click += (_, _) =>
            {
                var block = new EventBlock(dragger.BlockCreated);
                Canvas.SetLeft(block, Scroller.HorizontalOffset / Scroller.ZoomFactor + 240);
                Canvas.SetTop(block, Scroller.VerticalOffset / Scroller.ZoomFactor + 50);
            };

            pButton.Click += (_, _) =>
            {
                var block = new ActionBlock(dragger.BlockCreated);
                Canvas.SetLeft(block, Scroller.HorizontalOffset / Scroller.ZoomFactor + 240);
                Canvas.SetTop(block, Scroller.VerticalOffset / Scroller.ZoomFactor + 50);
            };

            vButton.Click += (_, _) =>
            {
                var block = new ValueBlock(dragger.BlockCreated) { Size = (90, 58), ValueType = BlockValueType.Int };
                Canvas.SetLeft(block, Scroller.HorizontalOffset / Scroller.ZoomFactor + 240);
                Canvas.SetTop(block, Scroller.VerticalOffset / Scroller.ZoomFactor + 50);
            };

            #endregion

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

            Scroller.Width = UICanvas.ActualWidth;
            if (UICanvas.ActualHeight > 12) Scroller.Height = UICanvas.ActualHeight - 12;
        }
        private void UICanvas_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (canCanvasScroll)
            {
                var newX = Scroller.HorizontalOffset - e.Delta.Translation.X;
                var newY = Scroller.VerticalOffset - e.Delta.Translation.Y;
                Scroller.ChangeView(newX, newY, null, true);
            }
        }

        #endregion

        private async void CodeBlock_RemoveAsync(CodeBlock thisBlock)
        {
            int count = thisBlock.GetRelatedBlockCount();
            if (ghostBlock.Visibility == Visibility.Visible) dragger.ResetGhostBlock();
            if (count > 0)
            {
                var result = await dialog.ShowAsync("RemovingMultipleBlocks", DialogVariant.YesCancel);
                if (result == ContentDialogResult.None) { thisBlock.SetPosition(-100, -100, true); return; }
            }
            await thisBlock.RemoveAsync(BlockCanvas);
        }
    }
}
