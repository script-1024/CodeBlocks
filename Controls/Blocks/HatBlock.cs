using Windows.UI;
using CodeBlocks.Core;
using Microsoft.UI.Xaml.Controls;

namespace CodeBlocks.Controls
{
    public class HatBlock : CodeBlock
    {
        public HatBlock(BlockCreatedEventHandler handler, BlockCreatedEventArgs args = null) : base(handler, args)
        {
            BlockColor = Color.FromArgb(0xFF, 0xFF, 0xAA, 0x00);
            MetaData = new() { Type = BlockType.HatBlock, Variant = 8, Size = this.Size };
            TranslationKey = "Blocks.HatBlock.FunctionEntry.Text";
            Canvas.SetLeft(BlockDescription, 24);
            Canvas.SetTop(BlockDescription, 14);
        }

        public HatBlock() : this(null, null) { }
    }
}
