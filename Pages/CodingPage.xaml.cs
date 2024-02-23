using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;
using CodeBlocks.Core;
using CodeBlocks.Controls;
using System.Threading.Tasks.Dataflow;
using System.Threading.Tasks;

namespace CodeBlocks.Pages
{
    public sealed partial class CodingPage : Page
    {
        private bool isInitialized = false;
        private CheckBox checkbox;
        TextBlock labelTestResult = new() { Text = "[计算结果]\nunknown" };

        public CodingPage()
        {
            InitializeComponent();
            if (!isInitialized) InitializePage();
        }

        private void InitializePage()
        {
            isInitialized = true;
            var button = new Button() { Content = "New Block" };
            RootCanvas.Children.Add(button);
            Canvas.SetLeft(button, 10); Canvas.SetTop(button, 10);

            var labelWidth = new TextBlock() { Text = "Width: " };
            var labelHeight = new TextBlock() { Text = "Height: " };
            var labelVariant = new TextBlock() { Text = "Variant: " };
            var labelSlots = new TextBlock() { Text = "Slots: " };
            checkbox = new CheckBox() { Content = "碰撞检查", IsChecked = true };
            RootCanvas.Children.Add(labelWidth);
            RootCanvas.Children.Add(labelHeight);
            RootCanvas.Children.Add(labelVariant);
            RootCanvas.Children.Add(labelSlots);
            RootCanvas.Children.Add(checkbox);
            RootCanvas.Children.Add(labelTestResult);
            Canvas.SetLeft(labelWidth, 10); Canvas.SetTop(labelWidth, 60);
            Canvas.SetLeft(labelHeight, 10); Canvas.SetTop(labelHeight, 100);
            Canvas.SetLeft(labelVariant, 10); Canvas.SetTop(labelVariant, 140);
            Canvas.SetLeft(labelSlots, 10); Canvas.SetTop(labelSlots, 180);
            Canvas.SetLeft(checkbox, 10); Canvas.SetTop(checkbox, 220);
            Canvas.SetLeft(labelTestResult, 10); Canvas.SetTop(labelTestResult, 260);

            var inputWidth = new TextBox() { Width = 60, Text = "200" };
            var inputHeight = new TextBox() { Width = 60, Text = "66" };
            var inputVariant = new TextBox() { Width = 60, Text = "315" };
            var inputSlots = new TextBox() { Width = 60, Text = "1" };
            RootCanvas.Children.Add(inputWidth);
            RootCanvas.Children.Add(inputHeight);
            RootCanvas.Children.Add(inputVariant);
            RootCanvas.Children.Add(inputSlots);
            Canvas.SetLeft(inputWidth, 75); Canvas.SetTop(inputWidth, 55);
            Canvas.SetLeft(inputHeight, 75); Canvas.SetTop(inputHeight, 95);
            Canvas.SetLeft(inputVariant, 75); Canvas.SetTop(inputVariant, 135);
            Canvas.SetLeft(inputSlots, 75); Canvas.SetTop(inputSlots, 175);

            var colorPicker = new ColorPicker() { ColorSpectrumShape=ColorSpectrumShape.Ring, IsMoreButtonVisible=true };
            RootCanvas.Children.Add(colorPicker);
            Canvas.SetLeft(colorPicker, 600); Canvas.SetTop(colorPicker, 10);

            button.Click += (_, _) =>
            {
                var w = int.Parse(inputWidth.Text);
                var h = int.Parse(inputHeight.Text);
                var v = int.Parse(inputVariant.Text) % 100;
                var t = int.Parse(inputVariant.Text) / 100;
                var s = int.Parse(inputSlots.Text);
                var block = new CodeBlock()
                {
                    BlockColor = colorPicker.Color,
                    MetaData = new() { Type = (BlockType)t, Size = (w, h), Variant = v, Slots = s}
                };

                RootCanvas.Children.Add(block);
                CodeBlock_AddManipulationEvents(block);
            };
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Canvas.SetLeft(TrashCan, RootCanvas.ActualWidth - 120);
            Canvas.SetTop(TrashCan, RootCanvas.ActualHeight - 120);
        }

