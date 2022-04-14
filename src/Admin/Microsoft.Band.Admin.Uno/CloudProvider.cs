using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Band.Admin.Phone;

namespace Microsoft.Band.Admin;

internal sealed class CloudProvider
{
    private static readonly MediaTypeHeaderValue OctetStreamContentTypeHeaderValue = MediaTypeHeaderValue.Parse("application/octet-stream");

    private static readonly HttpHeader MSBlockBlobHeader = new HttpHeader("x-ms-blob-type", "BlockBlob");

    private static readonly TimeSpan SensorLogUploadTimeout = TimeSpan.FromMinutes(2.0);

    private readonly string podAddress;

    private readonly HttpHeader authorizationHeader;

    private readonly string authorizationHeaderForDiscoveryService;

    private readonly Uri profileUri;

    private readonly string firmwareUpdateInfoUrlFormat;

    private readonly string ephemerisProfileUrl;

    private readonly string timezoneUpdateInfoUrl;

    private HttpHeader userAgentHeader;

    private bool userAgentOverridden;

    private object debugLock = new object();

    internal string UserAgent => userAgentHeader.Value;

    internal CloudProvider(ServiceInfo serviceInfo)
    {
        if (serviceInfo == null)
        {
            ArgumentNullException ex = new ArgumentNullException("serviceInfo");
            Logger.LogException(LogLevel.Error, ex);
            throw ex;
        }
        if (string.IsNullOrWhiteSpace(serviceInfo.PodAddress))
        {
            ArgumentException ex2 = new ArgumentException(SR.ServiceAddressIsMissing, "serviceInfo");
            Logger.LogException(LogLevel.Error, ex2);
            throw ex2;
        }
        if (string.IsNullOrWhiteSpace(serviceInfo.AccessToken))
        {
            ArgumentException ex3 = new ArgumentException(SR.AccessTokenIsMissing, "serviceInfo");
            Logger.LogException(LogLevel.Error, ex3);
            throw ex3;
        }
        if (string.IsNullOrWhiteSpace(serviceInfo.DiscoveryServiceAddress))
        {
            ArgumentException ex4 = new ArgumentException(SR.DiscoveryServiceAddressIsMissing, "serviceInfo");
            Logger.LogException(LogLevel.Error, ex4);
            throw ex4;
        }
        if (string.IsNullOrWhiteSpace(serviceInfo.DiscoveryServiceAccessToken))
        {
            ArgumentException ex5 = new ArgumentException(SR.DiscoveryServiceTokenIsMissing, "serviceInfo");
            Logger.LogException(LogLevel.Error, ex5);
            throw ex5;
        }
        podAddress = serviceInfo.PodAddress;
        if (!string.IsNullOrEmpty(serviceInfo.UserAgent))
        {
            SetUserAgent(serviceInfo.UserAgent, appOverride: true);
        }
        else
        {
            userAgentOverridden = false;
        }
        authorizationHeader = new HttpHeader("Authorization", $"WRAP access_token=\"{serviceInfo.AccessToken}\"");
        authorizationHeaderForDiscoveryService = $"WRAP access_token=\"{serviceInfo.DiscoveryServiceAccessToken}\"";
        profileUri = new Uri($"{serviceInfo.PodAddress}/v1/userprofiles");
        firmwareUpdateInfoUrlFormat = $"{serviceInfo.FileUpdateServiceAddress}/api/FirmwareQuery?deviceFamily={{0}}&publishType=Latest&OneBL={{1}}&TwoUp={{2}}&currentFirmwareVersion={{3}}&IsForcedUpdate={{4}}";
        ephemerisProfileUrl = $"{serviceInfo.FileUpdateServiceAddress}/api/Ephemeris";
        timezoneUpdateInfoUrl = $"{serviceInfo.FileUpdateServiceAddress}/api/TimeZone";
    }

