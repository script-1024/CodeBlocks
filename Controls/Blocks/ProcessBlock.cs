using Windows.UI;
using CodeBlocks.Core;
using Microsoft.UI.Xaml.Controls;

namespace CodeBlocks.Controls
{
    public class ProcessBlock : CodeBlock
    {
        public ProcessBlock(BlockCreatedEventHandler handler, BlockCreatedEventArgs args = null) : base(handler, args)
        {
            BlockColor = Color.FromArgb(0xFF, 0xFF, 0xC8, 0x00);
            MetaData = new() { Type = BlockType.ProcessBlock, Variant = 10, Size = this.Size };
            TranslationKey = "Blocks.ProcessBlock.Say.Text";
            Canvas.SetLeft(BlockDescription, 24);
            Canvas.SetTop(BlockDescription, 16);
        }

        public ProcessBlock() : this(null, null) { }
    }
}
