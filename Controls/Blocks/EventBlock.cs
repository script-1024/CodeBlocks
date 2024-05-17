using CodeBlocks.Core;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;

namespace CodeBlocks.Controls
{
    public class EventBlock : CodeBlock
    {
        public EventBlock(BlockCreatedEventHandler handler, BlockCreatedEventArgs args = null) : base(handler, args)
        {
            BlockColor = (App.Current.Resources["EventBlockColorBrush"] as SolidColorBrush).Color;
            MetaData = new() { Type = BlockType.Event, Variant = 8, Size = this.Size };
            TranslationKey = "Blocks.EventBlock.FunctionEntry.Text";
            Canvas.SetTop(BlockDescription, 14);
        }

        public EventBlock() : this(null, null) { }
    }
}
