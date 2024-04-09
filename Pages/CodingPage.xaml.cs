using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Controls;
using CodeBlocks.Core;
using CodeBlocks.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace CodeBlocks.Pages
{
    public sealed partial class CodingPage : Page
    {
        private float zoomFactor = 1.0f;
        private bool isCanvasScrolling = false;
        private readonly MessageDialog dialog = new();
        private readonly App app = Application.Current as App;
        private string GetLocalizedString(string key) => app.Localizer.GetString(key);
        public bool Edited = false;

        public CodingPage()
        {
            InitializeComponent();
            InitializePage();
            this.Loaded += Page_Loaded;
        }

        private void InitializePage()
        {
            UICanvas.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
            UICanvas.ManipulationDelta += BlockCanvas_ManipulationDelta;

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
                var block = new HatBlock(BlockCreated);
                Canvas.SetLeft(block, 50); Canvas.SetTop(block, 50);
            };

            pButton.Click += (_, _) =>
            {
                var block = new ProcessBlock(BlockCreated);
                Canvas.SetLeft(block, 50);
                Canvas.SetTop(block, 50);
            };

            vButton.Click += (_, _) =>
            {
                var block = new ValueBlock(BlockCreated) { Size = (90, 58), ValueType = BlockValueType.Text };
                Canvas.SetLeft(block, 50);
                Canvas.SetTop(block, 50);
            };

            //ZoomIn.KeyUp += (_, _) => {app.MainWindow.Title = zoomFactor.ToString(); zoomFactor += 2f; Scroller.ZoomBy(zoomFactor, new((float)(Scroller.ActualWidth / 2), (float)(Scroller.ActualHeight / 2))); };
            //ZoomOut.KeyUp += (_, _) => { zoomFactor -= 2f; Scroller.ZoomBy(zoomFactor, new((float)(Scroller.ActualWidth / 2), (float)(Scroller.ActualHeight / 2))); };
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            dialog.XamlRoot = this.XamlRoot;

            // 默认 Scroller 水平和垂直置中
            var centerX = (BlockCanvas.ActualWidth - Scroller.ActualWidth) / 2;
            var centerY = (BlockCanvas.ActualHeight - Scroller.ActualHeight) / 2;
            Scroller.ChangeView(centerX, centerY, null, true);

            Scroller.PointerPressed += (_, _) => isCanvasScrolling = true;
            Scroller.PointerReleased += (_, _) => isCanvasScrolling = false;
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
            Scroller.Height = UICanvas.ActualHeight - 12;
        }

        private void BlockCanvas_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (isCanvasScrolling)
            {
                var newX = Scroller.HorizontalOffset - e.Delta.Translation.X;
                var newY = Scroller.VerticalOffset - e.Delta.Translation.Y;
                Scroller.ChangeView(newX, newY, null, true);
            }
        }

        private void BlockCreated(CodeBlock block, BlockCreatedEventArgs e)
        {
            BlockCanvas.Children.Add(block);
            Canvas.SetLeft(block, e.Position.X);
            Canvas.SetTop(block, e.Position.Y);

            block.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
            block.ManipulationStarted += CodeBlock_ManipulationStarted;
            block.ManipulationDelta += CodeBlock_ManipulationDelta;
            block.ManipulationCompleted += CodeBlock_ManipulationCompleted;
        }

        private void CodeBlock_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            Edited = true;

            var thisBlock = sender as CodeBlock;
            thisBlock.SetZIndex(+5, true);
            ghostBlock.CopyDataFrom(thisBlock);
            ghostBlock.Visibility = Visibility.Collapsed;

            var parentBlock = thisBlock.ParentBlock;
            if (parentBlock != null)
            {
                thisBlock.ParentBlock = null;
                if (thisBlock.DependentSlot == -1) { parentBlock.BottomBlock = null; }
                else { parentBlock.RightBlocks[thisBlock.DependentSlot-1] = null; }
            }

            thisBlock.DependentSlot = 0;
        }

        private void CodeBlock_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            var thisBlock = sender as CodeBlock;
            if (thisBlock.HasBeenRemoved) return;

            double newX = Canvas.GetLeft(thisBlock) + e.Delta.Translation.X;
            double newY = Canvas.GetTop(thisBlock) + e.Delta.Translation.Y;
            var maxX = BlockCanvas.ActualWidth - thisBlock.Size.Width;
            var maxY = BlockCanvas.ActualHeight - thisBlock.Size.Height;
            var minX = Canvas.GetLeft(BlockCanvas);
            var minY = Canvas.GetTop(BlockCanvas);

            // 边界检查
            newX = (newX < 0) ? 0 : (newX > maxX) ? maxX : newX;
            newY = (newY < 0) ? 0 : (newY > maxY) ? maxY : newY;

            // 移动方块
            thisBlock.SetPosition(newX, newY);

            // 碰撞检查
            CodeBlock_CheckCollisions(thisBlock);
        }

        private void CodeBlock_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            var thisBlock = sender as CodeBlock;
            thisBlock.SetZIndex(-5, true);

            if (ghostBlock.Visibility == Visibility.Visible)
            {
                // 移动到示意矩形的位置
                thisBlock.MoveTo(ghostBlock);
                ghostBlock.Visibility = Visibility.Collapsed;

                // 移动其他子块
                var parentBlock = thisBlock.ParentBlock;
                if (thisBlock.DependentSlot == -1) // 自己在下方
                {
                    if (parentBlock.BottomBlock == null) { parentBlock.BottomBlock = thisBlock; }
                    else
                    {
                        // 定位到队列的尾端
                        var endBlock = thisBlock;
                        while (endBlock.BottomBlock != null) endBlock = endBlock.BottomBlock;

                        var bottomBlock = parentBlock.BottomBlock;
                        parentBlock.BottomBlock = thisBlock;
                        bottomBlock.ParentBlock = endBlock;
                        endBlock.BottomBlock = bottomBlock;
                        bottomBlock.MoveTo(endBlock, 0, endBlock.Size.Height - 10);
                    }
                }
                else if (thisBlock.DependentSlot > 0) // 自己在右侧
                {
                    var right = parentBlock.RightBlocks;
                    right[thisBlock.DependentSlot-1] = thisBlock;
                }
            }
        }

        private void CodeBlock_CheckCollisions(CodeBlock thisBlock)
        {
            var self = new Point(Canvas.GetLeft(thisBlock), Canvas.GetTop(thisBlock));
            var selfW = thisBlock.Size.Width;
            var selfH = thisBlock.Size.Height;
            bool isAligned = false;

            // 碰撞检查
            foreach (var uiElement in BlockCanvas.Children)
            {
                if (uiElement == ghostBlock) continue; // Skip

                {
                    // 判断是否和垃圾桶重叠

                    // 计算中心点距离
                    var dx = Canvas.GetLeft(TrashCan) + 26 - self.X - selfW / 2;
                    var dy = Canvas.GetTop(TrashCan) + 26 - self.Y - selfH / 2;
                    var distance = Math.Sqrt(dx * dx + dy * dy);

                    // 距离小于 取较大值(宽, 高) 的四分之一 --> 方块已和垃圾桶重叠;
                    var threshold = Utils.GetBigger(selfW, selfH) * 0.25;
                    if (distance < threshold)
                    {
                        CodeBlock_RemoveAsync(thisBlock);
                        return;
                    }
                }

                if (uiElement == thisBlock) continue;
                var target = new Point(Canvas.GetLeft(uiElement), Canvas.GetTop(uiElement));

                if (uiElement is CodeBlock targetBlock)
                {
                    // 获取自身的相对方位
                    var rq = thisBlock.GetRelativeQuadrant(targetBlock);

                    // 在四个角落 不和目标相邻 --> 跳过
                    if (rq.x * rq.y != 0)
                    {
                        ghostBlock.Visibility = Visibility.Collapsed;
                        continue;
                    }

                    // 取得方块资料值
                    int selfVar = thisBlock.MetaData.Variant;
                    int targetVar = targetBlock.MetaData.Variant;

                    // 在左侧时不可吸附 --> 跳过
                    if (rq.x == -1) continue;

                    // 在上方时不可吸附 --> 跳过
                    if (rq.y == -1) continue;

                    // 在右侧时不可吸附 --> 跳过
                    if (rq.x == 1 && (!Utils.GetFlag(targetVar, 2) || !Utils.GetFlag(selfVar, 0))) continue;

                    // 在下方时不可吸附 --> 跳过
                    if (rq.y == 1 && (!Utils.GetFlag(targetVar, 3) || !Utils.GetFlag(selfVar, 1))) continue;

                    // 开始尝试自动吸附
                    int threshold = 30;
                    int slot = (int)((self.Y - target.Y) / 48);
                    if (rq.dx > 0 && (rq.dx - selfW + 10) < threshold)
                    {
                        self.X -= (rq.dx - selfW + 10) * rq.x;
                        self.Y = target.Y + slot * 48;
                        thisBlock.DependentSlot = slot+1;
                        isAligned = true;
                    }
                    else if (rq.dy > 0 && (rq.dy - selfH + 10) < threshold)
                    {
                        self.Y -= (rq.dy - selfH + 10) * rq.y;
                        self.X = target.X;
                        thisBlock.DependentSlot = -1;
                        isAligned = true;
                    }

                    if (isAligned)
                    {
                        thisBlock.ParentBlock = targetBlock;
                        ghostBlock.SetPosition(self.X, self.Y);
                        ghostBlock.Visibility = Visibility.Visible;
                        return;
                    }
                    else ghostBlock.Visibility = Visibility.Collapsed;
                }
            }
        }

        private async Task CodeBlock_RemoveAsync(CodeBlock thisBlock)
        {
            thisBlock.HasBeenRemoved = true;
            int count = thisBlock.GetRelatedBlockCount();
            if (ghostBlock.Visibility == Visibility.Visible) ghostBlock.Visibility = Visibility.Collapsed;
            if (count == 0) BlockCanvas.Children.Remove(thisBlock);
            else
            {
                var result = await dialog.ShowAsync("RemovingMultipleBlocks", DialogVariant.YesCancel);
                if (result == ContentDialogResult.Primary)
                {
                    await thisBlock.RemoveAsync(BlockCanvas);
                }
                else
                {
                    thisBlock.HasBeenRemoved = false;
                    thisBlock.SetPosition(-100, -100, true);
                }
            }
        }
    }
}