        private void CodeBlock_AddManipulationEvents(CodeBlock thisBlock)
        {
            thisBlock.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
            thisBlock.ManipulationStarted += CodeBlock_ManipulationStarted;
            thisBlock.ManipulationDelta += CodeBlock_ManipulationDelta;
            thisBlock.ManipulationCompleted += CodeBlock_ManipulationCompleted;
        }

        private void CodeBlock_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            var thisBlock = sender as CodeBlock;
            Canvas.SetZIndex(thisBlock, Canvas.GetZIndex(thisBlock) + 5);
            ghostBlock.CopyFrom(thisBlock);
            ghostBlock.Visibility = Visibility.Collapsed;

            var parentBlock = thisBlock.ParentBlock;
            if (parentBlock != null)
            {
                thisBlock.ParentBlock = null;
                if (parentBlock.RelatedBlocks.Bottom == thisBlock)
                {
                    parentBlock.RelatedBlocks.Bottom = null;
                }
                else
                {
                    parentBlock.RelatedBlocks.Right.Remove(thisBlock);
                }
            }
        }

        private void CodeBlock_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            var thisBlock = sender as CodeBlock;
            if (thisBlock.HasBeenRemoved) return;

            double newX = Canvas.GetLeft(thisBlock) + e.Delta.Translation.X;
            double newY = Canvas.GetTop(thisBlock) + e.Delta.Translation.Y;
            var maxX = RootCanvas.ActualWidth - thisBlock.Size.Width;
            var maxY = RootCanvas.ActualHeight - thisBlock.Size.Height;

            // 边界检查
            newX = (newX < 0) ? 0 : (newX > maxX) ? maxX : newX;
            newY = (newY < 0) ? 0 : (newY > maxY) ? maxY : newY;

            // 移动方块
            thisBlock.SetPosition(newX, newY);

