// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.LocalExtensions
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System;

namespace Microsoft.Band.Admin
{
  public static class LocalExtensions
  {
    private const int LocaleValueCount = 31;
    private static bool integrityCheckDone;

    public static string ToRegionName(this Locale locale)
    {
      if (!LocalExtensions.integrityCheckDone)
      {
        if (Enum.GetValues(typeof (Locale)).Length != 31)
          throw new InvalidOperationException("Internal error: Locale out of sync");
        LocalExtensions.integrityCheckDone = true;
      }
      switch (locale)
      {
        case Locale.GreatBritian:
          return "GB";
        case Locale.Canada:
          return "CA";
        case Locale.France:
          return "FR";
        case Locale.Germany:
          return "DE";
        case Locale.Italy:
          return "IT";
        case Locale.Mexico:
          return "MX";
        case Locale.Spain:
          return "ES";
        case Locale.Australia:
          return "AU";
        case Locale.NewZealand:
          return "NZ";
        case Locale.Denmark:
          return "DK";
        case Locale.Finland:
          return "FI";
        case Locale.Norway:
          return "NO";
        case Locale.Netherlands:
          return "NL";
        case Locale.Portugal:
          return "PT";
        case Locale.Sweden:
          return "SE";
        case Locale.Poland:
          return "PL";
        case Locale.China:
          return "CN";
        case Locale.Taiwan:
          return "TW";
        case Locale.Japan:
          return "JP";
        case Locale.Korea:
          return "KR";
        case Locale.Austria:
          return "AT";
        case Locale.Belgium:
          return "BE";
        case Locale.HongKong:
          return "HK";
        case Locale.Ireland:
          return "IE";
        case Locale.Singapore:
          return "SG";
        case Locale.Switzerland:
          return "CH";
        case Locale.SouthAfrica:
          return "ZA";
        case Locale.SaudiArabia:
          return "SA";
        case Locale.UnitedArabEmirates:
          return "AE";
        default:
          return "US";
      }
    }
  }
}
