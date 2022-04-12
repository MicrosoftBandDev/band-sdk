// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.Phone.SR
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

using System.Reflection;
using Windows.ApplicationModel.Resources;

namespace Microsoft.Band.Admin.Phone
{
  public class SR
  {
    private static ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView(typeof (SR).GetTypeInfo().Assembly.FullName.Split(',')[0] + "/SR");

    public static string AccessTokenIsMissing => SR.resourceLoader.GetString(nameof (AccessTokenIsMissing));

    public static string BeginWriteFailed => SR.resourceLoader.GetString(nameof (BeginWriteFailed));

    public static string BluetoothDisabled => SR.resourceLoader.GetString(nameof (BluetoothDisabled));

    public static string CannotReadFromStream => SR.resourceLoader.GetString(nameof (CannotReadFromStream));

    public static string ConnectionClosed => SR.resourceLoader.GetString(nameof (ConnectionClosed));

    public static string DiscoveryServiceAddressIsMissing => SR.resourceLoader.GetString(nameof (DiscoveryServiceAddressIsMissing));

    public static string DiscoveryServiceTokenIsMissing => SR.resourceLoader.GetString(nameof (DiscoveryServiceTokenIsMissing));

    public static string EndTimeIsSmallerThanStartTime => SR.resourceLoader.GetString(nameof (EndTimeIsSmallerThanStartTime));

    public static string EndWriteFailed => SR.resourceLoader.GetString(nameof (EndWriteFailed));

    public static string LogUploadFailed => SR.resourceLoader.GetString(nameof (LogUploadFailed));

    public static string NotConnectedError => SR.resourceLoader.GetString(nameof (NotConnectedError));

    public static string ReaderError => SR.resourceLoader.GetString(nameof (ReaderError));

    public static string ReadProfileFailed => SR.resourceLoader.GetString(nameof (ReadProfileFailed));

    public static string ServiceAddressIsMissing => SR.resourceLoader.GetString(nameof (ServiceAddressIsMissing));

    public static string WriteProfileFailed => SR.resourceLoader.GetString(nameof (WriteProfileFailed));

    public static string PushServiceStreamClosed => SR.resourceLoader.GetString(nameof (PushServiceStreamClosed));

    public static string PushServiceStreamException => SR.resourceLoader.GetString(nameof (PushServiceStreamException));

    public static string SpeechDisabledMessage => SR.resourceLoader.GetString(nameof (SpeechDisabledMessage));

    public static string SpeechEmptyResponseMessage => SR.resourceLoader.GetString(nameof (SpeechEmptyResponseMessage));

    public static string SpeechFinishOnPhoneMessage => SR.resourceLoader.GetString(nameof (SpeechFinishOnPhoneMessage));

    public static string SpeechFlowTerminatedMessage => SR.resourceLoader.GetString(nameof (SpeechFlowTerminatedMessage));

    public static string SpeechUnsupportedScenarioMessage => SR.resourceLoader.GetString(nameof (SpeechUnsupportedScenarioMessage));

    public static string EmptyResultStringForceGotItMessage => SR.resourceLoader.GetString(nameof (EmptyResultStringForceGotItMessage));

    public static string BatterySaverOnMessage => SR.resourceLoader.GetString(nameof (BatterySaverOnMessage));
  }
}
