namespace Microsoft.Band.Admin;

public abstract class TilePageElement : ITilePageElement
{
    public ushort ElementId { get; set; }

    internal abstract ushort ElementType { get; }

    protected internal TilePageElement(ushort elementId)
    {
        ElementId = elementId;
    }
}
