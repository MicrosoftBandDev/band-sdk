// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.FirmwareVersion
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System;

namespace Microsoft.Band.Admin
{
    public sealed class FirmwareVersion :
      IComparable,
      IComparable<FirmwareVersion>,
      IEquatable<FirmwareVersion>
    {
        public static FirmwareVersion Parse(string version, bool debug) => new FirmwareVersion(Version.Parse(version), debug);

        public static bool TryParse(string input, bool debug, out FirmwareVersion version)
        {
            version = (FirmwareVersion)null;
            Version result;
            int num = Version.TryParse(input, out result) ? 1 : 0;
            if (num == 0)
                return num != 0;
            version = new FirmwareVersion(result, debug);
            return num != 0;
        }

        public FirmwareVersion(int major, int minor, int build, int revision, bool debug)
        {
            this.Version = new Version(major, minor, build, revision);
            this.Debug = debug;
        }

        public FirmwareVersion(string version, bool debug)
        {
            this.Version = new Version(version);
            this.Debug = debug;
        }

        public FirmwareVersion(Version version, bool debug)
        {
            this.Version = version;
            this.Debug = debug;
        }

        internal FirmwareVersion(CargoVersion version)
          : this((int)version.VersionMajor, (int)version.VersionMinor, (int)version.BuildNumber, (int)version.Revision, version.DebugBuild > (byte)0)
        {
        }

        public Version Version { get; private set; }

        public int Major => this.Version.Major;

        public int Minor => this.Version.Minor;

        public int Build => this.Version.Build;

        public int Revision => this.Version.Revision;

        public bool Debug { get; private set; }

        public override string ToString() => this.ToString(false);

        public string ToString(bool showDebug) => string.Format("{0}{1}", new object[2]
        {
      (object) this.Version,
      showDebug ? (this.Debug ? (object) " D" : (object) " R") : (object) ""
        });

        public string ToString(int format) => this.ToString(format, false);

        public string ToString(int format, bool showDebug) => string.Format("{0}{1}", new object[2]
        {
      (object) this.Version.ToString(format),
      showDebug ? (this.Debug ? (object) " D" : (object) " R") : (object) ""
        });

        public int CompareTo(object that) => this.CompareTo((FirmwareVersion)that);

        public int CompareTo(FirmwareVersion that) => this.Version.CompareTo(that.Version);

        public int CompareTo(Version that) => this.Version.CompareTo(that);

        public override bool Equals(object that) => this.Equals((FirmwareVersion)that);

        public bool Equals(FirmwareVersion that) => this.CompareTo(that) == 0;

        public bool Equals(Version that) => this.Version.CompareTo(that) == 0;

        public static bool operator ==(FirmwareVersion left, FirmwareVersion right) => left.Version == right.Version;

        public static bool operator <=(FirmwareVersion left, FirmwareVersion right) => left.Version <= right.Version;

        public static bool operator <(FirmwareVersion left, FirmwareVersion right) => left.Version < right.Version;

        public static bool operator >=(FirmwareVersion left, FirmwareVersion right) => left.Version >= right.Version;

        public static bool operator >(FirmwareVersion left, FirmwareVersion right) => left.Version > right.Version;

        public static bool operator !=(FirmwareVersion left, FirmwareVersion right) => left.Version != right.Version;

        public override int GetHashCode() => this.Version.GetHashCode();
    }
}