            // 碰撞检查
            if (checkbox.IsChecked == true) CodeBlock_CheckCollisions(thisBlock);
        }

        private void CodeBlock_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            var thisBlock = sender as CodeBlock;
            Canvas.SetZIndex(thisBlock, Canvas.GetZIndex(thisBlock) - 5);

            if (ghostBlock.Visibility == Visibility.Visible)
            {
                // 移动到示意矩形的位置
                thisBlock.MoveTo(ghostBlock);
                ghostBlock.Visibility = Visibility.Collapsed;

                // 移动其他子块
                var parentBlock = thisBlock.ParentBlock;
                var rq = thisBlock.GetRelativeQuadrant(parentBlock);
                if (rq.y == 1) // 自己在下方
                {
                    if (parentBlock.RelatedBlocks.Bottom == null)
                    {
                        parentBlock.RelatedBlocks.Bottom = thisBlock;
                    }
                    else
                    {
                        // 定位到队列的尾端
                        var endBlock = thisBlock.RelatedBlocks.Bottom ?? thisBlock;
                        while (endBlock.RelatedBlocks.Bottom != null) endBlock = endBlock.RelatedBlocks.Bottom;
                        
                        var bottomBlock = parentBlock.RelatedBlocks.Bottom;
                        parentBlock.RelatedBlocks.Bottom = thisBlock;
                        bottomBlock.ParentBlock = endBlock;
                        endBlock.RelatedBlocks.Bottom = bottomBlock;
                        bottomBlock.MoveTo(endBlock, 0, endBlock.Size.Height - 12);
                    }
                }
                else if (rq.x == 1) // 自己在右侧
                {
                    var right = parentBlock.RelatedBlocks.Right;
                    right.Add(thisBlock);
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
            foreach (var uiElement in RootCanvas.Children)
            {
                if (uiElement == thisBlock || uiElement == ghostBlock) continue; // Skip
                var target = new Point(Canvas.GetLeft(uiElement), Canvas.GetTop(uiElement));

                if (uiElement == TrashCan)
                {
                    // 计算中心点距离
                    var dx = target.X + 30 - self.X - selfW / 2;
                    var dy = target.Y + 30 - self.Y - selfH / 2;
                    var distance = Math.Sqrt(dx * dx + dy * dy);

                    // 距离小于 取较大值(宽, 高) 的四分之一 --> 方块已和垃圾桶重叠;
                    var threshold = Utils.GetBigger(selfW, selfH) * 0.25;
                    if (distance < threshold)
                    {
                        CodeBlock_RemoveAsync(thisBlock);
                        return;
                    }
                }

                if (uiElement is CodeBlock targetBlock)
                {
                    // 不要吸附自己的子块
                    if (thisBlock.IsRelatedBlock(targetBlock)) continue;

                    // 获取自身的相对方位
                    var rq = thisBlock.GetRelativeQuadrant(targetBlock);

                    labelTestResult.Text =
                        $"[相对方位]\n" +
                        $"({rq.x}, {rq.y})\n\n" +
                        $"[相同边距离]\n" +
                        $"dx: {rq.dx}\n" +
                        $"dy: {rq.dy}\n\n" +
                        $"[间隔]\n" +
                        "水平: " + ((rq.dx == 0) ? 0 : rq.dx - selfW + 12) + "\n" +
                        "垂直: " + ((rq.dy == 0) ? 0 : rq.dy - selfH + 12);

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
                    if (rq.x == -1/* && (!Utils.GetFlag(targetVar, 0) || !Utils.GetFlag(selfVar, 2))*/) continue;

                    // 在上方时不可吸附 --> 跳过
                    if (rq.y == -1/* && (!Utils.GetFlag(targetVar, 1) || !Utils.GetFlag(selfVar, 3))*/) continue;

                    // 在右侧时不可吸附 --> 跳过
                    if (rq.x == 1 && (!Utils.GetFlag(targetVar, 2) || !Utils.GetFlag(selfVar, 0))) continue;

                    // 在下方时不可吸附 --> 跳过
                    if (rq.y == 1 && (!Utils.GetFlag(targetVar, 3) || !Utils.GetFlag(selfVar, 1))) continue;

                    // 开始尝试自动吸附
                    int threshold = 30;
                    if (rq.dx > 0 && (rq.dx - selfW + 12) < threshold)
                    {
                        self.X -= (rq.dx - selfW + 12) * rq.x;
                        self.Y = target.Y;
                        isAligned = true;
                    }
                    else if (rq.dy > 0 && (rq.dy - selfH + 12) < threshold)
                    {
                        self.Y -= (rq.dy - selfH + 12) * rq.y;
                        self.X = target.X;
                        isAligned = true;
                    }

                    if (isAligned)
                    {
                        thisBlock.ParentBlock = targetBlock;
                        ghostBlock.SetPosition(self.X, self.Y);
                        ghostBlock.Visibility = Visibility.Visible;
                        if (rq.x == 1)
                        {
                            var right = targetBlock.RelatedBlocks.Right;
                            ghostBlock.SetPosition(0, 54 * right.Count, true);
                        }
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
            if (count == 0) RootCanvas.Children.Remove(thisBlock);
            else
            {
                var dialog = new ContentDialog()
                {
                    Title = "移除确认",
                    Content = "即将移除多个程式块，是否继续？",
                    CloseButtonText = "取消",
                    PrimaryButtonText = "确认",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.XamlRoot
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    await thisBlock.RemoveAsync(RootCanvas);
                }
                else
                {
                    thisBlock.HasBeenRemoved = false;
                    thisBlock.SetPosition(-60, -60, true);
                }
            }
        }
    }
}
