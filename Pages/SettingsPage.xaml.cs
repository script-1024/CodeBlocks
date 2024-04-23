using Windows.Storage;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CodeBlocks.Controls;

namespace CodeBlocks.Pages
{
    public sealed partial class SettingsPage : Page
    {
        private readonly App app = Application.Current as App;
        private string GetLocalizedString(string key) => app.Localizer.GetString(key);

        public SettingsPage()
        {
            InitializeComponent();
            InitializePage();
            app.OnLanguageChanged += GetLocalized;
        }

        private void GetLocalized()
        {
            Title.Text = GetLocalizedString("Settings.Title");
            foreach (var item in RootPanel.Children)
            {
                if (item is ContentBar thisItem)
                {
                    string tag = thisItem.Tag?.ToString();
                    if (tag != "NoTitle") thisItem.Title = GetLocalizedString($"Settings.{thisItem.Name}.Title");
                    if (tag != "NoDescription") thisItem.Description = GetLocalizedString($"Settings.{thisItem.Name}.Description");
                }
            }

            ComboBox_Theme.ItemsSource = new string[]
            {
                GetLocalizedString("Settings.ThemeOptions.Options.FollowSystem"),
                GetLocalizedString("Settings.ThemeOptions.Options.Light"),
                GetLocalizedString("Settings.ThemeOptions.Options.Dark")
            };
        }

        private void InitializePage()
        {
            VersionInfo.Description = App.Version;
            ComboBox_Language.ItemsSource = App.SupportedLanguagesByName;
            ComboBox_Language.SelectedItem = App.CurrentLanguage;
            ComboBox_Language.SelectionChanged += (s, e) =>
            {
                if (ComboBox_Language.SelectedIndex == -1) ComboBox_Language.SelectedIndex = 0;
                var lang = ComboBox_Language.SelectedItem.ToString();
                if (App.CurrentLanguage == lang) return;
                ApplicationData.Current.LocalSettings.Values["Language"] = lang;
                App.CurrentLanguage = lang;
                app.LanguageChanged();
            };

            ComboBox_Theme.SelectedIndex = App.CurrentTheme;
            ComboBox_Theme.SelectionChanged += (s, e) =>
            {
                if (ComboBox_Theme.SelectedIndex == -1) ComboBox_Theme.SelectedIndex = App.CurrentTheme;
                if (App.CurrentTheme == ComboBox_Theme.SelectedIndex) return;
                ApplicationData.Current.LocalSettings.Values["RequestedTheme"] = ComboBox_Theme.SelectedIndex;
                App.CurrentTheme = ComboBox_Theme.SelectedIndex;
                app.ThemeChanged();
            };

            GetLocalized();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            var wnd = app.MainWindow;
            wnd.RootGrid.Children.Remove(this);
            wnd.Tab.Visibility = Visibility.Visible;
            wnd.UpdateDragRects();
        }
    }
}
