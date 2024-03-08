using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using CodeBlocks.Controls;
using Windows.Storage;
using Microsoft.UI.Xaml;
using CodeBlocks.Core;
using System;

namespace CodeBlocks.Pages
{
    public sealed partial class SettingsPage : Page
    {
        private readonly App app = Application.Current as App;
        private string GetLocalizedString(string key) => app.Localizer.GetString(key);

        public SettingsPage()
        {
            InitializeComponent();
            app.OnLanguageChanged += GetLocalized;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            InitializePage();
        }

        private void GetLocalized()
        {
            VersionInfo.Title = GetLocalizedString("Settings.VersionInfo.Title");
            LangConfig.Title = GetLocalizedString("Settings.LanguageConfig.Title");
            LangConfig.Description = GetLocalizedString("Settings.LanguageConfig.Description");
        }

        private void InitializePage()
        {
            VersionInfo.Description = App.Version;

            ComboBox_Language.ItemsSource = App.SupportedLanguagesByName;
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
