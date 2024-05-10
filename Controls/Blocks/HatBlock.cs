using CodeBlocks.Core;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;

namespace CodeBlocks.Controls
{
    public class HatBlock : CodeBlock
    {
        public HatBlock(BlockCreatedEventHandler handler, BlockCreatedEventArgs args = null) : base(handler, args)
        {
            BlockColor = (App.Current.Resources["EventBlockColorBrush"] as SolidColorBrush).Color;
            MetaData = new() { Type = BlockType.HatBlock, Variant = 8, Size = this.Size };
            TranslationKey = "Blocks.HatBlock.FunctionEntry.Text";
            Canvas.SetTop(BlockDescription, 14);
        }

        public HatBlock() : this(null, null) { }
    }
}
