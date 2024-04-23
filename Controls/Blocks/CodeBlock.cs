using System;
using System.Threading.Tasks;
using CodeBlocks.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Windows.Foundation;

namespace CodeBlocks.Controls
{
    public class BlockCreatedEventArgs : EventArgs
    {
        public readonly CodeBlock Source;
        public readonly Point Position;
        public static readonly BlockCreatedEventArgs Null = new();

        public BlockCreatedEventArgs(double x = 0, double y = 0, CodeBlock source = null) : base()
        {
            Position = new Point(x, y);
            Source = source;
        }

        public BlockCreatedEventArgs(Point pos, CodeBlock source = null) : base()
        {
            Position = pos;
            Source = source;
        }
    }

    public class CodeBlock : BlockControl
    {
        private string key;
        private BlockMetaData metaData = BlockMetaData.Null;

        private readonly MenuFlyout ContentMenu = new();
        private readonly CodeBlockPainter painter = new();
        private readonly App app = Application.Current as App;

        public delegate void BlockCreatedEventHandler(CodeBlock sender, BlockCreatedEventArgs e);
        public event BlockCreatedEventHandler OnBlockCreated;

        public CodeBlock(BlockCreatedEventHandler createdEventHandler, BlockCreatedEventArgs args = null) : base()
        {
            app.OnLanguageChanged += LocalizeBlock;
            app.OnLanguageChanged += LocalizeMenu;
            InitializeMenu();

            OnBlockCreated += createdEventHandler;
            OnBlockCreated?.Invoke(this, args);
        }

        public CodeBlock() : this(null, null) { }

        private void InitializeMenu()
        {
            this.ContextFlyout = ContentMenu;

            var item_copy = new MenuFlyoutItem() { Tag = "Copy", Icon = new FontIcon() { Glyph = "\uE8C8" } };
            item_copy.Click += (_, _) =>
            {
                var left = Canvas.GetLeft(this) + 30;
                var top = Canvas.GetTop(this) + 30;
                var block = Copy( new(left, top, this) );
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
            LocalizeMenu();
        }

        private void LocalizeBlock()
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

            (int w, int h) size = Size;
            size.w = maxWidth + 40;
            Resize(size);
        }

        private void LocalizeMenu()
        {
            foreach (MenuFlyoutItem item in ContentMenu.Items)
            {
                item.Text = app.Localizer.GetString("ContentMenu.CodeBlock." + item.Tag);
            }
        }

        private void Resize((int w, int h) size)
        {
            // 确保方块高度合法
            var minHeight = (metaData.Slots > 0) ? 16 * (metaData.Slots * 3) + 10 : 58;
            if (size.h < minHeight) size.h = minHeight;

            metaData.Size = size;
            painter.MetaData = metaData;
            BlockBorder.Data = painter.DrawBlockBorder();
            Array.Resize(ref RightBlocks, metaData.Slots);
        }

        #region "Properties"
        public (int Width, int Height) Size
        {
            get => metaData.Size;
            set => Resize(value);
        }

        public BlockMetaData MetaData
        {
            get => metaData;
            set
            {
                metaData = value;
                Resize(value.Size);
            }
        }

        public string TranslationKey
        {
            get => key;
            set { if (value == key) return; key = value; LocalizeBlock(); }
        }

        public bool HasBeenRemoved = false;
        #endregion

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
            CodeBlock block;
            if (this is ValueBlock vb)
            {
                block = new ValueBlock(this.OnBlockCreated, args)
                {
                    MetaData = vb.MetaData,
                    ValueType = vb.ValueType
                };
            }
            else
            {
                block = new CodeBlock(this.OnBlockCreated, args)
                {
                    MetaData = this.MetaData,
                    BlockColor = this.BlockColor,
                    TranslationKey = this.TranslationKey
                };
            }
            
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

            Canvas.SetLeft(block.BlockDescription, Canvas.GetLeft(BlockDescription));
            Canvas.SetTop(block.BlockDescription, Canvas.GetTop(BlockDescription));
            return block;
        }

        public void CopyDataFrom(CodeBlock other)
        {
            this.MetaData = other.metaData;
        }

        public void PopUp()
        {
            if (this.ParentBlock is null) return;

            if (this.DependentSlot == -1) this.ParentBlock.BottomBlock = null;
            else if (this.DependentSlot > 0) this.ParentBlock.RightBlocks[DependentSlot - 1] = null;

            this.SetPosition(+40, +20, true);
            this.SetZIndex(+1, true);
            this.ParentBlock = null;
            this.DependentSlot = 0;
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
            BottomBlock?.SetZIndex(value);
            foreach (var block in RightBlocks)
            {
                block?.SetZIndex(value);
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
            BottomBlock?.SetPosition(x, y + Size.Height - 10);

            // 移动右侧方块
            for (int i = 0; i < metaData.Slots; i++)
            {
                var block = RightBlocks[i];
                block?.SetPosition(x + Size.Width - 10, y + i * 48);
            }
        }

        public async Task RemoveAsync(Canvas rootCanvas, bool deleteAll = true)
        {
            if (rootCanvas == null) return;

            if (deleteAll) BottomBlock?.RemoveAsync(rootCanvas);
            else if(this.DependentSlot == -1)
            {
                // 仅删除自身，需接上相邻块
                if (this.BottomBlock != null)
                {
                    this.ParentBlock.BottomBlock = this.BottomBlock;
                    this.BottomBlock.ParentBlock = this.ParentBlock;
                    this.BottomBlock.MoveTo(this); // 向上移动到此块的位置
                }
            }

            rootCanvas.Children.Remove(this);
            HasBeenRemoved = true;

            foreach (var block in RightBlocks)
            {
                if (deleteAll) await block?.RemoveAsync(rootCanvas);
                else block?.PopUp();
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
            if (self.X + Size.Width < target.X + 10) rq.x = -1;

            if (self.Y + 10 > target.Y + targetH - 10) rq.y = 1;
            if (self.Y + Size.Height < target.Y + 10) rq.y = -1;

            if (rq.x > 0) rq.dx = (self.X + Size.Width) - (target.X + targetW);
            else if (rq.x < 0) rq.dx = target.X - self.X;

            if (rq.y > 0) rq.dy = (self.Y + Size.Height) - (target.Y + targetH);
            else if (rq.y < 0) rq.dy = target.Y - self.Y;

            return rq;
        }
        #endregion
    }
}
