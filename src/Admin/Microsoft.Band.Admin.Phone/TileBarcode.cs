// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.TileBarcode
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

using System;

namespace Microsoft.Band.Admin
{
  internal sealed class TileBarcode : TilePageElement, ITileBarcode, ITilePageElement
  {
    private string barcodeValue;

    internal TileBarcode(ushort elementId, BarcodeType codeType, string barcodeValue)
      : base(elementId)
    {
      this.CodeType = codeType;
      this.BarcodeValue = barcodeValue;
    }

    public BarcodeType CodeType { get; set; }

    public string BarcodeValue
    {
      get => this.barcodeValue;
      set => this.barcodeValue = value != null ? value : throw new ArgumentNullException(nameof (value));
    }

    internal override ushort ElementType
    {
      get
      {
        switch (this.CodeType)
        {
          case BarcodeType.Code39:
            return 3201;
          case BarcodeType.Pdf417:
            return 3202;
          default:
            throw new BandException("CodeType");
        }
      }
    }
  }
}
