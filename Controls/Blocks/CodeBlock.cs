using System;
using System.Linq;
using Windows.Foundation;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;
using CodeBlocks.Core;

namespace CodeBlocks.Controls;
public class BlockCreatedEventArgs
{
    public readonly CodeBlock Source;
    public readonly Point Position;
    public static readonly BlockCreatedEventArgs Null = new();

    public BlockCreatedEventArgs(double x = 0, double y = 0, CodeBlock source = null)
    {
        Position = new Point(x, y);
        Source = source;
    }

    public BlockCreatedEventArgs(Point pos, CodeBlock source = null)
    {
        Position = pos;
        Source = source;
    }
}

public class CodeBlock : BlockControl
{
    private string id;
    private string key;
    private BlockMetaData metaData = BlockMetaData.Null;

    private readonly CodeBlockPainter painter = new();
    private readonly CommandBarFlyout ContextMenu = new();
    private readonly App app = Application.Current as App;

    // 方块被创建后要引发的事件，用于在 CodingPage 中初始化方块
    public delegate void BlockCreatedEventHandler(CodeBlock sender, BlockCreatedEventArgs e);

    // Ignore warn CA1070
    // Fix bug when ValueBlock was copied, it wasn't initialized correctly.
    // It has a different behavior than the default (THIS base class).
    #pragma warning disable CA1070
    public virtual event BlockCreatedEventHandler OnBlockCreated;
    #pragma warning restore CA1070

    public CodeBlock(BlockCreatedEventHandler createdEventHandler, BlockCreatedEventArgs args = null) : base()
    {
        app.LanguageChanged += RefreshBlockText;
        app.LanguageChanged += LocalizeMenu;
        InitializeMenu();

        OnBlockCreated += createdEventHandler;
        OnBlockCreated?.Invoke(this, args);
    }

    public CodeBlock() : this(null, null) 
    {
        
    }

    private void InitializeMenu()
    {
        this.ContextFlyout = ContextMenu;
        ContextMenu.AlwaysExpanded = true;
        ContextMenu.Placement = Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.RightEdgeAlignedTop;

        var item_copy = new AppBarButton() { Tag = "Copy", Icon = new FontIcon() { Glyph = "\uE8C8" } };
        item_copy.Click += (_, _) =>
        {
            var left = Canvas.GetLeft(this) + 30;
            var top = Canvas.GetTop(this) + 30;
            var block = this.Clone(new(left, top, this));
            ContextMenu.Hide();
        };
        ContextMenu.PrimaryCommands.Add(item_copy);

        var item_del = new AppBarButton() { Tag = "Delete", Icon = new FontIcon() { Glyph = "\uE74D" } };
        item_del.Click += async (_, _) => await RemoveAsync(this.Parent as Canvas, false);
        ContextMenu.PrimaryCommands.Add(item_del);

        var item_delAll = new AppBarButton() { Tag = "DeleteAll", Icon = new FontIcon() { Glyph = "\uE74D" }, Margin = new(0, 1, 0, 0) }; // 不加上 Margin 就会出现一条不和谐的透明细线
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
        ContextMenu.SecondaryCommands.Add(item_delAll);

        LocalizeMenu();
    }

    private void SetText(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        var parts = text.Split('{', '}');
        int slots = 0, maxWidth = 0, textWidth;
        ValueIndex.Clear();

        // 移除所有 TextBlock
        var blocks = RootCanvas.Children.OfType<TextBlock>().ToList();
        foreach (var block in blocks) { RootCanvas.Children.Remove(block); }

        // 作用中的文字框
        TextBlock currentTextBlock;
        int currentY = SlotWidth - 4; // 暫时不知为何会差+4像素，扣回去

        foreach (var part in parts)
        {
            if (string.IsNullOrWhiteSpace(part)) continue;

            string str = part.Trim(); // 移除前后空格
            if (str.StartsWith('%'))
            {
                ValueIndex.Add(str, slots++);
            }
            else
            {
                currentTextBlock = new()
                {
                    Text = str,
                    Foreground = Microsoft.UI.Colors.White.GetSolidColorBrush(),
                    FontFamily = CodeBlock.FontFamily,
                    FontSize = CodeBlock.FontSize
                };

                textWidth = (int)(TextHelper.GetWidth(str) * FontSize);
                if (textWidth > maxWidth) maxWidth = textWidth;
                RootCanvas.Children.Add(currentTextBlock);

                // 左边距
                Canvas.SetLeft(currentTextBlock, SlotWidth);

                // 上边距
                Canvas.SetTop(currentTextBlock, currentY);

                int newpartY = 0;
                if (RightBlocks.TryGetValue(slots, out CodeBlock block) && block is not null)
                {
                    newpartY += block.Size.Height;
                }
                else newpartY += SlotWidth * 3;
                
                currentY += newpartY;
                currentTextBlock = null;
            }
        }

        metaData.Slots = slots;

        // 调整方块宽度
        (int w, int h) size = Size;
        size.w = maxWidth + SlotWidth * 3;
        size.w += metaData.Variant.CheckIfContain(0b_0100) ? SlotHeight : 0;
        Resize(size);
    }