    internal void SetUserAgent(string newUserAgent, bool appOverride)
    {
        if (!userAgentOverridden || appOverride)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", newUserAgent);
            }
            userAgentHeader = new HttpHeader("User-Agent", newUserAgent);
            userAgentOverridden |= appOverride;
        }
    }

    internal FileUploadStatus UploadFileToCloud(Stream stream, LogFileTypes logType, string uploadId, UploadMetaData metadata, CancellationToken cancellationToken)
    {
        if (stream == null)
        {
            Exception ex = new ArgumentNullException("stream");
            Logger.LogException(LogLevel.Error, ex);
            throw ex;
        }
        if (uploadId == null)
        {
            Exception ex2 = new ArgumentNullException("uploadId");
            Logger.LogException(LogLevel.Error, ex2);
            throw ex2;
        }
        if (!stream.CanRead)
        {
            Exception ex3 = new ArgumentException(SR.CannotReadFromStream, "stream");
            Logger.LogException(LogLevel.Error, ex3);
            throw ex3;
        }
        if (string.IsNullOrWhiteSpace(uploadId))
        {
            Exception ex4 = new ArgumentException(CommonSR.UploadIdNotSpecified);
            Logger.LogException(LogLevel.Error, ex4);
            throw ex4;
        }
        if (logType != LogFileTypes.Telemetry && logType != LogFileTypes.CrashDump && logType != LogFileTypes.Sensor && logType != LogFileTypes.KAppLogs)
        {
            Exception ex5 = new ArgumentOutOfRangeException("logType", CommonSR.UnsupportedFileTypeForCloudUpload);
            Logger.LogException(LogLevel.Error, ex5);
            throw ex5;
        }
        if (logType == LogFileTypes.Sensor && metadata == null)
        {
            Exception ex6 = new ArgumentException(CommonSR.SensorLogMetaDataCantBeNull, "metaData");
            Logger.LogException(LogLevel.Error, ex6);
            throw ex6;
        }
        cancellationToken.ThrowIfCancellationRequested();
        Logger.Log(LogLevel.Info, "Uploading file to the cloud; Type: {0}, Upload Id: {1}, Size: {2}", logType, uploadId, stream.Length);
        string requestUri = string.Format("{0}/v2/MultiDevice/UploadSensorPayload?logType={1}", new object[2]
        {
            podAddress,
            logType.ToCloudUploadPameterValue()
        });
        using HttpClient httpClient = CreateHttpClient(authorizationHeader, MSBlockBlobHeader);
        httpClient.Timeout = SensorLogUploadTimeout;
        using HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri);
        httpRequestMessage.Headers.ExpectContinue = false;
        httpRequestMessage.Headers.Add("UploadId", uploadId);
        httpRequestMessage.Headers.Add("UploadMetaData", CargoClient.SerializeJson(metadata));
        httpRequestMessage.Content = new StreamContent(stream);
        httpRequestMessage.Content.Headers.ContentType = OctetStreamContentTypeHeaderValue;
        using HttpResponseMessage httpResponseMessage = Send(httpClient, httpRequestMessage, cancellationToken);
        string responseContent = string.Empty;
        if (httpResponseMessage.Content != null)
        {
            responseContent = httpResponseMessage.Content.ReadAsStringAsync().Result;
        }
        switch (httpResponseMessage.StatusCode)
        {
        case HttpStatusCode.OK:
            LogRequestAndResponseMessages(LogLevel.Verbose, httpRequestMessage, httpResponseMessage, responseContent);
            return FileUploadStatus.UploadDone;
        case HttpStatusCode.Accepted:
            LogRequestAndResponseMessages(LogLevel.Verbose, httpRequestMessage, httpResponseMessage, responseContent);
            return FileUploadStatus.Processing;
        case HttpStatusCode.Conflict:
            LogRequestAndResponseMessages(LogLevel.Info, httpRequestMessage, httpResponseMessage, responseContent);
            return FileUploadStatus.Conflict;
        case HttpStatusCode.ExpectationFailed:
            LogRequestAndResponseMessages(LogLevel.Warning, httpRequestMessage, httpResponseMessage, responseContent);
            return FileUploadStatus.UploadDone;
        default:
            LogRequestAndResponseMessages(LogLevel.Error, httpRequestMessage, httpResponseMessage, responseContent);
            throw CreateAppropriateException(httpResponseMessage, responseContent, string.Format(CommonSR.FileUploadToCloudFailed, new object[1] { logType }));
        }
    }

    internal Dictionary<string, LogUploadStatusInfo> GetLogProcessingUpdate(IEnumerable<string> uploadIDs, CancellationToken cancellationToken)
    {
        TimeSpan timeout = TimeSpan.FromSeconds(20.0);
        StringBuilder stringBuilder = new StringBuilder();
        bool flag = true;
        stringBuilder.AppendFormat("{0}/v2/MultiDevice/GetUploadStatus?uploadIds=", new object[1] { podAddress });
        foreach (string uploadID in uploadIDs)
        {
            stringBuilder.AppendFormat("{0}{1}", new object[2]
            {
                (!flag) ? "," : "",
                uploadID
            });
            flag = false;
        }
        using HttpClient httpClient = CreateHttpClient(authorizationHeader);
        httpClient.Timeout = timeout;
        using HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, stringBuilder.ToString());
        using HttpResponseMessage httpResponseMessage = Send(httpClient, requestMessage, cancellationToken);
        string text = string.Empty;
        if (httpResponseMessage.Content != null)
        {
            text = httpResponseMessage.Content.ReadAsStringAsync().Result;
        }
        if (!httpResponseMessage.IsSuccessStatusCode)
        {
            LogRequestAndResponseMessages(LogLevel.Warning, requestMessage, httpResponseMessage, text);
            throw CreateAppropriateException(httpResponseMessage, text, CommonSR.LogProcessingStatusDownloadError);
        }
        LogRequestAndResponseMessages(LogLevel.Verbose, requestMessage, httpResponseMessage, text);
        return CargoClient.DeserializeJson<Dictionary<string, LogUploadStatusInfo>>(text);
    }

    internal bool DownloadFile(string downloadFileUrl, Stream updateStream, TimeSpan timeout, params HttpHeader[] additionalHeaders)
    {
        return DownloadFile(downloadFileUrl, updateStream, timeout, CancellationToken.None, (IEnumerable<HttpHeader>)additionalHeaders);
    }

    internal bool DownloadFile(string downloadFileUrl, Stream updateStream, TimeSpan timeout, IEnumerable<HttpHeader> additionalHeaders)
    {
        return DownloadFile(downloadFileUrl, updateStream, timeout, CancellationToken.None, additionalHeaders);
    }

    internal bool DownloadFile(string downloadFileUrl, Stream updateStream, TimeSpan timeout, CancellationToken cancel, params HttpHeader[] additionalHeaders)
    {
        return DownloadFile(downloadFileUrl, updateStream, timeout, cancel, (IEnumerable<HttpHeader>)additionalHeaders);
    }

    internal bool DownloadFile(string downloadFileUrl, Stream updateStream, TimeSpan timeout, CancellationToken cancel, IEnumerable<HttpHeader> additionalHeaders)
    {
        string responseContent = string.Empty;
        Logger.Log(LogLevel.Info, "Downloading file using the URL: {0}", downloadFileUrl);
        bool result = false;
        using HttpClient httpClient = CreateHttpClient(additionalHeaders);
        httpClient.Timeout = timeout;
        using HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, downloadFileUrl);
        using HttpResponseMessage httpResponseMessage = Send(httpClient, requestMessage, cancel);
        if (httpResponseMessage.StatusCode == HttpStatusCode.OK)
        {
            LogRequestAndResponseMessages(LogLevel.Verbose, requestMessage, httpResponseMessage);
            httpResponseMessage.Content.CopyToAsync(updateStream).Wait();
            return true;
        }
        if (httpResponseMessage.Content != null)
        {
            responseContent = httpResponseMessage.Content.ReadAsStringAsync().Result;
        }
        LogRequestAndResponseMessages(LogLevel.Warning, requestMessage, httpResponseMessage, responseContent);
        return result;
    }

    internal EphemerisCloudVersion GetEphemerisVersion(CancellationToken cancellationToken)
    {
        string responseContent = string.Empty;
        Logger.Log(LogLevel.Info, "Downloading ephemeris version file from the cloud");
        TimeSpan timeout = TimeSpan.FromSeconds(20.0);
        using HttpClient httpClient = CreateHttpClient(authorizationHeader);
        httpClient.Timeout = timeout;
        using HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, ephemerisProfileUrl);
        using HttpResponseMessage httpResponseMessage = Send(httpClient, requestMessage, cancellationToken);
        if (httpResponseMessage.Content != null)
        {
            responseContent = httpResponseMessage.Content.ReadAsStringAsync().Result;
        }
        if (httpResponseMessage.StatusCode != HttpStatusCode.OK)
        {
            LogRequestAndResponseMessages(LogLevel.Warning, requestMessage, httpResponseMessage, responseContent);
            throw CreateAppropriateException(httpResponseMessage, responseContent, CommonSR.EphemerisVersionDownloadError);
        }
        LogRequestAndResponseMessages(LogLevel.Verbose, requestMessage, httpResponseMessage, responseContent);
        using Stream inputStream = httpResponseMessage.Content.ReadAsStreamAsync().Result;
        return CargoClient.DeserializeJson<EphemerisCloudVersion>(inputStream);
    }

    internal bool GetEphemeris(EphemerisCloudVersion ephemerisVersion, Stream updateStream, CancellationToken cancel)
    {
        Logger.Log(LogLevel.Info, "Downloading ephemeris data from the cloud");
        TimeSpan timeout = TimeSpan.FromMinutes(2.0);
        return DownloadFile(ephemerisVersion.EphemerisProcessedFileDataUrl, updateStream, timeout, cancel);
    }

    internal TimeZoneDataCloudVersion GetTimeZoneDataVersion(IUserProfile profile, CancellationToken cancellationToken)
    {
        string responseContent = string.Empty;
        TimeSpan timeout = TimeSpan.FromSeconds(20.0);
        using HttpClient httpClient = CreateHttpClient(EnumerableExtensions.Concat(authorizationHeader, GetLocalizationHeadersFromProfile(profile)));
        httpClient.Timeout = timeout;
        using HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, timezoneUpdateInfoUrl);
        using HttpResponseMessage httpResponseMessage = Send(httpClient, requestMessage, cancellationToken);
        if (httpResponseMessage.StatusCode != HttpStatusCode.OK)
        {
            if (httpResponseMessage.Content != null)
            {
                responseContent = httpResponseMessage.Content.ReadAsStringAsync().Result;
            }
            LogRequestAndResponseMessages(LogLevel.Warning, requestMessage, httpResponseMessage, responseContent);
            throw CreateAppropriateException(httpResponseMessage, responseContent, CommonSR.TimeZoneDataVersionDownloadError);
        }
        using Stream stream = httpResponseMessage.Content.ReadAsStreamAsync().Result;
        LogRequestAndResponseMessages(LogLevel.Verbose, requestMessage, httpResponseMessage, stream);
        return CargoClient.DeserializeJson<TimeZoneDataCloudVersion>(stream);
    }

    internal bool GetTimeZoneData(TimeZoneDataCloudVersion timeZoneDataVersion, IUserProfile profile, Stream updateStream)
    {
        TimeSpan timeout = TimeSpan.FromMinutes(2.0);
        return DownloadFile(timeZoneDataVersion.Url, updateStream, timeout, GetLocalizationHeadersFromProfile(profile));
    }

    internal FirmwareUpdateInfo GetLatestAvailableFirmwareVersion(FirmwareVersions deviceVersions, bool firmwareOnDeviceValid, List<KeyValuePair<string, string>> queryParams, CancellationToken cancellationToken)
    {
        string responseContent = string.Empty;
        TimeSpan timeout = TimeSpan.FromSeconds(20.0);
        FirmwareUpdateInfo firmwareUpdateInfo = null;
        StringBuilder stringBuilder = null;
        StringBuilder stringBuilder2 = new StringBuilder();
        if (queryParams != null)
        {
            stringBuilder = new StringBuilder();
            if (queryParams != null)
            {
                foreach (KeyValuePair<string, string> queryParam in queryParams)
                {
                    if (string.Compare(queryParam.Key, "debug.force", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        if (string.Compare(queryParam.Value, "true", StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            Logger.Log(LogLevel.Warning, "Firmware check parameter Debug.Force set");
                            firmwareOnDeviceValid = false;
                        }
                    }
                    else
                    {
                        stringBuilder.AppendFormat("&{0}={1}", new object[2] { queryParam.Key, queryParam.Value });
                    }
                }
            }
        }
        stringBuilder2.AppendFormat(firmwareUpdateInfoUrlFormat, deviceVersions.PcbId, deviceVersions.BootloaderVersion, deviceVersions.UpdaterVersion, deviceVersions.ApplicationVersion, firmwareOnDeviceValid ? "false" : "true");
        if (stringBuilder != null && stringBuilder.Length > 0)
        {
            stringBuilder2.Append(stringBuilder.ToString());
        }
        Logger.Log(LogLevel.Info, "Getting latest available firmware version from cloud: deviceFamily: {0}, OneBLVersion: {1}, TwoUpVersion: {2}, currentFirmwareVersion: {3}, IsForcedUpdate: {4}", deviceVersions.PcbId, deviceVersions.BootloaderVersion, deviceVersions.UpdaterVersion, deviceVersions.ApplicationVersion, firmwareOnDeviceValid ? "false" : "true");
        Uri requestUri = new Uri(stringBuilder2.ToString());
        using (HttpClient httpClient = CreateHttpClient(authorizationHeader))
        {
            httpClient.Timeout = timeout;
            using HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);
            using HttpResponseMessage httpResponseMessage = Send(httpClient, requestMessage, cancellationToken);
            if (httpResponseMessage.Content != null)
            {
                responseContent = httpResponseMessage.Content.ReadAsStringAsync().Result;
            }
            if (httpResponseMessage.StatusCode != HttpStatusCode.OK)
            {
                LogRequestAndResponseMessages(LogLevel.Warning, requestMessage, httpResponseMessage, responseContent);
                throw CreateAppropriateException(httpResponseMessage, responseContent, CommonSR.FirmwareUpdateInfoError);
            }
            LogRequestAndResponseMessages(LogLevel.Verbose, requestMessage, httpResponseMessage, responseContent);
            using Stream inputStream = httpResponseMessage.Content.ReadAsStreamAsync().Result;
            firmwareUpdateInfo = CargoClient.DeserializeJson<FirmwareUpdateInfo>(inputStream);
        }
        if (firmwareUpdateInfo.IsFirmwareUpdateAvailable)
        {
            Logger.Log(LogLevel.Info, "Firmware availability: {0} version from cloud: deviceFamily: {1}, Version: {2}", firmwareUpdateInfo.IsFirmwareUpdateAvailable, firmwareUpdateInfo.DeviceFamily, firmwareUpdateInfo.FirmwareVersion);
        }
        else
        {
            Logger.Log(LogLevel.Info, "Firmware availability: {0}", firmwareUpdateInfo.IsFirmwareUpdateAvailable);
        }
        if (!firmwareOnDeviceValid && !firmwareUpdateInfo.IsFirmwareUpdateAvailable)
        {
            Logger.Log(LogLevel.Warning, "Reported device firmware invalid, but cloud did not honor it");
        }
        return firmwareUpdateInfo;
    }

    internal void GetFirmwareUpdate(FirmwareUpdateInfo updateInfo, Stream updateStream, CancellationToken cancellationToken)
    {
        TimeSpan timeout = TimeSpan.FromMinutes(5.0);
        Logger.Log(LogLevel.Info, "Attempting to download firmware update into a local file from the cloud");
        string[] array = new string[3] { updateInfo.PrimaryUrl, updateInfo.MirrorUrl, updateInfo.FallbackUrl };
        bool flag = false;
        StringBuilder stringBuilder = new StringBuilder();
        for (int i = 0; i < array.Length; i++)
        {
            if (string.IsNullOrEmpty(array[i]))
            {
                ArgumentNullException ex = new ArgumentNullException("updateInfo");
                Logger.LogException(LogLevel.Error, ex);
                throw ex;
            }
            flag = DownloadFile(array[i], updateStream, timeout);
            if (flag)
            {
                break;
            }
        }
        if (!flag)
        {
            BandCloudException ex2 = new BandCloudException(string.Format(CommonSR.FirmwareUpdateDownloadError, new object[1] { stringBuilder }));
            Logger.LogException(LogLevel.Error, ex2);
            throw ex2;
        }
    }

    internal CloudProfile GetUserProfile(CancellationToken cancellationToken)
    {
        string responseContent = string.Empty;
        Logger.Log(LogLevel.Info, "Getting user profile from the cloud");
        TimeSpan timeout = TimeSpan.FromMinutes(1.0);
        using HttpClient httpClient = CreateHttpClient(authorizationHeader);
        httpClient.Timeout = timeout;
        using HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, profileUri);
        using HttpResponseMessage httpResponseMessage = Send(httpClient, requestMessage, cancellationToken);
        if (httpResponseMessage.Content != null)
        {
            responseContent = httpResponseMessage.Content.ReadAsStringAsync().Result;
        }
        if (httpResponseMessage.StatusCode != HttpStatusCode.OK)
        {
            LogRequestAndResponseMessages(LogLevel.Warning, requestMessage, httpResponseMessage, responseContent);
            throw CreateAppropriateException(httpResponseMessage, responseContent, CommonSR.ReadProfileFailed);
        }
        LogRequestAndResponseMessages(LogLevel.Verbose, requestMessage, httpResponseMessage, responseContent);
        using Stream inputStream = httpResponseMessage.Content.ReadAsStreamAsync().Result;
        return CargoClient.DeserializeJson<CloudProfile>(inputStream);
    }

    internal void SaveUserProfile(CloudProfile profile, bool createNew, CancellationToken cancellationToken)
    {
        string responseContent = string.Empty;
        Logger.Log(LogLevel.Info, "Saving user profile to the cloud");
        TimeSpan timeout = TimeSpan.FromMinutes(1.0);
        if (profile == null)
        {
            ArgumentNullException ex = new ArgumentNullException("profile");
            Logger.LogException(LogLevel.Error, ex);
            throw ex;
        }
        string json = CargoClient.SerializeJson(profile);
        using HttpClient httpClient = CreateHttpClient(authorizationHeader);
        httpClient.Timeout = timeout;
        using HttpRequestMessage httpRequestMessage = new HttpRequestMessage(createNew ? HttpMethod.Post : HttpMethod.Put, string.Format("{0}/{1}", new object[2]
        {
            profileUri,
            createNew ? "post" : "put"
        }));
        httpRequestMessage.Content = CreateJsonContent(json);
        using HttpResponseMessage httpResponseMessage = Send(httpClient, httpRequestMessage, cancellationToken);
        if (httpResponseMessage.Content != null)
        {
            responseContent = httpResponseMessage.Content.ReadAsStringAsync().Result;
        }
        if (httpResponseMessage.StatusCode != HttpStatusCode.OK)
        {
            LogRequestAndResponseMessages(LogLevel.Warning, httpRequestMessage, httpResponseMessage, responseContent);
            throw CreateAppropriateException(httpResponseMessage, responseContent, CommonSR.WriteProfileFailed);
        }
        LogRequestAndResponseMessages(LogLevel.Verbose, httpRequestMessage, httpResponseMessage, responseContent);
    }

    internal void SaveDeviceLinkToUserProfile(CloudProfileDeviceLink profile, CancellationToken cancellationToken)
    {
        string responseContent = string.Empty;
        Logger.Log(LogLevel.Info, "Saving device link to user profile");
        TimeSpan timeout = TimeSpan.FromMinutes(1.0);
        if (profile == null)
        {
            ArgumentNullException ex = new ArgumentNullException("profile");
            Logger.LogException(LogLevel.Error, ex);
            throw ex;
        }
        string json = CargoClient.SerializeJson(profile);
        using HttpClient httpClient = CreateHttpClient(authorizationHeader);
        httpClient.Timeout = timeout;
        using HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Put, $"{profileUri}/put");
        httpRequestMessage.Content = CreateJsonContent(json);
        using HttpResponseMessage httpResponseMessage = Send(httpClient, httpRequestMessage, cancellationToken);
        if (httpResponseMessage.Content != null)
        {
            responseContent = httpResponseMessage.Content.ReadAsStringAsync().Result;
        }
        if (httpResponseMessage.StatusCode != HttpStatusCode.OK)
        {
            LogRequestAndResponseMessages(LogLevel.Warning, httpRequestMessage, httpResponseMessage, responseContent);
            throw CreateAppropriateException(httpResponseMessage, responseContent, CommonSR.WriteProfileFailed);
        }
        LogRequestAndResponseMessages(LogLevel.Verbose, httpRequestMessage, httpResponseMessage, responseContent);
    }

    internal void SaveUserProfileFirmware(byte[] firmwareBytes, CancellationToken cancellationToken)
    {
        string responseContent = string.Empty;
        Logger.Log(LogLevel.Info, "Saving device firmware bytes to user profile");
        TimeSpan timeout = TimeSpan.FromMinutes(1.0);
        string json = CargoClient.SerializeJson(new CloudProfileFirmwareBytes
        {
            DeviceSettings = new CloudDeviceSettingsFirmwareBytes
            {
                FirmwareByteArray = Convert.ToBase64String(firmwareBytes)
            }
        });
        using HttpClient httpClient = CreateHttpClient(authorizationHeader);
        httpClient.Timeout = timeout;
        using HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Put, $"{profileUri}/put");
        httpRequestMessage.Content = CreateJsonContent(json);
        using HttpResponseMessage httpResponseMessage = Send(httpClient, httpRequestMessage, cancellationToken);
        if (httpResponseMessage.Content != null)
        {
            responseContent = httpResponseMessage.Content.ReadAsStringAsync().Result;
        }
        if (httpResponseMessage.StatusCode != HttpStatusCode.OK)
        {
            LogRequestAndResponseMessages(LogLevel.Warning, httpRequestMessage, httpResponseMessage, responseContent);
            throw CreateAppropriateException(httpResponseMessage, responseContent, CommonSR.WriteProfileFailed);
        }
        LogRequestAndResponseMessages(LogLevel.Verbose, httpRequestMessage, httpResponseMessage, responseContent);
    }

    private IEnumerable<HttpHeader> GetLocalizationHeadersFromProfile(IUserProfile profile)
    {
        yield return new HttpHeader("Accept-Language", profile.DeviceSettings.LocaleSettings.Language.ToLanguageCultureName());
        yield return new HttpHeader("Region", profile.DeviceSettings.LocaleSettings.LocaleId.ToRegionName());
    }

    private HttpClient CreateHttpClient(params HttpHeader[] additionalHeaders)
    {
        return CreateHttpClient((IEnumerable<HttpHeader>)additionalHeaders);
    }

    private HttpClient CreateHttpClient(IEnumerable<HttpHeader> additionalHeaders)
    {
        HttpClient httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add(userAgentHeader.Name, userAgentHeader.Value);
        if (additionalHeaders != null)
        {
            foreach (HttpHeader additionalHeader in additionalHeaders)
            {
                if (string.Compare(userAgentHeader.Name, additionalHeader.Name, StringComparison.OrdinalIgnoreCase) != 0)
                {
                    httpClient.DefaultRequestHeaders.Add(additionalHeader.Name, additionalHeader.Value);
                }
            }
            return httpClient;
        }
        return httpClient;
    }

    private HttpResponseMessage Send(HttpClient client, HttpRequestMessage requestMessage, CancellationToken cancellationToken)
    {
        try
        {
            return client.SendAsync(requestMessage, cancellationToken).Result;
        }
        catch (AggregateException ex)
        {
            if (ex.InnerExceptions.Count == 1)
            {
                if (ex.InnerException is TaskCanceledException)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    TimeoutException ex2 = new TimeoutException();
                    Logger.LogException(LogLevel.Error, ex2);
                    throw ex2;
                }
                Logger.LogException(LogLevel.Error, ex.InnerException);
                throw ex.InnerException;
            }
            throw;
        }
    }

    private static HttpContent CreateJsonContent(string json)
    {
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    private static BandHttpException CreateAppropriateException(HttpResponseMessage responseMessage, string responseContent, string message, Exception innerException = null)
    {
        HttpStatusCode statusCode = responseMessage.StatusCode;
        if (statusCode == HttpStatusCode.Unauthorized || statusCode == HttpStatusCode.Forbidden)
        {
            return new BandHttpSecurityException(responseContent, FormatMessage(responseMessage, responseContent, message), innerException);
        }
        return new BandHttpException(responseContent, FormatMessage(responseMessage, responseContent, message), innerException);
    }

    private static string FormatMessage(HttpResponseMessage responseMessage, string responseContent, string message)
    {
        StringWriter stringWriter = new StringWriter();
        stringWriter.WriteLine(message);
        if (string.IsNullOrWhiteSpace(responseMessage.ReasonPhrase))
        {
            stringWriter.WriteLine(" {0}: {1} {2}", new object[3]
            {
                CommonSR.HttpExceptionStatusLineLabel,
                (int)responseMessage.StatusCode,
                responseMessage.StatusCode
            });
        }
        else
        {
            stringWriter.WriteLine(" {0}: {1} {2} {3}", CommonSR.HttpExceptionStatusLineLabel, (int)responseMessage.StatusCode, responseMessage.StatusCode, responseMessage.ReasonPhrase);
        }
        stringWriter.Write(" {0}: {1} {2}", new object[3]
        {
            CommonSR.HttpExceptionRequestLineLabel,
            responseMessage.RequestMessage.Method,
            responseMessage.RequestMessage.RequestUri
        });
        if (!string.IsNullOrWhiteSpace(responseContent) && responseContent.Trim() != string.Empty)
        {
            stringWriter.WriteLine();
            stringWriter.Write(" {0}: {1}", new object[2]
            {
                CommonSR.HttpExceptionResponseContentLabel,
                responseContent
            });
        }
        return stringWriter.ToString();
    }

    private void LogRequestAndResponseMessages(LogLevel level, HttpRequestMessage requestMessage, HttpResponseMessage responseMessage)
    {
        lock (debugLock)
        {
            Logger.Log(level, "Request: {0} {1}", requestMessage.Method, requestMessage.RequestUri);
            Logger.Log(level, "Response StatusCode: {0} ({1})", responseMessage.StatusCode, (int)responseMessage.StatusCode);
        }
    }

    private void LogRequestAndResponseMessages(LogLevel level, HttpRequestMessage requestMessage, HttpResponseMessage responseMessage, string responseContent)
    {
        lock (debugLock)
        {
            LogRequestAndResponseMessages(level, requestMessage, responseMessage);
        }
    }

    private void LogRequestAndResponseMessages(LogLevel level, HttpRequestMessage requestMessage, HttpResponseMessage responseMessage, Stream responseStream)
    {
        lock (debugLock)
        {
            LogRequestAndResponseMessages(level, requestMessage, responseMessage);
        }
    }
}
