// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.Phone.PhoneProvider
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

using Microsoft.Band.Admin.Store;
using Windows.ApplicationModel;

namespace Microsoft.Band.Admin.Phone
{
  internal sealed class PhoneProvider : StoreProvider
  {
    public override string GetAssemblyVersion()
    {
      PackageVersion version = Package.Current.Id.Version;
      return string.Format("{0}.{1}.{2}.{3}", (object) (int) version.Major, (object) version.Minor, (object) version.Build, (object) version.Revision);
    }

    public override string GetHostOS() => "Windows Phone";
  }
}
