using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;
using CodeBlocks.Core;
using CodeBlocks.Controls;
using WinRT;
using System.Data.SqlTypes;

namespace CodeBlocks.Pages
{
    public sealed partial class CodingPage : Page
    {
        private bool isInitialized = false;
        private CheckBox checkbox;

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
            Canvas.SetLeft(labelWidth, 10); Canvas.SetTop(labelWidth, 60);
            Canvas.SetLeft(labelHeight, 10); Canvas.SetTop(labelHeight, 100);
            Canvas.SetLeft(labelVariant, 10); Canvas.SetTop(labelVariant, 140);
            Canvas.SetLeft(labelSlots, 10); Canvas.SetTop(labelSlots, 180);
            Canvas.SetLeft(checkbox, 10); Canvas.SetTop(checkbox, 220);

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
            thisBlock.ManipulationMode = ManipulationModes.All;
            thisBlock.ManipulationStarted += CodeBlock_ManipulationStarted;
            thisBlock.ManipulationDelta += CodeBlock_ManipulationDelta;
            thisBlock.ManipulationCompleted += CodeBlock_ManipulationCompleted;
        }

        public CodingPage()
        {
            InitializeComponent();
            if (!isInitialized) InitializePage();
        }

        double offsetX; double offsetY;
        private void CodeBlock_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            var thisBlock = sender as CodeBlock;
            ghostBlock.CopyFrom(thisBlock);
            ghostBlock.Visibility = Visibility.Collapsed;

            // 允许拖拽
            e.Handled = true;
            offsetX = e.Position.X; offsetY = e.Position.Y;
        }

        private void CodeBlock_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            var thisBlock = sender as CodeBlock;
            double newX = Canvas.GetLeft(thisBlock) + e.Delta.Translation.X;
            double newY = Canvas.GetTop(thisBlock) + e.Delta.Translation.Y;

            // 移动方块
            //thisBlock.TrySetPosition(newX, newY);
            Canvas.SetLeft(thisBlock, newX);
            Canvas.SetTop(thisBlock, newY);

            // 碰撞检查
            if (checkbox.IsChecked == true) CodeBlock_CheckCollisions(thisBlock, e);
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

        private void CodeBlock_CheckCollisions(CodeBlock thisBlock, ManipulationDeltaRoutedEventArgs e)
        {
            // 碰撞检查
            foreach (var uiElement in RootCanvas.Children)
            {
                if (uiElement == thisBlock || uiElement == ghostBlock || uiElement == TrashCan) continue; // Skip
                Point self = new Point(Canvas.GetLeft(thisBlock), Canvas.GetTop(thisBlock));
                Point target = new Point(Canvas.GetLeft(uiElement), Canvas.GetTop(uiElement));
                var dx = target.X - self.X;
                var dy = target.Y - self.Y;
                double distance = Math.Sqrt(dx*dx + dy*dy);

                
                var targetX = Canvas.GetLeft(uiElement);
                var targetY = Canvas.GetTop(uiElement);
                var selfW = thisBlock.Size.Width;
                var selfH = thisBlock.Size.Height;

                if (uiElement == TrashCan) // 删除块
                {
                    //var dx = Math.Abs((targetX + 30) - (selfX + selfW / 2));
                    //var dy = Math.Abs((targetY + 30) - (selfY + selfH / 2));
                    if (distance < 50)
                    {
                        CodeBlock_Remove(thisBlock);
                        return;
                    }
                }
                
                if (uiElement is CodeBlock otherBlock)
                {
                    var targetW = otherBlock.Size.Width;
                    var targetH = otherBlock.Size.Height;
                    dx = target.X - self.X;
                    dy = target.Y - self.Y;
                    if (dx < 0) dx += targetW;
                    if (dy < 0) dy += targetH;

                    // 检查是否需要自动吸附
                    double delta = 20; // 自动吸附距离阈值
                    if (dx < selfW + delta && dy < selfH + delta)
                    {
                        // 设置示意矩形
                        double rectX = self.X; double rectY = self.Y;
                        if (self.X >= targetX - delta && self.X < targetX + delta)
                        {
                            // 水平对齐:中央
                            rectX = targetX;
                            if (self.Y < targetY + targetH / 2)
                            { rectY = targetY - selfH + 12; }   // 垂直对齐:上方
                            else
                            { rectY = targetY + targetH - 12; } // 垂直对齐:下方
                        }
                        else if (self.X < targetX && self.Y >= targetY - delta && self.Y < targetY + delta)
                        {
                            // 水平对齐:左侧, 垂直对齐:中央
                            rectX = targetX - selfW + 12;
                            rectY = targetY;
                        }
                        else if (self.X >= targetX + targetW && self.Y >= targetY - delta && self.Y < targetY + delta)
                        {
                            // 水平对齐:右侧, 垂直对齐:中央
                            rectX = targetX + targetW - 12;
                            rectY = targetY;
                        }
                        else continue;

                        Canvas.SetLeft(ghostBlock, rectX);
                        Canvas.SetTop(ghostBlock, rectY);
                        ghostBlock.Visibility = Visibility.Visible;
                        break;
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
