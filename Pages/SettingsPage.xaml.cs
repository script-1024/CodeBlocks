using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using CodeBlocks.Controls;
using Windows.Storage;

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
            var tip = new TeachingTip() { PreferredPlacement = TeachingTipPlacementMode.Auto, IsOpen = false };
            RootPanel.Children.Add(tip);

            var versionInfo = new ContentBar() { Title = "版本号", Description = "Beta 1.0" };
            RootPanel.Children.Add(versionInfo);

            object selectedLanguage = ApplicationData.Current.LocalSettings.Values["Language"];

            var langComboBox = new ComboBox() { };

            var langList = new string[2] { "简体中文", "繁體中文" };
            langComboBox.ItemsSource = langList;
            langComboBox.SelectedItem = selectedLanguage ?? langList[0];

            langComboBox.SelectionChanged += (s, e) =>
            {
                var self = langComboBox;
                ApplicationData.Current.LocalSettings.Values["Language"] = self.SelectedItem as string;
                tip.Target = self; tip.IsLightDismissEnabled = true;
                tip.Title = "小提醒"; tip.Content = "此设置将在下次启动后生效"; tip.IsOpen = true;
            };

            var langSetting = new ContentBar() { Title = "语言", Description = "更改软件的显示语言", IconGlyph = "\uF2B7", Content = langComboBox };
            RootPanel.Children.Add(langSetting);
        }
    }
}
