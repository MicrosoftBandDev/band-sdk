// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.IBandAdminClientManager
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System.Threading.Tasks;

namespace Microsoft.Band.Admin
{
  public interface IBandAdminClientManager
  {
    Task<IBandInfo[]> GetBandsAsync();

    IBandInfo[] GetBands();

    Task<ICargoClient> ConnectAsync(IBandInfo bandInfo);

    ICargoClient Connect(IBandInfo bandInfo);

    Task<ICargoClient> ConnectAsync(ServiceInfo serviceInfo);

    ICargoClient Connect(ServiceInfo serviceInfo);

    Task<ICargoClient> ConnectAsync(IBandInfo bandInfo, string userId);

    ICargoClient Connect(IBandInfo bandInfo, string userId);

    Task<ICargoClient> ConnectAsync(string bandId, ServiceInfo serviceInfo);

    ICargoClient Connect(string bandId, ServiceInfo serviceInfo);

    Task<ICargoClient> ConnectAsync(IBandInfo bandInfo, ServiceInfo serviceInfo);

    ICargoClient Connect(IBandInfo bandInfo, ServiceInfo serviceInfo);
  }
}
