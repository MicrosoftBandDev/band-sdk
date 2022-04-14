using System;

namespace Microsoft.Band.Admin;

internal sealed class TileIconbox : TilePageElement, ITileIconbox, ITilePageElement
{
    private ushort iconIndex;

    public ushort IconIndex
    {
        get
        {
            return iconIndex;
        }
        set
        {
            if (value >= 10)
            {
                throw new ArgumentOutOfRangeException("IconIndex");
            }
            iconIndex = value;
        }
    }

    internal override ushort ElementType => 3101;

    internal TileIconbox(ushort elementId, ushort iconIndex)
        : base(elementId)
    {
        IconIndex = iconIndex;
    }
}
