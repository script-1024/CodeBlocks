using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;
using CodeBlocks.Core;
using Windows.UI;
using System;

namespace CodeBlocks.Controls
{
    public class ValueBlock : CodeBlock
    {
        private BlockValueType type = BlockValueType.None;
        private readonly App app = Application.Current as App;
        private string GetLocalizedString(string key) => app.Localizer.GetString(key);

        public object Value
        {
            get => txtbox.Text;
            set => txtbox.Text = value.ToString();
        }

        public BlockValueType ValueType
        {
            get => type;
            set { type = value; OnTypeChanged(); }
        }

        private readonly TextBlock t1 = new() { Text = "“ " }, t2 = new() { Text = " ”" };
        private readonly TextBox txtbox = new() { VerticalContentAlignment = VerticalAlignment.Center, BorderBrush = new SolidColorBrush(Colors.Transparent), CornerRadius = new(3) };

        // 覆写预设行为，用于修复方块被复制后未正确初始化的错误。
        public override event BlockCreatedEventHandler OnBlockCreated;

        public ValueBlock(BlockCreatedEventHandler handler, BlockCreatedEventArgs args = null) : base(null, args)
        {
            MetaData = new() { Type = BlockType.Value, Variant = 1, Size = this.Size };
            t1.Foreground = t2.Foreground = new SolidColorBrush(Colors.White);
            t1.FontFamily = t2.FontFamily = txtbox.FontFamily = CodeBlock.FontFamily;
            t1.FontSize = t2.FontSize = CodeBlock.FontSize;
            var c = this.Content as Canvas;
            c.Children.Add(t1); c.Children.Add(txtbox); c.Children.Add(t2);
            Canvas.SetTop(t1, 8); Canvas.SetTop(txtbox, 8); Canvas.SetTop(t2, 8);

            txtbox.SizeChanged += (_, _) => ResizeTextBox();
            txtbox.KeyDown += (_, e) =>
            {
                if (e.Key == Windows.System.VirtualKey.Enter) (this.Parent as Canvas).Focus(FocusState.Pointer);
                ResizeTextBox();
            };

            app.OnLanguageChanged += () =>
            {
                string key = $"Blocks.ValueBlock.{(type.CheckIfContain(BlockValueType.String) ? "Text" : "Number")}.PlaceholderText";
                txtbox.PlaceholderText = GetLocalizedString(key);
            };

            OnBlockCreated += handler;
            OnBlockCreated?.Invoke(this, args);
        }

        public ValueBlock() : this(null, null) { }

        private void OnTypeChanged()
        {
            if (type.CheckIfContain(BlockValueType.String))
            {
                t1.Visibility = t2.Visibility = Visibility.Visible;
                BlockColor = (app.Resources["ValueBlockTextColorBrush"] as SolidColorBrush).Color;
                Canvas.SetLeft(txtbox, 30);
                txtbox.PlaceholderText = GetLocalizedString("Blocks.ValueBlock.Text.PlaceholderText");

                txtbox.TextChanged -= CheckIllegalCharacter;
            }
            if (type.CheckIfContain(BlockValueType.Number))
            {
                t1.Visibility = t2.Visibility = Visibility.Collapsed;
                BlockColor = (app.Resources["ValueBlockNumberColorBrush"] as SolidColorBrush).Color;
                Canvas.SetLeft(txtbox, 12);
                txtbox.PlaceholderText = GetLocalizedString("Blocks.ValueBlock.Number.PlaceholderText");

                txtbox.TextChanged += CheckIllegalCharacter;
            }
        }

        private void CheckIllegalCharacter(object sender, TextChangedEventArgs e)
        {
            if (type.CheckIfContain(BlockValueType.Decimal) && double.TryParse(txtbox.Text, out _))
            {
                if (BlockTip.IsOpen) BlockTip.IsOpen = false;
            }
            else if (type.CheckIfContain(BlockValueType.Integer) && int.TryParse(txtbox.Text, out _))
            {
                if (BlockTip.IsOpen) BlockTip.IsOpen = false;
            }
            else
            {
                BlockTip.Title = "非法字元";
                BlockTip.Subtitle = "输入的内容无法转成指定类型";
                if (! BlockTip.IsOpen) BlockTip.IsOpen = true;
            }
        }

        private void ResizeTextBox()
        {
            int w = (int)txtbox.ActualWidth;
            if (type.CheckIfContain(BlockValueType.String)) w += 58;
            if (type.CheckIfContain(BlockValueType.Number)) w += 30;
            SetData(BlockProperties.Width, w);
            double x = Canvas.GetLeft(txtbox);
            Canvas.SetLeft(t1, x-14);
            Canvas.SetLeft(t2, x+w-58);
        }

        public override CodeBlock Copy(BlockCreatedEventArgs args)
        {
            var block = new ValueBlock(this.OnBlockCreated, args)
            {
                Value = this.Value,
                MetaData = this.MetaData,
                ValueType = this.ValueType
            };

            for (int i = 0; i < this.RightBlocks.Length; i++)
            {
                var thisBlock = this.RightBlocks[i];
                if (thisBlock is null) block.RightBlocks[i] = null;
                else
                {
                    var left = Canvas.GetLeft(thisBlock) + 30;
                    var top = Canvas.GetTop(thisBlock) + 30;
                    var newArgs = new BlockCreatedEventArgs(left, top, thisBlock);
                    block.RightBlocks[i] = thisBlock.Copy(newArgs);
                }
            }

            return block;
        }

        public override void RefreshBlockText()
        {
            return;
        }
    }
}
