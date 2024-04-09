using CodeBlocks.Core;
using CodeBlocks.Pages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using System;
using System.Data.SqlTypes;
using System.Threading.Tasks;
using Windows.Foundation;

namespace CodeBlocks.Controls
{
    public class BlockCreatedEventArgs : EventArgs
    {
        public readonly CodeBlock Source;
        public readonly (double X, double Y) Position;
        public static readonly BlockCreatedEventArgs Null = new(null, (0, 0));

        public BlockCreatedEventArgs(CodeBlock source, (double X, double Y) position) : base()
        {
            Source = source; Position = position;
        }
    }

    public class CodeBlock : BlockControl
    {
        private int width;
        private int height;
        private string key;
        private BlockMetaData metaData;
        private readonly MenuFlyout ContentMenu = new();
        private readonly CodeBlockPainter painter = new();
        private readonly App app = Application.Current as App;

        public CodeBlock(BlockCreatedEventHandler createdEventHandler, BlockCreatedEventArgs args = null) : base()
        {
            metaData = BlockMetaData.Null;
            app.OnLanguageChanged += Localize_Block;
            app.OnLanguageChanged += Localize_Menu;
            InitializeMenu();

            OnBlockCreated += createdEventHandler;
            OnBlockCreated?.Invoke(this, (args == null) ? BlockCreatedEventArgs.Null : args);
        }

        public CodeBlock() : this(null, null) { }

    public delegate void BlockCreatedEventHandler(CodeBlock sender, BlockCreatedEventArgs e);
        public event BlockCreatedEventHandler OnBlockCreated;

        private void InitializeMenu()
        {
            this.ContextFlyout = ContentMenu;

            var item_copy = new MenuFlyoutItem() { Tag = "Copy", Icon = new FontIcon() { Glyph = "\uE8C8" } };
            item_copy.Click += (_, _) =>
            {
                var left = Canvas.GetLeft(this) + 30;
                var top = Canvas.GetTop(this) + 30;
                var block = Copy(new(this, (left, top)));
            };
            ContentMenu.Items.Add(item_copy);

            var item_del = new MenuFlyoutItem() { Tag = "Delete", Icon = new FontIcon() { Glyph = "\uE74D" } };
            item_del.Click += async (_, _) => await RemoveAsync(this.Parent as Canvas, false);
            ContentMenu.Items.Add(item_del);

            var item_delAll = new MenuFlyoutItem() { Tag = "DeleteAll", Icon = new FontIcon() { Glyph = "\uE74D" } };
            item_delAll.Click += async (_, _) =>
            {
                if (GetRelatedBlockCount() > 0)
                {
                    var dialog = new MessageDialog() { XamlRoot = app.MainWindow.Content.XamlRoot };
                    var result = await dialog.ShowAsync("RemovingMultipleBlocks", DialogVariant.YesCancel);
                    if (result != ContentDialogResult.Primary) return;
                }

                await RemoveAsync(this.Parent as Canvas);
            };
            ContentMenu.Items.Add(item_delAll);
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
                    textWidth = (int)(TextHelper.CalculateStringWidth(part) * BlockDescription.FontSize);
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

        public CodeBlock Copy(BlockCreatedEventArgs args)
        {
            var block = new CodeBlock(this.OnBlockCreated, args) { MetaData = this.MetaData, BlockColor = this.BlockColor, TranslationKey = this.TranslationKey };
            for (int i = 0; i < this.RightBlocks.Length; i++)
            {
                if (this.RightBlocks[i] == null) block.RightBlocks[i] = null;
                else block.RightBlocks[i] = this.RightBlocks[i].Copy(BlockCreatedEventArgs.Null);
            }
            Canvas.SetLeft(block.BlockDescription, Canvas.GetLeft(BlockDescription));
            Canvas.SetTop(block.BlockDescription, Canvas.GetTop(BlockDescription));
            return block;
        }

        public void CopyDataFrom(CodeBlock other)
        {
            this.MetaData = other.metaData;
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
            if (RightBlocks != null) for (int i = 0; i < metaData.Slots; i++)
            {
                var block = RightBlocks[i];
                if (block != null) block.SetPosition(x + width - 10, y + i * 48);
            }
        }

        public bool HasBeenRemoved = false;
        public async Task RemoveAsync(Canvas rootCanvas, bool deleteAll = true)
        {
            if (rootCanvas == null) return;
            rootCanvas.Children.Remove(this);
            HasBeenRemoved = true;

            foreach (var block in RightBlocks)
            {
                if (block != null)
                {
                    if (deleteAll) block.RemoveAsync(rootCanvas);
                    else block.ParentBlock = null;
                }
            }

            if (BottomBlock != null)
            {
                if (deleteAll) BottomBlock.RemoveAsync(rootCanvas);
                else BottomBlock.ParentBlock = null;
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
