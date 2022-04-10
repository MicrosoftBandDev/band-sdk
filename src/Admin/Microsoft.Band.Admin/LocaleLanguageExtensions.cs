// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.LocaleLanguageExtensions
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System;

namespace Microsoft.Band.Admin
{
  public static class LocaleLanguageExtensions
  {
    public const int LocaleLanguageValueCount = 21;
    private static bool integrityCheckDone;

    public static string ToLanguageCultureName(this LocaleLanguage localeLanguage)
    {
      if (!LocaleLanguageExtensions.integrityCheckDone)
      {
        if (Enum.GetValues(typeof (LocaleLanguage)).Length != 21)
          throw new InvalidOperationException("Internal error: LocaleLanguage out of sync");
        LocaleLanguageExtensions.integrityCheckDone = true;
      }
      switch (localeLanguage)
      {
        case LocaleLanguage.English_GB:
          return "en-GB";
        case LocaleLanguage.French_CA:
          return "fr-CA";
        case LocaleLanguage.French_FR:
          return "fr-FR";
        case LocaleLanguage.German_DE:
          return "de-DE";
        case LocaleLanguage.Italian_IT:
          return "it-IT";
        case LocaleLanguage.Spanish_MX:
          return "es-MX";
        case LocaleLanguage.Spanish_ES:
          return "es-ES";
        case LocaleLanguage.Spanish_US:
          return "es-US";
        case LocaleLanguage.Danish_DK:
          return "da-DK";
        case LocaleLanguage.Finnish_FI:
          return "fi-FI";
        case LocaleLanguage.NorwegianBokmal_NO:
          return "nn-NO";
        case LocaleLanguage.Dutch_NL:
          return "nl_NL";
        case LocaleLanguage.Portuguese_PT:
          return "pt-PT";
        case LocaleLanguage.Swedish_SE:
          return "sv-SE";
        case LocaleLanguage.Polish_PL:
          return "pl-PL";
        case LocaleLanguage.SimplifiedChinese_CN:
          return "zh-CHS";
        case LocaleLanguage.TraditionalChinese_TW:
          return "zh-TW";
        case LocaleLanguage.Japanese_JP:
          return "ja-JP";
        case LocaleLanguage.Korean_KR:
          return "ko-KR";
        default:
          return "en-US";
      }
    }
  }
}
