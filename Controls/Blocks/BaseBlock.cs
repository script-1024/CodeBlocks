﻿using CodeBlocks.Core;
using CodeBlocks.Pages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using System;
using System.Threading.Tasks;
using Windows.Foundation;

namespace CodeBlocks.Controls
{
    public class BaseBlock : BlockControl
    {
        private int width;
        private int height;
        private string key;
        private BlockMetaData metaData;
        private readonly MenuFlyout ContentMenu = new();
        private readonly CodeBlockPainter painter = new();
        private readonly App app = Application.Current as App;

        public BaseBlock() : base()
        {
            metaData = BlockMetaData.Null;
            app.OnLanguageChanged += Localize_Block;
            app.OnLanguageChanged += Localize_Menu;
            InitializeMenu();
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

        private void Localize_Block()
        {
            if (string.IsNullOrEmpty(key)) return;
            var rawText = app.Localizer.GetString(key);
            var parts = rawText.Split('(', ')');
            int slots = 0, textWidth = 0, maxWidth = 0;
            ValueIndex.Clear();
            BlockDescription.Inlines.Clear();

            foreach (var part in parts)
            {
                if (part.StartsWith('&'))
                {
                    ValueIndex.Add(part.Replace("&", ""), slots++);
                    BlockDescription.Inlines.Add(new LineBreak());
                }
                else
                {
                    BlockDescription.Inlines.Add(new Run() { Text = part });
                    textWidth = (int)(TextHelper.CalculateStringWidth(part) * 20);
                    if (textWidth > maxWidth) maxWidth = textWidth;
                }
            }

            if (slots > 0)
            {
                metaData.Slots = slots;
                metaData.Variant |= 0b_0100;
                
            }

            Resize(maxWidth + 40, height);
        }

        private void Localize_Menu()
        {
            foreach (MenuFlyoutItem item in ContentMenu.Items)
            {
                item.Text = app.Localizer.GetString("ContentMenu.CodeBlock." + item.Tag);
            }
        }

        private void Resize(int width, int height)
        {
            // 确保方块高度合法
            var minHeight = (metaData.Slots > 0) ? 16 * (metaData.Slots * 3) + 10 : 58;
            if (height < minHeight) height = minHeight;

            this.width = width;
            this.height = height;
            metaData.Size = (width, height);
            painter.MetaData = metaData;
            BlockBorder.Data = painter.DrawBlockBorder();
            Array.Resize(ref RightBlocks, metaData.Slots);
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

        public BlockMetaData MetaData
        {
            get => metaData;
            set
            {
                metaData = value;
                Size = metaData.Size;
            }
        }

        public string TranslationKey
        {
            get => key;
            set { if (value == key) return; key = value; Localize_Block(); }
        }

        #region "Methods"
        public int GetRelatedBlockCount()
        {
            int count = 0;
            foreach (var block in RightBlocks) { if (block != null) count++; }
            if (BottomBlock != null) count++;
            return count;
        }

        public BaseBlock Copy()
        {
            var block = new BaseBlock() { MetaData = this.MetaData, BlockColor = this.BlockColor, TranslationKey = this.TranslationKey };
            for (int i = 0; i < this.RightBlocks.Length; i++)
            {
                if (this.RightBlocks[i] == null) block.RightBlocks[i] = null;
                else block.RightBlocks[i] = this.RightBlocks[i].Copy();
            }
            Canvas.SetLeft(block.BlockDescription, Canvas.GetLeft(BlockDescription));
            Canvas.SetTop(block.BlockDescription, Canvas.GetTop(BlockDescription));
            return block;
        }

        public void CopyFrom(BaseBlock other)
        {
            this.MetaData = other.metaData;
        }

        public void MoveTo(BaseBlock other, double dx = 0, double dy = 0)
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
            if (RightBlocks != null) for (int i = 0; i < metaData.Slots; i++)
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

        public (int x, int y, double dx, double dy) GetRelativeQuadrant(BaseBlock targetBlock)
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
