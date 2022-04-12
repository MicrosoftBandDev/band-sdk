using System;

namespace Microsoft.Band.Admin;

internal sealed class TileTextbox : TilePageElement, ITileTextbox, ITilePageElement
{
    private string textboxValue;

    public string TextboxValue
    {
        get
        {
            return textboxValue;
        }
        set
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            textboxValue = value;
        }
    }

    internal override ushort ElementType => 3001;

    internal TileTextbox(ushort elementId, string textboxValue)
        : base(elementId)
    {
        TextboxValue = textboxValue;
    }
}
