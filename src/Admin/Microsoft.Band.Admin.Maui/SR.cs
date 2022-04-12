namespace Microsoft.Band.Admin;

// FIXME: Find the actual messages
public class SR
{
    public static string AccessTokenIsMissing => "Access token is missing from service info.";

    public static string BeginWriteFailed => "Failed to begin writing to stream.";

    public static string BluetoothDisabled => "Bluetooth is disabled.";

    public static string CannotReadFromStream => "Failed to read from stream.";

    public static string ConnectionClosed => "Connection was closed unexpectedly.";

    public static string DiscoveryServiceAddressIsMissing => "Discovery service address is missing from service info.";

    public static string DiscoveryServiceTokenIsMissing => "Discovery service token is missing from service info.";

    public static string EndTimeIsSmallerThanStartTime => "End time cannot be before start time.";

    public static string EndWriteFailed => "Failed to end write to stream.";

    public static string LogUploadFailed => "Failed to upload log.";

    public static string NotConnectedError => "No connection available.";

    public static string ReaderError => "Reader failed to read.";

    public static string ReadProfileFailed => "Failed to read profile.";

    public static string ServiceAddressIsMissing => "Service address is missing from service info.";

    public static string WriteProfileFailed => "Failed to write profile.";

    public static string PushServiceStreamClosed => "Push service stream was closed unexpectedly.";

    public static string PushServiceStreamException => "An unknown exception occurred while using the push service stream.";

    public static string SpeechDisabledMessage => "Speech is disabled.";

    public static string SpeechEmptyResponseMessage => "Speech response was empty.";

    public static string SpeechFinishOnPhoneMessage => "Finish speech on phone.";

    public static string SpeechFlowTerminatedMessage => "Speech flow terminated.";

    public static string SpeechUnsupportedScenarioMessage => "The scenario is unsupported for speech.";

    public static string EmptyResultStringForceGotItMessage => "Forced got it message.";

    public static string BatterySaverOnMessage => "Battery saver is on.";
}
