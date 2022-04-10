// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.DynamicBandConstants
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

namespace Microsoft.Band.Admin
{
    internal class DynamicBandConstants : BandTypeConstants, IDynamicBandConstants
    {
        internal static readonly DynamicBandConstants CargoConstants = new(BandClass.Cargo);
        internal static readonly DynamicBandConstants EnvoyConstants = new(BandClass.Envoy);

        private DynamicBandConstants(BandClass bandClass)
          : base(bandClass.ToBandType())
        {
            this.BandClass = bandClass;
        }

        public BandClass BandClass { get; private set; }
        ushort IDynamicBandConstants.MeTileWidth => this.MeTileWidth;

        ushort IDynamicBandConstants.MeTileHeight => this.MeTileHeight;

        ushort IDynamicBandConstants.TileIconPreferredSize => this.TileIconPreferredSize;

        ushort IDynamicBandConstants.BadgeIconPreferredSize => this.BadgeIconPreferredSize;

        ushort IDynamicBandConstants.NotificiationIconPreferredSize => this.NotificiationIconPreferredSize;

        int IDynamicBandConstants.MaxIconsPerTile => this.MaxIconsPerTile;
    }
}
