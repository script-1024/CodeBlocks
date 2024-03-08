using Windows.UI;
using CodeBlocks.Core;
using Microsoft.UI.Xaml.Controls;

namespace CodeBlocks.Controls
{
    public class ProcessBlock : CodeBlock
    {
        public ProcessBlock() : base()
        {
            BlockColor = Color.FromArgb(255, 255, 200, 0);
            MetaData = new() { Type = BlockType.ProcessBlock, Variant = 10, Size = this.Size };
            TranslationKey = "Blocks.ProcessBlock.Say.Text";
            Canvas.SetLeft(BlockDescription, 22);
            Canvas.SetTop(BlockDescription, 12);
        }
    }
}
