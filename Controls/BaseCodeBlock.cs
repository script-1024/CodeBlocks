using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;
using Windows.Foundation;
using CodeBlocks.Core;

namespace CodeBlocks.Controls
{
    public sealed class BaseCodeBlock : Control
    {
        public BaseCodeBlock(Canvas parent)
        {
            DefaultStyleKey = typeof(BaseCodeBlock);
            this.parent = parent;
            InitializeProperties();
        }

        private void InitializeProperties()
        {
            borderPath = new Path();
            BlockWidth = 200;
            BlockHeight = 66;
            HasLeftConvex = false;
            HasTopConcave = true;
            HasRightConcave = false;
            HasBottomConvex = true;
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            var border = GetTemplateChild("Border") as Border;
            //border.SizeChanged += Border_SizeChanged;
        }

        private void Border_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawBorder();
        }

        public void DrawBorder()
        {
            int x = h + r;
            int y = 0;
            
            PathGeometry pathGeometry = new PathGeometry();

            // 从左上角开始 
            PathFigure pathFigure = new PathFigure();
            pathFigure.StartPoint = new Point(x, y);

            // Top
            if (!HasTopConcave)
            {
                LineSegment topSide = new LineSegment();
                topSide.Point = new Point(x = width - r, y);
                pathFigure.Segments.Add(topSide);
            }
            else
            {
                // 左半部分
                LineSegment topSideLeft = new LineSegment();
                topSideLeft.Point = new Point(x += w - r, y);
                pathFigure.Segments.Add(topSideLeft);

                // 圆弧部分
                LineSegment left = new LineSegment();
                left.Point = new Point(x, y += h - r);
                pathFigure.Segments.Add(left);

                ArcSegment leftArc = new ArcSegment();
                leftArc.Point = new Point(x + r, y += r);
                leftArc.Size = new Size(r, r);
                leftArc.SweepDirection = SweepDirection.Counterclockwise;
                pathFigure.Segments.Add(leftArc);

                LineSegment bottom = new LineSegment();
                bottom.Point = new Point(x += w - r, y);
                pathFigure.Segments.Add(bottom);

                ArcSegment rightArc = new ArcSegment();
                rightArc.Point = new Point(x += r, y - r);
                rightArc.Size = new Size(r, r);
                rightArc.SweepDirection = SweepDirection.Counterclockwise;
                pathFigure.Segments.Add(rightArc);

                LineSegment right = new LineSegment();
                right.Point = new Point(x, y -= h);
                pathFigure.Segments.Add(right);

                // 右半部分
                LineSegment topSideRight = new LineSegment();
                topSideRight.Point = new Point(x = width - r, y);
                pathFigure.Segments.Add(topSideRight);
            }

            // Right
            {
                // 右上角
                ArcSegment arc = new ArcSegment();
                arc.Point = new Point(x += r, y += r);
                arc.Size = new Size(r, r);
                arc.RotationAngle = 90;
                arc.SweepDirection = SweepDirection.Clockwise;
                pathFigure.Segments.Add(arc);

                if (!HasRightConcave)
                {
                    LineSegment rightSide = new LineSegment();
                    rightSide.Point = new Point(x, y = height - h - r);
                    pathFigure.Segments.Add(rightSide);
                }
                else
                {
                    // 上半部分
                    LineSegment rightSideTop = new LineSegment();
                    rightSideTop.Point = new Point(x, y += w - r);
                    pathFigure.Segments.Add(rightSideTop);

                    // 圆弧部分
                    LineSegment top = new LineSegment();
                    top.Point = new Point(x -= h - r, y);
                    pathFigure.Segments.Add(top);

                    ArcSegment topArc = new ArcSegment();
                    topArc.Point = new Point(x -= r, y + r);
                    topArc.Size = new Size(r, r);
                    topArc.SweepDirection = SweepDirection.Counterclockwise;
                    pathFigure.Segments.Add(topArc);

                    LineSegment left = new LineSegment();
                    left.Point = new Point(x, y += w - r);
                    pathFigure.Segments.Add(left);

                    ArcSegment botArc = new ArcSegment();
                    botArc.Point = new Point(x + r, y += r);
                    botArc.Size = new Size(r, r);
                    botArc.SweepDirection = SweepDirection.Counterclockwise;
                    pathFigure.Segments.Add(botArc);

                    LineSegment bottom = new LineSegment();
                    bottom.Point = new Point(x += h, y);
                    pathFigure.Segments.Add(bottom);

                    // 下半部分
                    LineSegment rightSideBot = new LineSegment();
                    rightSideBot.Point = new Point(x, y = height - h - r);
                    pathFigure.Segments.Add(rightSideBot);
                }
            }

            // Bottom
            {
                // 右下角
                ArcSegment arc = new ArcSegment();
                arc.Point = new Point(x -= r, y += r);
                arc.Size = new Size(r, r);
                arc.SweepDirection = SweepDirection.Clockwise;
                pathFigure.Segments.Add(arc);

                if (!HasBottomConvex)
                {
                    LineSegment botSide = new LineSegment();
                    botSide.Point = new Point(x = h + r, y);
                    pathFigure.Segments.Add(botSide);
                }
                else
                {
                    // 右半部分
                    LineSegment botSideRight = new LineSegment();
                    botSideRight.Point = new Point(x = h + w*2, y);
                    pathFigure.Segments.Add(botSideRight);

                    // 圆弧部分
                    LineSegment right = new LineSegment();
                    right.Point = new Point(x, y += h - r);
                    pathFigure.Segments.Add(right);

                    ArcSegment rightArc = new ArcSegment();
                    rightArc.Point = new Point(x - r, y += r);
                    rightArc.Size = new Size(r, r);
                    rightArc.SweepDirection = SweepDirection.Counterclockwise;
                    pathFigure.Segments.Add(rightArc);

                    LineSegment bottom = new LineSegment();
                    bottom.Point = new Point(x -= w - r, y);
                    pathFigure.Segments.Add(bottom);

                    ArcSegment leftArc = new ArcSegment();
                    leftArc.Point = new Point(x -= r, y - r);
                    leftArc.Size = new Size(r, r);
                    leftArc.SweepDirection = SweepDirection.Counterclockwise;
                    pathFigure.Segments.Add(leftArc);

                    LineSegment left = new LineSegment();
                    left.Point = new Point(x, y -= h);
                    pathFigure.Segments.Add(left);

                    // 左半部分
                    LineSegment botSideLeft = new LineSegment();
                    botSideLeft.Point = new Point(x = h + r, y);
                    pathFigure.Segments.Add(botSideLeft);
                }
            }

            // Left
            {
                // 左下角
                ArcSegment arc = new ArcSegment();
                arc.Point = new Point(x -= r, y -= r);
                arc.Size = new Size(r, r);
                arc.SweepDirection = SweepDirection.Clockwise;
                pathFigure.Segments.Add(arc);

                if (!HasLeftConvex)
                {
                    LineSegment leftSide = new LineSegment();
                    leftSide.Point = new Point(x, y = r);
                    pathFigure.Segments.Add(leftSide);
                }
                else
                {
                    // 下半部分
                    LineSegment leftSideBot = new LineSegment();
                    leftSideBot.Point = new Point(x, y = h*2);
                    pathFigure.Segments.Add(leftSideBot);

                    // 圆弧部分
                    LineSegment bottom = new LineSegment();
                    bottom.Point = new Point(x -= h - r, y);
                    pathFigure.Segments.Add(bottom);

                    ArcSegment botArc = new ArcSegment();
                    botArc.Point = new Point(x -= r, y - r);
                    botArc.Size = new Size(r, r);
                    botArc.SweepDirection = SweepDirection.Counterclockwise;
                    pathFigure.Segments.Add(botArc);

                    LineSegment left = new LineSegment();
                    left.Point = new Point(x, y -= w - r);
                    pathFigure.Segments.Add(left);

                    ArcSegment topArc = new ArcSegment();
                    topArc.Point = new Point(x + r, y -= r);
                    topArc.Size = new Size(r, r);
                    topArc.SweepDirection = SweepDirection.Counterclockwise;
                    pathFigure.Segments.Add(topArc);

                    LineSegment top = new LineSegment();
                    top.Point = new Point(x += h, y);
                    pathFigure.Segments.Add(top);

                    // 上半部分
                    LineSegment leftSideTop = new LineSegment();
                    leftSideTop.Point = new Point(x, y -= w - r);
                    pathFigure.Segments.Add(leftSideTop);
                }
            }

            {
                // 左上角
                ArcSegment arc = new ArcSegment();
                arc.Point = new Point(x += r, y -= r);
                arc.Size = new Size(r, r);
                arc.SweepDirection = SweepDirection.Clockwise;
                pathFigure.Segments.Add(arc);
            }

            pathGeometry.Figures.Add(pathFigure);
            borderPath.Data = pathGeometry;
        }

