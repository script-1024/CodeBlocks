using CodeBlocks.Pages;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CodeBlocks.Controls
{
    public sealed partial class AppBar : UserControl
    {
        private readonly App app = Application.Current as App;

        public AppBar()
        {
            this.InitializeComponent();
            app.OnLanguageChanged += GetLocalized;
            GetLocalized();
        }

        private void GetLocalized()
        {
            foreach (MenuBarItem menuItem in Menu.Items)
            {
                menuItem.Title = app.Localizer.GetString($"MenuBar.{menuItem.Tag}.Title");
                foreach (MenuFlyoutItem flyoutItem in menuItem.Items)
                {
                    flyoutItem.Text = app.Localizer.GetString($"MenuBar.{menuItem.Tag}.{flyoutItem.Tag}");
                }
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var wnd = (Application.Current as App).m_window;
            wnd.Tab.Visibility = Visibility.Collapsed;
            wnd.RootGrid.Children.Add(new SettingsPage());
            wnd.UpdateDragRects(48);
        }
    }
}
