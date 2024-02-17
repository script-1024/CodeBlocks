using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;
using CodeBlocks.Core;
using CodeBlocks.Controls;

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
            var inputVariant = new TextBox() { Width = 60, Text = "15" };
            var inputSlots = new TextBox() { Width = 60, Text = "1" };
            RootCanvas.Children.Add(inputWidth);
            RootCanvas.Children.Add(inputHeight);
            RootCanvas.Children.Add(inputVariant);
            RootCanvas.Children.Add(inputSlots);
            Canvas.SetLeft(inputWidth, 75); Canvas.SetTop(inputWidth, 55);
            Canvas.SetLeft(inputHeight, 75); Canvas.SetTop(inputHeight, 95);
            Canvas.SetLeft(inputVariant, 75); Canvas.SetTop(inputVariant, 135);
            Canvas.SetLeft(inputSlots, 75); Canvas.SetTop(inputSlots, 175);

            button.Click += (_, _) =>
            {
                var w = int.Parse(inputWidth.Text);
                var h = int.Parse(inputHeight.Text);
                var v = int.Parse(inputVariant.Text);
                var s = int.Parse(inputSlots.Text);
                var block = new CodeBlock()
                {
                    BlockColor = ColorHelper.ToWindowsUIColor(System.Drawing.Color.Orange),
                    MetaData = new() { Type = BlockType.StackBlock, Size = (w, h), Variant = v, Slots = s}
                };

                RootCanvas.Children.Add(block);
                CodeBlock_AddManipulationEvents(block);
            };
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
            ghostBlock.CopyFrom(thisBlock);
            ghostBlock.Visibility = Visibility.Collapsed;
        }

        private void CodeBlock_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            var thisBlock = sender as CodeBlock;
            double newX = Canvas.GetLeft(thisBlock) + e.Delta.Translation.X;
            double newY = Canvas.GetTop(thisBlock) + e.Delta.Translation.Y;
            var maxX = RootCanvas.ActualWidth - thisBlock.Size.Width;
            var maxY = RootCanvas.ActualHeight - thisBlock.Size.Height;

            // 边界检查
            newX = (newX < 0) ? 0 : (newX > maxX) ? maxX : newX;
            newY = (newY < 0) ? 0 : (newY > maxY) ? maxY : newY;

            // 移动方块
            Canvas.SetLeft(thisBlock, newX);
            Canvas.SetTop(thisBlock, newY);

            // 碰撞检查
            if (checkbox.IsChecked == true) CodeBlock_CheckCollisions(thisBlock);
        }

        private void CodeBlock_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            var thisBlock = sender as CodeBlock;
            if (ghostBlock.Visibility == Visibility.Visible)
            {
                thisBlock.MoveTo(ghostBlock);
                ghostBlock.Visibility = Visibility.Collapsed;
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
                        CodeBlock_Remove(thisBlock);
                        return;
                    }
                }

                if (uiElement is CodeBlock targetBlock)
                {
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
                    if (rq.x == -1 && (!Utils.GetFlag(targetVar, 0) || !Utils.GetFlag(selfVar, 2))) continue;

                    // 在上方时不可吸附 --> 跳过
                    if (rq.y == -1 && (!Utils.GetFlag(targetVar, 1) || !Utils.GetFlag(selfVar, 3))) continue;

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
                        Canvas.SetLeft(ghostBlock, self.X);
                        Canvas.SetTop(ghostBlock, self.Y);
                        ghostBlock.Visibility = Visibility.Visible;
                        return;
                    }
                    else ghostBlock.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Canvas.SetLeft(TrashCan, RootCanvas.ActualWidth - 120);
            Canvas.SetTop(TrashCan, RootCanvas.ActualHeight - 120);
        }

        private void CodeBlock_Remove(CodeBlock thisBlock)
        {
            if (ghostBlock.Visibility == Visibility.Visible) ghostBlock.Visibility = Visibility.Collapsed;
            RootCanvas.Children.Remove(thisBlock);
        }
    }
}
