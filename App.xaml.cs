using System;
using System.Linq;
using Windows.Storage;
using Microsoft.UI.Xaml;
using System.Collections.Generic;
using Microsoft.Windows.AppLifecycle;
using CodeBlocks.Core;
namespace CodeBlocks
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            CurrentLanguage = ApplicationData.Current.LocalSettings.Values["Language"]?.ToString();
            CurrentTheme = (int)(ApplicationData.Current.LocalSettings.Values["RequestedTheme"] ?? 0);

            Localizer.ReloadLanguageFiles();
            LanguageChanged();
        }

        public MainWindow MainWindow;
        public BlockEditor BlockEditor;

        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            var appActivationArgs = AppInstance.GetCurrent().GetActivatedEventArgs();

            if (appActivationArgs.Kind is ExtendedActivationKind.File &&
                appActivationArgs.Data is Windows.ApplicationModel.Activation.IFileActivatedEventArgs fileActivationArgs &&
                fileActivationArgs.Files.FirstOrDefault() is StorageFile file)
            {
                // 由支持的文件类型启动
                BlockEditor = new(file);
                BlockEditor.Activate();
            }
            else
            {
                // 常规启动
                MainWindow = new();
                MainWindow.Activate();
            }

            ThemeChanged();
        }

        // 0:FollowSystem | 1:Light | 2:Dark
        public static int CurrentTheme;
        public static string CurrentLanguage;
        public static string[] SupportedLanguagesByName;

        public static readonly string Version = "Beta 1.0.5 Build 0425";
        public static readonly Dictionary<string, string> LoadedLanguages = new();
        public static readonly string Path = AppDomain.CurrentDomain.BaseDirectory;

        public delegate void LanguageChangedEventHandler();
        public delegate void ThemeChangedEventHandler();
        public event LanguageChangedEventHandler OnLanguageChanged;
        public event ThemeChangedEventHandler OnThemeChanged;
        public void LanguageChanged()
        {
            if (CurrentLanguage is null)
            {
                // 优先使用电脑现有的语言
                var id = System.Globalization.CultureInfo.InstalledUICulture.Name;
                CurrentLanguage = LoadedLanguages.ContainsKey(id) ? LoadedLanguages[id] : "English";
            }
            this.Localizer = new(LoadedLanguages[CurrentLanguage]);
            OnLanguageChanged?.Invoke();
        }

        public void ThemeChanged() => OnThemeChanged?.Invoke();

        public Localizer Localizer { get; private set; }
    }
}