        private void SetColor(Color value)
        {
            var fillBrush = new SolidColorBrush(value);
            var blankBrush = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
            var borderBrush = new SolidColorBrush(borderColor);
            borderPath.Stroke = borderBrush;
            borderPath.StrokeThickness = 2;
            borderPath.Fill = fillBrush;
        }

        private void SetPosition(double x, double y)
        {
            Canvas.SetLeft(this, x);
            Canvas.SetTop(this, y);
        }

        public void TrySetPosition(double x, double y)
        {
            double maxX = parent.ActualWidth - BlockWidth;
            double maxY = parent.ActualHeight - BlockHeight;

            // 边界检查
            if (x < 0) x = 0;
            else if (x > maxX) x = maxX;

            if (y < 0) y = 0;
            else if (y > maxY) y = maxY;

            SetPosition(x, y);
        }

        #region "properties"
        #region "private"

        private int width;
        private int height;
        private int w = 18;
        private int h = 12;
        private int r = 5;
        private Color fillColor;
        private Color borderColor;
        public Path borderPath;
        private Canvas parent;
        private bool hasLeftConvex;
        private bool hasTopConcave;
        private bool hasRightConcave;
        private bool hasBottomConvex;

        #endregion

        public int BlockWidth
        {
            get => width;
            set
            {
                if (value >= 66) width = value;
                //DrawBorder();
            }
        }

        public int BlockHeight
        {
            get => height;
            set
            {
                if (value >= 66) height = value;
                //DrawBorder();
            }
        }

        public Color BlockColor
        {
            get => fillColor;
            set
            {
                fillColor = value;
                borderColor = ColorHelper.GetBorderColor(value);
                SetColor(value);
            }
        }

        public bool HasLeftConvex
        {
            get => hasLeftConvex;
            set
            {
                hasLeftConvex = value;
                //DrawBorder();
            }
        }

        public bool HasTopConcave
        {
            get => hasTopConcave;
            set
            {
                hasTopConcave = value;
                //DrawBorder();
            }
        }

        public bool HasRightConcave
        {
            get => hasRightConcave;
            set
            {
                hasRightConcave = value;
                //DrawBorder();
            }
        }

        public bool HasBottomConvex
        {
            get => hasBottomConvex;
            set
            {
                hasBottomConvex = value;
                //DrawBorder();
            }
        }

        #endregion
    }
}