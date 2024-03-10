using Windows.UI;
using CodeBlocks.Core;

namespace CodeBlocks.Controls
{
    public enum BlockValueType { None = 0, Text = 1, Number = 2, Boolean = 3 }

    public class ValueBlock : BaseBlock
    {
        private BlockValueType type;

        public BlockValueType ValueType
        {
            get => type;
            set
            {
                type = value;
                OnTypeChanged();
            }
        }

        public ValueBlock() : base()
        {
            BlockColor = Color.FromArgb(255, 150, 25, 25);
            MetaData = new() { Type = BlockType.ValueBlock, Variant = 1, Size = this.Size };
        }

        private void OnTypeChanged()
        {
            
        }
    }
}
