using CodeBlocks.Controls;
using Windows.Foundation;
using Microsoft.UI.Xaml.Media;

namespace CodeBlocks.Core
{
    public enum BlockType
    {
        Undefined = 0, ValueBlock = 1, HatBlock = 2, ProcessBlock = 3, BranchBlock = 4
    }

    public struct BlockMetaData
    {
        public BlockType Type;
        public string Content;
        public int Variant;
        public int Slots;
        public (int Width, int Height) Size;
        public static readonly BlockMetaData Null = new() { Type = 0, Content = "", Variant = 0, Slots = 0, Size = (0, 0) };
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
            //Sign : Top 1 | Down -1

            LineSegment line1 = new LineSegment();
            line1.Point = new Point(x, y += h);
            pathFigure.Segments.Add(line1);

            LineSegment line2 = new LineSegment();
            line2.Point = new Point(x += w * sign, y);
            pathFigure.Segments.Add(line2);

            LineSegment line3 = new LineSegment();
            line3.Point = new Point(x, y -= h);
            pathFigure.Segments.Add(line3);
        }

        private void DrawLeftOrRightCurve(int sign, int dir = 0)
        {
            //Sign : Left -1 | Right 1

            LineSegment line1 = new LineSegment();
            line1.Point = new Point(x -= h, y);
            pathFigure.Segments.Add(line1);

            LineSegment line2 = new LineSegment();
            line2.Point = new Point(x, y += w * sign);
            pathFigure.Segments.Add(line2);

            LineSegment line3 = new LineSegment();
            line3.Point = new Point(x += h, y);
            pathFigure.Segments.Add(line3);
        }

        private void DrawLine(int dx, int dy, bool relative = true)
        {
            LineSegment line = new LineSegment();
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
            if (MetaData.Type == BlockType.ValueBlock || MetaData.Type == BlockType.ProcessBlock) { x = h; y = 0; }
            else if (MetaData.Type == BlockType.HatBlock) { x = h; y = 0; }

            pathFigure = new PathFigure();
            pathFigure.StartPoint = new Point(x, y);

            // 上边
            if (MetaData.Type == BlockType.ProcessBlock)
            {
                if (hasTop)
                {
                    DrawLine(w, 0); // 左半部分
                    DrawTopOrDownCurve(1); // 凹口
                }
            }
            else if (MetaData.Type == BlockType.HatBlock)
            {
                ArcSegment arc = new ArcSegment();
                arc.Size = new Size(35, 24);
                arc.Point = new Point(x += 60, y = 0);
                arc.SweepDirection = SweepDirection.Clockwise;
                pathFigure.Segments.Add(arc);
            }
            DrawLine(Width, y, false); // 其余部分

            // 右边
            //DrawCorner(1, 1); // 右上角
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
            //DrawCorner(-1, 1); // 右下角
            if (hasBottom && MetaData.Type != BlockType.ValueBlock)
            {
                DrawLine(h + w * 2, y, false); // 右半部分
                DrawTopOrDownCurve(-1, 1); // 凸起
            }
            DrawLine(h, y, false); // 其余部分

            // 左边
            //DrawCorner(-1, -1); // 左下角
            if (hasLeft)
            {
                DrawLine(x, w * 2, false); // 下半部分
                DrawLeftOrRightCurve(-1, 1); // 凸起
            }

            if (MetaData.Type == BlockType.ValueBlock || MetaData.Type == BlockType.ProcessBlock)
            {
                DrawLine(x, 0, false); // 其余部分
                //DrawCorner(1, -1); // 左上角
            }
            else if (MetaData.Type == BlockType.HatBlock)
            {
                DrawLine(x, 0, false); // 其余部分
            }

            pathGeo.Figures.Add(pathFigure);
            return pathGeo;
        }

    }
}
