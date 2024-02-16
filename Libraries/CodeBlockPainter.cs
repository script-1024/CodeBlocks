using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System.Diagnostics.CodeAnalysis;
using Windows.Foundation;
using Windows.Graphics.Printing;

namespace CodeBlocks.Core
{
    public enum BlockType
    {
        Undefined = 0, ValueBlock = 1, HatBlock = 2, StackBlock = 3, SurroundBlock = 4, CapBlock = 5
    }

    public struct BlockMetaData
    {
        public BlockType Type;
        public string Content;
        public int Variant;
        public int Slots;
        public (int Width, int Height) Size;
        public static readonly BlockMetaData Null = new BlockMetaData() { Type = 0, Content = "", Variant = 0, Slots = 0, Size = (0, 0) };

        public override readonly bool Equals([NotNullWhen(true)] object obj) => (obj is BlockMetaData other) ? this == other : false;
        public override readonly int GetHashCode() => base.GetHashCode();

        public static bool operator ==(BlockMetaData left, BlockMetaData right)
        {
            if (left.Type != right.Type) return false;
            if (left.Variant != right.Variant) return false;
            if (left.Size.Width != right.Size.Width) return false;
            if (left.Size.Height != right.Size.Height) return false;
            return true;
        }

        public static bool operator !=(BlockMetaData left, BlockMetaData right) => !(left == right);
    }

    public class CodeBlockPainter
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public BlockMetaData MetaData;

        private int x, y;
        private int w = 18;
        private int h = 12;
        private int r = 5;
        private PathFigure pathFigure;

        private void DrawTopOrDownCurve(int sign, int dir = 0)
        {
            //Sign : Top 1 | Down -1

            LineSegment line1 = new LineSegment();
            line1.Point = new Point(x, y += h - r);
            pathFigure.Segments.Add(line1);

            ArcSegment arc1 = new ArcSegment();
            arc1.Point = new Point(x + r * sign, y += r);
            arc1.Size = new Size(r, r);
            arc1.SweepDirection = (SweepDirection)dir;
            pathFigure.Segments.Add(arc1);

            LineSegment line2 = new LineSegment();
            line2.Point = new Point(x += (w - r) * sign, y);
            pathFigure.Segments.Add(line2);

            ArcSegment arc2 = new ArcSegment();
            arc2.Point = new Point(x += r * sign, y - r);
            arc2.Size = new Size(r, r);
            arc2.SweepDirection = (SweepDirection)dir;
            pathFigure.Segments.Add(arc2);

            LineSegment line3 = new LineSegment();
            line3.Point = new Point(x, y -= h);
            pathFigure.Segments.Add(line3);
        }

        private void DrawLeftOrRightCurve(int sign, int dir = 0)
        {
            //Sign : Left -1 | Right 1

            LineSegment line1 = new LineSegment();
            line1.Point = new Point(x -= h - r, y);
            pathFigure.Segments.Add(line1);

            ArcSegment arc1 = new ArcSegment();
            arc1.Point = new Point(x -= r, y + r * sign);
            arc1.Size = new Size(r, r);
            arc1.SweepDirection = (SweepDirection)dir;
            pathFigure.Segments.Add(arc1);

            LineSegment line2 = new LineSegment();
            line2.Point = new Point(x, y += (w - r) * sign);
            pathFigure.Segments.Add(line2);

            ArcSegment arc2 = new ArcSegment();
            arc2.Point = new Point(x + r, y += r * sign);
            arc2.Size = new Size(r, r);
            arc2.SweepDirection = (SweepDirection)dir;
            pathFigure.Segments.Add(arc2);

            LineSegment line3 = new LineSegment();
            line3.Point = new Point(x += h, y);
            pathFigure.Segments.Add(line3);
        }

        private void DrawCorner(int signX, int signY)
        {
            /* Sign
             *  +x, +y
             *  -x, +y
             *  -x, -y
             *  +x, -y
             */

            ArcSegment arc = new ArcSegment();
            arc.Size = new Size(r, r);
            arc.Point = new Point(x += r * signX, y += r * signY);
            arc.SweepDirection = SweepDirection.Clockwise;
            pathFigure.Segments.Add(arc);
        }

        private void DrawLine(int dx, int dy, bool relative = true)
        {
            LineSegment line = new LineSegment();
            if (relative) line.Point = new Point(x += dx, y += dy);
            else line.Point = new Point(x = dx, y = dy);
            pathFigure.Segments.Add(line);
        }

        public void SetData(BlockMetaData metaData)
        {
            MetaData = metaData;
            //Width = metaData.Size.Width;
            //Height = metaData.Size.Height;
        }

        public PathGeometry DrawBlockBorder()
        {
            if (MetaData.Type == BlockType.StackBlock) return DrawStackBlock();
            else return null;
        }

        public PathGeometry DrawBlockBorder(BlockMetaData metaData)
        {
            SetData(metaData);
            return DrawBlockBorder();
        }

        public PathGeometry DrawStackBlock()
        {
            x = h + r; y = 0;
            Width = MetaData.Size.Width;
            Height = MetaData.Size.Height;
            int slots = MetaData.Slots;
            bool hasLeft = Utils.GetFlag(MetaData.Variant, 0);
            bool hasTop = Utils.GetFlag(MetaData.Variant, 1);
            bool hasRight = Utils.GetFlag(MetaData.Variant, 2);
            bool hasBottom = Utils.GetFlag(MetaData.Variant, 3);
            var pathGeo = new PathGeometry();

            // 从左上角开始 
            pathFigure = new PathFigure();
            pathFigure.StartPoint = new Point(x, y);

            // 上边
            if (hasTop)
            {
                DrawLine(w - r, 0); // 左半部分
                DrawTopOrDownCurve(1); // 圆弧部分
            }
            DrawLine(Width - r, y, false); // 其余部分

            // 右边
            DrawCorner(1, 1); // 右上角
            if (hasRight)
            {
                DrawLine(0, w - r); // 上半部分
                while (slots >= 1)
                {
                    DrawLeftOrRightCurve(1); // 圆弧部分
                    if (--slots >= 1) DrawLine(0, w*2);
                }
            }
            DrawLine(x, Height - h - r, false); // 其余部分

            // 下边
            DrawCorner(-1, 1); // 右下角
            if (hasBottom)
            {
                DrawLine(h + w * 2, y, false); // 右半部分
                DrawTopOrDownCurve(-1, 1); // 圆弧部分
            }
            DrawLine(h + r, y, false); // 其余部分

            // 左边
            DrawCorner(-1, -1); // 左下角
            if (hasLeft)
            {
                DrawLine(x, w * 2, false); // 下半部分
                DrawLeftOrRightCurve(-1, 1); // 圆弧部分
            }
            DrawLine(x, r, false); // 其余部分

            DrawCorner(1, -1); // 左上角

            pathGeo.Figures.Add(pathFigure);
            return pathGeo;
        }

    }
}
