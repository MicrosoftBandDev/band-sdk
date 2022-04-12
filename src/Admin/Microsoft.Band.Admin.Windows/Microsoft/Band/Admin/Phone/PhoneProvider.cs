using Microsoft.Band.Admin.Store;
using Windows.ApplicationModel;

namespace Microsoft.Band.Admin.Phone;

internal sealed class PhoneProvider : StoreProvider
{
    public override string GetAssemblyVersion()
    {
        //IL_000a: Unknown result type (might be due to invalid IL or missing references)
        //IL_000f: Unknown result type (might be due to invalid IL or missing references)
        //IL_0010: Unknown result type (might be due to invalid IL or missing references)
        //IL_002d: Unknown result type (might be due to invalid IL or missing references)
        //IL_003b: Unknown result type (might be due to invalid IL or missing references)
        //IL_0049: Unknown result type (might be due to invalid IL or missing references)
        PackageVersion version = Package.get_Current().get_Id().get_Version();
        int major = version.Major;
        return $"{major}.{version.Minor}.{version.Build}.{version.Revision}";
    }

    public override string GetHostOS()
    {
        return "Windows Phone";
    }
}
