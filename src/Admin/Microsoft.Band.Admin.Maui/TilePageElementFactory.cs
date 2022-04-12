namespace Microsoft.Band.Admin;

public class TilePageElementFactory : ITilePageElementFactory
{
    public ITileBarcode CreateTileBarcode(ushort elementId, BarcodeType codeType, string barcodeValue)
    {
        return new TileBarcode(elementId, codeType, barcodeValue);
    }

    public ITileIconbox CreateTileIconbox(ushort elementId, ushort iconIndex)
    {
        return new TileIconbox(elementId, iconIndex);
    }

    public ITileTextbox CreateTileTextbox(ushort elementId, string textboxValue)
    {
        return new TileTextbox(elementId, textboxValue);
    }

    public ITileWrappableTextbox CreateTileWrappableTextbox(ushort elementId, string textboxValue)
    {
        return new TileWrappableTextbox(elementId, textboxValue);
    }
}
