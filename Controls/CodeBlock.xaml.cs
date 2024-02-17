using Windows.UI;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;
using CodeBlocks.Core;
using Windows.Foundation;
using System;

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
        // private --- adjacentBlocks;

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
                BlockColor = BlockColor
            };
        }

        public void CopyFrom(CodeBlock other)
        {
            this.MetaData = other.MetaData;
        }

        public void MoveTo(CodeBlock other)
        {
            Canvas.SetLeft(this, Canvas.GetLeft(other));
            Canvas.SetTop(this, Canvas.GetTop(other));
        }

        public (int x, int y, double dx, double dy) GetRelativeQuadrant(CodeBlock targetBlock)
        {
            //  x,  y 定义: 右下为正，左上为负，中间为零
            // dx, dy 定义: 相同边的距离
            (int x, int y, double dx, double dy) rq = (0, 0, 0, 0);

            Point self = new(Canvas.GetLeft(this), Canvas.GetTop(this));
            Point target = new(Canvas.GetLeft(targetBlock), Canvas.GetTop(targetBlock));
            int targetW = targetBlock.Size.Width;
            int targetH = targetBlock.Size.Height;

            if (self.X + 12 > target.X + targetW * 0.5) rq.x = 1;
            if (self.X + width * 0.5 < target.X + 12) rq.x = -1;

            if (self.Y + 12 > target.Y + targetH * 0.5) rq.y = 1;
            if (self.Y + height * 0.5 < target.Y + 12) rq.y = -1;

            if (rq.x > 0) rq.dx = (self.X + width) - (target.X + targetW);
            else if (rq.x < 0) rq.dx = target.X - self.X;

            if (rq.y > 0) rq.dy = (self.Y + height) - (target.Y + targetH);
            else if (rq.y < 0) rq.dy = target.Y - self.Y;

            return rq;
        }
    }
}
