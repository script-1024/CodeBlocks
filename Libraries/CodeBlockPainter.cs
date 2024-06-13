using System;
using Windows.Foundation;
using CodeBlocks.Controls;
using Microsoft.UI.Xaml.Media;

namespace CodeBlocks.Core
{
    public enum BlockType { Undefined = 0, Event = 1, Process = 2, Action = 3, Value = 4 }

    public struct BlockBranchData
    {
        public int Slots;
        public double BarHeight, InnerHeight;
    }

    public struct BlockMetaData
    {
        public BlockType Type;
        public string Code;
        public byte Variant;
        public int Slots;
        public Size Size;
        public BlockBranchData[] Parts;
        public static readonly BlockMetaData Null = new()
        {
            Code = "", Type = 0, Variant = 0, Slots = 0,
            Size = default, Parts = []
        };
    }

    public class CodeBlockPainter
    {
        public bool IsExpand;
        public BlockMetaData MetaData;

        private double x, y;
        private double blockWidth, blockHeight;
        private readonly int w = CodeBlock.SlotWidth;
        private readonly int h = CodeBlock.SlotHeight;
        private PathFigure pathFigure;

        private void DrawTopOrDownCurve(int sign)
        {
            // Sign : Top 1 | Down -1
            LineSegment line1 = new() { Point = new Point(x, y += h) };
            pathFigure.Segments.Add(line1);

            LineSegment line2 = new() { Point = new Point(x += w * sign, y) };
            pathFigure.Segments.Add(line2);

            LineSegment line3 = new() { Point = new Point(x, y -= h) };
            pathFigure.Segments.Add(line3);
        }
        // 0 1
        private void DrawLeftOrRightCurve(int sign)
        {
            // Sign : Left -1 | Right 1
            LineSegment line1 = new() { Point = new Point(x -= h, y) };
            pathFigure.Segments.Add(line1);

            LineSegment line2 = new() { Point = new Point(x, y += w * sign) };
            pathFigure.Segments.Add(line2);

            LineSegment line3 = new() { Point = new Point(x += h, y) };
            pathFigure.Segments.Add(line3);
        }

        /// <summary>
        /// 绘制直线
        /// </summary>
        /// <param name="dx">水平变化量</param>
        /// <param name="dy">垂直变化量</param>
        /// <param name="newX">赋给 x 的新值</param>
        /// <param name="newY">赋给 y 的新值</param>
        private void DrawLine(double? newX = null, double? newY = null, double dx = 0, double dy = 0)
        {
            LineSegment line = new();
            x = (newX != null) ? (double)newX : x;
            y = (newY != null) ? (double)newY : y;

            line.Point = new Point(x += dx, y += dy);
            pathFigure.Segments.Add(line);
        }

        public PathGeometry DrawBlockBorder()
        {
            blockWidth = MetaData.Size.Width;
            blockHeight = MetaData.Size.Height;
            int slots = MetaData.Slots;

            // Only used for process blocks, always be zero for otherwise
            // 分支数量，用于循环(for/while)、判断(if-then-else)、切换(switch-case-default)、尝试(try-catch-finally)等类型的方块
            int branchCount = (MetaData.Type == BlockType.Process) ? MetaData.Variant >> 4 : 0;

            bool hasLeft = MetaData.Variant.HasFlag(0b_0001);
            bool hasTop = MetaData.Variant.HasFlag(0b_0010);
            bool hasRight = MetaData.Variant.HasFlag(0b_0100);
            bool hasBottom = MetaData.Variant.HasFlag(0b_1000);
            var pathGeo = new PathGeometry();

            // 从左上角开始
            pathFigure = new() { StartPoint = new Point(x = 0, y = 0) };

            // 上边
            if (MetaData.Type == BlockType.Event)
            {
                // 事件块的外观要特别处理
                ArcSegment arc = new()
                {
                    Size = new Size(35, 24),
                    Point = new Point(x += 60, y = 0),
                    SweepDirection = SweepDirection.Clockwise
                };
                pathFigure.Segments.Add(arc);
            }
            else
            {
                if (hasTop)
                {
                    DrawLine(dx: w); // 左半部分
                    DrawTopOrDownCurve(1); // 凹口
                }
            }
            DrawLine(newX: blockWidth - h); // 其余部分

            // 右边
            if (MetaData.Type == BlockType.Process)
            {
                // 流程块的外观要特别处理
                for (int i=0; i<=branchCount; i++)
                {
                    if (!MetaData.Parts.TryGetValue(i, out var part))
                        part = new() { Slots = 0, BarHeight = w * 3, InnerHeight = w * 3 + h };

                    var barH = part.BarHeight;
                    var innerH = part.InnerHeight;
                    var pSlots = part.Slots;

                    if (IsExpand && pSlots > 0)
                    {
                        while (pSlots > 0)
                        {
                            DrawLine(dy: w); // 上半部分
                            DrawLeftOrRightCurve(1); // 凹口
                            DrawLine(dy: w);
                            pSlots--;
                        }
                    }
                    else DrawLine(dy: barH);

                    // 内部上边
                    DrawLine(newX: w * 5);  // 右半部分
                    DrawTopOrDownCurve(-1); // 凸起
                    DrawLine(newX: w * 3);  // 其余部分

                    // 内部左边
                    DrawLine(dy: innerH); // 下半部分

                    // 内部下边
                    DrawLine(newX: blockWidth - h);
                }

                // 右侧下方其余部分
                DrawLine(dy: w);
            }
            else
            {
                if (hasRight)
                {
                    if (IsExpand && slots > 0)
                    {
                        while (slots > 0)
                        {
                            DrawLine(dy: w); // 上半部分
                            DrawLeftOrRightCurve(1); // 凹口
                            DrawLine(dy: w);
                            slots--;
                        }
                    }
                    else DrawLine(newY: blockHeight); // 其余部分
                }
                else
                {
                    // 后续版本才会加入方块省略状态
                    // 此时不显示右侧方块，且不允许添加/删除子方块，右侧无法互动
                }
            }

            // 下边
            if (hasBottom)
            {
                DrawLine(newX: w * 2); // 右半部分
                DrawTopOrDownCurve(-1); // 凸起
            }
            DrawLine(newX: 0); // 其余部分

            // 左边
            if (hasLeft)
            {
                DrawLine(newY: w * 2); // 下半部分
                DrawLeftOrRightCurve(-1); // 凸起
            }
            DrawLine(newY: 0); // 其余部分

            pathGeo.Figures.Add(pathFigure);
            return pathGeo;
        }
    }
}
