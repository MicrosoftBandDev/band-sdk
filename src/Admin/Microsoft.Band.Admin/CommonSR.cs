// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.CommonSR
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

namespace Microsoft.Band.Admin
{
    internal class CommonSR
    {
        public static string AppointmentsExceedLimit => "The amount of appointments cannot exceed {0}.";

        public static string BadDeviceCommandStatusPacket => "Unexpected packet type encountered while reading command status packet";

        public static string BadgingEnabledDisplayIconCannotBeLast => "A tile that supports badging must have the badge icon after the normal display icon.";

        public static string BadgingRequiresMultipleImages => "A tile that supports badging must have at least 2 icons.";

        public static string BeginWriteFailed => "An error occured while trying to initiate the upload process.";

        public static string ByteReadFailure => "Cannot read the byte value form the stream.";

        public static string CargoClientDisposed => "CargoClient was disposed.";

        public static string CargoCommandStatusUnavailable => "CommandStatus is not available until all data has been transferred to/from the device";

        public static string CommandStatusError => "Device status code: 0x{0:X8} received.";

        public static string DeserializeSizeError => "Incorrect size of data sample when attempting to deserialize.";

        public static string DeviceCurrentlyUpdating => "An internal firmware update is currently in progess.";

        public static string DeviceFamilyRecognitionFailed => "Could not determine device family";

        public static string DeviceFileDownloadError => "Error occurred when downloading device file {0}";

        public static string DeviceFileSizeGreatherThanMaxAllowedSize => "The size of the {0} device file is greater than the maximum size allowed for that file";

        public static string DeviceNotInUpdateMode => "The device is not in the expected firmware update mode.";

        public static string DeviceNotOobeCompleted => "Device can not be in OOBE state for this procedure.";

        public static string DeviceReconnectMaxAttemptsExceeded => "An attempt to reconnect to the device failed.";

        public static string EndWriteFailed => "An error occured while trying to finalize the upload process.";

        public static string EphemeriDataDownloadTempFileOpenError => "Could not open temp file {0} to save downloaded ephemeris data.";

        public static string EphemerisDownloadError => "An error occurred while downloading the Ephemeris blob file.";

        public static string EphemerisVersionDownloadError => "An error occurred while downloading the Ephemeris Version json file.";

        public static string FileNotFound => "The file {0} is not found";

        public static string FileUploadToCloudFailed => "Unable to upload the {0} file to the cloud";

        public static string FileWriteMaxSizeExceeded => "File is {0} bytes which exceeds the max size {1} bytes for the sector.";

        public static string FirmwareUpdateDownloadError => "Cannot download firmware update from the cloud.";

        public static string FirmwareUpdateDownloadTempFileOpenError => "Could not open temp file {0} to save downloaded firmware update.";

        public static string FirmwareUpdateDownloadTempFileSizeMismatchError => "Downloaded firmware update size {0} does not match the update size on the cloud {1}.";

        public static string FirmwareUpdateInfoError => "Cannot read firmware update info from the cloud.";

        public static string FirmwareUpdateIntegrityError => "Downloaded firmware update file failed data integrity check.";

        public static string GenericCountMax => "{0} has too many entries.";

        public static string GenericCountZero => "{0} must have at least one entry.";

        public static string GenericLengthZero => "{0} cannot have length 0.";

        public static string GenericNullOrWhiteSpace => "{0} cannot be null or whitespace.";

        public static string GenericRepeatEntry => "{0} cannot have more than one {1} with the same {2}.";

        public static string HttpExceptionRequestLineLabel => "Request";

        public static string HttpExceptionResponseContentLabel => "Response Content";

        public static string HttpExceptionStatusLineLabel => "Status";

        public static string IncorrectGuid => "Incorrect GUID.";

        public static string InvalidCargoBikeDisplayMetrics => "Invalid CargoBikeDisplayMetrics. Each BikeDisplayMetricsType must appear only once as properties of CargoBikeDisplayMetrics.";

