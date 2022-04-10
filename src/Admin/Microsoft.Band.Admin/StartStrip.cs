// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.StartStrip
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Band.Admin
{
  public sealed class StartStrip : 
    IList<AdminBandTile>,
    ICollection<AdminBandTile>,
    IEnumerable<AdminBandTile>,
    IEnumerable
  {
    private List<AdminBandTile> internalList;

    public StartStrip() => this.internalList = new List<AdminBandTile>();

    public StartStrip(IEnumerable<AdminBandTile> tiles) => this.internalList = tiles != null ? tiles.ToList<AdminBandTile>() : throw new ArgumentNullException(nameof (tiles));

    public bool Contains(Guid guid) => this.internalList.Any<AdminBandTile>((Func<AdminBandTile, bool>) (t => t.Id == guid));

    public bool Remove(Guid guid)
    {
      for (int index = 0; index < this.internalList.Count; ++index)
      {
        if (this.internalList[index].Id == guid)
        {
          this.internalList.RemoveAt(index);
          return true;
        }
      }
      return false;
    }

    public int IndexOf(Guid guid)
    {
      for (int index = 0; index < this.internalList.Count; ++index)
      {
        if (this.internalList[index].Id == guid)
          return index;
      }
      return -1;
    }

    public IEnumerator<AdminBandTile> GetEnumerator() => (IEnumerator<AdminBandTile>) this.internalList.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => (IEnumerator) this.internalList.GetEnumerator();

    public int Count => this.internalList.Count;

    public bool IsReadOnly => false;

    public void Add(AdminBandTile item)
    {
      if (this.Contains(item.Id))
        throw new ArgumentException(string.Format(CommonSR.GenericRepeatEntry, new object[3]
        {
          (object) nameof (StartStrip),
          (object) "Tile",
          (object) "GUID"
        }));
      this.internalList.Add(item);
    }

    public void Clear() => this.internalList.Clear();

    public bool Contains(AdminBandTile item) => this.internalList.Contains(item);

    public void CopyTo(AdminBandTile[] array, int arrayIndex) => this.internalList.CopyTo(array, arrayIndex);

    public bool Remove(AdminBandTile item) => this.internalList.Remove(item);

    public AdminBandTile this[int index]
    {
      get => this.internalList[index];
      set
      {
        int num = value != null ? this.IndexOf(value.Id) : throw new ArgumentNullException(nameof (value));
        if (num != -1 && num != index)
          throw new ArgumentException(string.Format(CommonSR.GenericRepeatEntry, new object[3]
          {
            (object) nameof (StartStrip),
            (object) "Tile",
            (object) "GUID"
          }));
        this.internalList[index] = value;
      }
    }

    public int IndexOf(AdminBandTile item) => this.internalList.IndexOf(item);

    public void Insert(int index, AdminBandTile item)
    {
      if (item == null)
        throw new ArgumentNullException(nameof (item));
      if (this.Contains(item.Id))
        throw new ArgumentException(string.Format(CommonSR.GenericRepeatEntry, new object[3]
        {
          (object) nameof (StartStrip),
          (object) "Tile",
          (object) "GUID"
        }));
      this.internalList.Insert(index, item);
    }

    public void RemoveAt(int index) => this.internalList.RemoveAt(index);
  }
}
