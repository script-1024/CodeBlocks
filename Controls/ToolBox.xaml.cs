using System;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

/// <summary>
/// 工具箱
/// </summary>
namespace CodeBlocks.Controls
{
    public sealed partial class ToolBox : UserControl
    {
        public ToolBox()
        {
            InitializeComponent();
        }

        private void LoadBlocks()
        {
            var enumerateFiles = Directory.EnumerateFiles($"{App.Path}/Blocks", "*.cbd");
        }
    }
}
