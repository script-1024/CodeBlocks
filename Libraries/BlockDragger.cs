using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;
using CodeBlocks.Controls;

namespace CodeBlocks.Core;

public class BlockDragger(Canvas workspace, ScrollViewer scroller, CodeBlock ghostBlock)
{

    #region "Properties"

    private CodeBlock focusBlock;
    public CodeBlock FocusBlock
    {
        get => focusBlock;
        set { focusBlock = value; FocusChanged?.Invoke(); }
    }

    #endregion

    #region "Fields"

    // 方块焦点改变事件
    public delegate void FocusChangedHandler();

    /// <summary>
    /// 选中方块更变时将引发此事件
    /// </summary>
    public event FocusChangedHandler FocusChanged;

    /// <summary>
    /// 用于取得垃圾桶的中心坐标，由外部指定
    /// </summary>
    public Func<Point> GetTrashCanPosition;

    /// <summary>
    /// 删除方块的处理程序，由外部指定
    /// </summary>
    public Action<CodeBlock> RemoveBlock;

    #endregion

    #region "Methods"

    /// <summary>
    /// 将控件坐标由工作区变换至窗口空间
    /// </summary>
    /// <returns>控件对应于窗口空间的左上角坐标。若指定了控件的尺寸，则改为中心点坐标</returns>
    public Point TransformPositionFromWorkspaceToWindow(Point position, Size? size = null)
    {
        var point = new Point()
        {
            X = (position.X * scroller.ZoomFactor - scroller.HorizontalOffset),
            Y = (position.Y * scroller.ZoomFactor - scroller.VerticalOffset)
        };

        if (size != null)
        {
            point.X += (size.Value.Width * scroller.ZoomFactor) / 2;
            point.Y += (size.Value.Height * scroller.ZoomFactor) / 2;
        }

        return point;
    }

    /// <summary>
    /// 将控件坐标由窗口空间变换至工作区
    /// </summary>
    /// <returns>控件对应于工作区的左上角坐标。若指定了控件的尺寸，则改为中心点坐标</returns>
    public Point TransformPositionFromWindowToWorkspace(Point position, Size? size = null)
    {
        var point = new Point()
        {
            X = (position.X + scroller.HorizontalOffset / scroller.ZoomFactor),
            Y = (position.Y + scroller.VerticalOffset / scroller.ZoomFactor)
        };

        if (size != null)
        {
            point.X += (size.Value.Width * scroller.ZoomFactor) / 2;
            point.Y += (size.Value.Height * scroller.ZoomFactor) / 2;
        }

        return point;
    }

    /// <summary>
    /// 检查方块是否已接触到垃圾桶
    /// </summary>
    private bool? IsContactedWithTrashCan(CodeBlock thisBlock, Point position, Size size)
    {
        // 未指定必要的函数，无法继续计算，直接返回
        if (GetTrashCanPosition is null || RemoveBlock is null) return null;

        // 计算中心点距离
        var trashCanCenter = GetTrashCanPosition();
        var selfCenter = TransformPositionFromWorkspaceToWindow(position, size);
        var xDiff = trashCanCenter.X - selfCenter.X;
        var yDiff = trashCanCenter.Y - selfCenter.Y;

        var distance = Math.Sqrt(xDiff * xDiff + yDiff * yDiff);

        // 距离小于 取较大值(宽, 高) 的三分之一 --> 方块已和垃圾桶重叠;
        var threshold = Utils.GetBigger(size.Width, size.Height) / 3;
        if (distance < threshold)
        {
            FocusBlock = null;
            RemoveBlock(thisBlock);
            return true;
        }
        else return false;
    }

    /// <summary>
    /// 检查指定方块和其他物件的重叠状态
    /// </summary>
    public void CheckCollisions(CodeBlock thisBlock)
    {
        var self = new Point(Canvas.GetLeft(thisBlock), Canvas.GetTop(thisBlock));
        var size = thisBlock.Size;
        bool isAligned = false;

        // 若判断与垃圾桶的接触状况为 ‘是’ 或 ‘未知’ 则直接结束
        if (IsContactedWithTrashCan(thisBlock, self, size) != false) return;

        // 碰撞检查
        foreach (var uiElement in workspace.Children)
        {
            if (uiElement == ghostBlock || uiElement == thisBlock ) continue; // 跳过特定目标

            var target = new Point(Canvas.GetLeft(uiElement), Canvas.GetTop(uiElement));
            if (uiElement is CodeBlock targetBlock)
            {
                // 获取自身的相对方位
                var (x, y, dx, dy) = thisBlock.GetRelativeQuadrant(targetBlock);

                // 在四个角落 不和目标相邻 --> 跳过
                if (x * y != 0)
                {
                    ResetGhostBlock();
                    continue;
                }

                // 取得方块资料值
                int selfVar = thisBlock.MetaData.Variant;
                int targetVar = targetBlock.MetaData.Variant;

                // 在左侧时不可吸附 --> 跳过
                if (x == -1) continue;

                // 在上方时不可吸附 --> 跳过
                if (y == -1) continue;

                // 在右侧时不可吸附 --> 跳过
                if (x == 1 && (!Utils.GetFlag(targetVar, 2) || !Utils.GetFlag(selfVar, 0))) continue;

                // 在下方时不可吸附 --> 跳过
                if (y == 1 && (!Utils.GetFlag(targetVar, 3) || !Utils.GetFlag(selfVar, 1))) continue;

                // 开始尝试自动吸附
                int threshold = 30;
                int slot = (int)((self.Y - target.Y) / (CodeBlock.SlotWidth * 3));
                if (dx > 0 && (dx - size.Width + CodeBlock.SlotHeight) < threshold)
                {
                    self.X -= (dx - size.Width + CodeBlock.SlotHeight) * x;
                    self.Y = target.Y + slot * (CodeBlock.SlotWidth * 3);
                    thisBlock.DependentSlot = slot + 1;
                    isAligned = true;
                }
                else if (dy > 0 && (dy - size.Height + CodeBlock.SlotHeight) < threshold)
                {
                    self.Y -= (dy - size.Height + CodeBlock.SlotHeight) * y;
                    self.X = target.X;
                    thisBlock.DependentSlot = -1;
                    isAligned = true;
                }

                if (isAligned)
                {
                    thisBlock.ParentBlock = targetBlock;
                    ghostBlock.SetPosition(self.X, self.Y);
                    ghostBlock.Visibility = Visibility.Visible;
                    if (thisBlock.DependentSlot == -1)
                    {
                        ResetGhostBlock(hide: false);
                        ghostBlock.ParentBlock = targetBlock.BottomBlock;
                        targetBlock.BottomBlock?.MoveToBack(ghostBlock, false);
                    }
                    return;
                }
                else
                {
                    thisBlock.ParentBlock = null;
                    ResetGhostBlock();
                }
            }
        }
    }

