using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System.Threading.Tasks;
using System;
using Windows.Storage;
using WinUI3Localizer;
using System.IO;

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

        private async Task InitializeLocalizer()
        {
            // Initialize a "Strings" folder in the "LocalFolder" for the packaged app.
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFolder stringsFolder = await localFolder.CreateFolderAsync(
              "Strings",
               CreationCollisionOption.OpenIfExists);

            // Create string resources file from app resources if doesn't exists.
            string resourceFileName = "Resources.resw";
            await CreateStringResourceFileIfNotExists(stringsFolder, "zh-Hans", resourceFileName);
            await CreateStringResourceFileIfNotExists(stringsFolder, "zh-Hant", resourceFileName);

            ILocalizer localizer = await new LocalizerBuilder()
                .AddStringResourcesFolderForLanguageDictionaries(stringsFolder.Path)
                .SetOptions(options =>
                {
                    options.DefaultLanguage = "zh-Hans";
                })
                .Build();
        }

        private static async Task CreateStringResourceFileIfNotExists(StorageFolder stringsFolder, string language, string resourceFileName)
        {
            StorageFolder languageFolder = await stringsFolder.CreateFolderAsync(
                language,
                CreationCollisionOption.OpenIfExists);

            if (await languageFolder.TryGetItemAsync(resourceFileName) is null)
            {
                string resourceFilePath = Path.Combine(stringsFolder.Name, language, resourceFileName);
                StorageFile resourceFile = await LoadStringResourcesFileFromAppResource(resourceFilePath);
                _ = await resourceFile.CopyAsync(languageFolder);
            }
        }

        private static async Task<StorageFile> LoadStringResourcesFileFromAppResource(string filePath)
        {
            Uri resourcesFileUri = new($"ms-appx:///{filePath}");
            return await StorageFile.GetFileFromApplicationUriAsync(resourcesFileUri);
        }
    }
}
