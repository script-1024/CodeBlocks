using Windows.UI;
using CodeBlocks.Core;
using Microsoft.UI.Xaml.Controls;

namespace CodeBlocks.Controls
{
    public class HatBlock : CodeBlock
    {
        public HatBlock() : base()
        {
            BlockColor = Color.FromArgb(255, 255, 160, 0);
            MetaData = new() { Type = BlockType.HatBlock, Variant = 8, Size = this.Size };
            TranslationKey = "Blocks.HatBlock.FunctionEntry.Text";
            Canvas.SetLeft(BlockDescription, 22);
            Canvas.SetTop(BlockDescription, 12);
        }
    }
}
