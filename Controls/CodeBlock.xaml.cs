using Windows.UI;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;
using CodeBlocks.Core;
using Windows.Foundation;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
            RelatedBlocks.Right = new();
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
            BlockBorder.Data = painter.DrawBlockBorder();
        }

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

        public CodeBlock ParentBlock;
        public (CodeBlock Bottom, List<CodeBlock> Right) RelatedBlocks;

        public bool IsRelatedBlock(CodeBlock target)
        {
            if (RelatedBlocks.Bottom == target || RelatedBlocks.Right.Contains(target)) return true;
            else return false;
        }

        public int GetRelatedBlockCount()
        {
            int count = RelatedBlocks.Right.Count;
            if (RelatedBlocks.Bottom != null) count++;
            return count;
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

        public void MoveTo(CodeBlock other, double dx = 0, double dy = 0)
        {
            double x = Canvas.GetLeft(other) + dx;
            double y = Canvas.GetTop(other) + dy;
            SetPosition(x, y);
        }

        public void SetPosition(double x, double y, bool isRelative = false)
        {
            if (isRelative)
            {
                x += Canvas.GetLeft(this);
                y += Canvas.GetTop(this);
            }

            // 移动方块
            Canvas.SetLeft(this, x);
            Canvas.SetTop(this, y);

            // 移动下方方块
            if (RelatedBlocks.Bottom != null)
            {
                RelatedBlocks.Bottom.SetPosition(x, y + height - 12);
            }
            
            // 移动右侧方块
            foreach (var block in RelatedBlocks.Right)
            {
                block.SetPosition(x + width - 12, y);
                y += 54;
            }
        }

        public bool HasBeenRemoved = false;
        public async Task RemoveAsync(Canvas rootCanvas)
        {
            rootCanvas.Children.Remove(this);
            HasBeenRemoved = true;

            if (RelatedBlocks.Right.Count > 0)
            {
                foreach (var block in RelatedBlocks.Right)
                {
                    block.RemoveAsync(rootCanvas);
                }
                RelatedBlocks.Right.Clear();
            }

            var bottom = RelatedBlocks.Bottom;
            while (bottom != null)
            {
                await bottom.RemoveAsync(rootCanvas);
                bottom = bottom.RelatedBlocks.Bottom;
            }
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
