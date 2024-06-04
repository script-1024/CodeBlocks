using Windows.Storage;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CodeBlocks.Controls;
using System.Diagnostics;

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
            app.LanguageChanged += GetLocalized;
        }

        private void GetLocalized()
        {
            Title.Text = GetLocalizedString("Settings.Title");
            foreach (var item in RootPanel.Children)
            {
                if (item is ContentBar thisItem)
                {
                    thisItem.Title = GetLocalizedString($"Settings.{thisItem.Name}.Title");
                    if (thisItem.Tag?.ToString() != "NoDescription") thisItem.Description = GetLocalizedString($"Settings.{thisItem.Name}.Description");
                }
            }

            foreach (ComboBoxItem item in ComboBox_Theme.Items)
            {
                item.Content = GetLocalizedString($"Settings.ThemeOptions.Options.{item.Tag}");
            }
            ComboBox_Theme.SelectedIndex = -1;

            AppFolderButton.Content = GetLocalizedString("Settings.OpenAppFolder.ActionButton");
        }

        private void InitializePage()
        {
            VersionInfo.Description = App.Version;
            OpenAppFolder.Description = App.Path;
            ComboBox_Language.ItemsSource = App.SupportedLanguagesByName;
            ComboBox_Language.SelectedItem = app.CurrentLanguageName;
            ComboBox_Language.SelectionChanged += (_, _) =>
            {
                var lang = ComboBox_Language.SelectedItem.ToString();
                if (app.CurrentLanguageName == lang) return;
                ApplicationData.Current.LocalSettings.Values["Language"] = lang;
                app.CurrentLanguageName = lang;
            };

            ComboBox_Theme.SelectedIndex = app.CurrentThemeId;
            ComboBox_Theme.SelectionChanged += (_, _) =>
            {
                var theme = ComboBox_Theme.SelectedIndex;
                if (theme == -1) theme = ComboBox_Theme.SelectedIndex = app.CurrentThemeId;
                if (app.CurrentThemeId == theme) return;
                ApplicationData.Current.LocalSettings.Values["RequestedTheme"] = theme;
                app.CurrentThemeId = theme;
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

        private void AppFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"{App.Path}"
            };

            Process.Start(startInfo);
        }
    }
}
