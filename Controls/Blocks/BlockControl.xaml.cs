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
            BlockBorder = border;
            if (fillColor == default) BlockColor = Color.FromArgb(255, 80, 80, 80);
            BlockDescription = description;
        }

        private void SetColor(Color value)
        {
            var fillBrush = new SolidColorBrush(value);
            var borderBrush = new SolidColorBrush(borderColor);
            border.Stroke = borderBrush;
            border.StrokeThickness = 2;
            border.Fill = fillBrush;
        }

        #region "Properties"
        public int DependentSlot;
        public BaseBlock ParentBlock;
        public BaseBlock BottomBlock;
        public BaseBlock[] RightBlocks = [];
        public readonly Path BlockBorder;
        public readonly TextBlock BlockDescription;
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
