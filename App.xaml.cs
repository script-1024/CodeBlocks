using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace CodeBlocks
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();
            m_window.Activate();
        }

        internal Window m_window;
    }
}