    // 旧方法，从全局翻译文件取得本地化字串
    private void LocalizeBlock()
    {
        if (string.IsNullOrEmpty(key)) return;
        var text = app.Localizer.GetString(key);
        SetText(text);
    }

    /// <summary>
    /// 刷新方块的本地化文本
    /// </summary>
    public virtual void RefreshBlockText()
    {
        if (TranslationsDict != null)
        {
            if (TranslationsDict.TryGetValue(App.CurrentLanguageId, out string text))
            {
                SetText(text);
                return;
            }
            else if (TranslationsDict.Count > 0)
            {
                text = TranslationsDict.First().Value;
                SetText(text);
                return;
            }
        }

        // fallback
        if (IsInteractionDisabled) return; // 若已禁用交互，并且没有指定翻译字典，则该方块不需要翻译，例如:  GhostBlock
        if (string.IsNullOrEmpty(id)) TranslationKey = "Blocks.Demo";
        else SetText(id);
    }

    private void LocalizeMenu()
    {
        foreach (var item in ContextMenu.PrimaryCommands)
        {
            if (item is AppBarButton btn) btn.Label = app.Localizer.GetString("ContentMenu.CodeBlock." + btn.Tag);
        }

        foreach (var item in ContextMenu.SecondaryCommands)
        {
            if (item is AppBarButton btn) btn.Label = app.Localizer.GetString("ContentMenu.CodeBlock." + btn.Tag);
        }
    }

    private void Resize(Core.Size size)
    {
        // 确保方块高度合法
        var minHeight = (metaData.Slots > 0) ? SlotWidth * (metaData.Slots * 3) + SlotHeight : 58;
        if (size.Height < minHeight) size.Height = minHeight;

        // 确保方块宽度合法
        if (size.Width < 100) size.Width = 100;

        metaData.Size = size;
        this.Width = size.Width;
        this.Height = size.Height;
        painter.MetaData = metaData;
        BlockBorder.Data = painter.DrawBlockBorder();
        Array.Resize(ref RightBlocks, metaData.Slots);
    }

    #region "Properties"

    public Core.Size Size
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

    public string Identifier
    {
        get => id;
        set { id = value; RefreshBlockText(); } 
    }

    public string TranslationKey
    {
        get => key;
        set { key = value; LocalizeBlock(); }
    }

    public Dictionary<string, string> TranslationsDict;

    public int DependentSlot;
    public CodeBlock ParentBlock;
    public CodeBlock BottomBlock;
    public CodeBlock[] RightBlocks = [];
    public Dictionary<string, int> ValueIndex { get; private set; } = new();

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

    /// <summary>
    /// 取得方块的克隆对象
    /// </summary>
    public virtual CodeBlock Clone(BlockCreatedEventHandler eventHandler, BlockCreatedEventArgs args = null)
    {
        var block = new CodeBlock(eventHandler, args)
        {
            MetaData = this.MetaData,
            BlockColor = this.BlockColor,
            Identifier = this.Identifier
        };

        if (TranslationsDict is null) block.TranslationKey = this.TranslationKey;
        else block.TranslationsDict = this.TranslationsDict;

        for (int i = 0; i < this.RightBlocks.Length; i++)
        {
            if (this.RightBlocks.TryGetValue(i, out var thisBlock))
            {
                if (thisBlock is null) continue;
                var left = Canvas.GetLeft(thisBlock) + 30;
                var top = Canvas.GetTop(thisBlock) + 30;
                var newArgs = new BlockCreatedEventArgs(left, top, block);
                var newBlock = thisBlock.Clone(eventHandler, newArgs);
                block.RightBlocks.TrySetValue(i, newBlock);
            }
            else block.RightBlocks.TrySetValue(i, null);
        }

        block.RefreshBlockText();
        return block;
    }

