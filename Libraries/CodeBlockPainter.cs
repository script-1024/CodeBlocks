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
        public string Content;
        public byte Variant;
        public int Slots;
        public Size Size;
        public static readonly BlockMetaData Null = new() { Type = 0, Content = "", Variant = 0, Slots = 0, Size = Size.Zero };
    }

    public struct Size(int w, int h)
    {
        public static readonly Size Zero = new(0, 0);
        public int Width = w;
        public int Height = h;

        public static implicit operator Point(Size size) => new(size.Width, size.Height);
        public static implicit operator Size((int w, int h) size) => new(size.w, size.h);
        public static implicit operator (int Width, int Height)(Size size) => (size.Width, size.Height);
        public static implicit operator Windows.Foundation.Size(Size size) => new(size.Width, size.Height);
        public static explicit operator Size(Windows.Foundation.Size size) => new((int)size.Width, (int)size.Height);
    }

    public class CodeBlockPainter
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public BlockMetaData MetaData;

        private int x, y;
        private readonly int w = CodeBlock.SlotWidth;
        private readonly int h = CodeBlock.SlotHeight;
        private PathFigure pathFigure;

        private void DrawTopOrDownCurve(int sign, int dir = 0)
        {
            // Sign : Top 1 | Down -1
            LineSegment line1 = new() { Point = new Point(x, y += h) };
            pathFigure.Segments.Add(line1);

            LineSegment line2 = new() { Point = new Point(x += w * sign, y) };
            pathFigure.Segments.Add(line2);

            LineSegment line3 = new() { Point = new Point(x, y -= h) };
            pathFigure.Segments.Add(line3);
        }

        private void DrawLeftOrRightCurve(int sign, int dir = 0)
        {
            // Sign : Left -1 | Right 1
            LineSegment line1 = new() { Point = new Point(x -= h, y) };
            pathFigure.Segments.Add(line1);

            LineSegment line2 = new() { Point = new Point(x, y += w * sign) };
            pathFigure.Segments.Add(line2);

            LineSegment line3 = new() { Point = new Point(x += h, y) };
            pathFigure.Segments.Add(line3);
        }

        private void DrawLine(int dx, int dy, bool relative = true)
        {
            LineSegment line = new();
            if (relative) line.Point = new Point(x += dx, y += dy);
            else line.Point = new Point(x = dx, y = dy);
            pathFigure.Segments.Add(line);
        }

        public PathGeometry DrawBlockBorder()
        {
            Width = MetaData.Size.Width;
            Height = MetaData.Size.Height;
            int slots = MetaData.Slots;
            bool hasLeft = Utils.GetFlag(MetaData.Variant, 0);
            bool hasTop = Utils.GetFlag(MetaData.Variant, 1);
            bool hasRight = Utils.GetFlag(MetaData.Variant, 2);
            bool hasBottom = Utils.GetFlag(MetaData.Variant, 3);
            var pathGeo = new PathGeometry();

            // 从左上角开始
            if (MetaData.Type == BlockType.Value || MetaData.Type == BlockType.Action) { x = h; y = 0; }
            else if (MetaData.Type == BlockType.Event) { x = h; y = 0; }

            pathFigure = new() { StartPoint = new Point(x, y) };

            // 上边
            if (MetaData.Type == BlockType.Action)
            {
                if (hasTop)
                {
                    DrawLine(w, 0); // 左半部分
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
            DrawLine(Width, y, false); // 其余部分

            // 右边
            if (hasRight)
            {
                DrawLine(0, w); // 上半部分
                while (slots >= 1)
                {
                    DrawLeftOrRightCurve(1); // 凹口
                    if (--slots >= 1) DrawLine(0, w*2);
                }
            }
            DrawLine(x, Height - h, false); // 其余部分

            // 下边
            if (hasBottom && MetaData.Type != BlockType.Value)
            {
                DrawLine(h + w * 2, y, false); // 右半部分
                DrawTopOrDownCurve(-1, 1); // 凸起
            }
            DrawLine(h, y, false); // 其余部分

            // 左边
            if (hasLeft)
            {
                DrawLine(x, w * 2, false); // 下半部分
                DrawLeftOrRightCurve(-1, 1); // 凸起
            }

            if (MetaData.Type == BlockType.Value || MetaData.Type == BlockType.Action)
            {
                DrawLine(x, 0, false); // 其余部分
            }
            else if (MetaData.Type == BlockType.Event)
            {
                DrawLine(x, 0, false); // 其余部分
            }

            pathGeo.Figures.Add(pathFigure);
            return pathGeo;
        }
    }
}
