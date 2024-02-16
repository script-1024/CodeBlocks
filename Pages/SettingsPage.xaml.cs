using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using CodeBlocks.Controls;

namespace CodeBlocks.Pages
{
    public sealed partial class SettingsPage : Page
    {
        private bool isInitialized = false;

        public SettingsPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (!isInitialized) InitializePage();
        }

        private void InitializePage()
        {
            isInitialized = true;
            var versionInfo = new ContentCard()
            {
                Title = "Version",
                Description = "Beta 1.0"
            };
            RootPanel.Children.Add(versionInfo);
        }
    }
}
