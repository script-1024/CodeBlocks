using System;
using Windows.Storage;
using Microsoft.UI.Xaml;
using System.Collections.Generic;
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

        public MainWindow m_window;
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            m_window = new();
            m_window.Activate();
        }

        public static string CurrentLanguage;
        public static string[] SupportedLanguagesByName;

        public static readonly string Version = "Beta 1.0.3 Build 0318";
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