        public static string InvalidCargoRunDisplayMetrics => "Invalid CargoRunDisplayMetrics. Each RunDisplayMetricsType must appear only once as properties of CargoRunDisplayMetrics.";

        public static string InvalidCharactersInFolderName => "The folder name '{0}' that is being created has invalid characters";

        public static string InvalidEventEndTime => "The end time for this event must occur after the start time.";

        public static string InvalidGuidFromDevice => "Unable to decode GUID data received from device";

        public static string InvalidTimestampPayloadSize => "Invalid payload size for a timestamp value.";

        public static string InvalidUpdateDataSize => "Update data size retrieved from cloud is invalid.";

        public static string LogDownloadError => "Bad status code received when downloading sensor log chunk.";

        public static string LogProcessingStatusDownloadError => "An error occurred while downloading the Log Processing Status json file.";

        public static string MetadataReadFailure => "Starting position: {0}. Expected bytes: {1}. Ending position {2}.";

        public static string MissingMetadataTag => "Missing metadata tag. State: {0}.";

        public static string NewTileRequiresImages => "Registering a new tile requires that you supply images.";

        public static string ObsoleteFirmwareVersionOnDevice => "The firmware version on the device {0} cannot be updated to the latest version {1}.";

        public static string OperationRequiredCloudConnection => "The attempted operation requires a cloud connection.";

        public static string OperationRequiredConnectedDevice => "The attempted operation requires a connected device.";

        public static string OperationRequiredStorageProvider => "The attempted operation requires a storage provider.";

        public static string OutOfDateMetadata => "Metadata version: {0} found. Latest is: {1}.";

        public static string ReadProfileFailed => "Unable to get the user profile.";

        public static string RepeatMetadataTag => "Metadata tag: {0} found more than once.";

        public static string ResponseStringExceedsMaxLength => "{0} exceeds the maximum length of {1} characters.";

        public static string SensorLogMetaDataCantBeNull => "The UploadMetaData object used when uploading sensor logs to the cloud cannot be null";

        public static string StructMarshallingError => "Incorrect size for expected struct: {0} Received size: {1} Expected size: {2}";

        public static string TileDefaultTitle => "(DEFAULT NAME)";

        public static string TimestampReadFailure => "Cannot read the timestamp value from the stream.";

        public static string TimestampRetrievalFailure => "Unable to extract the StartTime and EndTime from the sensor log file.";

        public static string TimeZoneDataDownloadError => "An error occurred while downloading the TimeZoneData blob file.";

        public static string TimeZoneDataVersionDownloadError => "An error occurred while downloading the TimeZoneData Version json file.";

        public static string TimeZoneDownloadTempFileOpenError => "Could not open temp file {0} to save downloaded timezone.";

        public static string UInt16ReadFailure => "Cannot read the UInt16 value from the stream.";

        public static string UInt32ReadFailure => "Cannot read the UInt32 value from the stream.";

        public static string UnexpectedMetadataTag => "Unexpected metadata tag encountered. Expected tag: {0}, found tag: {1}.";

        public static string UnrecognizedMetadataTag => "The tag from the metadata file is not recognized.";

        public static string UnsupportedFileTypeForCloudUpload => "Unsupported file type for upload to cloud. Valid types are 'telemetry' and 'crashdumps'";

        public static string UnsupportedFileTypeToObtainFromDevice => "Unsupported file type to obtain from the device. Valid types are 'Instrumentation' and 'CrashDump'";

        public static string UploadIdNotSpecified => "Upload Id is not specified in the CloudDataResource parameter object";

        public static string WebTileDownloadError => "An error occurred while downloading the web tile file.";

        public static string WebTileDownloadTempFileOpenError => "Could not open temp file {0} to save downloaded web tile data.";

        public static string WriteProfileFailed => "Unable to save the user profile.";

        public static string WTAuthenticationNeedsHttpsUri => "Authentication requires an HTTPS Uri";

        public static string WTBadHTTPRequestHeader => "Bad HTTP RequestHeader name={0} value={1}";

