using System;
using Microsoft.Band.Phone;

namespace Microsoft.Band.Admin.Phone;

public class WindowsRuntimeResourceManager
{
    public static void InjectIntoResxGeneratedApplicationResourcesClass(Type resxGeneratedApplicationResourcesClass, bool ignoreException)
    {
        WinRtResourceManager.InjectIntoResxGeneratedApplicationResourcesClass(resxGeneratedApplicationResourcesClass, ignoreException);
    }
}
