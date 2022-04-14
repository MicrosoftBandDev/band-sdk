// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.Store.StoreProvider
// Assembly: Microsoft.Band.Admin.Store, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: EC92377A-0E8E-4FAF-A1F6-F0E52AADA96B
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Store.dll

using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Security.Cryptography.Core;

namespace Microsoft.Band.Admin.Phone
{
    internal class PhoneProvider : IPlatformProvider
    {
        public virtual int MaxChunkRange => 64;

        public virtual void Sleep(int milliseconds) => Task.Delay(milliseconds).Wait();

        public string GetAssemblyVersion()
        {
            PackageVersion version = Package.Current.Id.Version;
            int major = version.Major;
            return $"{major}.{version.Minor}.{version.Build}.{version.Revision}";
        }

        public virtual Version GetHostOSVersion() => new Version(8, 10, 14136, 0);

        public string GetHostOS()
        {
            return "Windows Phone";
        }

        public virtual string GetDefaultUserAgent(FirmwareVersions firmwareVersions)
        {
            StringBuilder stringBuilder = new StringBuilder();
            string assemblyVersion = this.GetAssemblyVersion();
            stringBuilder.AppendFormat("KDK/{0} ({1}/{2}; {3})", (object)assemblyVersion, (object)this.GetHostOS(), (object)this.GetHostOSVersion(), (object)CultureInfo.CurrentCulture.Name);
            if (firmwareVersions != null)
                stringBuilder.AppendFormat(" Cargo/{0} (PcbId/{1})", new object[2]
                {
                    (object) firmwareVersions.ApplicationVersion,
                    (object) firmwareVersions.PcbId
                });
            return stringBuilder.ToString();
        }

        public virtual byte[] ComputeHashMd5(byte[] data) => HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Md5).HashData(data.AsBuffer()).ToArray();

        public virtual byte[] ComputeHashMd5(Stream data)
        {
            CryptographicHash hash = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Md5).CreateHash();
            int val1 = (int)(data.Length - data.Position);
            int num = 0;
            using (PooledBuffer buffer = BufferServer.GetBuffer(Math.Min(val1, 8192)))
            {
                while (num < val1)
                {
                    int length = data.Read(buffer.Buffer, 0, Math.Min(val1 - num, buffer.Length));
                    if (length == 0)
                        throw new EndOfStreamException();
                    num += length;
                    hash.Append(buffer.Buffer.AsBuffer(0, length));
                }
            }
            return hash.GetValueAndReset().ToArray();
        }
    }
}
