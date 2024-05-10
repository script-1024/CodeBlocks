using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;
using CodeBlocks.Core;
using Windows.UI;

namespace CodeBlocks.Controls
{
    public enum BlockValueType { None = 0, Text = 1, Number = 2, Boolean = 3 }

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

        public override event BlockCreatedEventHandler OnBlockCreated;

        public ValueBlock(BlockCreatedEventHandler handler, BlockCreatedEventArgs args = null) : base(null, args)
        {
            MetaData = new() { Type = BlockType.ValueBlock, Variant = 1, Size = this.Size };
            t1.Foreground = t2.Foreground = BlockDescription.Foreground;
            t1.FontFamily = t2.FontFamily = txtbox.FontFamily = BlockDescription.FontFamily;
            t1.FontSize = t2.FontSize = BlockDescription.FontSize;
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
                string key = $"Blocks.ValueBlock.{((type == BlockValueType.Text) ? "Text" : "Number")}.PlaceholderText";
                txtbox.PlaceholderText = GetLocalizedString(key);
            };

            OnBlockCreated += handler;
            OnBlockCreated?.Invoke(this, args);
        }

        public ValueBlock() : this(null, null) { }

        private void OnTypeChanged()
        {
            if (type == BlockValueType.Text)
            {
                t1.Visibility = t2.Visibility = Visibility.Visible;
                BlockColor = (app.Resources["TextValueBlockColorBrush"] as SolidColorBrush).Color;
                Canvas.SetLeft(txtbox, 34);
                txtbox.PlaceholderText = GetLocalizedString("Blocks.ValueBlock.Text.PlaceholderText");

                txtbox.TextChanged -= CheckIllegalCharacter;
            }
            if (type == BlockValueType.Number)
            {
                t1.Visibility = t2.Visibility = Visibility.Collapsed;
                BlockColor = (app.Resources["NumberValueBlockColorBrush"] as SolidColorBrush).Color;
                Canvas.SetLeft(txtbox, 20);
                txtbox.PlaceholderText = GetLocalizedString("Blocks.ValueBlock.Number.PlaceholderText");

                txtbox.TextChanged += CheckIllegalCharacter;
            }
        }

        private void CheckIllegalCharacter(object sender, TextChangedEventArgs e)
        {
            double result;
            if (double.TryParse(txtbox.Text, out result))
            {
                if (BlockTip.IsOpen) BlockTip.IsOpen = false;
            }
            else
            {
                BlockTip.Title = "非法字元";
                BlockTip.Subtitle = "输入的内容无法转成数字";
                if (! BlockTip.IsOpen) BlockTip.IsOpen = true;
            }
        }

        private void ResizeTextBox()
        {
            int w = (int)txtbox.ActualWidth + ((type == BlockValueType.Text) ? 58 : 30);
            Size = (w, Size.Height);
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
    }
}
