using Microsoft.Band.Windows;
using System;

namespace Microsoft.Band.Admin
{
    partial class CargoClient
    {
        public static CargoClient CreateRestrictedClient(IBandInfo deviceInfo)
        {
            if (deviceInfo == null)
            {
                ArgumentNullException ex = new ArgumentNullException("deviceInfo");
                Logger.LogException(LogLevel.Error, ex);
                throw ex;
            }
            if (deviceInfo is not BluetoothDeviceInfo)
            {
                Logger.Log(LogLevel.Error, "deviceInfo is not BluetoothDeviceInfo");
                throw new ArgumentException("deviceInfo");
            }
            CargoClient cargoClient = null;
            IDeviceTransport deviceTransport = BluetoothTransport.Create(deviceInfo, new LoggerProvider(), 2);
            try
            {
                cargoClient = new CargoClient(deviceTransport, StoreApplicationPlatformProvider.Current);
                cargoClient.InitializeCachedProperties();
                Logger.Log(LogLevel.Info, "Created CargoClient (Restricted)");
                return cargoClient;
            }
            catch
            {
                if (cargoClient != null)
                {
                    cargoClient.Dispose();
                }
                else
                {
                    deviceTransport.Dispose();
                }
                throw;
            }
        }
    }
}
