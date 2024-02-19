using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using CodeBlocks.Controls;
using Windows.Storage;
using Microsoft.UI.Xaml;
using CodeBlocks.Core;

namespace CodeBlocks.Pages
{
    public sealed partial class SettingsPage : Page
    {
        private bool isInitialized = false;
        private App app = Application.Current as App;

        public SettingsPage()
        {
            InitializeComponent();
            app.OnLanguageChanged += GetLocalized;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (!isInitialized) InitializePage();
        }

        private void GetLocalized()
        {
            Localizer localizer = new();
            VersionInfo.Title = localizer.GetString("Settings", "VersionInfo.Title");
            LangConfig.Title = localizer.GetString("Settings", "LanguageConfig.Title");
            LangConfig.Description = localizer.GetString("Settings", "LanguageConfig.Description");
        }

        private void InitializePage()
        {
            isInitialized = true;

            VersionInfo.Description = App.Version;

            ComboBox_Language.ItemsSource = App.SupportedLangList;
            ComboBox_Language.SelectedItem = App.CurrentLanguage;
            ComboBox_Language.SelectionChanged += (s, e) =>
            {
                ApplicationData.Current.LocalSettings.Values["Language"] = ComboBox_Language.SelectedItem.ToString();
                App.CurrentLanguage = ComboBox_Language.SelectedItem.ToString();
                app.LanguageChanged();
            };

            GetLocalized();
        }
    }
}
