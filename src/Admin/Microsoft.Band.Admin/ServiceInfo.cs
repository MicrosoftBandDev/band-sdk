// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.ServiceInfo
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

namespace Microsoft.Band.Admin
{
    public class ServiceInfo
    {
        private string fileUpdateServiceAddress = "http://fileupdatequeryservice.cloudapp.net";
        private string discoveryServiceAddress = "";
        private string podAddress = "";
        private string userAgent;

        public string DiscoveryServiceAddress
        {
            get => this.discoveryServiceAddress;
            set => this.discoveryServiceAddress = this.NormalizeAddress(value);
        }

        public string DiscoveryServiceAccessToken { get; set; }

        public string UserId { get; set; }

        public string PodAddress
        {
            get => this.podAddress;
            set => this.podAddress = this.NormalizeAddress(value);
        }

        public string AccessToken { get; set; }

        public string FileUpdateServiceAddress
        {
            get => this.fileUpdateServiceAddress;
            set => this.fileUpdateServiceAddress = this.NormalizeAddress(value);
        }

        public string UserAgent
        {
            get => this.userAgent;
            set => this.userAgent = value;
        }

        private string NormalizeAddress(string address)
        {
            string str = address.Trim();
            while (str.EndsWith("/"))
                str = str.Remove(str.Length - 1);
            return str;
        }
    }
}
