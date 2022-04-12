namespace Microsoft.Band.Admin;

internal enum TileElementType : ushort
{
    PageHeader = 1,
    Flowlist = 1001,
    ScrollFlowlist = 1002,
    FilledQuad = 1101,
    Text = 3001,
    WrappableText = 3002,
    Icon = 3101,
    BarcodeCode39 = 3201,
    BarcodePDF417 = 3202,
    Button = 3301,
    Invalid = ushort.MaxValue
}
