using System.Threading.Tasks;

namespace Microsoft.Band.Admin;

public abstract partial class BandAdminClientManagerBase
{
    public async Task<IBandInfo[]> GetBandsAsync()
    {
        return await Task.Run(() => GetBands());
    }

    public IBandInfo[] GetBands()
    {
        return ConcreteGetBands();
    }

    public ICargoClient Connect(IBandInfo bandInfo)
    {
        return ConcreteConnect(bandInfo);
    }

    public async Task<ICargoClient> ConnectAsync(IBandInfo bandInfo)
    {
        return await ConcreteConnectAsync(bandInfo);
    }

    public ICargoClient Connect(ServiceInfo serviceInfo)
    {
        return ConcreteConnect(serviceInfo);
    }

    public Task<ICargoClient> ConnectAsync(ServiceInfo serviceInfo)
    {
        return Task.FromResult((ICargoClient)ConcreteConnect(serviceInfo));
    }

    public ICargoClient Connect(IBandInfo bandInfo, string userId)
    {
        return ConcreteConnect(bandInfo, userId);
    }

    public async Task<ICargoClient> ConnectAsync(IBandInfo bandInfo, string userId)
    {
        return await ConcreteConnectAsync(bandInfo, userId);
    }

    public ICargoClient Connect(string bandId, ServiceInfo serviceInfo)
    {
        return ConcreteConnect(bandId, serviceInfo);
    }

    public Task<ICargoClient> ConnectAsync(string bandId, ServiceInfo serviceInfo)
    {
        return Task.FromResult((ICargoClient)ConcreteConnect(bandId, serviceInfo));
    }

    public ICargoClient Connect(IBandInfo bandInfo, ServiceInfo serviceInfo)
    {
        return ConcreteConnect(bandInfo, serviceInfo);
    }

    public async Task<ICargoClient> ConnectAsync(IBandInfo bandInfo, ServiceInfo serviceInfo)
    {
        return await ConcreteConnectAsync(bandInfo, serviceInfo);
    }

    internal CargoClient ConcreteConnect(IBandInfo bandInfo)
    {
        return ConcreteConnectAsync(bandInfo).Result;
    }

    internal abstract Task<CargoClient> ConcreteConnectAsync(IBandInfo bandInfo);

    internal CargoClient ConcreteConnect(IBandInfo bandInfo, string userId)
    {
        return ConcreteConnectAsync(bandInfo, userId).Result;
    }

    internal abstract Task<CargoClient> ConcreteConnectAsync(IBandInfo bandInfo, string userId);

    internal Task<CargoClient> ConcreteConnectAsync(string bandId, ServiceInfo serviceInfo)
    {
        return Task.FromResult(ConcreteConnect(bandId, serviceInfo));
    }

    internal CargoClient ConcreteConnect(IBandInfo bandInfo, ServiceInfo serviceInfo)
    {
        return ConcreteConnectAsync(bandInfo, serviceInfo).Result;
    }

    internal abstract Task<CargoClient> ConcreteConnectAsync(IBandInfo bandInfo, ServiceInfo serviceInfo);

    internal Task<CargoClient> ConcreteConnectAsync(ServiceInfo serviceInfo)
    {
        return Task.FromResult(ConcreteConnect(serviceInfo));
    }
}
