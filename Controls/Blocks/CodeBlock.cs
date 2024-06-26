﻿using System;
using System.Linq;
using Windows.Foundation;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;
using CodeBlocks.Core;
using Microsoft.UI;

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
    #region "Constructor"

    // 方块被创建后要引发的事件，用于在 CodingPage 中初始化方块
    public delegate void BlockCreatedEventHandler(CodeBlock sender, BlockCreatedEventArgs e);
    public event BlockCreatedEventHandler OnBlockCreated;

    public CodeBlock(BlockCreatedEventHandler createdEventHandler, BlockCreatedEventArgs args = null) : base()
    {
        app.LanguageChanged += RefreshBlockText;
        app.LanguageChanged += LocalizeMenu;
        InitializeMenu();

        OnBlockCreated += createdEventHandler;
        OnBlockCreated?.Invoke(this, args);
    }

    public CodeBlock() : this(null, null) { }

    #endregion

    #region "PrivateField"

    private string id;
    private string key;
    private bool isExpand = true;
    private bool canContextMenuShow = true;
    private BlockMetaData metaData = BlockMetaData.Null;

    private readonly CodeBlockPainter painter = new();
    private readonly CommandBarFlyout ContextMenu = new();
    private readonly App app = Application.Current as App;

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

        var icon_changeExpand = new FontIcon() { Glyph = "\uE976" };
        var item_changeExpand = new AppBarButton() { Tag = "ChangeExpandState.ToEmbedded", Icon = icon_changeExpand };
        item_changeExpand.Click += (_, _) =>
        {
            IsExpand = !IsExpand;
            if (IsExpand)
            {
                item_changeExpand.Label = app.Localizer.GetString("ContentMenu.CodeBlock.ChangeExpandState.ToEmbedded");
                icon_changeExpand.Glyph = "\uE976";
                item_changeExpand.Tag = "ChangeExpandState.ToEmbedded";
            }
            else
            {
                item_changeExpand.Label = app.Localizer.GetString("ContentMenu.CodeBlock.ChangeExpandState.ToExpanded");
                icon_changeExpand.Glyph = "\uEA4E";
                item_changeExpand.Tag = "ChangeExpandState.ToExpanded";
            }
        };
        ContextMenu.SecondaryCommands.Add(item_changeExpand);

        LocalizeMenu();
    }

    private void StackPanelResized(object sender)
    {
        if (IsExpand) return;
        if (sender is not StackPanel stackPanel) return;
        if (MetaData.Type != BlockType.Process)
        {
            // 只会有一个 StackPanel
            // 宽度: 左侧边距 SlotWidth, 右侧边距 CtrlMargin
            SetData(BlockProperties.Width, SlotWidth + stackPanel.ActualWidth + CtrlMargin);
        }
        else
        {
            double maxWidth = 0;
            var panels = RootCanvas.Children.OfType<StackPanel>().ToArray();
            foreach (var panel in panels)
            {
                if (panel.ActualWidth > maxWidth) maxWidth = panel.ActualWidth;
            }
            SetData(BlockProperties.Width, SlotWidth + maxWidth + CtrlMargin);
        }

        // 更新子方块位置
        UpdateSubBlockPosition();
    }

    private Point SetLabelAndInputBox(string text, Point origin, List<BlockValueType> slotTypes, List<BlockBranchData> branchDatas, int currentSection)
    {
        BlockBranchData branchData = new();

        // 被括号框起来 --> 输入项
        // 将会去除 ‘无’ 或 ‘空白’ 片段
        var parts = text.Split(['(', ')'], (StringSplitOptions)3);
        double minWidth = 0, maxWidth = 0, innerHeight = 0;
        int slots = 0;

        // 作用中的控件
        TextBlock textBlock;
        StackPanel stackPanel = new()
		{
			Orientation = Orientation.Horizontal
		};

		// 左边距
		origin.X += SlotWidth;

		// 上边距
		// 暫时不知为何会差+4像素，扣回去
		origin.Y += (SlotWidth - 4);

		if (!IsExpand)
		{
			RootCanvas.Children.Add(stackPanel);
			Canvas.SetLeft(stackPanel, origin.X);
			Canvas.SetTop(stackPanel, origin.Y);
			stackPanel.SizeChanged += (sender, _) => StackPanelResized(sender);
		}

		foreach (var part in parts)
        {
            string str = part.Trim();

            // 显式指定类型 (#a:int) (#b:double) (#c:string) 
            // 隐式指定类型 (#d) --> 视作字符串
            if (str.StartsWith('#'))
            {
                var inputTag = str.Split(':', (StringSplitOptions)3);
                if (inputTag.Length != 1 && inputTag.Length != 2) continue;
                (int, int, int) vi = (currentSection, slots, MetaData.Slots + slots);
                ValueIndex.Add(inputTag[0], vi);
                slots += 1;

                // 指定输入框类型，预设为文本
                BlockValueType type = BlockValueType.String;
				if (inputTag.TryGetValue(1, out var typeName))
                {
					type = typeName switch
					{
						"int"    => BlockValueType.Int,
						"double" => BlockValueType.Double,
						_        => BlockValueType.String,
					};
				}

                slotTypes.Add(type);
                if (!IsExpand)
                {
                    TextBox textBox = new()
                    {
                        IsSpellCheckEnabled = false,
                        FontSize = CodeBlock.FontSize,
                        FontFamily = CodeBlock.FontFamily,
                        Margin = new(0, -6, CtrlMargin, 0),
                        ManipulationMode = Microsoft.UI.Xaml.Input.ManipulationModes.None,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        BorderBrush = Colors.Transparent.GetSolidColorBrush(),
                        Style = app.Resources["NoClearButtonTextBoxStyle"] as Style /* 自定义样式 -- 无右侧清空按钮 */
                    };

                    minWidth += textBox.MinWidth + CtrlMargin;
                    stackPanel.Children.Add(textBox);

                    if (type.HasFlag(BlockValueType.Number))
                    {
                        textBox.CornerRadius = new(18);
                        textBox.PlaceholderText = app.Localizer.GetString("Blocks.ValueTypes.Number.PlaceholderText");
                    }
                    else if (type.HasFlag(BlockValueType.String))
                    {
                        textBox.CornerRadius = new(3);
                        textBox.PlaceholderText = app.Localizer.GetString("Blocks.ValueTypes.Text.PlaceholderText");
                    }

                    textBox.TextChanged += (_, _) =>
                    {
                        if (type.HasFlag(BlockValueType.Number))
                        {
                            if (textBox.Text.Length == 0)
                            {
                                // 避免出现空内容
								textBox.Text = "0";
								textBox.SelectionStart = 1;
							}
							else if (textBox.Text.Length > 1 && textBox.Text.StartsWith('0') && !textBox.Text.StartsWith("0."))
                            {
                                // 去除开头的 '0'
                                var cursor = textBox.SelectionStart;
								textBox.Text = textBox.Text.TrimStart('0');
								textBox.SelectionStart = cursor;
							}
                            else CheckIllegalCharacter(textBox, type);
						}

                        if (ValueDictionary.ContainsKey(inputTag[0])) ValueDictionary[inputTag[0]] = textBox.Text;
                        else ValueDictionary.Add(inputTag[0], textBox.Text);
                    };
                }
            }
            else
            {
                textBlock = new()
                {
                    Text = str,
                    FontSize = CodeBlock.FontSize,
                    FontFamily = CodeBlock.FontFamily,
                    Margin = new(0, 0, CtrlMargin, 0),
                    Foreground = Colors.White.GetSolidColorBrush()
                };

                if (IsExpand)
                {
					RootCanvas.Children.Add(textBlock);

					// 设置边距
					Canvas.SetLeft(textBlock, origin.X);
					Canvas.SetTop(textBlock, origin.Y);

					textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
					var textWidth = textBlock.DesiredSize.Width;
					if (textWidth > maxWidth) maxWidth = textWidth;

					double newpartY = 0;
					if (RightBlocks.TryGetValue(MetaData.Slots + slots, out var block, isNullAllowed: false))
					{
						newpartY += block.Size.Height;
					}
					else newpartY += SlotWidth * 3;

					origin.Y += newpartY;
				}
                else
                {
                    minWidth += textBlock.MinWidth + CtrlMargin;
					stackPanel.Children.Add(textBlock);
				}
            }
        }

        // 调整尺寸
        Size size = new();
        size.Width = SlotWidth + maxWidth + CtrlMargin;
        size.Width += (slots > 0) ? SlotHeight : 0;
        size.Height = SlotWidth * 3;
        if (IsExpand && slots > 1) size.Height += SlotWidth * (slots-1) * 3;
        if (false /*占位符: 内部方块高度*/) { }
        else innerHeight = SlotWidth * 3 + SlotHeight;

        metaData.Slots += slots;
        branchData.Slots = slots;
        branchData.BarHeight = size.Height;
        branchData.InnerHeight = innerHeight;
            
        // 宽度取最大值，确保所有内容都保持在边界内
        this.Width = Utils.GetBigger(this.Width, size.Width);
        this.Height += size.Height;
        if (MetaData.Type == BlockType.Process) this.Height += innerHeight;

        SlotTypes = [.. slotTypes];

        if (IsExpand)
        {
			if (ValueDictionary.Count != 0)
			{
				foreach (var key in ValueDictionary.Keys)
				{
					var value = ValueDictionary[key];
					var (_, _, id) = ValueIndex[key];
					var type = SlotTypes[id];

                    CodeBlock tempBlock = type switch
                    {
                        BlockValueType.Int    => ToolBox.CreateBlockFromID("minecraft:int"),
                        BlockValueType.Double => ToolBox.CreateBlockFromID("minecraft:decimal"),
                        _                     => ToolBox.CreateBlockFromID("minecraft:string")
                    };

					BlockCreatedEventArgs args = new(source: this);
					var block = tempBlock.Clone(OnBlockCreated, args);
					block.IsExpand = false;
					block.SetValue(0, 0, value);
					RightBlocks.TrySetValue(id, block);
				}

                ValueDictionary.Clear();
			}
		}
        else
        {
			if (ValueDictionary.Count != 0)
			{
				foreach (var key in ValueDictionary.Keys)
				{
					var value = ValueDictionary[key];
					var (section, index, _) = ValueIndex[key];
					SetValue(section, index, value);
				}
			}
			else if (RightBlocks.Length != 0)
			{
				foreach (var key in ValueIndex.Keys)
				{
					var (section, index, id) = ValueIndex[key];
					if (RightBlocks.TryGetValue(id, out var block, isNullAllowed: false))
					{
						var value = block.GetValue(0, 0);
						ValueDictionary.Add(key, value);
						RightBlocks[id] = null;
						SetValue(section, index, value);
						block.RemoveAsync(block.Parent as Canvas, showAnimation: false);
					}
				}
			}
		}

        stackPanel.MinWidth = minWidth;
        branchDatas.Add(branchData);
        return origin;
    }

    private void ClearAllContent()
    {
		// 移除所有 TextBlock
		var blocks = RootCanvas.Children.OfType<TextBlock>().ToList();
		foreach (var block in blocks) { RootCanvas.Children.Remove(block); }

		// 移除所有 StackPanel
		var panels = RootCanvas.Children.OfType<StackPanel>().ToList();
		foreach (var panel in panels) { RootCanvas.Children.Remove(panel); }

		ValueIndex.Clear();
        metaData.Slots = 0;

        // 重置方块控件的尺寸。此栏目仅用于保存数据，因此可放心操作
        this.Width = 0; this.Height = 0;
    }

    private void SetText(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        ClearAllContent(); // 清空方块内容

		List<BlockValueType> slotTypes = [];
		List<BlockBranchData> branchDatas = [];
		Point origin = new(0, 0);

        if (MetaData.Type == BlockType.Process)
        {
            // 流程块使用 '\' (反斜杠) 分割每个分支
            string[] arr = text.Split('\\', (StringSplitOptions)3);
            if (arr.Length > 1) metaData.Variant |= (byte)((arr.Length-1) << 4);

            int section = 0;
            foreach (var str in arr)
            {
                origin = SetLabelAndInputBox(str, origin, slotTypes, branchDatas, section);
                origin.X = 0;
                origin.Y += SlotWidth * 3;

				// 取得内部方块总高度
				// origin.Y += ...

				section += 1;
			}

            metaData.Parts = [.. branchDatas];
        }
        else SetLabelAndInputBox(text, origin, slotTypes, branchDatas, 0);

        Size size = new(this.Width, this.Height);
        Resize(size);

        if (IsExpand) UpdateSubBlockPosition(); // 更新子方块位置
    }

    // 旧方法，从全局翻译文件取得本地化字串
    private void LocalizeBlock()
    {
        if (string.IsNullOrEmpty(key)) return;
        var text = app.Localizer.GetString(key);
        SetText(text);
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

    private void Resize(Size size)
    {
        // 最小宽度
        if (MetaData.Type == BlockType.Process && size.Width < SlotWidth * 6) size.Width = SlotWidth * 6;
        else if (size.Width < SlotWidth * 3) size.Width = SlotWidth * 3;

        metaData.Size = size;
        this.Width = size.Width;
        this.Height = size.Height;
        painter.MetaData = metaData;
        painter.IsExpand = IsExpand;
        BlockBorder.Data = painter.DrawBlockBorder();
        Array.Resize(ref RightBlocks, metaData.Slots);
    }

    private void CheckIllegalCharacter(TextBox txtbox, BlockValueType type)
    {
        // TeachingTip 不会跟随设置的目标移动，故暫时禁用此行代码
        // BlockTip.Target = txtbox;

        if (type.HasFlag(BlockValueType.String)) BlockTip.IsOpen = false;
        else if (type.HasFlag(BlockValueType.Decimal) && double.TryParse(txtbox.Text, out _)) BlockTip.IsOpen = false;
        else if (type.HasFlag(BlockValueType.Integer) && int.TryParse(txtbox.Text, out _)) BlockTip.IsOpen = false;
        else
        {
            BlockTip.Title = app.Localizer.GetString("Tips.IllegalCharacters");
            BlockTip.Subtitle = app.Localizer.GetString("Tips.UnableToConvertType");
            BlockTip.IsOpen = true;
        }
    }

    #endregion

    #region "Properties"

    public Size Size
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

    public bool IsExpand
    {
        get => isExpand;
        set
        {
            isExpand = value;
            RefreshBlockText();
        }
    }

    public bool CanContextMenuShow
    {
        get => canContextMenuShow;
        set
        {
            canContextMenuShow = value;
            if (canContextMenuShow) this.ContextFlyout = ContextMenu;
            else this.ContextFlyout = null;
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

    public BlockValueType[] SlotTypes;
    public Dictionary<string, string> TranslationsDict;
    public Dictionary<string, (int section, int index, int id)> ValueIndex { get; private set; } = new();
    public Dictionary<string, string> ValueDictionary { get; private set; } = new();

    public int DependentSlot;
    public CodeBlock ParentBlock;
    public CodeBlock BottomBlock;
    public CodeBlock[] RightBlocks = [];

    public bool HasBeenRemoved = false;

    #endregion

    #region "Methods"

    /// <summary>
    /// 刷新方块的本地化文本
    /// </summary>
    public void RefreshBlockText()
    {
        if (TranslationsDict != null && TranslationsDict.Count > 0)
        {
            if (!TranslationsDict.TryGetValue(App.CurrentLanguageId, out string text))
            {
                // 若方块未提供当前选用语言的翻译文本，使用第一个值作为预设。
                text = TranslationsDict.First().Value;
            }

            SetText(text);
            return;
        }

        // fallback
        if (IsInteractionDisabled) return; // 若已禁用交互，并且没有指定翻译字典，则该方块不需要翻译，例如:  GhostBlock
        if (string.IsNullOrEmpty(id)) TranslationKey = "Blocks.Demo";
        else SetText(id);
    }

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
            IsExpand = this.IsExpand,
            BlockColor = this.BlockColor,
            Identifier = this.Identifier,
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
    public void SetData(BlockProperties key, object value, bool updateBorder = true)
    {
        BlockMetaData data = this.MetaData;
        switch (key)
        {
            case BlockProperties.Type:
                data.Type = (BlockType)value;
                if (data.Type == BlockType.Value && IsExpand) IsExpand = false;
                break;
            case BlockProperties.Code:
                data.Code = (string)value;
                updateBorder = false;
                break;
            case BlockProperties.Variant:
                if (value is int n) data.Variant = (byte)n; // 若value为int类型，直接object转byte会崩溃
                else if (value is byte b) data.Variant = b;
                break;
            case BlockProperties.Slots:
                data.Slots = (int)value;
                break;
            case BlockProperties.Size:
                data.Size = (Size)value;
                break;
            case BlockProperties.Width:
                data.Size.Width = (double)value;
                break;
            case BlockProperties.Height:
                data.Size.Height = (double)value;
                break;
            default:
                break;
        }

        if (updateBorder) MetaData = data;
        else metaData = data;
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

        double dx, dy;
        if (isRelative)
        {
            dx = x; dy = y;
            x += Canvas.GetLeft(this);
            y += Canvas.GetTop(this);
        }
        else
        {
            dx = x - Canvas.GetLeft(this);
            dy = y - Canvas.GetTop(this);
        }

        // 移动方块
        Canvas.SetLeft(this, x);
        Canvas.SetTop(this, y);

        // 移动下方方块
        BottomBlock?.SetPosition(dx, dy, isRelative: true);

        // 移动右侧方块
        foreach (var block in RightBlocks)
        {
            block?.SetPosition(dx, dy, isRelative: true);
        }
    }

    /// <summary>
    /// 更新子方块位置
    /// </summary>
    public void UpdateSubBlockPosition()
    {
        double x = Canvas.GetLeft(this), y = Canvas.GetTop(this);

        // 移动下方方块
        BottomBlock?.SetPosition(x, y + Size.Height);

        // 移动右侧方块
        for (int i = 0; i < metaData.Slots; i++)
        {
            if (RightBlocks.TryGetValue(i, out var block, isNullAllowed: false))
            {
                block.SetPosition(x + Size.Width, y + i * SlotWidth * 3);
            }
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
        this.MoveToBlock(endBlock, 0, endBlock.Size.Height);
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
    public async Task PlayScaleAnimationAsync(double from, double to, int delay = 5)
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
    public async Task RemoveAsync(Canvas rootCanvas, bool deleteAll = true, bool showAnimation = true)
    {
        if (rootCanvas == null || HasBeenRemoved) return;
        if (ContextMenu.IsOpen) ContextMenu.Hide();

        // 删除方块的缩小动画
        if (showAnimation) await PlayScaleAnimationAsync(1.0, 0.0);

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
        double targetW = targetBlock.Size.Width;
        double targetH = targetBlock.Size.Height;

        if (self.X + SlotHeight > target.X + targetW - SlotHeight) rq.x = 1;
        if (self.X + Size.Width < target.X + SlotHeight) rq.x = -1;

        if (self.Y + SlotHeight > target.Y + targetH - SlotHeight) rq.y = 1;
        if (self.Y + Size.Height < target.Y + SlotHeight) rq.y = -1;

        if (rq.x > 0) rq.dx = (self.X + Size.Width) - (target.X + targetW);
        else if (rq.x < 0) rq.dx = target.X - self.X;

        if (rq.y > 0) rq.dy = (self.Y + Size.Height) - (target.Y + targetH);
        else if (rq.y < 0) rq.dy = target.Y - self.Y;

        return rq;
    }

    public void SetValue(int section, int index, string value)
    {
        if (IsExpand) return;

		var stackPanels = RootCanvas.Children.OfType<StackPanel>().ToArray();
		if (stackPanels.TryGetValue(section, out var panel))
		{
			var txtboxes = panel.Children.OfType<TextBox>().ToArray();
			if (txtboxes.TryGetValue(index, out var txtbox)) txtbox.Text = value;
		}
    }

    public void SetValue(string[] values)
    {
        if (IsExpand) return;
        var txtboxes = RootCanvas.Children.OfType<TextBox>().ToArray();

        int index = 0;
        foreach (var txtbox in txtboxes)
        {
            if (values.TryGetValue(index, out var value)) txtbox.Text = value;
            else break;
        }
    }

    public string GetValue(int section, int index)
    {
        string result;
        if (IsExpand)
        {
            if (RightBlocks.TryGetValue(index, out var block) && block is not null)
            {
                result = block.GetValue(0, 0);
            }
            else
            {
                result = SlotTypes[index] switch
                {
                    BlockValueType.Int    => "0",
                    BlockValueType.Double => "0.0",
                    _                     => string.Empty
				};
            }
        }
        else
        {
            var stackPanels = RootCanvas.Children.OfType<StackPanel>().ToArray();
            if (stackPanels.TryGetValue(section, out var panel))
            {
				var txtboxes = panel.Children.OfType<TextBox>().ToArray();
				if (txtboxes.TryGetValue(index, out var txtbox)) result = txtbox.Text;
				else return string.Empty;
			}
			else return string.Empty;
		}

        if (SlotTypes[index].HasFlag(BlockValueType.Number))
        {
            if (!double.TryParse(result, out var temp)) temp = 0.0;

            if (SlotTypes[index] == BlockValueType.Int) result = ((int)temp).ToString();
            else result = temp.ToString();
        }
        return result;
    }

    public string GetCode()
    {
        string code = metaData.Code;
        foreach (string variable in ValueIndex.Keys)
        {
            var (section, index, _) = ValueIndex[variable];
            if (IsExpand)
            {
                if (RightBlocks.TryGetValue(index, out var block) && block is not null)
                {
                    string value = block.GetValue(0, 0);
                    code = code.Replace(variable, value);
                }
            }
            else
            {
                string value = this.GetValue(section, index);
                code = code.Replace(variable, value);
            }
        }
        return code;
    }
    
    #endregion

    #region "Static"

    public static readonly int SlotWidth = 16;
    public static readonly int SlotHeight = 8;
	public static readonly int CtrlMargin = 8;
	public static readonly new FontFamily FontFamily = new("/Fonts/HarmonyOS_Sans_B.ttf#HarmonyOS Sans SC");
    public static readonly new int FontSize = 16;

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