        public static string WTFailedToFetchResourceData => "Failed to fetch resource data";

        public static string WTContainsOperatorOnNumeric => "'contains' may not be used with numeric constants";

        public static string WTUndefinedVariable => "Undefined variable {0}";

        public static string WTUnexpectedTypeInCompare => "Unexpected type in Compare";

        public static string WTExpressionHasNoTokens => "Expression contains no tokens";

        public static string WTExtraInputAfterTrue => "Expression contains extraneous input after 'true'";

        public static string WTExpressionMissingFirstOperand => "Expression missing first operand";

        public static string WTExpressionMissingOperator => "Expression missing operator";

        public static string WTExpressionMissingSecondOperand => "Expression missing second operand";

        public static string WTExpressionHasExtraneousInput => "Expression contains extraneous input after the second operand";

        public static string WTIconNameCannotBeEmpty => "Icon name cannot be empty";

        public static string WTPropertyError => "{0}: {1}";

        public static string WTPropertyUintRangeError => "Must be between {0} and {1}";

        public static string WTPropertStringLengthError => "Must be {0} to {1} characters long";

        public static string WTPropertyInvalidLayoutName => "Invalid layout name";

        public static string WTPropertyInvalidCondition => "Invalid condition";

        public static string WTPropertyTooManyIconBindings => "Too many IconBindings";

        public static string WTPropertyConditionMustBeTrue => "Condition must be true";

        public static string WTPropertyColorInvalid => "Invalid color {0}";

        public static string WTVersionTooSmall => "Must be at least {0}";

        public static string WTMaxIconsExceeded => "Maximum of {0} additional icon filenames allowed";

        public static string WTTileIconRequired => "Tile icon is required";

        public static string WTTooManyPages => "Maximimum {0} pages allowed";

        public static string WTTooManyLayouts => "Too many layouts used";

        public static string WTInvalidIconDimensions => "Incorrect dimensions for icon {0}";

        public static string WTInvalidIconFile => "Invalid icon file {0}";

        public static string WTMissingIconFilenames => "iconFilenames does not have tile and badge icon filenames";

        public static string WTBadUrl => "Bad Url";

        public static string WTNameCannotBeNull => "Name cannot be null";

        public static string WTMultipleTextBindingsWithElementId => "Multiple TextBindings with ElementId {0}";

        public static string WTElementIDNotValidForLayout => "Element ID {0} not valid for layout {1}";

        public static string WTElementIDDoesNotSupportText => "Element ID {0} is a {1} which does not support text";

        public static string WTElementIDIsNotAnIconInLayout => "Element ID {0} is not an icon in layout {1}";

        public static string WTMissingVariableDefinitions => "Missing variable definitions";

        public static string WTInvalidVariableName => "Invalid variable name '{0}'";

        public static string WTInvalidVariableExpression => "Invalid variable expression for '{0}'";

        public static string WTStorageProviderNotSet => "StorageProvider not set";

        public static string WTDataFolderPathNotSet => "DataFolderPath not set";

        public static string WTImageProviderNotSet => "ImageProvider not set";

        public static string WTPackageFolderPathNotSet => "PackageFolderPath not set";

        public static string WTUndefinedVariableReferencedInTextBindings => "Variable {0} referenced in TextBindings but not defined";

        public static string WTUndefinedVariableReferencedInNotifications => "Variable {0} referenced in Notifications but not defined";

        public static string WTVariableNameNotUnique => "Resource variable name {0} not unique";

        public static string WTIconNotDefined => "Icon {0} not defined";

        public static string WTNoResources => "No Resources";

        public static string WTFeedMustHaveExactlyOneResource => "Resources.Length must be 1 for feed";

        public static string WTFeedMustHaveExactlyOnePage => "Pages.Length must be 1 for feed";

        public static string WTUnableToParseUrlFromUri => "Unable to parse WebTile url from uri";

        public static string WTUnrecognizedToken => "Unrecognized token at position {0}";
    }
}