    public void ResetGhostBlock(bool hide = true)
    {
        if (hide) ghostBlock.Visibility = Visibility.Collapsed;

        // 复原暫时移动的块
        if (ghostBlock.ParentBlock != null)
        {
            ghostBlock.ParentBlock.ReturnToLastRecordedPosition();
            ghostBlock.ParentBlock = null;
        }
    }

    #endregion

    #region "Events"

    /// <summary>
    /// 方块被创建时引发的事件
    /// </summary>
    public void BlockCreated(CodeBlock block, BlockCreatedEventArgs e)
    {
        e ??= BlockCreatedEventArgs.Null;

        workspace.Children.Add(block);
        Canvas.SetLeft(block, e.Position.X);
        Canvas.SetTop(block, e.Position.Y);

        block.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
        block.ManipulationStarted += BlockManipulationStarted;
        block.ManipulationDelta += BlockManipulationDelta;
        block.ManipulationCompleted += BlockManipulationCompleted;
    }

    public void BlockManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
    {
        ResetGhostBlock();
        var thisBlock = sender as CodeBlock;
        ghostBlock.CopyDataFrom(thisBlock);
        thisBlock.SetZIndex(+5, true);

        FocusBlock = thisBlock;

        ghostBlock.Size = thisBlock.Size;
        var endBlock = thisBlock.BottomBlock;
        while (endBlock != null)
        {
            ghostBlock.SetData(BlockProperties.Height, ghostBlock.Size.Height + endBlock.Size.Height - CodeBlock.SlotHeight);
            endBlock = endBlock.BottomBlock;
        }

        foreach (var block in thisBlock.RightBlocks)
        {
            if (block != null)
            {
                // 第一个非null的方块
                ghostBlock.SetData(BlockProperties.Width, ghostBlock.Size.Width + block.Size.Width - CodeBlock.SlotHeight);
                ghostBlock.SetData(BlockProperties.Variant, ghostBlock.MetaData.Variant ^ 0b0100);
                break;
            }
        }

        var parentBlock = thisBlock.ParentBlock;
        if (parentBlock != null)
        {
            thisBlock.ParentBlock = null;
            if (thisBlock.DependentSlot == -1) { parentBlock.BottomBlock = null; }
            else if (thisBlock.DependentSlot > 0) { parentBlock.RightBlocks[thisBlock.DependentSlot - 1] = null; }
        }

        thisBlock.DependentSlot = 0;
    }

    public void BlockManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
    {
        if (FocusBlock is null) return;
        var thisBlock = sender as CodeBlock;

        double newX = Canvas.GetLeft(thisBlock) + e.Delta.Translation.X / scroller.ZoomFactor;
        double newY = Canvas.GetTop(thisBlock) + e.Delta.Translation.Y / scroller.ZoomFactor;
        var maxX = workspace.ActualWidth - thisBlock.Size.Width;
        var maxY = workspace.ActualHeight - thisBlock.Size.Height;

        // 边界检查
        newX = (newX < 0) ? 0 : (newX > maxX) ? maxX : newX;
        newY = (newY < 0) ? 0 : (newY > maxY) ? maxY : newY;

        // 移动方块
        thisBlock.SetPosition(newX, newY);

        // 碰撞检查
        CheckCollisions(thisBlock);
    }

    public void BlockManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
    {
        FocusBlock = null;

        var thisBlock = sender as CodeBlock;
        thisBlock.SetZIndex(0, false);

        if (ghostBlock.Visibility == Visibility.Visible)
        {
            // 移动到示意矩形的位置
            thisBlock.MoveToBlock(ghostBlock);
            ResetGhostBlock();

            // 移动其他子块
            var parentBlock = thisBlock.ParentBlock;
            if (thisBlock.DependentSlot == -1) // 自己在下方
            {
                if (parentBlock.BottomBlock == null) { parentBlock.BottomBlock = thisBlock; }
                else parentBlock.BottomBlock.MoveToBack(thisBlock);
            }
            else if (thisBlock.DependentSlot > 0) // 自己在右侧
            {
                var right = parentBlock.RightBlocks;
                var targetSlot = thisBlock.DependentSlot - 1;

                // 若位置已被其他块占据就将其弹出
                if (right[targetSlot] != thisBlock) right[targetSlot]?.PopUp();
                right[targetSlot] = thisBlock;
            }
        }
    }

    #endregion
}
