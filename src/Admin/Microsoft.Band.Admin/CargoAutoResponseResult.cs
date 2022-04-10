// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.CargoAutoResponseResult
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using Google.Protobuf;
using Google.Protobuf.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Band.Admin
{
  public sealed class CargoAutoResponseResult : IEnumerable<string>, IEnumerable
  {
    internal const int MaxReplies = 4;
    internal const int MaxRepliesSerializedBytes = 80;
    private static readonly FieldCodec<string> protobufCodec = FieldCodec.ForString(66U);
    private List<string> candidates;
    private RepeatedField<string> protobufCandidates;
    private int dataLength;

    public int Count => this.candidates == null ? 0 : this.candidates.Count;

    public bool AddCandidate(string candidate)
    {
      if (this.candidates == null)
        this.candidates = new List<string>();
      if (this.candidates.Count >= 4 || this.dataLength >= 80)
        return false;
      this.candidates.Add(candidate);
      this.protobufCandidates = (RepeatedField<string>) null;
      return true;
    }

    public IEnumerator<string> GetEnumerator()
    {
      if (this.candidates == null)
        this.candidates = new List<string>();
      return (IEnumerator<string>) this.candidates.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => (IEnumerator) this.GetEnumerator();

    internal int GetSerializedByteCount(int bytesAvailable)
    {
      if (this.Count == 0)
        return 0;
      int serializedByteCount = 0;
      if (bytesAvailable > 6 && this.candidates != null && this.candidates.Count > 0)
      {
        bytesAvailable = Math.Min(bytesAvailable, 80);
        serializedByteCount += 2;
        bytesAvailable -= 2;
        foreach (string s in this.Take<string>(4))
        {
          int num = Encoding.Unicode.GetByteCount(s) + 2;
          if (bytesAvailable >= num + 2)
          {
            serializedByteCount += num;
            bytesAvailable -= num;
          }
          else
            break;
        }
        if (bytesAvailable > 0)
          serializedByteCount += bytesAvailable;
      }
      return serializedByteCount;
    }

    internal void SerializeToBand(ICargoWriter writer, int bytesAvailable)
    {
      bytesAvailable = Math.Min(bytesAvailable, 80);
      writer.WriteByte((byte) 0, 2);
      bytesAvailable -= 2;
      foreach (string s in this.Take<string>(4))
      {
        int num = Encoding.Unicode.GetByteCount(s) + 2;
        if (bytesAvailable >= num + 2)
        {
          writer.WriteString(s);
          writer.WriteByte((byte) 0, 2);
          bytesAvailable -= num;
        }
        else
          break;
      }
      writer.WriteByte((byte) 0, bytesAvailable);
    }

    internal int GetSerializedProtobufByteCount()
    {
      if (this.Count == 0)
        return 0;
      this.PopulateProtobufCandidates();
      return this.protobufCandidates.CalculateSize(CargoAutoResponseResult.protobufCodec);
    }

    internal void SerializeProtobufToBand(CodedOutputStream output)
    {
      if (this.Count == 0)
        return;
      this.PopulateProtobufCandidates();
      this.protobufCandidates.WriteTo(output, CargoAutoResponseResult.protobufCodec);
    }

    private void PopulateProtobufCandidates()
    {
      if (this.protobufCandidates != null)
        return;
      this.protobufCandidates = new RepeatedField<string>();
      foreach (string s in this.Take<string>(4))
      {
        int danglingHighSurrogate = BandUtf8Encoding.GetCharCountToMaxUtf8ByteCountTrimDanglingHighSurrogate(s, 160);
        this.protobufCandidates.Add(s.Truncate(danglingHighSurrogate));
      }
    }
  }
}
