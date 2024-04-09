using CodeBlocks.Pages;
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
            SubscribeClickEvent();
            GetLocalized();
        }

        private void SubscribeClickEvent()
        {
            foreach (MenuBarItem menuItem in Menu.Items)
            {
                foreach (MenuFlyoutItem flyoutItem in menuItem.Items)
                {
                    flyoutItem.Click += FlyoutItem_Click;
                }
            }
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

        private void FlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            var thisItem = sender as MenuFlyoutItem;
            if (thisItem == null) return;
            switch (thisItem.Tag)
            {
                case "ShowBlockEditor":
                    var editor = new BlockEditor();
                    editor.Activate();
                    break;
                case "Exit":
                    app.MainWindow.Close();
                    break;
                default: return;
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var wnd = (Application.Current as App).MainWindow;
            wnd.Tab.Visibility = Visibility.Collapsed;
            wnd.RootGrid.Children.Add(new SettingsPage());
            wnd.UpdateDragRects(48);
        }
    }
}