    /// <summary>
    /// 取得方块的克隆对象
    /// </summary>
    public virtual CodeBlock Clone(BlockCreatedEventArgs args = null) => Clone(this.OnBlockCreated, args);

    /// <summary>
    /// 更改方块数据
    /// </summary>
    public void SetData(BlockProperties key, object value)
    {
        BlockMetaData data = this.MetaData;
        switch (key)
        {
            case BlockProperties.Type:
                data.Type = (BlockType)value;
                break;
            case BlockProperties.Code:
                data.Code = (string)value;
                break;
            case BlockProperties.Variant:
                if (value is int n) data.Variant = (byte)n; // 若value为int类型，直接object转byte会崩溃
                else if (value is byte b) data.Variant = b;
                break;
            case BlockProperties.Slots:
                data.Slots = (int)value;
                break;
            case BlockProperties.Size:
                data.Size = (Core.Size)value;
                break;
            case BlockProperties.Width:
                data.Size.Width = (int)value;
                break;
            case BlockProperties.Height:
                data.Size.Height = (int)value;
                break;
            default:
                break;
        }
        this.MetaData = data;
    }

    /// <summary>
    /// 使用指定方块的数据覆盖原始数据
    /// </summary>
    public void CopyDataFrom(CodeBlock other)
    {
        this.MetaData = other.metaData;
    }

    /// <summary>
    /// 弹出方块，这会重置方块间的从属关系
    /// </summary>
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

    /// <summary>
    /// 设置方块在画布上的遮挡关系
    /// </summary>
    /// <param name="isRelative">是否为相对值</param>
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

    bool moved = false;
    private (double x, double y) prevPosition;

    /// <summary>
    /// 设置方块在画布上的坐标
    /// </summary>
    /// <param name="isRelative">是否为相对值</param>
    public void SetPosition(double x, double y, bool isRelative = false)
    {
        moved = true;
        prevPosition.x = Canvas.GetLeft(this);
        prevPosition.y = Canvas.GetTop(this);

        if (isRelative)
        {
            x += Canvas.GetLeft(this);
            y += Canvas.GetTop(this);
        }

        // 移动方块
        Canvas.SetLeft(this, x);
        Canvas.SetTop(this, y);

        // 移动下方方块
        BottomBlock?.SetPosition(x, y + Size.Height - SlotHeight);

        // 移动右侧方块
        for (int i = 0; i < metaData.Slots; i++)
        {
            var block = RightBlocks[i];
            block?.SetPosition(x + Size.Width - SlotHeight, y + i * 48);
        }
    }

    /// <summary>
    /// 移动自身到指定方块的位置
    /// </summary>
    /// <param name="dx">水平偏移量</param>
    /// <param name="dy">垂直偏移量</param>
    public void MoveToBlock(CodeBlock other, double dx = 0, double dy = 0)
    {
        double x = Canvas.GetLeft(other) + dx;
        double y = Canvas.GetTop(other) + dy;
        SetPosition(x, y);
    }

    /// <summary>
    /// 移动此方块到队列的尾端
    /// </summary>
    /// <param name="replacer">引发此事件的方块</param>
    /// <param name="replace">是否取代原先的从属关系</param>
    public void MoveToBack(CodeBlock replacer, bool replace = true)
    {
        // 定位到队列的尾端
        var endBlock = replacer;
        while (endBlock.BottomBlock != null) endBlock = endBlock.BottomBlock;

        if (replace)
        {
            ParentBlock.BottomBlock = replacer;
            endBlock.BottomBlock = this;
            this.ParentBlock = endBlock;
        }
        this.MoveToBlock(endBlock, 0, endBlock.Size.Height - SlotHeight);
    }

    /// <summary>
    /// 将方块移到最后记录的坐标
    /// </summary>
    public void ReturnToLastRecordedPosition() { if (moved) SetPosition(prevPosition.x, prevPosition.y); moved = false; }

    /// <summary>
    /// 以指定间隔时间播放缩放动画
    /// </summary>
    /// <param name="from">开始前的缩放倍率</param>
    /// <param name="to">结束后的缩放倍率</param>
    /// <param name="delay">(可选) 间隔时间，单位毫秒</param>
    public async Task PlayScaleAnimation(double from, double to, int delay = 5)
    {
        if (from == to) return;
        bool isAskedToZoomIn = (to > from);
        double delta = (isAskedToZoomIn) ? 0.05 : -0.05;
        var transform = new ScaleTransform()
        {
            CenterX = Size.Width / 2,
            CenterY = Size.Height / 2
        };
        this.RenderTransform = transform;
        for (double d = from; ; d += delta)
        {
            if ( isAskedToZoomIn && d > to) break;
            if (!isAskedToZoomIn && d < to) break;
            transform.ScaleX = transform.ScaleY = d;
            await Task.Delay(delay);
        }
    }

