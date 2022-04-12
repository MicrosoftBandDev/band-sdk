using System.Reflection;
using Windows.ApplicationModel.Resources;

namespace Microsoft.Band.Admin.Phone;

public class SR
{
    private static ResourceLoader resourceLoader;

    public static string AccessTokenIsMissing => resourceLoader.GetString("AccessTokenIsMissing");

    public static string BeginWriteFailed => resourceLoader.GetString("BeginWriteFailed");

    public static string BluetoothDisabled => resourceLoader.GetString("BluetoothDisabled");

    public static string CannotReadFromStream => resourceLoader.GetString("CannotReadFromStream");

    public static string ConnectionClosed => resourceLoader.GetString("ConnectionClosed");

    public static string DiscoveryServiceAddressIsMissing => resourceLoader.GetString("DiscoveryServiceAddressIsMissing");

    public static string DiscoveryServiceTokenIsMissing => resourceLoader.GetString("DiscoveryServiceTokenIsMissing");

    public static string EndTimeIsSmallerThanStartTime => resourceLoader.GetString("EndTimeIsSmallerThanStartTime");

    public static string EndWriteFailed => resourceLoader.GetString("EndWriteFailed");

    public static string LogUploadFailed => resourceLoader.GetString("LogUploadFailed");

    public static string NotConnectedError => resourceLoader.GetString("NotConnectedError");

    public static string ReaderError => resourceLoader.GetString("ReaderError");

    public static string ReadProfileFailed => resourceLoader.GetString("ReadProfileFailed");

    public static string ServiceAddressIsMissing => resourceLoader.GetString("ServiceAddressIsMissing");

    public static string WriteProfileFailed => resourceLoader.GetString("WriteProfileFailed");

    public static string PushServiceStreamClosed => resourceLoader.GetString("PushServiceStreamClosed");

    public static string PushServiceStreamException => resourceLoader.GetString("PushServiceStreamException");

    public static string SpeechDisabledMessage => resourceLoader.GetString("SpeechDisabledMessage");

    public static string SpeechEmptyResponseMessage => resourceLoader.GetString("SpeechEmptyResponseMessage");

    public static string SpeechFinishOnPhoneMessage => resourceLoader.GetString("SpeechFinishOnPhoneMessage");

    public static string SpeechFlowTerminatedMessage => resourceLoader.GetString("SpeechFlowTerminatedMessage");

    public static string SpeechUnsupportedScenarioMessage => resourceLoader.GetString("SpeechUnsupportedScenarioMessage");

    public static string EmptyResultStringForceGotItMessage => resourceLoader.GetString("EmptyResultStringForceGotItMessage");

    public static string BatterySaverOnMessage => resourceLoader.GetString("BatterySaverOnMessage");

    static SR()
    {
        resourceLoader = ResourceLoader.GetForCurrentView(typeof(SR).GetTypeInfo().get_Assembly().FullName.Split(',')[0] + "/SR");
    }
}
