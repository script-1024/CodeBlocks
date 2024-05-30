using Windows.UI;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;
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
            if (fillColor == default) BlockColor = ColorHelper.FromInt(0x505050);
        }

        private void SetColor(Color value)
        {
            var fillColorBrush = new SolidColorBrush(value);
            var borderColorBrush = new SolidColorBrush(borderColor);
            BlockBorder.Stroke = borderColorBrush;
            BlockBorder.StrokeThickness = 2;
            BlockBorder.Fill = fillColorBrush;
        }

        internal virtual void OnInteractionStateChanged()
        {
            if (disabled)
            {
                this.ManipulationMode = ManipulationModes.None;
            }
            else
            {
                this.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
            }
        }

        #region "Properties"

        private bool disabled = false;
        public bool IsInteractionDisabled
        {
            get => disabled;
            set
            {
                disabled = value;
                OnInteractionStateChanged();
            }
        }
        
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
