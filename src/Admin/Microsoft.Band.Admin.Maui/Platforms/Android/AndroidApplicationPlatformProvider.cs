using Android.App;
using Android.Content;
using Microsoft.Band.Tiles;
using Microsoft.Maui.Essentials;
using Plugin.Settings;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Band
{
    internal sealed class AndroidApplicationPlatformProvider : IApplicationPlatformProvider
    {
        private static readonly IApplicationPlatformProvider current = new AndroidApplicationPlatformProvider();

        private AndroidApplicationPlatformProvider()
        {
        }

        public static IApplicationPlatformProvider Current => current;

        public Task<Guid> GetApplicationIdAsync(CancellationToken token)
        {
            bool useDebugId = false;
#if DEBUG
            useDebugId = true;
#endif
            var hash = System.Security.Cryptography.MD5.HashData(Encoding.UTF8.GetBytes(AppInfo.PackageName));
            Guid result = new(hash);

            if (result == Guid.Empty || useDebugId)
                result = new Guid(Encoding.UTF8.GetBytes("#DEBUG-ONLY-GUID"));
            return Task.FromResult(result);
        }

        public Task<bool> GetAddTileConsentAsync(BandTile tile, Context context, CancellationToken token)
            => RequestConsentAsync(string.Format(BandResources.AddTileConsentPrompt, tile.Name), context, token);

        public UserConsent GetCurrentSensorConsent(Type sensorType)
        {
            bool flag = CrossSettings.Current.GetValueOrDefault(CreateSensorAccessConsentSettingsKey(sensorType), false);
            return !flag ? UserConsent.Declined : UserConsent.Granted;
        }

        public async Task<bool> RequestSensorConsentAsync(Type sensorType, string prompt, Context context, CancellationToken token)
        {
            bool flag = await RequestConsentAsync(prompt, context, token);
            CrossSettings.Current.AddOrUpdateValue(CreateSensorAccessConsentSettingsKey(sensorType), flag);
            return flag;
        }

        private static async Task<bool> RequestConsentAsync(string prompt, Context context, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            bool accepted = false;

            AlertDialog.Builder builder = new(context);
            builder.SetTitle(BandResources.ConsentDialogTitle);
            builder.SetMessage(prompt);
            builder.SetPositiveButton("Yes", (s, e) => accepted = true);
            builder.SetNegativeButton("No", (s, e) => accepted = false);

            builder.Show();
            return accepted;
        }

        private static string CreateSensorAccessConsentSettingsKey(Type sensorType)
            => "BandSensorAccessConcent-" + sensorType.ToString();

        public Task<bool> GetAddTileConsentAsync(BandTile tile, CancellationToken token)
            => GetAddTileConsentAsync(tile, null, token);

        public Task<bool> RequestSensorConsentAsync(Type sensorType, string prompt, CancellationToken token)
            => RequestSensorConsentAsync(sensorType, null, token);
    }
}
