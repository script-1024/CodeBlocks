using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;
using CodeBlocks.Core;
using Microsoft.UI.Xaml.Shapes;

namespace CodeBlocks.Controls
{
    public partial class BlockControl : UserControl
    {
        private Color fillColor;
        private Color borderColor;

        public BlockControl()
        {
            InitializeComponent();
            if (fillColor == default) BlockColor = Color.FromArgb(255, 80, 80, 80);
        }

        private void SetColor(Color value)
        {
            var fillBrush = new SolidColorBrush(value);
            var borderBrush = new SolidColorBrush(borderColor);
            BlockBorder.Stroke = borderBrush;
            BlockBorder.StrokeThickness = 2;
            BlockBorder.Fill = fillBrush;
        }

        #region "Properties"
        public int DependentSlot;
        public CodeBlock ParentBlock;
        public CodeBlock BottomBlock;
        public CodeBlock[] RightBlocks = [];
        public Dictionary<string, int> ValueIndex { get; private set; } = new();
        
        public Color BlockColor
        {
            get => fillColor;
            set
            {
                fillColor = value;
                borderColor = ColorHelper.GetBorderColor(value);
                SetColor(fillColor);
            }
        }
        #endregion
    }
}
