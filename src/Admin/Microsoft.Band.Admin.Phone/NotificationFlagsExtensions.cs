// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.NotificationFlagsExtensions
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

using Microsoft.Band.Notifications;
using System;

namespace Microsoft.Band.Admin
{
  internal static class NotificationFlagsExtensions
  {
    internal static BandNotificationFlags ToBandNotificationFlags(
      this NotificationFlags flags)
    {
      BandNotificationFlags notificationFlags = BandNotificationFlags.UnmodifiedNotificationSettings;
      if (flags.HasFlag((Enum) NotificationFlags.ForceNotificationDialog))
        notificationFlags |= BandNotificationFlags.ForceNotificationDialog;
      if (flags.HasFlag((Enum) NotificationFlags.SuppressNotificationDialog))
        notificationFlags |= BandNotificationFlags.SuppressNotificationDialog;
      if (flags.HasFlag((Enum) NotificationFlags.SuppressSmsReply))
        notificationFlags |= BandNotificationFlags.SuppressSmsReply;
      return notificationFlags;
    }
  }
}
