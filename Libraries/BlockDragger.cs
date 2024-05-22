using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Windows.Foundation;
using CodeBlocks.Controls;

namespace CodeBlocks.Core;

public class BlockDragger(Canvas workspace, CodeBlock ghostBlock)
{
    #region "Delegates"

    public delegate Point GetPointDelegate(Point point = default, Size size = default);
    public GetPointDelegate GetTrashCanPosition;
    public GetPointDelegate GetCenterPosition;

    #endregion

    #region "Properties"

    public Func<CodeBlock, Task> RemoveBlock;
    public Canvas Workspace { get; private set; } = workspace;
    public CodeBlock GhostBlock { get; private set; } = ghostBlock;

    #endregion

    #region "Methods"

    private bool IsContactedWithTrashCan(CodeBlock thisBlock, Point self, Size size)
    {
        // 计算中心点距离
        var trashCanCenter = GetTrashCanPosition();
        var selfCenter = GetCenterPosition(self, size);
        var xDiff = trashCanCenter.X - selfCenter.X;
        var yDiff = trashCanCenter.Y - selfCenter.Y;

        var distance = Math.Sqrt(xDiff * xDiff + yDiff * yDiff);

        // 距离小于 取较大值(宽, 高) 的三分之一 --> 方块已和垃圾桶重叠;
        var threshold = Utils.GetBigger(size.Width, size.Height) / 3;
        if (distance < threshold) { RemoveBlock(thisBlock); return true; }
        else return false;
    }

    public void CheckCollisions(CodeBlock thisBlock)
    {
        var self = new Point(Canvas.GetLeft(thisBlock), Canvas.GetTop(thisBlock));
        var size = thisBlock.Size;
        bool isAligned = false;

        // 若接触垃圾桶直接结束
        if (IsContactedWithTrashCan(thisBlock, self, size)) return;

        // 碰撞检查
        foreach (var uiElement in Workspace.Children)
        {
            if (uiElement == GhostBlock || uiElement == thisBlock ) continue; // 跳过特定目标

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
                int slot = (int)((self.Y - target.Y) / 48);
                if (dx > 0 && (dx - size.Width + CodeBlock.SlotHeight) < threshold)
                {
                    self.X -= (dx - size.Width + CodeBlock.SlotHeight) * x;
                    self.Y = target.Y + slot * 48;
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
                    GhostBlock.SetPosition(self.X, self.Y);
                    GhostBlock.Visibility = Visibility.Visible;
                    if (thisBlock.DependentSlot == -1)
                    {
                        ResetGhostBlock(hide: false);
                        GhostBlock.ParentBlock = targetBlock.BottomBlock;
                        targetBlock.BottomBlock?.MoveToBack(GhostBlock, false);
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
        if (hide) GhostBlock.Visibility = Visibility.Collapsed;

        // 复原暫时移动的块
        if (GhostBlock.ParentBlock != null)
        {
            GhostBlock.ParentBlock.ReturnToPreviousPosition();
            GhostBlock.ParentBlock = null;
        }
    }

    #endregion
}
