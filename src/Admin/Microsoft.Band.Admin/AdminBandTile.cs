// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.AdminBandTile
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using Microsoft.Band.Tiles;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Band.Admin
{
    public sealed class AdminBandTile
    {
        private BandIcon image;
        public static readonly Guid WebTileOwnerId = new Guid("124bee1a-8bff-c433-4836-3f6ff700f2db");

        public AdminBandTile(Guid id, string name, AdminTileSettings tileSettings)
        {
            this.Id = id;
            this.Name = name;
            this.SettingsMask = tileSettings;
            this.Images = (IList<BandIcon>)null;
            this.TileImageIndex = 0U;
            this.BadgeImageIndex = 0U;
            this.NotificationImageIndex = 0U;
            this.LayoutsToRemove = (IList<uint>)new List<uint>();
            this.Layouts = new Dictionary<uint, TileLayout>();
        }

        public AdminBandTile(Guid id, string name, AdminTileSettings tileSettings, BandIcon tileIcon)
          : this(id, name, tileSettings)
        {
            this.image = tileIcon;
        }

        public AdminBandTile(
          Guid id,
          string name,
          AdminTileSettings tileSettings,
          IList<BandIcon> icons,
          uint tileIconIndex,
          IList<TileLayout> layouts = null,
          uint? badgeIconIndex = null,
          uint? notificationIconIndex = null)
        {
            this.Id = id;
            this.SetName(id, name);
            this.SettingsMask = tileSettings;
            this.SetImageList(id, icons, tileIconIndex, badgeIconIndex, notificationIconIndex);
            this.LayoutsToRemove = (IList<uint>)new List<uint>();
            this.Layouts = new Dictionary<uint, TileLayout>();
            if (layouts == null)
                return;
            if (layouts.Count > 5)
                throw new ArgumentException(string.Format(CommonSR.GenericCountMax, new object[1]
                {
          (object) nameof (layouts)
                }));
            for (int index = 0; index < layouts.Count; ++index)
                this.Layouts.Add((uint)index, layouts[index]);
        }

        public bool IsWebTile => this.OwnerId == AdminBandTile.WebTileOwnerId;

        public Guid OwnerId { get; set; }

        public Guid TileId => this.Id;

        public Guid Id { get; private set; }

        public string Name { get; internal set; }

        public AdminTileSettings SettingsMask { get; internal set; }

        public BandTheme Theme { get; internal set; }

        public IList<BandIcon> Images { get; private set; }

        public uint TileImageIndex { get; private set; }

        public uint BadgeImageIndex { get; private set; }

        public uint NotificationImageIndex { get; private set; }

        public BandIcon Image => this.Images != null && this.TileImageIndex >= 0U && (long)this.TileImageIndex < (long)this.Images.Count ? this.Images[(int)this.TileImageIndex] : this.image;

        public IList<uint> LayoutsToRemove { get; set; }

        public Dictionary<uint, TileLayout> Layouts { get; set; }

        public bool IdEquals(Guid id) => id == this.Id;

        public void SetName(Guid id, string name)
        {
            if (id != this.Id)
                throw new ArgumentException(CommonSR.IncorrectGuid);
            switch (name)
            {
                case "":
                    throw new ArgumentException(string.Format(CommonSR.GenericLengthZero, new object[1]
                    {
            (object) nameof (name)
                    }));
                case null:
                    throw new ArgumentNullException(nameof(name));
                default:
                    this.Name = name.Length <= 29 ? name : throw new ArgumentException(string.Format(BandResources.GenericLengthExceeded, new object[1]
                    {
            (object) nameof (name)
                    }));
                    break;
            }
        }

        public void SetSettings(Guid id, AdminTileSettings settings)
        {
            if (id != this.Id)
                throw new ArgumentException(CommonSR.IncorrectGuid);
            this.SettingsMask = settings;
        }

        public void SetImageList(
          Guid id,
          IList<BandIcon> icons,
          uint tileIconIndex,
          uint? badgeIconIndex = null,
          uint? notificationIconIndex = null)
        {
            if (id != this.Id)
                throw new ArgumentException(CommonSR.IncorrectGuid);
            if (icons == null)
                throw new ArgumentNullException(nameof(icons));
            if (icons.Count == 0)
                throw new ArgumentException(string.Format(CommonSR.GenericCountZero, new object[1]
                {
          (object) nameof (icons)
                }));
            if (icons.Count > 10)
                throw new ArgumentException(string.Format(CommonSR.GenericCountMax, new object[1]
                {
          (object) nameof (icons)
                }));
            if ((long)tileIconIndex >= (long)icons.Count)
                throw new ArgumentOutOfRangeException(nameof(tileIconIndex));
            if (!badgeIconIndex.HasValue)
                badgeIconIndex = (long)icons.Count <= (long)(tileIconIndex + 1U) ? new uint?(tileIconIndex) : new uint?(tileIconIndex + 1U);
            uint? nullable1 = badgeIconIndex;
            long? nullable2 = nullable1.HasValue ? new long?((long)nullable1.GetValueOrDefault()) : new long?();
            long count1 = (long)icons.Count;
            if ((nullable2.GetValueOrDefault() >= count1 ? (nullable2.HasValue ? 1 : 0) : 0) != 0)
                throw new IndexOutOfRangeException("badgeIconIndex is greater than number of icons");
            if (!notificationIconIndex.HasValue)
            {
                long count2 = (long)icons.Count;
                nullable1 = badgeIconIndex;
                uint num1 = 1;
                nullable2 = nullable1.HasValue ? new long?((long)(nullable1.GetValueOrDefault() + num1)) : new long?();
                long valueOrDefault = nullable2.GetValueOrDefault();
                if ((count2 > valueOrDefault ? (nullable2.HasValue ? 1 : 0) : 0) != 0)
                {
                    nullable1 = badgeIconIndex;
                    uint num2 = 1;
                    notificationIconIndex = nullable1.HasValue ? new uint?(nullable1.GetValueOrDefault() + num2) : new uint?();
                }
                else
                    notificationIconIndex = badgeIconIndex;
            }
            nullable1 = notificationIconIndex;
            nullable2 = nullable1.HasValue ? new long?((long)nullable1.GetValueOrDefault()) : new long?();
            long count3 = (long)icons.Count;
            if ((nullable2.GetValueOrDefault() >= count3 ? (nullable2.HasValue ? 1 : 0) : 0) != 0)
                throw new IndexOutOfRangeException("notificationIconIndex is greater than number of icons");
            this.Images = (IList<BandIcon>)icons.ToArray<BandIcon>();
            this.TileImageIndex = tileIconIndex;
            this.BadgeImageIndex = badgeIconIndex.Value;
            this.NotificationImageIndex = notificationIconIndex.Value;
        }

        public void SetLayout(Guid id, uint registeredIndex, TileLayout layout)
        {
            if (id != this.Id)
                throw new ArgumentException(CommonSR.IncorrectGuid);
            if (layout == null)
                throw new ArgumentNullException(nameof(layout));
            if (registeredIndex >= 5U)
                throw new ArgumentOutOfRangeException(nameof(registeredIndex));
            for (int index = 0; index < this.LayoutsToRemove.Count; ++index)
            {
                if ((int)this.LayoutsToRemove[index] == (int)registeredIndex)
                {
                    this.LayoutsToRemove.RemoveAt(index);
                    --index;
                }
            }
            if (this.Layouts.ContainsKey(registeredIndex))
                this.Layouts.Remove(registeredIndex);
            this.Layouts.Add(registeredIndex, layout);
        }

        public void RemoveLayout(Guid id, uint registeredIndex)
        {
            if (id != this.Id)
                throw new ArgumentException(CommonSR.IncorrectGuid);
            if (this.Layouts.ContainsKey(registeredIndex))
                this.Layouts.Remove(registeredIndex);
            if (this.LayoutsToRemove.Contains(registeredIndex))
                return;
            this.LayoutsToRemove.Add(registeredIndex);
        }
    }
}
