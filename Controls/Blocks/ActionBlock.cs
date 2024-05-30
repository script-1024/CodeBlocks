using CodeBlocks.Core;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;

namespace CodeBlocks.Controls
{
    public class ActionBlock : CodeBlock
    {
        public ActionBlock(BlockCreatedEventHandler handler, BlockCreatedEventArgs args = null) : base(handler, args)
        {
            BlockColor = (App.Current.Resources["ControlBlockColorBrush"] as SolidColorBrush).Color;
            MetaData = new() { Type = BlockType.Action, Variant = 10, Size = this.Size };
            TranslationKey = "Blocks.ActionBlock.Say.Text";
        }

        public ActionBlock() : this(null, null) { }
    }
}
