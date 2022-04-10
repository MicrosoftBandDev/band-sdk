// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.TileMessage
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System;

namespace Microsoft.Band.Admin
{
  public sealed class TileMessage
  {
    private string title;
    private string body;
    public bool timestampHasValue;
    private DateTime timestamp;
    public NotificationFlags flags;

    public TileMessage(string title, string body)
    {
      this.Title = title;
      this.Body = body;
      this.timestampHasValue = false;
    }

    public TileMessage(string title, string body, DateTime timestamp, NotificationFlags flagbits = NotificationFlags.UnmodifiedNotificationSettings)
    {
      this.Title = title;
      this.Body = body;
      this.Timestamp = timestamp;
      this.Flags = flagbits;
    }

    public string Title
    {
      get => this.title;
      set => this.title = value != null ? value : throw new ArgumentNullException(nameof (Title));
    }

    public string Body
    {
      get => this.body;
      set => this.body = value != null ? value : throw new ArgumentNullException(nameof (Body));
    }

    public DateTime Timestamp
    {
      get => this.timestamp;
      set
      {
        this.timestamp = value;
        this.timestampHasValue = true;
      }
    }

    public NotificationFlags Flags
    {
      get => this.flags;
      set => this.flags = value;
    }
  }
}
