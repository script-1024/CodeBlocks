using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;
using CodeBlocks.Core;
using static System.Reflection.Metadata.BlobBuilder;

namespace CodeBlocks.Controls
{
    public sealed partial class CodeBlock : UserControl
    {
        private int width;
        private int height;
        private Color fillColor;
        private Color borderColor;
        private BlockMetaData metaData;
        private CodeBlockPainter painter;

        public CodeBlock()
        {
            InitializeComponent();
            painter = new CodeBlockPainter();
            metaData = new BlockMetaData();
            if (fillColor == default) BlockColor = Color.FromArgb(255, 80, 80, 80);
        }

        private void Resize(int width, int height)
        {
            if (Utils.GetFlag(metaData.Variant, 2))
            {
                // 确保方块高度合法
                var minHeight = 18 * (metaData.Slots * 3) + 12;
                if (height < minHeight) height = minHeight;
            }
            
            this.width = width;
            this.height = height;
            metaData.Size = (width, height);
            painter.MetaData = metaData;
            SetBorder(painter.DrawBlockBorder());
        }

        private void SetBorder(PathGeometry geo) => BlockBorder.Data = geo;

        private void SetColor(Color value)
        {
            var fillBrush = new SolidColorBrush(value);
            var borderBrush = new SolidColorBrush(borderColor);
            BlockBorder.Stroke = borderBrush;
            BlockBorder.StrokeThickness = 2;
            BlockBorder.Fill = fillBrush;
        }
        
        public (int Width, int Height) Size
        {
            get => (width, height);
            set
            {
                width = value.Width;
                height = value.Height;
                Resize(width, height);
            }
        }
        
        public Color BlockColor
        {
            get => fillColor;
            set
            {
                fillColor = value;
                borderColor = ColorHelper.GetBorderColor(value);
                SetColor(fillColor);
            }
        }

        public BlockMetaData MetaData
        {
            get => metaData;
            set
            {
                metaData = value;
                Size = metaData.Size;
            }
        }

        public CodeBlock Copy()
        {
            return new CodeBlock()
            {
                MetaData = MetaData,
                BlockColor = BlockColor,
                Size = Size
            };
        }

        public void CopyFrom(CodeBlock other)
        {
            this.MetaData = other.MetaData;
            this.Size = other.Size;
        }

        public void MoveTo(CodeBlock other)
        {
            Canvas.SetLeft(this, Canvas.GetLeft(other));
            Canvas.SetTop(this, Canvas.GetTop(other));
        }
    }
}
