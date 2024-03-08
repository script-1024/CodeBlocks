using System;
using Windows.Storage;
using Microsoft.UI.Xaml;
using System.Collections.Generic;
using Microsoft.Windows.ApplicationModel.Resources;
using CodeBlocks.Core;

namespace CodeBlocks
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            CurrentLanguage = ApplicationData.Current.LocalSettings.Values["Language"] as string ?? "English";
            
            Localizer.ReloadLanguageFiles();
            this.Localizer = new();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            var m_window = new MainWindow();
            m_window.Activate();
        }

        public static string CurrentLanguage;
        public static string[] SupportedLanguagesByName;

        public static readonly string Version = "Beta 1.0";
        public static readonly Dictionary<string, string> LoadedLanguages = new();
        public static readonly string AppPath = AppDomain.CurrentDomain.BaseDirectory;

        public delegate void LanguageChangedEventHandler();
        public event LanguageChangedEventHandler OnLanguageChanged;
        public void LanguageChanged()
        {
            this.Localizer = new();
            OnLanguageChanged.Invoke();
        }

        public Localizer Localizer { get; private set; }
    }
}
