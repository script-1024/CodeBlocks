using Windows.Foundation;
using CodeBlocks.Controls;
using Microsoft.UI.Xaml.Media;

namespace CodeBlocks.Core
{
    public enum BlockType
    {
        Undefined = 0, Event = 1, Control = 2, Action = 3, Value = 4
    }

    public struct BlockMetaData
    {
        public BlockType Type;
        public byte Variant;
        public string Code;
        public int Slots;
        public Size Size;
        public static readonly BlockMetaData Null = new() { Type = 0, Code = "", Variant = 0, Slots = 0, Size = default };
    }

    public class CodeBlockPainter
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public BlockMetaData MetaData;

        private double x, y;
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

        private void DrawLine(double dx = 0, double dy = 0, double? newX = null, double? newY = null, bool isRelative = true)
        {
            LineSegment line = new();

            if (isRelative)
            { 
                x += dx;
                y += dy;
            }
            else
            {
                if (newX != null) x = (double)newX;
                if (newY != null) y = (double)newY;
            }

            line.Point = new Point(x, y);
            pathFigure.Segments.Add(line);
        }

        public PathGeometry DrawBlockBorder()
        {
            Width = MetaData.Size.Width;
            Height = MetaData.Size.Height;
            int slots = MetaData.Slots;
            bool hasLeft = MetaData.Variant.CheckIfContain(0b_0001);
            bool hasTop = MetaData.Variant.CheckIfContain(0b_0010);
            bool hasRight = MetaData.Variant.CheckIfContain(0b_0100);
            bool hasBottom = MetaData.Variant.CheckIfContain(0b_1000);
            var pathGeo = new PathGeometry();

            // 从左上角开始
            pathFigure = new() { StartPoint = new Point(x = 0, y = 0) };

            // 上边
            if (MetaData.Type == BlockType.Action)
            {
                if (hasTop)
                {
                    DrawLine(dx: w); // 左半部分
                    DrawTopOrDownCurve(1); // 凹口
                }
            }
            else if (MetaData.Type == BlockType.Event)
            {
                ArcSegment arc = new() {
                    Size = new Size(35, 24),
                    Point = new Point(x += 60, y = 0),
                    SweepDirection = SweepDirection.Clockwise
                };
                pathFigure.Segments.Add(arc);
            }
            DrawLine(newX: Width - h, isRelative: false); // 其余部分

            // 右边
            if (hasRight)
            {
                DrawLine(dy: w); // 上半部分
                while (slots >= 1)
                {
                    DrawLeftOrRightCurve(1); // 凹口
                    if (--slots >= 1) DrawLine(0, w*2);
                }
            }
            DrawLine(newY: Height - h, isRelative: false); // 其余部分

            // 下边
            if (hasBottom && MetaData.Type != BlockType.Value)
            {
                DrawLine(newX: w * 2, isRelative: false); // 右半部分
                DrawTopOrDownCurve(-1); // 凸起
            }
            DrawLine(newX: 0, isRelative: false); // 其余部分

            // 左边
            if (hasLeft)
            {
                DrawLine(newY: w * 2, isRelative: false); // 下半部分
                DrawLeftOrRightCurve(-1); // 凸起
            }

            if (MetaData.Type == BlockType.Value || MetaData.Type == BlockType.Action)
            {
                DrawLine(newY: 0, isRelative: false); // 其余部分
            }
            else if (MetaData.Type == BlockType.Event)
            {
                DrawLine(newY: 0, isRelative: false); // 其余部分
            }

            pathGeo.Figures.Add(pathFigure);
            return pathGeo;
        }
    }
}
