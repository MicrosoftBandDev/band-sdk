// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.CargoBikeDisplayMetrics
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Band.Admin
{
    public class CargoBikeDisplayMetrics
    {
        private const int MaximumDisplaySlots = 7;
        private static readonly HashSet<BikeDisplayMetricType> CargoValidMetrics = new HashSet<BikeDisplayMetricType>()
    {
      BikeDisplayMetricType.None,
      BikeDisplayMetricType.Duration,
      BikeDisplayMetricType.HeartRate,
      BikeDisplayMetricType.Calories,
      BikeDisplayMetricType.Distance,
      BikeDisplayMetricType.Speed
    };
        private BikeDisplayMetricType[] metrics;

        public CargoBikeDisplayMetrics() => this.metrics = Enumerable.Repeat<BikeDisplayMetricType>(BikeDisplayMetricType.None, 7).ToArray<BikeDisplayMetricType>();

        public CargoBikeDisplayMetrics(BikeDisplayMetricType[] metrics)
        {
            if (metrics == null)
                throw new ArgumentNullException(nameof(metrics));
            if (metrics.Length > 7)
                throw new ArgumentOutOfRangeException(nameof(metrics));
            if (metrics.Length < 7)
                this.metrics = ((IEnumerable<BikeDisplayMetricType>)metrics).Concat<BikeDisplayMetricType>(Enumerable.Repeat<BikeDisplayMetricType>(BikeDisplayMetricType.None, 7 - metrics.Length)).ToArray<BikeDisplayMetricType>();
            else
                this.metrics = metrics;
        }

        public int GetSize() => this.metrics.Length;

        public BikeDisplayMetricType this[int index]
        {
            get => this.metrics[index];
            set => this.metrics[index] = value;
        }

        public BikeDisplayMetricType PrimaryMetric
        {
            get => this[0];
            set => this[0] = value;
        }

        public BikeDisplayMetricType TopLeftMetric
        {
            get => this[1];
            set => this[1] = value;
        }

        public BikeDisplayMetricType TopRightMetric
        {
            get => this[2];
            set => this[2] = value;
        }

        public BikeDisplayMetricType DrawerTopLeftMetric
        {
            get => this[3];
            set => this[3] = value;
        }

        public BikeDisplayMetricType DrawerBottomLeftMetric
        {
            get => this[4];
            set => this[4] = value;
        }

        public BikeDisplayMetricType DrawerBottomRightMetric
        {
            get => this[5];
            set => this[5] = value;
        }

        public BikeDisplayMetricType Metric07
        {
            get => this[6];
            set => this[6] = value;
        }

        public CargoBikeDisplayMetrics Clone() => new CargoBikeDisplayMetrics((BikeDisplayMetricType[])this.metrics.Clone());

        internal bool IsValid(DynamicAdminBandConstants constants)
        {
            IOrderedEnumerable<BikeDisplayMetricType> orderedEnumerable = ((IEnumerable<BikeDisplayMetricType>)this.metrics).Take<BikeDisplayMetricType>(constants.BikeMetricsDisplaySlotCount).Where<BikeDisplayMetricType>((Func<BikeDisplayMetricType, bool>)(metric => (uint)metric > 0U)).OrderBy<BikeDisplayMetricType, BikeDisplayMetricType>((Func<BikeDisplayMetricType, BikeDisplayMetricType>)(metric => metric));
            BikeDisplayMetricType displayMetricType1 = BikeDisplayMetricType.None;
            foreach (BikeDisplayMetricType displayMetricType2 in (IEnumerable<BikeDisplayMetricType>)orderedEnumerable)
            {
                if (constants.BandClass == BandClass.Cargo && !CargoBikeDisplayMetrics.CargoValidMetrics.Contains(displayMetricType2) || displayMetricType2 == displayMetricType1)
                    return false;
                displayMetricType1 = displayMetricType2;
            }
            return true;
        }

        internal static int GetSerializedByteCount(DynamicAdminBandConstants constants) => constants.BikeMetricsDisplaySlotCount * 2;

        internal static CargoBikeDisplayMetrics DeserializeFromBand(
          ICargoReader reader,
          DynamicAdminBandConstants constants)
        {
            CargoBikeDisplayMetrics bikeDisplayMetrics = new CargoBikeDisplayMetrics();
            for (int index = 0; index < constants.BikeMetricsDisplaySlotCount; ++index)
                bikeDisplayMetrics[index] = (BikeDisplayMetricType)reader.ReadUInt16();
            return bikeDisplayMetrics;
        }

        internal void SerializeToBand(ICargoWriter writer, DynamicAdminBandConstants constants)
        {
            foreach (BikeDisplayMetricType i in ((IEnumerable<BikeDisplayMetricType>)this.metrics).Take<BikeDisplayMetricType>(constants.BikeMetricsDisplaySlotCount))
                writer.WriteUInt16((ushort)i);
        }
    }
}
