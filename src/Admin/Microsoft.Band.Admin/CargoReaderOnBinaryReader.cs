// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.CargoReaderOnBinaryReader
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System;
using System.IO;
using System.Text;

namespace Microsoft.Band.Admin
{
  public sealed class CargoReaderOnBinaryReader : ICargoReader, IDisposable
  {
    private BinaryReader reader;

    public CargoReaderOnBinaryReader(BinaryReader reader) => this.reader = reader;

    public int Read(byte[] destination) => this.Read(destination, 0, destination.Length);

    public int Read(byte[] destination, int offset, int count)
    {
      this.CheckIfDisposed();
      return this.reader.Read(destination, offset, count);
    }

    public void ReadExact(byte[] destination, int offset, int count)
    {
      this.CheckIfDisposed();
      int num;
      for (int index = 0; index < count; index += num)
      {
        num = this.Read(destination, offset + index, count - index);
        if (num == 0)
          throw new EndOfStreamException();
      }
    }

    public byte[] ReadExact(int count)
    {
      this.CheckIfDisposed();
      byte[] destination = new byte[count];
      this.ReadExact(destination, 0, count);
      return destination;
    }

    public void ReadExactAndDiscard(int count) => this.ReadExact(count);

    public byte ReadByte()
    {
      this.CheckIfDisposed();
      return this.reader.ReadByte();
    }

    public short ReadInt16()
    {
      this.CheckIfDisposed();
      return this.reader.ReadInt16();
    }

    public ushort ReadUInt16()
    {
      this.CheckIfDisposed();
      return this.reader.ReadUInt16();
    }

    public int ReadInt32()
    {
      this.CheckIfDisposed();
      return this.reader.ReadInt32();
    }

    public uint ReadUInt32()
    {
      this.CheckIfDisposed();
      return this.reader.ReadUInt32();
    }

    public long ReadInt64()
    {
      this.CheckIfDisposed();
      return this.reader.ReadInt64();
    }

    public ulong ReadUInt64()
    {
      this.CheckIfDisposed();
      return this.reader.ReadUInt64();
    }

    public bool ReadBool32()
    {
      this.CheckIfDisposed();
      return (uint) this.reader.ReadInt32() > 0U;
    }

    public Guid ReadGuid()
    {
      this.CheckIfDisposed();
      return new Guid(this.reader.ReadBytes(16));
    }

    public string ReadString(int length)
    {
      this.CheckIfDisposed();
      int count = length * 2;
      byte[] bytes = this.ReadExact(count);
      int index = 0;
      while (index < count && bytes[index] != (byte) 0)
        index += 2;
      return Encoding.Unicode.GetString(bytes, 0, count);
    }

    private void CheckIfDisposed()
    {
      if (this.reader == null)
        throw new ObjectDisposedException(nameof (CargoReaderOnBinaryReader));
    }

    public void Dispose()
    {
      if (this.reader == null)
        return;
      this.reader.Dispose();
      this.reader = (BinaryReader) null;
    }
  }
}
