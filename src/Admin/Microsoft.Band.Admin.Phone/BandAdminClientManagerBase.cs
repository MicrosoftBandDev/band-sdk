// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.BandAdminClientManagerBase
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

using Microsoft.Band.Admin.Phone;
using Microsoft.Band.Windows;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.Band.Admin
{
  public abstract class BandAdminClientManagerBase
  {
    public async Task<IBandInfo[]> GetBandsAsync() => await Task.Run<IBandInfo[]>((Func<IBandInfo[]>) (() => this.GetBands()));

    public IBandInfo[] GetBands() => (IBandInfo[]) this.ConcreteGetBands();

    public ICargoClient Connect(IBandInfo bandInfo) => (ICargoClient) this.ConcreteConnect(bandInfo);

    public async Task<ICargoClient> ConnectAsync(IBandInfo bandInfo) => (ICargoClient) await this.ConcreteConnectAsync(bandInfo);

    public ICargoClient Connect(ServiceInfo serviceInfo) => (ICargoClient) this.ConcreteConnect(serviceInfo);

    public Task<ICargoClient> ConnectAsync(ServiceInfo serviceInfo) => Task.FromResult<ICargoClient>((ICargoClient) this.ConcreteConnect(serviceInfo));

    public ICargoClient Connect(IBandInfo bandInfo, string userId) => (ICargoClient) this.ConcreteConnect(bandInfo, userId);

    public async Task<ICargoClient> ConnectAsync(IBandInfo bandInfo, string userId) => (ICargoClient) await this.ConcreteConnectAsync(bandInfo, userId);

    public ICargoClient Connect(string bandId, ServiceInfo serviceInfo) => (ICargoClient) this.ConcreteConnect(bandId, serviceInfo);

    public Task<ICargoClient> ConnectAsync(string bandId, ServiceInfo serviceInfo) => Task.FromResult<ICargoClient>((ICargoClient) this.ConcreteConnect(bandId, serviceInfo));

    public ICargoClient Connect(IBandInfo bandInfo, ServiceInfo serviceInfo) => (ICargoClient) this.ConcreteConnect(bandInfo, serviceInfo);

    public async Task<ICargoClient> ConnectAsync(
      IBandInfo bandInfo,
      ServiceInfo serviceInfo)
    {
      return (ICargoClient) await this.ConcreteConnectAsync(bandInfo, serviceInfo);
    }

    internal Task<BluetoothDeviceInfo[]> ConcreteGetBandsAsync() => Task.Run<BluetoothDeviceInfo[]>((Func<BluetoothDeviceInfo[]>) (() => this.ConcreteGetBands()));

    internal BluetoothDeviceInfo[] ConcreteGetBands() => this.ConcreteGetBands(new Guid("{A502CA97-2BA5-413C-A4E0-13804E47B38F}"));

    internal Task<BluetoothDeviceInfo[]> ConcreteGetBandsAsync(Guid service) => Task.Run<BluetoothDeviceInfo[]>((Func<BluetoothDeviceInfo[]>) (() => this.ConcreteGetBands(service)));

    internal BluetoothDeviceInfo[] ConcreteGetBands(Guid service) => BluetoothTransport.GetConnectedDevices(service, (ILoggerProvider) new LoggerProvider());

    internal Task<CargoClient> ConcreteConnectAsync(ServiceInfo serviceInfo) => Task.FromResult<CargoClient>(this.ConcreteConnect(serviceInfo));

    internal CargoClient ConcreteConnect(ServiceInfo serviceInfo)
    {
      Stopwatch stopwatch = Stopwatch.StartNew();
      LoggerProvider loggerProvider = new LoggerProvider();
      PhoneProvider phoneProvider = new PhoneProvider();
      CloudProvider cloudProvider = new CloudProvider(serviceInfo);
      CargoClient cargoClient = new CargoClient((IDeviceTransport) null, cloudProvider, (ILoggerProvider) loggerProvider, (IPlatformProvider) phoneProvider, StoreApplicationPlatformProvider.Current);
      cloudProvider.SetUserAgent(phoneProvider.GetDefaultUserAgent((FirmwareVersions) null), false);
      loggerProvider.Log(ProviderLogLevel.Info, "BandAdminClientManager.ConcreteConnect(ServiceInfo serviceInfo) succeeded: Elapsed: {0}", new object[1]
      {
        (object) stopwatch.Elapsed
      });
      return cargoClient;
    }

    internal CargoClient ConcreteConnect(IBandInfo bandInfo) => this.ConcreteConnectAsync(bandInfo).Result;

    internal abstract Task<CargoClient> ConcreteConnectAsync(IBandInfo bandInfo);

    internal CargoClient ConcreteConnect(IBandInfo bandInfo, string userId) => this.ConcreteConnectAsync(bandInfo, userId).Result;

    internal abstract Task<CargoClient> ConcreteConnectAsync(
      IBandInfo bandInfo,
      string userId);

    internal Task<CargoClient> ConcreteConnectAsync(
      string bandId,
      ServiceInfo serviceInfo)
    {
      return Task.FromResult<CargoClient>(this.ConcreteConnect(bandId, serviceInfo));
    }

    internal CargoClient ConcreteConnect(string bandId, ServiceInfo serviceInfo)
    {
      Stopwatch stopwatch = Stopwatch.StartNew();
      LoggerProvider loggerProvider = new LoggerProvider();
      PhoneProvider phoneProvider = new PhoneProvider();
      CloudProvider cloudProvider = new CloudProvider(serviceInfo);
      CargoClient cargoClient = new CargoClient((IDeviceTransport) null, cloudProvider, (ILoggerProvider) loggerProvider, (IPlatformProvider) phoneProvider, StoreApplicationPlatformProvider.Current);
      cargoClient.DeviceUniqueId = Guid.Parse(bandId);
      cargoClient.SerialNumber = (string) null;
      cloudProvider.SetUserAgent(phoneProvider.GetDefaultUserAgent((FirmwareVersions) null), false);
      StorageProvider storageProvider = StorageProvider.Create(serviceInfo.UserId, cargoClient.DeviceUniqueId.ToString("N"));
      cargoClient.InitializeStorageProvider((IStorageProvider) storageProvider);
      loggerProvider.Log(ProviderLogLevel.Info, "BandAdminClientManager.ConcreteConnect(string bandId, ServiceInfo serviceInfo) succeeded: Elapsed: {0}", new object[1]
      {
        (object) stopwatch.Elapsed
      });
      return cargoClient;
    }

    internal CargoClient ConcreteConnect(IBandInfo bandInfo, ServiceInfo serviceInfo) => this.ConcreteConnectAsync(bandInfo, serviceInfo).Result;

    internal abstract Task<CargoClient> ConcreteConnectAsync(
      IBandInfo bandInfo,
      ServiceInfo serviceInfo);
  }
}
