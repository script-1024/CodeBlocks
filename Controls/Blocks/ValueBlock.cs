using Windows.UI;
using CodeBlocks.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Core;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

namespace CodeBlocks.Controls
{
    public enum BlockValueType { None = 0, Text = 1, Number = 2, Boolean = 3 }

    public class ValueBlock : CodeBlock
    {
        private BlockValueType type = BlockValueType.None;
        private string GetLocalizedString(string key) => (Application.Current as App).Localizer.GetString(key);
        public BlockValueType ValueType
        {
            get => type;
            set
            {
                type = value;
                OnTypeChanged();
            }
        }
        private void OnTypeChanged()
        {
            if (type == BlockValueType.Text)
            {
                t1.Visibility = t2.Visibility = Visibility.Visible;
                BlockColor = Color.FromArgb(255, 150, 25, 25);
                Canvas.SetLeft(txtbox, 34);
                txtbox.PlaceholderText = GetLocalizedString("Blocks.ValueBlock.Text.PlaceholderText");
            }
            if (type == BlockValueType.Number)
            {
                t1.Visibility = t2.Visibility = Visibility.Collapsed;
                BlockColor = Color.FromArgb(255, 0, 85, 255);
                Canvas.SetLeft(txtbox, 20);
                txtbox.PlaceholderText = GetLocalizedString("Blocks.ValueBlock.Number.PlaceholderText");
            }
        }

        private readonly TextBlock t1 = new() { Text = "“ " }, t2 = new() { Text = " ”" };
        private readonly TextBox txtbox = new() { VerticalContentAlignment = VerticalAlignment.Center, BorderBrush = new SolidColorBrush(Colors.Transparent), CornerRadius = new(10) };

        public ValueBlock(BlockCreatedEventHandler handler, BlockCreatedEventArgs args = null) : base(handler, args)
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

            (Application.Current as App).OnLanguageChanged += () =>
            {
                string key = $"Blocks.ValueBlock.{((type == BlockValueType.Text) ? "Text" : "Number")}.PlaceholderText";
                txtbox.PlaceholderText = GetLocalizedString(key);
            };
        }

        public ValueBlock() : this(null, null) { }

        void ResizeTextBox()
        {
            int w = (int)txtbox.ActualWidth + ((type == BlockValueType.Text) ? 58 : 30);
            Size = (w, Size.Height);
            double x = Canvas.GetLeft(txtbox);
            Canvas.SetLeft(t1, x-14);
            Canvas.SetLeft(t2, x+w-58);
        }
    }
}
