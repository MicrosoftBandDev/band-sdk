using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Band.Admin.Windows;
using Microsoft.Band.Windows;

namespace Microsoft.Band.Admin;

partial class BandAdminClientManagerBase
{
    internal Task<BluetoothDeviceInfo[]> ConcreteGetBandsAsync()
    {
        return Task.Run(() => ConcreteGetBands());
    }

    internal BluetoothDeviceInfo[] ConcreteGetBands()
    {
        return ConcreteGetBands(new Guid("{A502CA97-2BA5-413C-A4E0-13804E47B38F}"));
    }

    internal Task<BluetoothDeviceInfo[]> ConcreteGetBandsAsync(Guid service)
    {
        return Task.Run(() => ConcreteGetBands(service));
    }

    internal BluetoothDeviceInfo[] ConcreteGetBands(Guid service)
    {
        return BluetoothTransport.GetConnectedDevices(service, new LoggerProvider());
    }

    internal CargoClient ConcreteConnect(ServiceInfo serviceInfo)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        LoggerProvider loggerProvider = new();
        PhoneProvider phoneProvider = new();
        CloudProvider cloudProvider = new(serviceInfo);
        CargoClient result = new(null, cloudProvider, loggerProvider, phoneProvider, StoreApplicationPlatformProvider.Current);
        cloudProvider.SetUserAgent(phoneProvider.GetDefaultUserAgent(null), appOverride: false);
        loggerProvider.Log(ProviderLogLevel.Info, $"BandAdminClientManager.ConcreteConnect(ServiceInfo serviceInfo) succeeded: Elapsed: {stopwatch.Elapsed}");
        return result;
    }

    internal CargoClient ConcreteConnect(string bandId, ServiceInfo serviceInfo)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        LoggerProvider loggerProvider = new();
        PhoneProvider phoneProvider = new();
        CloudProvider cloudProvider = new(serviceInfo);
        CargoClient cargoClient = new(null, cloudProvider, loggerProvider, phoneProvider, StoreApplicationPlatformProvider.Current)
        {
            DeviceUniqueId = Guid.Parse(bandId),
            SerialNumber = null
        };
        cloudProvider.SetUserAgent(phoneProvider.GetDefaultUserAgent(null), appOverride: false);
        StorageProvider storageProvider = StorageProvider.Create(serviceInfo.UserId, cargoClient.DeviceUniqueId.ToString("N"));
        cargoClient.InitializeStorageProvider(storageProvider);
        loggerProvider.Log(ProviderLogLevel.Info, $"BandAdminClientManager.ConcreteConnect(string bandId, ServiceInfo serviceInfo) succeeded: Elapsed: {stopwatch.Elapsed}");
        return cargoClient;
    }
}
