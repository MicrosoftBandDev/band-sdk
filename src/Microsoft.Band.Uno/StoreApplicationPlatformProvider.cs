// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.StoreApplicationPlatformProvider
// Assembly: Microsoft.Band.Store, Version=1.3.20628.2, Culture=neutral, PublicKeyToken=608d7da3159f502b
// MVID: 91750BE8-70C6-4542-841C-664EE611AF0B
// Assembly location: .\netcore451\Microsoft.Band.Store.dll

using Microsoft.Band.Tiles;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Store;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Popups;

namespace Microsoft.Band
{
    internal sealed class StoreApplicationPlatformProvider : IApplicationPlatformProvider
    {
        private static readonly IApplicationPlatformProvider current = new StoreApplicationPlatformProvider();

        private StoreApplicationPlatformProvider()
        {
        }

        public static IApplicationPlatformProvider Current => current;

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public Task<Guid> GetApplicationIdAsync(CancellationToken token)
        {
            bool useDebugId = false;
#if DEBUG
            useDebugId = true;
#endif
            Guid result;
            try
            {
                result = CurrentApp.AppId;
            }
            catch
            {
                IBuffer binary = CryptographicBuffer.ConvertStringToBinary(Package.Current.Id.Name, 0);
                result = new Guid(CryptographicBuffer.EncodeToHexString(HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Md5).HashData(binary)));
            }
            if (result == Guid.Empty || useDebugId)
                result = new Guid(Encoding.UTF8.GetBytes("#DEBUG-ONLY-GUID"));
            return Task.FromResult(result);
        }

        public Task<bool> GetAddTileConsentAsync(BandTile tile, CancellationToken token)
            => RequestConsentAsync(string.Format(BandResources.AddTileConsentPrompt, new object[] { tile.Name }), token);

        public UserConsent GetCurrentSensorConsent(Type sensorType)
        {
            object obj = ApplicationData.Current.LocalSettings.Values[CreateSensorAccessConsentSettingsKey(sensorType)];
            if (obj == null || obj is not bool flag)
                return UserConsent.NotSpecified;
            return !flag ? UserConsent.Declined : UserConsent.Granted;
        }

        public async Task<bool> RequestSensorConsentAsync(Type sensorType, string prompt, CancellationToken token)
        {
            bool flag = await RequestConsentAsync(prompt, token);
            ApplicationData.Current.LocalSettings.Values[CreateSensorAccessConsentSettingsKey(sensorType)] = flag;
            return flag;
        }

        private static async Task<bool> RequestConsentAsync(string prompt, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            MessageDialog dialog = new(prompt, BandResources.ConsentDialogTitle);
            dialog.Commands.Add(new UICommand("Yes", command => command.Id = true));
            dialog.Commands.Add(new UICommand("No", command => command.Id = false));
            dialog.DefaultCommandIndex = 0;
            dialog.CancelCommandIndex = 1;

            return (await dialog.ShowAsync()).Id as bool? == true;
        }

        private static string CreateSensorAccessConsentSettingsKey(Type sensorType)
            => "BandSensorAccessConcent-" + sensorType.ToString();
    }
}
