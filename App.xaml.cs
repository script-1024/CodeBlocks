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
            Localizer.ReloadLanguageProfiles();
            CurrentLanguage = ApplicationData.Current.LocalSettings.Values["Language"] as string ?? "English";
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            var m_window = new MainWindow();
            m_window.Activate();
        }

        private readonly ResourceMap strings = new ResourceManager().MainResourceMap;

        public static string Version = "Beta 1.0";
        public static string CurrentLanguage;
        public static string[] SupportedLangList;
        public static Dictionary<string, string> LanguageIdentifiers = new();
        public static readonly string AppPath = AppDomain.CurrentDomain.BaseDirectory;

        public delegate void LanguageChangedEventHandler();
        public event LanguageChangedEventHandler OnLanguageChanged;
        public void LanguageChanged()
        {
            OnLanguageChanged.Invoke();
        }

        public string GetLocalized(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return string.Empty;
            return strings.TryGetValue(key).ValueAsString ?? string.Empty;
        }
    }
}
