using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CodeBlocks.Controls
{

    public sealed partial class ContentCard : UserControl
    {
        public string Title
        {
            get { return TitleTextBlock.Text; }
            set { TitleTextBlock.Text = value;}
        }

        public string Description
        {
            get { return DescriptionTextBlock.Text; }
            set { DescriptionTextBlock.Text = value; }
        }

        public string IconGlyph
        {
            get { return Icon.Glyph; }
            set
            {
                if (value != null) { Icon.Glyph = value; Icon.Visibility = Visibility.Visible; }
                else Icon.Visibility = Visibility.Collapsed;
            }
        }

        public new Control Content
        {
            get { return ContentPresenter.Content as Control; }
            set => ContentPresenter.Content = value;
        }

        public ContentCard()
        {
            InitializeComponent();
            IconGlyph = "\uE946";
        }

        public ContentCard(object control, string iconGlyph = "\uE946")
        {
            InitializeComponent();
            IconGlyph = iconGlyph;
            Content = control as Control;
        }
    }
}
