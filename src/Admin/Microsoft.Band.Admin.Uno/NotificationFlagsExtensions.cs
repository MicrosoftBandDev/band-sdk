using Microsoft.Band.Notifications;

namespace Microsoft.Band.Admin;

internal static class NotificationFlagsExtensions
{
    internal static BandNotificationFlags ToBandNotificationFlags(this NotificationFlags flags)
    {
        BandNotificationFlags bandNotificationFlags = BandNotificationFlags.UnmodifiedNotificationSettings;
        if (flags.HasFlag(NotificationFlags.ForceNotificationDialog))
        {
            bandNotificationFlags |= BandNotificationFlags.ForceNotificationDialog;
        }
        if (flags.HasFlag(NotificationFlags.SuppressNotificationDialog))
        {
            bandNotificationFlags |= BandNotificationFlags.SuppressNotificationDialog;
        }
        if (flags.HasFlag(NotificationFlags.SuppressSmsReply))
        {
            bandNotificationFlags |= BandNotificationFlags.SuppressSmsReply;
        }
        return bandNotificationFlags;
    }
}
