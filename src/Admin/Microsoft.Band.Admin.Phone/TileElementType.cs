// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.TileElementType
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

namespace Microsoft.Band.Admin
{
  internal enum TileElementType : ushort
  {
    PageHeader = 1,
    Flowlist = 1001, // 0x03E9
    ScrollFlowlist = 1002, // 0x03EA
    FilledQuad = 1101, // 0x044D
    Text = 3001, // 0x0BB9
    WrappableText = 3002, // 0x0BBA
    Icon = 3101, // 0x0C1D
    BarcodeCode39 = 3201, // 0x0C81
    BarcodePDF417 = 3202, // 0x0C82
    Button = 3301, // 0x0CE5
    Invalid = 65535, // 0xFFFF
  }
}
