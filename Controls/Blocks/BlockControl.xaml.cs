using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Windows.UI;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using CodeBlocks.Core;
using CodeBlocks.Pages;

namespace CodeBlocks.Controls
{
    public partial class CodeBlock : UserControl
    {
        private readonly App app = Application.Current as App;

        private string key;
        private int width;
        private int height;
        private Color fillColor;
        private Color borderColor;
        private BlockMetaData metaData;
        private CodeBlockPainter painter;

        private readonly MenuFlyout ContentMenu = new();

        public CodeBlock()
        {
            InitializeComponent();
            painter = new CodeBlockPainter();
            metaData = BlockMetaData.Null;
            if (fillColor == default) BlockColor = Color.FromArgb(255, 80, 80, 80);
            app.OnLanguageChanged += Localize_Block;
            app.OnLanguageChanged += Localize_Menu;
            BlockDescription = description;

            InitializeMenu(); // 右键选单
        }

        private void InitializeMenu()
        {
            this.RightTapped += (s, e) => { ContentMenu.ShowAt(this, e.GetPosition(this)); };
            var item_copy = new MenuFlyoutItem() { Tag = "Copy", Icon = new FontIcon() { Glyph = "\uE8C8" } };
            item_copy.Click += (_, _) =>
            {
                var canvas = this.Parent as Canvas;
                var block = Copy();
                canvas.Children.Add(block);
                block.SetPosition(Canvas.GetLeft(this) + 50, Canvas.GetTop(this) + 50);
                (canvas.Parent as CodingPage).CodeBlock_AddManipulationEvents(block);
            };
            ContentMenu.Items.Add(item_copy);
            Localize_Menu();
        }

        private void Resize(int width, int height)
        {
            if (Utils.GetFlag(metaData.Variant, 2))
            {
                // 确保方块高度合法
                var minHeight = 16 * (metaData.Slots * 3) + 10;
                if (height < minHeight) height = minHeight;
            }
            
            this.width = width;
            this.height = height;
            metaData.Size = (width, height);
            painter.MetaData = metaData;
            border.Data = painter.DrawBlockBorder();
        }

        private void SetColor(Color value)
        {
            var fillBrush = new SolidColorBrush(value);
            var borderBrush = new SolidColorBrush(borderColor);
            border.Stroke = borderBrush;
            border.StrokeThickness = 2;
            border.Fill = fillBrush;
        }

        private void Localize_Block()
        {
            if (string.IsNullOrEmpty(key)) return;
            var rawText = app.Localizer.GetString(key);
            var parts = rawText.Split('(', ')');
            int slots = 0;
            ValueIndex.Clear();
            description.Inlines.Clear();
            foreach (var part in parts)
            {
                if (part.StartsWith('&'))
                {
                    ValueIndex.Add(part.Replace("&", ""), slots++);
                    description.Inlines.Add(new LineBreak());
                }
                else description.Inlines.Add(new Run() { Text = part });
            }
            if (slots > 0)
            {
                metaData.Slots = slots;
                metaData.Variant |= 0b_0100;
                Resize(width, height);
            }
        }

        private void Localize_Menu()
        {
            foreach (MenuFlyoutItem item in ContentMenu.Items)
            {
                item.Text = app.Localizer.GetString("ContentMenu.CodeBlock." + item.Tag);
            }
        }

        #region "Properties"
        public Dictionary<string, int> ValueIndex { get; private set; } = new();

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
                RightBlocks = new CodeBlock[metaData.Slots];
            }
        }

        public string TranslationKey
        {
            get => key;
            set { if (value == key) return; key = value; Localize_Block(); }
        }
        #endregion

        public int DependentSlot;
        public CodeBlock ParentBlock;
        public CodeBlock BottomBlock;
        public CodeBlock[] RightBlocks;
        public readonly TextBlock BlockDescription;

        #region "Methods"
        public int GetRelatedBlockCount()
        {
            int count = 0;
            foreach (var block in RightBlocks) { if (block != null) count++; }
            if (BottomBlock != null) count++;
            return count;
        }

        public CodeBlock Copy()
        {
            var block = new CodeBlock() { MetaData = this.MetaData, BlockColor = this.BlockColor, TranslationKey = this.TranslationKey };
            for (int i=0; i<this.RightBlocks.Length; i++)
            {
                if (this.RightBlocks[i] == null) block.RightBlocks[i] = null;
                else block.RightBlocks[i] = this.RightBlocks[i].Copy();
            }
            Canvas.SetLeft(block.description, Canvas.GetLeft(description));
            Canvas.SetTop(block.description, Canvas.GetTop(description));
            return block;
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

        public void SetZIndex(int value, bool isRelative = false)
        {
            if (isRelative) value = checked(value + Canvas.GetZIndex(this));
            Canvas.SetZIndex(this, value);
            if (BottomBlock != null) BottomBlock.SetZIndex(value);
            foreach (var block in RightBlocks)
            {
                if (block != null) block.SetZIndex(value);
            }
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
            if (BottomBlock != null) { BottomBlock.SetPosition(x, y + height - 10); }
            
            // 移动右侧方块
            if (RightBlocks.Length > 0) for (int i=0; i<metaData.Slots; i++)
            {
                var block = RightBlocks[i];
                if (block != null) block.SetPosition(x + width - 10, y + i * 48);
            }
        }

        public bool HasBeenRemoved = false;
        public async Task RemoveAsync(Canvas rootCanvas)
        {
            rootCanvas.Children.Remove(this);
            HasBeenRemoved = true;

            foreach (var block in RightBlocks)
            {
                if (block != null) block.RemoveAsync(rootCanvas);
            }

            var bottom = BottomBlock;
            while (bottom != null)
            {
                await bottom.RemoveAsync(rootCanvas);
                bottom = bottom.BottomBlock;
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

            if (self.X + 10 > target.X + targetW - 10) rq.x = 1;
            if (self.X + width < target.X + 10) rq.x = -1;

            if (self.Y + 10 > target.Y + targetH - 10) rq.y = 1;
            if (self.Y + height < target.Y + 10) rq.y = -1;

            if (rq.x > 0) rq.dx = (self.X + width) - (target.X + targetW);
            else if (rq.x < 0) rq.dx = target.X - self.X;

            if (rq.y > 0) rq.dy = (self.Y + height) - (target.Y + targetH);
            else if (rq.y < 0) rq.dy = target.Y - self.Y;

            return rq;
        }
        #endregion
    }
}
