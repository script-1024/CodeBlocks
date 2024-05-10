using CodeBlocks.Core;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;

namespace CodeBlocks.Controls
{
    public class ProcessBlock : CodeBlock
    {
        public ProcessBlock(BlockCreatedEventHandler handler, BlockCreatedEventArgs args = null) : base(handler, args)
        {
            BlockColor = (App.Current.Resources["ControlBlockColorBrush"] as SolidColorBrush).Color;
            MetaData = new() { Type = BlockType.ProcessBlock, Variant = 10, Size = this.Size };
            TranslationKey = "Blocks.ProcessBlock.Say.Text";
            Canvas.SetTop(BlockDescription, 16);
        }

        public ProcessBlock() : this(null, null) { }
    }
}
