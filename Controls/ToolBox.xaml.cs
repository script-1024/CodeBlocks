using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Shapes;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;
using CodeBlocks.Core;

/// <summary>
/// 工具箱
/// </summary>
namespace CodeBlocks.Controls
{
    public sealed partial class ToolBox : UserControl
    {
        // 暫存CBD以便可重复使用
        private static readonly Dictionary<string /* ID */, CodeBlockDefinition /* FILE */> Register = new();

        private readonly App app = App.Current as App;

        private bool isOpen = true;
        public bool IsOpen
        {
            get => isOpen;
            set
            {
                isOpen = value;
                Scroller.Visibility = isOpen.ToVisibility();

                var fontIcon = ClosePanelButton.Icon as FontIcon;
                if (isOpen) fontIcon.Glyph = "\uE8A0";
                else fontIcon.Glyph = "\uE89F";
            }
        }

        public ToolBox()
        {
            InitializeComponent();
            ReloadBlocks();
        }

        private async void ReloadBlocks()
        {
            BlocksDepot.Children.Clear();
            var directories = Directory.GetDirectories($"{App.Path}Blocks\\");
            foreach (var dir in directories)
            {
                if (File.Exists($"{dir}\\profile.json"))
                {
                    var jsonStr = File.ReadAllText($"{dir}\\profile.json");
                    using (JsonDocument document = JsonDocument.Parse(jsonStr))
                    {
                        var root = document.RootElement.Clone();
                        var categories = root.GetChildElement("categories");

                        foreach (var category in categories.EnumerateObject())
                        {
                            var element = category.Value;
                            AddNewCategory(element);

                            var blocks = element.GetChildElement("blocks");
                            foreach(var blockElement in blocks.EnumerateObject())
                            {
                                var blockFilePath = blockElement.Value.GetString();
                                var block = await CreateBlockFromPathAsync($"{dir}\\{blockFilePath}.cbd");
                                block.Margin = new(16,16,0,0);
                                block.HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Left;
                                BlocksDepot.Children.Add(block);
                                await Task.Delay(10);
                            }
                        }
                    }
                }
                else continue; // 忽略格式错误的文件夹
            }
        }

        private void AddNewCategory(JsonElement element)
        {
            var dictionary = element.GetChildElement("translation").GetDictionary();
            AppBarButton btn = new()
            {
                Content = new Ellipse()
                {
                    Fill = ColorHelper.FromHexString(element.GetChildElement("color").GetString()).ToSolidColorBrush(),
                    Width = 36, Height = 36
                },
                Margin = new(5, 5, 5, 0),
                Width = 40, Height = 50
            };

            PositioningTags.Children.Add(btn);
            TextBlock label = new()
            {
                FontSize = 14,
                Margin = new(8, 8, 0, 0),
                FontFamily = CodeBlock.FontFamily
            };


            void RefreshText()
            {
                label.Text = dictionary[App.CurrentLanguageId].ToString();
            }

            app.OnLanguageChanged += RefreshText;
            BlocksDepot.Children.Add(label);
            RefreshText();
        }

        public static CodeBlock CreateBlockFromID(string id)
        {
            if (Register.TryGetValue(id, out var cbd)) return CreateBlockFromCBD(cbd);
            else return null;
        }

        public static async Task<CodeBlock> CreateBlockFromPathAsync(string path)
        {
            CodeBlockDefinition cbd = new();
            if (await cbd.ReadFileAsync(path)) return CreateBlockFromCBD(cbd);
            else return null;
        }

        public static CodeBlock CreateBlockFromFile(StorageFile file)
        {
            CodeBlockDefinition cbd = new();
            if (cbd.ReadFile(file)) return CreateBlockFromCBD(cbd);
            else return null;
        }

        public static CodeBlock CreateBlockFromCBD(CodeBlockDefinition cbd)
        {
            // 尝试注册此ID以便后续可重复使用
            Register.TryAdd(cbd.Identifier, cbd);

            BlockMetaData data = new()
            {
                Variant = cbd.Variant,
                Type = cbd.BlockType,
                Code = cbd.McfCode
            };

            CodeBlock block = new()
            {
                BlockColor = ColorHelper.FromInt(cbd.ColorHex),
                TranslationsDict = cbd.TranslationsDict,
                Identifier = cbd.Identifier,
                MetaData = data
            };

            block.RefreshBlockText();

            return block;
        }

        private void ClosePanelButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            IsOpen = !isOpen;
        }
    }
}
