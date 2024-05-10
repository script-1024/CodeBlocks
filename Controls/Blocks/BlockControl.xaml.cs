using Windows.UI;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using CodeBlocks.Core;

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
            var fillColorBrush = new SolidColorBrush(value);
            var borderColorBrush = new SolidColorBrush(borderColor);
            BlockBorder.Stroke = borderColorBrush;
            BlockBorder.StrokeThickness = 2;
            BlockBorder.Fill = fillColorBrush;
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
