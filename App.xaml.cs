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
            CurrentLanguage = ApplicationData.Current.LocalSettings.Values["Language"] as string ?? "English";
            
            Localizer.ReloadLanguageFiles();
            this.Localizer = new();
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
        }

        public static string CurrentLanguage;
        public static string[] SupportedLanguagesByName;

        public static readonly string Version = "Beta 1.0.4 Build 0409";
        public static readonly Dictionary<string, string> LoadedLanguages = new();
        public static readonly string AppPath = AppDomain.CurrentDomain.BaseDirectory;

        public delegate void LanguageChangedEventHandler();
        public event LanguageChangedEventHandler OnLanguageChanged;
        public void LanguageChanged()
        {
            this.Localizer = new();
            OnLanguageChanged?.Invoke();
        }

        public Localizer Localizer { get; private set; }
    }
}
