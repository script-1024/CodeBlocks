using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.Storage;
using CodeBlocks.Core;

namespace CodeBlocks.Controls
{
    public sealed partial class ToolBox : UserControl
    {
        // 暫存CBD以便可重复使用
        private static readonly Dictionary<string /* ID */, CodeBlockDefinition /* FILE */> Register = new();
        private readonly App app = App.Current as App;
        private bool canScroll = true;

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

        public BlockDragger BlockDragger { get; set; }

        private void ClosePanelButton_Click(object sender, RoutedEventArgs e) => IsOpen = !isOpen;

        private double lastWindowWidth = 0;
        public ToolBox()
        {
            InitializeComponent();
            
            RootGrid.Loaded += (_, _) =>
            {
                ReloadBlocks();
                lastWindowWidth = app.MainWindow.AppWindow.Size.Width;
                app.MainWindow.SizeChanged += (_, e) =>
                {
                    // 窗口正在缩小或放大时，检查是否应该自动调整工具箱占用的空间
                    if (e.Size.Width < 750 && e.Size.Width < lastWindowWidth) IsOpen = false;
                    else if (e.Size.Width > 1000 && e.Size.Width > lastWindowWidth) IsOpen = true;
                    lastWindowWidth = e.Size.Width;
                };
            };
        }

        private async void ReloadBlocks()
        {
            PositioningTags.Children.Clear();
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
                                block.Margin = new(20, 20, 12, 0);
                                block.HorizontalAlignment = HorizontalAlignment.Left;
                                block.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;

                                block.ManipulationStarted += Block_ManipulationStarted;
                                block.ManipulationDelta += Block_ManipulationDelta;
                                block.ManipulationCompleted += Block_ManipulationCompleted;
                                BlocksDepot.Children.Add(block);
                            }

                            TextBlock blank = new() { Margin = new(24) };
                            BlocksDepot.Children.Add(blank);
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
                    Fill = ColorHelper.FromHexString(element.GetChildElement("color").GetString()).GetSolidColorBrush(),
                    Width = 36, Height = 36
                },
                Margin = new(5, 3, 5, 0),
                Width = 40, Height = 50,
            };

            PositioningTags.Children.Add(btn);

            TextBlock label = new()
            {
                FontSize = 14,
                Margin = new(8, 8, 0, 16),
                FontFamily = CodeBlock.FontFamily
            };

            void RefreshText()
            {
                label.Text = dictionary[App.CurrentLanguageId].ToString();
                ToolTipService.SetToolTip(btn, label.Text);
            }

            void OnButtonClick()
            {
                IsOpen = true;
                double position = label.TransformToVisual(BlocksDepot).TransformPoint(new Point(0, 0)).Y - 8;
                Scroller.ChangeView(null, position, null);
            }

            btn.Click += (_, _) => OnButtonClick();
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
                BlockColor = ColorHelper.FromInt(cbd.ColorInt),
                TranslationsDict = cbd.TranslationsDict,
                Identifier = cbd.Identifier,
                MetaData = data
            };

            block.RefreshBlockText();

            return block;
        }

        private void BlocksDepot_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (!canScroll) return;
            var newY = Scroller.VerticalOffset - e.Delta.Translation.Y;
            Scroller.ChangeView(null, newY, null, true);
        }

        private void Block_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            canScroll = false;
            var thisBlock = sender as CodeBlock;
            var selfPosition = thisBlock.TransformToVisual(BlocksDepot).TransformPoint(new(PositioningTags.ActualWidth, Scroller.VerticalOffset));
            var transformedPosition = BlockDragger.TransformPositionFromWindowToWorkspace(selfPosition);
            var args = new BlockCreatedEventArgs(transformedPosition, thisBlock);
            var clonedBlock = thisBlock.Clone(BlockDragger.BlockCreated, args);
            thisBlock.Tag = clonedBlock;

            BlockDragger.BlockManipulationStarted(clonedBlock, e);
        }
        private void Block_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            var clonedBlock = (sender as CodeBlock).Tag as CodeBlock;
            BlockDragger.BlockManipulationDelta(clonedBlock, e);
        }

        private void Block_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            var thisBlock = sender as CodeBlock;
            var clonedBlock = thisBlock.Tag as CodeBlock;
            BlockDragger.BlockManipulationCompleted(clonedBlock, e);

            thisBlock.Tag = null;
            canScroll = true;
        }

    }
}