    /// <summary>
    /// 从指定画布上移除方块
    /// </summary>
    /// <param name="rootCanvas">方块所在的画布</param>
    /// <param name="deleteAll">(可选) 是否删除依附自身的所有方块</param>
    /// <returns></returns>
    public async Task RemoveAsync(Canvas rootCanvas, bool deleteAll = true)
    {
        if (rootCanvas == null || HasBeenRemoved) return;
        if (ContextMenu.IsOpen) ContextMenu.Hide();

        // 删除方块的缩小动画
        await PlayScaleAnimation(1.0, 0.0);

        if (deleteAll) BottomBlock?.RemoveAsync(rootCanvas);
        else if(this.DependentSlot == -1)
        {
            // 仅删除自身，需接上相邻块
            if (this.BottomBlock != null)
            {
                this.ParentBlock.BottomBlock = this.BottomBlock;
                this.BottomBlock.ParentBlock = this.ParentBlock;
                this.BottomBlock.MoveToBlock(this); // 向上移动到此块的位置
            }
        }

        rootCanvas.Children.Remove(this);
        HasBeenRemoved = true;

        foreach (var block in RightBlocks)
        {
            if (deleteAll) block?.RemoveAsync(rootCanvas);
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

    #region "Static"

    public static readonly int SlotWidth = 16;
    public static readonly int SlotHeight = 10;
    public static readonly new FontFamily FontFamily = new("/Fonts/HarmonyOS_Sans_B.ttf#HarmonyOS Sans SC");
    public static readonly new int FontSize = 15;

    #endregion
}

public enum BlockProperties
{
    Undefined = 0,
    Type      = 1,
    Code      = 2,
    Variant   = 3,
    Slots     = 4,
    Size      = 5,
    Width     = 6,
    Height    = 7
}
public enum BlockValueType
{
    None          = 0b_0000_0000, // 0x00 不指定类型
    Int8          = 0b_0000_0010, // 0x02 指定元素类型为  8位整数
    Int16         = 0b_0000_0011, // 0x03 指定元素类型为 16位整数
    Int32         = 0b_0000_0100, // 0x04 指定元素类型为 32位整数
    Int64         = 0b_0000_0101, // 0x05 指定元素类型为 64位整数
    Void          = 0b_0000_1111, // 0x0F 空类型
    SingleValue   = 0b_0001_0000, // 0x10 接受单一元素
    Enumerate     = 0b_0001_0001, // 0x11 期望枚举类型
    Number        = 0b_0010_0000, // 0x20 期望一个数字
    Integer       = 0b_0011_0000, // 0x30 期望一个整数 (预设 int)
    Bool          = 0b_0011_0001, // 0x31 指定 bool  数值
    Byte          = 0b_0011_0010, // 0x32 指定 byte  数值
    Short         = 0b_0011_0011, // 0x33 指定 short 数值
    Int           = 0b_0011_0100, // 0x34 指定 int   数值
    Long          = 0b_0011_0101, // 0x35 指定 long  数值
    Decimal       = 0b_0011_1000, // 0x38 期望一个小数 (预设 double)
    Float         = 0b_0011_1110, // 0x3E 指定 float  数值
    Double        = 0b_0011_1111, // 0x3F 指定 double 数值
    MultipleValue = 0b_0100_0000, // 0x40 接受多个元素
    List          = 0b_0100_0001, // 0x60 期望一个列表
    Dictionary    = 0b_0100_0010, // 0x70 期望一个字典
    Array         = 0b_0100_1000, // 0x50 期望一个数组
    ByteArray     = 0b_0100_1010, // 0x52 指定 byte 数组
    IntArray      = 0b_0100_1100, // 0x54 指定 int  数组
    LongArray     = 0b_0100_1101, // 0x55 指定 long 数组
    String        = 0b_1000_0000, // 0x80 期望一个字串
    PlainString   = 0b_1000_0001, // 0x81 指定 纯字串
    JsonString    = 0b_1000_0010, // 0x82 指定 JSON 字串
    UnknownType   = 0b_1111_1111  // 0xFF 未知类型
}
