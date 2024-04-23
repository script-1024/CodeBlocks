using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CodeBlocks.Controls
{
    public sealed partial class ContentBar : UserControl
    {
        private readonly App app = Application.Current as App;

        public string Title
        {
            get => TitleTextBlock.Text;
            set => TitleTextBlock.Text = value;
        }

        public string Description
        {
            get => DescriptionTextBlock.Text;
            set => DescriptionTextBlock.Text = value;   
        }

        public string IconGlyph
        {
            get => Icon.Glyph;
            set
            {
                if (value != null) { Icon.Glyph = value; Icon.Visibility = Visibility.Visible; }
                else Icon.Visibility = Visibility.Collapsed;
            }
        }

        public new Control Content
        {
            get => ContentPresenter.Content as Control;
            set => ContentPresenter.Content = value;
        }

        public ContentBar() : this(null, "\uE946") => InitializeComponent();

        public ContentBar(object control, string iconGlyph)
        {
            InitializeComponent();
            Content = control as Control;
            IconGlyph = iconGlyph;
            app.OnThemeChanged += () => this.RequestedTheme = (ElementTheme)App.CurrentTheme;
        }
    }
}
