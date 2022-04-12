// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.Phone.BandAdminClientManager
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

using Microsoft.Band.Store;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.Band.Admin.Phone
{
  public class BandAdminClientManager : BandAdminClientManagerBase, IBandAdminClientManager
  {
    private static readonly BandAdminClientManager instance = new BandAdminClientManager();
    private const int MaximumBluetoothConnectAttempts = 3;

    private BandAdminClientManager()
    {
    }

    public static IBandAdminClientManager Instance => (IBandAdminClientManager) BandAdminClientManager.ConcreteInstance;

    internal static BandAdminClientManager ConcreteInstance => BandAdminClientManager.instance;

    internal override async Task<CargoClient> ConcreteConnectAsync(IBandInfo bandInfo)
    {
      LoggerProvider loggerProvider = (LoggerProvider) null;
      PhoneProvider phoneProvider = (PhoneProvider) null;
      BluetoothTransport bluetoothTransport = (BluetoothTransport) null;
      CargoClient client = (CargoClient) null;
      try
      {
        Stopwatch connectTime = Stopwatch.StartNew();
        loggerProvider = new LoggerProvider();
        phoneProvider = new PhoneProvider();
        bluetoothTransport = await BluetoothTransport.CreateAsync(bandInfo, (ILoggerProvider) loggerProvider, (ushort) 3);
        client = new CargoClient((IDeviceTransport) bluetoothTransport, (CloudProvider) null, (ILoggerProvider) loggerProvider, (IPlatformProvider) phoneProvider, StoreApplicationPlatformProvider.Current);
        client.InitializeCachedProperties();
        loggerProvider.Log(ProviderLogLevel.Info, "BandAdminClientManager.ConcreteConnectAsync(IBandInfo bandinfo) succeeded: Elapsed: {0}", new object[1]
        {
          (object) connectTime.Elapsed
        });
        connectTime = (Stopwatch) null;
      }
      catch
      {
        if (client != null)
          client.Dispose();
        else
          bluetoothTransport?.Dispose();
        throw;
      }
      return client;
    }

    internal override async Task<CargoClient> ConcreteConnectAsync(
      IBandInfo bandInfo,
      string userId)
    {
      LoggerProvider loggerProvider = (LoggerProvider) null;
      PhoneProvider phoneProvider = (PhoneProvider) null;
      BluetoothTransport bluetoothTransport = (BluetoothTransport) null;
      CargoClient client = (CargoClient) null;
      try
      {
        Stopwatch connectTime = Stopwatch.StartNew();
        loggerProvider = new LoggerProvider();
        phoneProvider = new PhoneProvider();
        bluetoothTransport = await BluetoothTransport.CreateAsync(bandInfo, (ILoggerProvider) loggerProvider, (ushort) 3);
        client = new CargoClient((IDeviceTransport) bluetoothTransport, (CloudProvider) null, (ILoggerProvider) loggerProvider, (IPlatformProvider) phoneProvider, StoreApplicationPlatformProvider.Current);
        client.InitializeCachedProperties();
        client.InitializeStorageProvider((IStorageProvider) StorageProvider.Create(userId, client.DeviceUniqueId.ToString("N")));
        loggerProvider.Log(ProviderLogLevel.Info, "BandAdminClientManager.ConcreteConnectAsync(IBandInfo bandInfo, string userId) succeeded: Elapsed: {0}", new object[1]
        {
          (object) connectTime.Elapsed
        });
        connectTime = (Stopwatch) null;
      }
      catch
      {
        if (client != null)
          client.Dispose();
        else
          bluetoothTransport?.Dispose();
        throw;
      }
      return client;
    }

    internal override async Task<CargoClient> ConcreteConnectAsync(
      IBandInfo bandInfo,
      ServiceInfo serviceInfo)
    {
      LoggerProvider loggerProvider = (LoggerProvider) null;
      PhoneProvider phoneProvider = (PhoneProvider) null;
      BluetoothTransport bluetoothTransport = (BluetoothTransport) null;
      CargoClient client = (CargoClient) null;
      try
      {
        Stopwatch connectTime = Stopwatch.StartNew();
        loggerProvider = new LoggerProvider();
        phoneProvider = new PhoneProvider();
        bluetoothTransport = await BluetoothTransport.CreateAsync(bandInfo, (ILoggerProvider) loggerProvider, (ushort) 3);
        CloudProvider cloudProvider = new CloudProvider(serviceInfo);
        client = new CargoClient((IDeviceTransport) bluetoothTransport, cloudProvider, (ILoggerProvider) loggerProvider, (IPlatformProvider) phoneProvider, StoreApplicationPlatformProvider.Current);
        client.InitializeCachedProperties();
        cloudProvider.SetUserAgent(phoneProvider.GetDefaultUserAgent(client.FirmwareVersions), false);
        client.InitializeStorageProvider((IStorageProvider) StorageProvider.Create(serviceInfo.UserId, client.DeviceUniqueId.ToString("N")));
        loggerProvider.Log(ProviderLogLevel.Info, "BandAdminClientManager.ConcreteConnectAsync(IBandInfo bandInfo, ServiceInfo serviceInfo) succeeded: Elapsed: {0}", new object[1]
        {
          (object) connectTime.Elapsed
        });
        connectTime = (Stopwatch) null;
      }
      catch
      {
        if (client != null)
          client.Dispose();
        else
          bluetoothTransport?.Dispose();
        throw;
      }
      return client;
    }
  }
}
