using System;

namespace Microsoft.Band.Admin;

internal sealed class TileBarcode : TilePageElement, ITileBarcode, ITilePageElement
{
    private string barcodeValue;

    public BarcodeType CodeType { get; set; }

    public string BarcodeValue
    {
        get
        {
            return barcodeValue;
        }
        set
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            barcodeValue = value;
        }
    }

    internal override ushort ElementType => CodeType switch
    {
        BarcodeType.Code39 => 3201, 
        BarcodeType.Pdf417 => 3202, 
        _ => throw new BandException("CodeType"), 
    };

    internal TileBarcode(ushort elementId, BarcodeType codeType, string barcodeValue)
        : base(elementId)
    {
        CodeType = codeType;
        BarcodeValue = barcodeValue;
    }
}
