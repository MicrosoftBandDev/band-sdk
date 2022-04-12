// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.Phone.PhoneProvider
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace Microsoft.Band.Admin.Windows
{
    internal sealed class PhoneProvider : IPlatformProvider
    {
        public int MaxChunkRange => 64;

        public byte[] ComputeHashMd5(byte[] data) => System.Security.Cryptography.MD5.HashData(data);

        public byte[] ComputeHashMd5(Stream dataStream)
        {
            byte[] data;
            using (MemoryStream ms = new())
            {
                dataStream.CopyTo(ms);
                data = ms.ToArray();
            }
            return ComputeHashMd5(data);
        }

        public string GetAssemblyVersion()
        {
            PackageVersion version = Package.Current.Id.Version;
            return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }

        public string GetDefaultUserAgent(FirmwareVersions firmwareVersions)
        {
            StringBuilder stringBuilder = new();
            string assemblyVersion = GetAssemblyVersion();
            stringBuilder.AppendFormat("KDK/{0} ({1}/{2}; {3})", assemblyVersion, GetHostOS(), GetHostOSVersion(), CultureInfo.CurrentCulture.Name);
            if (firmwareVersions != null)
                stringBuilder.AppendFormat(" Cargo/{0} (PcbId/{1})", new object[2]
                {
                    firmwareVersions.ApplicationVersion,
                    firmwareVersions.PcbId
                });
            return stringBuilder.ToString();
        }

        public string GetHostOS() => "Windows";

        public Version GetHostOSVersion() => Environment.OSVersion.Version;

        public void Sleep(int milliseconds) => Task.Delay(milliseconds).Wait();
    }
}
