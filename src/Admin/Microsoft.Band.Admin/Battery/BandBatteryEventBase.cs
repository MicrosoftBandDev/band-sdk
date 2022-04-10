// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.Battery.BandBatteryEventBase
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

namespace Microsoft.Band.Admin.Battery
{
    internal abstract class BandBatteryEventBase
    {
        internal static BandBatteryEventBase DeserializeFromBand(ICargoReader reader) => BandBatteryEventBase.GetEmptyBatteryEvent(reader);

        private static BandBatteryEventBase GetEmptyBatteryEvent(ICargoReader reader) => reader.ReadByte() == (byte)1 ? (BandBatteryEventBase)new BandBatteryFullEvent() : (BandBatteryEventBase)null;
    }
}
