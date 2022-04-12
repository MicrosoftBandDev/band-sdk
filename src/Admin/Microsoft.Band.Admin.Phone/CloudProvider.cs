// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.CloudProvider
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Band.Admin
{
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

    internal CloudProvider(ServiceInfo serviceInfo)
    {
      if (serviceInfo == null)
      {
        ArgumentNullException e = new ArgumentNullException(nameof (serviceInfo));
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      if (string.IsNullOrWhiteSpace(serviceInfo.PodAddress))
      {
        ArgumentException e = new ArgumentException(Microsoft.Band.Admin.Phone.SR.ServiceAddressIsMissing, nameof (serviceInfo));
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      if (string.IsNullOrWhiteSpace(serviceInfo.AccessToken))
      {
        ArgumentException e = new ArgumentException(Microsoft.Band.Admin.Phone.SR.AccessTokenIsMissing, nameof (serviceInfo));
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      if (string.IsNullOrWhiteSpace(serviceInfo.DiscoveryServiceAddress))
      {
        ArgumentException e = new ArgumentException(Microsoft.Band.Admin.Phone.SR.DiscoveryServiceAddressIsMissing, nameof (serviceInfo));
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      if (string.IsNullOrWhiteSpace(serviceInfo.DiscoveryServiceAccessToken))
      {
        ArgumentException e = new ArgumentException(Microsoft.Band.Admin.Phone.SR.DiscoveryServiceTokenIsMissing, nameof (serviceInfo));
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      this.podAddress = serviceInfo.PodAddress;
      if (!string.IsNullOrEmpty(serviceInfo.UserAgent))
        this.SetUserAgent(serviceInfo.UserAgent, true);
      else
        this.userAgentOverridden = false;
      this.authorizationHeader = new HttpHeader("Authorization", string.Format("WRAP access_token=\"{0}\"", new object[1]
      {
        (object) serviceInfo.AccessToken
      }));
      this.authorizationHeaderForDiscoveryService = string.Format("WRAP access_token=\"{0}\"", new object[1]
      {
        (object) serviceInfo.DiscoveryServiceAccessToken
      });
      this.profileUri = new Uri(string.Format("{0}/v1/userprofiles", new object[1]
      {
        (object) serviceInfo.PodAddress
      }));
      this.firmwareUpdateInfoUrlFormat = string.Format("{0}/api/FirmwareQuery?deviceFamily={{0}}&publishType=Latest&OneBL={{1}}&TwoUp={{2}}&currentFirmwareVersion={{3}}&IsForcedUpdate={{4}}", new object[1]
      {
        (object) serviceInfo.FileUpdateServiceAddress
      });
      this.ephemerisProfileUrl = string.Format("{0}/api/Ephemeris", new object[1]
      {
        (object) serviceInfo.FileUpdateServiceAddress
      });
      this.timezoneUpdateInfoUrl = string.Format("{0}/api/TimeZone", new object[1]
      {
        (object) serviceInfo.FileUpdateServiceAddress
      });
    }

    internal string UserAgent => this.userAgentHeader.Value;

    internal void SetUserAgent(string newUserAgent, bool appOverride)
    {
      if (!(!this.userAgentOverridden | appOverride))
        return;
      using (HttpClient httpClient = new HttpClient())
        httpClient.DefaultRequestHeaders.Add("User-Agent", newUserAgent);
      this.userAgentHeader = new HttpHeader("User-Agent", newUserAgent);
      this.userAgentOverridden |= appOverride;
    }

    internal FileUploadStatus UploadFileToCloud(
      Stream stream,
      LogFileTypes logType,
      string uploadId,
      UploadMetaData metadata,
      CancellationToken cancellationToken)
    {
      if (stream == null)
      {
        Exception e = (Exception) new ArgumentNullException(nameof (stream));
        Logger.LogException(LogLevel.Error, e);
        throw e;
      }
      if (uploadId == null)
      {
        Exception e = (Exception) new ArgumentNullException(nameof (uploadId));
        Logger.LogException(LogLevel.Error, e);
        throw e;
      }
      if (!stream.CanRead)
      {
        Exception e = (Exception) new ArgumentException(Microsoft.Band.Admin.Phone.SR.CannotReadFromStream, nameof (stream));
        Logger.LogException(LogLevel.Error, e);
        throw e;
      }
      if (string.IsNullOrWhiteSpace(uploadId))
      {
        Exception e = (Exception) new ArgumentException(CommonSR.UploadIdNotSpecified);
        Logger.LogException(LogLevel.Error, e);
        throw e;
      }
      if (logType != LogFileTypes.Telemetry && logType != LogFileTypes.CrashDump && logType != LogFileTypes.Sensor && logType != LogFileTypes.KAppLogs)
      {
        Exception e = (Exception) new ArgumentOutOfRangeException(nameof (logType), CommonSR.UnsupportedFileTypeForCloudUpload);
        Logger.LogException(LogLevel.Error, e);
        throw e;
      }
      if (logType == LogFileTypes.Sensor && metadata == null)
      {
        Exception e = (Exception) new ArgumentException(CommonSR.SensorLogMetaDataCantBeNull, "metaData");
        Logger.LogException(LogLevel.Error, e);
        throw e;
      }
      cancellationToken.ThrowIfCancellationRequested();
      Logger.Log(LogLevel.Info, "Uploading file to the cloud; Type: {0}, Upload Id: {1}, Size: {2}", (object) logType, (object) uploadId, (object) stream.Length);
      string requestUri = string.Format("{0}/v2/MultiDevice/UploadSensorPayload?logType={1}", new object[2]
      {
        (object) this.podAddress,
        (object) logType.ToCloudUploadPameterValue()
      });
      using (HttpClient httpClient = this.CreateHttpClient(this.authorizationHeader, CloudProvider.MSBlockBlobHeader))
      {
        httpClient.Timeout = CloudProvider.SensorLogUploadTimeout;
        using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri))
        {
          requestMessage.Headers.ExpectContinue = new bool?(false);
          requestMessage.Headers.Add("UploadId", uploadId);
          requestMessage.Headers.Add("UploadMetaData", CargoClient.SerializeJson((object) metadata));
          requestMessage.Content = (HttpContent) new StreamContent(stream);
          requestMessage.Content.Headers.ContentType = CloudProvider.OctetStreamContentTypeHeaderValue;
          using (HttpResponseMessage responseMessage = this.Send(httpClient, requestMessage, cancellationToken))
          {
            string responseContent = string.Empty;
            if (responseMessage.Content != null)
              responseContent = responseMessage.Content.ReadAsStringAsync().Result;
            switch (responseMessage.StatusCode)
            {
              case HttpStatusCode.OK:
                this.LogRequestAndResponseMessages(LogLevel.Verbose, requestMessage, responseMessage, responseContent);
                return FileUploadStatus.UploadDone;
              case HttpStatusCode.Accepted:
                this.LogRequestAndResponseMessages(LogLevel.Verbose, requestMessage, responseMessage, responseContent);
                return FileUploadStatus.Processing;
              case HttpStatusCode.Conflict:
                this.LogRequestAndResponseMessages(LogLevel.Info, requestMessage, responseMessage, responseContent);
                return FileUploadStatus.Conflict;
              case HttpStatusCode.ExpectationFailed:
                this.LogRequestAndResponseMessages(LogLevel.Warning, requestMessage, responseMessage, responseContent);
                return FileUploadStatus.UploadDone;
              default:
                this.LogRequestAndResponseMessages(LogLevel.Error, requestMessage, responseMessage, responseContent);
                throw CloudProvider.CreateAppropriateException(responseMessage, responseContent, string.Format(CommonSR.FileUploadToCloudFailed, new object[1]
                {
                  (object) logType
                }));
            }
          }
        }
      }
    }

    internal Dictionary<string, LogUploadStatusInfo> GetLogProcessingUpdate(
      IEnumerable<string> uploadIDs,
      CancellationToken cancellationToken)
    {
      TimeSpan timeSpan = TimeSpan.FromSeconds(20.0);
      StringBuilder stringBuilder = new StringBuilder();
      bool flag = true;
      stringBuilder.AppendFormat("{0}/v2/MultiDevice/GetUploadStatus?uploadIds=", new object[1]
      {
        (object) this.podAddress
      });
      foreach (string uploadId in uploadIDs)
      {
        stringBuilder.AppendFormat("{0}{1}", new object[2]
        {
          !flag ? (object) "," : (object) "",
          (object) uploadId
        });
        flag = false;
      }
      using (HttpClient httpClient = this.CreateHttpClient(this.authorizationHeader))
      {
        httpClient.Timeout = timeSpan;
        using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, stringBuilder.ToString()))
        {
          using (HttpResponseMessage responseMessage = this.Send(httpClient, requestMessage, cancellationToken))
          {
            string str = string.Empty;
            if (responseMessage.Content != null)
              str = responseMessage.Content.ReadAsStringAsync().Result;
            if (!responseMessage.IsSuccessStatusCode)
            {
              this.LogRequestAndResponseMessages(LogLevel.Warning, requestMessage, responseMessage, str);
              throw CloudProvider.CreateAppropriateException(responseMessage, str, CommonSR.LogProcessingStatusDownloadError);
            }
            this.LogRequestAndResponseMessages(LogLevel.Verbose, requestMessage, responseMessage, str);
            return CargoClient.DeserializeJson<Dictionary<string, LogUploadStatusInfo>>(str);
          }
        }
      }
    }

    internal bool DownloadFile(
      string downloadFileUrl,
      Stream updateStream,
      TimeSpan timeout,
      params HttpHeader[] additionalHeaders)
    {
      return this.DownloadFile(downloadFileUrl, updateStream, timeout, CancellationToken.None, (IEnumerable<HttpHeader>) additionalHeaders);
    }

    internal bool DownloadFile(
      string downloadFileUrl,
      Stream updateStream,
      TimeSpan timeout,
      IEnumerable<HttpHeader> additionalHeaders)
    {
      return this.DownloadFile(downloadFileUrl, updateStream, timeout, CancellationToken.None, additionalHeaders);
    }

    internal bool DownloadFile(
      string downloadFileUrl,
      Stream updateStream,
      TimeSpan timeout,
      CancellationToken cancel,
      params HttpHeader[] additionalHeaders)
    {
      return this.DownloadFile(downloadFileUrl, updateStream, timeout, cancel, (IEnumerable<HttpHeader>) additionalHeaders);
    }

    internal bool DownloadFile(
      string downloadFileUrl,
      Stream updateStream,
      TimeSpan timeout,
      CancellationToken cancel,
      IEnumerable<HttpHeader> additionalHeaders)
    {
      string responseContent = string.Empty;
      Logger.Log(LogLevel.Info, "Downloading file using the URL: {0}", (object) downloadFileUrl);
      bool flag = false;
      using (HttpClient httpClient = this.CreateHttpClient(additionalHeaders))
      {
        httpClient.Timeout = timeout;
        using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, downloadFileUrl))
        {
          using (HttpResponseMessage responseMessage = this.Send(httpClient, requestMessage, cancel))
          {
            if (responseMessage.StatusCode == HttpStatusCode.OK)
            {
              this.LogRequestAndResponseMessages(LogLevel.Verbose, requestMessage, responseMessage);
              responseMessage.Content.CopyToAsync(updateStream).Wait();
              flag = true;
            }
            else
            {
              if (responseMessage.Content != null)
                responseContent = responseMessage.Content.ReadAsStringAsync().Result;
              this.LogRequestAndResponseMessages(LogLevel.Warning, requestMessage, responseMessage, responseContent);
            }
          }
        }
      }
      return flag;
    }

    internal EphemerisCloudVersion GetEphemerisVersion(
      CancellationToken cancellationToken)
    {
      string responseContent = string.Empty;
      Logger.Log(LogLevel.Info, "Downloading ephemeris version file from the cloud");
      TimeSpan timeSpan = TimeSpan.FromSeconds(20.0);
      using (HttpClient httpClient = this.CreateHttpClient(this.authorizationHeader))
      {
        httpClient.Timeout = timeSpan;
        using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, this.ephemerisProfileUrl))
        {
          using (HttpResponseMessage responseMessage = this.Send(httpClient, requestMessage, cancellationToken))
          {
            if (responseMessage.Content != null)
              responseContent = responseMessage.Content.ReadAsStringAsync().Result;
            if (responseMessage.StatusCode != HttpStatusCode.OK)
            {
              this.LogRequestAndResponseMessages(LogLevel.Warning, requestMessage, responseMessage, responseContent);
              throw CloudProvider.CreateAppropriateException(responseMessage, responseContent, CommonSR.EphemerisVersionDownloadError);
            }
            this.LogRequestAndResponseMessages(LogLevel.Verbose, requestMessage, responseMessage, responseContent);
            using (Stream result = responseMessage.Content.ReadAsStreamAsync().Result)
              return CargoClient.DeserializeJson<EphemerisCloudVersion>(result);
          }
        }
      }
    }

    internal bool GetEphemeris(
      EphemerisCloudVersion ephemerisVersion,
      Stream updateStream,
      CancellationToken cancel)
    {
      Logger.Log(LogLevel.Info, "Downloading ephemeris data from the cloud");
      TimeSpan timeout = TimeSpan.FromMinutes(2.0);
      return this.DownloadFile(ephemerisVersion.EphemerisProcessedFileDataUrl, updateStream, timeout, cancel);
    }

    internal TimeZoneDataCloudVersion GetTimeZoneDataVersion(
      IUserProfile profile,
      CancellationToken cancellationToken)
    {
      string responseContent = string.Empty;
      TimeSpan timeSpan = TimeSpan.FromSeconds(20.0);
      using (HttpClient httpClient = this.CreateHttpClient(EnumerableExtensions.Concat<HttpHeader>(this.authorizationHeader, this.GetLocalizationHeadersFromProfile(profile))))
      {
        httpClient.Timeout = timeSpan;
        using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, this.timezoneUpdateInfoUrl))
        {
          using (HttpResponseMessage responseMessage = this.Send(httpClient, requestMessage, cancellationToken))
          {
            if (responseMessage.StatusCode != HttpStatusCode.OK)
            {
              if (responseMessage.Content != null)
                responseContent = responseMessage.Content.ReadAsStringAsync().Result;
              this.LogRequestAndResponseMessages(LogLevel.Warning, requestMessage, responseMessage, responseContent);
              throw CloudProvider.CreateAppropriateException(responseMessage, responseContent, CommonSR.TimeZoneDataVersionDownloadError);
            }
            using (Stream result = responseMessage.Content.ReadAsStreamAsync().Result)
            {
              this.LogRequestAndResponseMessages(LogLevel.Verbose, requestMessage, responseMessage, result);
              return CargoClient.DeserializeJson<TimeZoneDataCloudVersion>(result);
            }
          }
        }
      }
    }

    internal bool GetTimeZoneData(
      TimeZoneDataCloudVersion timeZoneDataVersion,
      IUserProfile profile,
      Stream updateStream)
    {
      TimeSpan timeout = TimeSpan.FromMinutes(2.0);
      return this.DownloadFile(timeZoneDataVersion.Url, updateStream, timeout, this.GetLocalizationHeadersFromProfile(profile));
    }

    internal FirmwareUpdateInfo GetLatestAvailableFirmwareVersion(
      FirmwareVersions deviceVersions,
      bool firmwareOnDeviceValid,
      List<KeyValuePair<string, string>> queryParams,
      CancellationToken cancellationToken)
    {
      string responseContent = string.Empty;
      TimeSpan timeSpan = TimeSpan.FromSeconds(20.0);
      FirmwareUpdateInfo availableFirmwareVersion = (FirmwareUpdateInfo) null;
      StringBuilder stringBuilder1 = (StringBuilder) null;
      StringBuilder stringBuilder2 = new StringBuilder();
      if (queryParams != null)
      {
        stringBuilder1 = new StringBuilder();
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
              stringBuilder1.AppendFormat("&{0}={1}", new object[2]
              {
                (object) queryParam.Key,
                (object) queryParam.Value
              });
          }
        }
      }
      stringBuilder2.AppendFormat(this.firmwareUpdateInfoUrlFormat, (object) deviceVersions.PcbId, (object) deviceVersions.BootloaderVersion, (object) deviceVersions.UpdaterVersion, (object) deviceVersions.ApplicationVersion, firmwareOnDeviceValid ? (object) "false" : (object) "true");
      if (stringBuilder1 != null && stringBuilder1.Length > 0)
        stringBuilder2.Append(stringBuilder1.ToString());
      Logger.Log(LogLevel.Info, "Getting latest available firmware version from cloud: deviceFamily: {0}, OneBLVersion: {1}, TwoUpVersion: {2}, currentFirmwareVersion: {3}, IsForcedUpdate: {4}", (object) deviceVersions.PcbId, (object) deviceVersions.BootloaderVersion, (object) deviceVersions.UpdaterVersion, (object) deviceVersions.ApplicationVersion, firmwareOnDeviceValid ? (object) "false" : (object) "true");
      Uri requestUri = new Uri(stringBuilder2.ToString());
      using (HttpClient httpClient = this.CreateHttpClient(this.authorizationHeader))
      {
        httpClient.Timeout = timeSpan;
        using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri))
        {
          using (HttpResponseMessage responseMessage = this.Send(httpClient, requestMessage, cancellationToken))
          {
            if (responseMessage.Content != null)
              responseContent = responseMessage.Content.ReadAsStringAsync().Result;
            if (responseMessage.StatusCode != HttpStatusCode.OK)
            {
              this.LogRequestAndResponseMessages(LogLevel.Warning, requestMessage, responseMessage, responseContent);
              throw CloudProvider.CreateAppropriateException(responseMessage, responseContent, CommonSR.FirmwareUpdateInfoError);
            }
            this.LogRequestAndResponseMessages(LogLevel.Verbose, requestMessage, responseMessage, responseContent);
            using (Stream result = responseMessage.Content.ReadAsStreamAsync().Result)
              availableFirmwareVersion = CargoClient.DeserializeJson<FirmwareUpdateInfo>(result);
          }
        }
      }
      if (availableFirmwareVersion.IsFirmwareUpdateAvailable)
        Logger.Log(LogLevel.Info, "Firmware availability: {0} version from cloud: deviceFamily: {1}, Version: {2}", (object) availableFirmwareVersion.IsFirmwareUpdateAvailable, (object) availableFirmwareVersion.DeviceFamily, (object) availableFirmwareVersion.FirmwareVersion);
      else
        Logger.Log(LogLevel.Info, "Firmware availability: {0}", (object) availableFirmwareVersion.IsFirmwareUpdateAvailable);
      if (!firmwareOnDeviceValid && !availableFirmwareVersion.IsFirmwareUpdateAvailable)
        Logger.Log(LogLevel.Warning, "Reported device firmware invalid, but cloud did not honor it");
      return availableFirmwareVersion;
    }

    internal void GetFirmwareUpdate(
      FirmwareUpdateInfo updateInfo,
      Stream updateStream,
      CancellationToken cancellationToken)
    {
      TimeSpan timeout = TimeSpan.FromMinutes(5.0);
      Logger.Log(LogLevel.Info, "Attempting to download firmware update into a local file from the cloud");
      string[] strArray = new string[3]
      {
        updateInfo.PrimaryUrl,
        updateInfo.MirrorUrl,
        updateInfo.FallbackUrl
      };
      bool flag = false;
      StringBuilder stringBuilder = new StringBuilder();
      for (int index = 0; index < strArray.Length; ++index)
      {
        if (string.IsNullOrEmpty(strArray[index]))
        {
          ArgumentNullException e = new ArgumentNullException(nameof (updateInfo));
          Logger.LogException(LogLevel.Error, (Exception) e);
          throw e;
        }
        flag = this.DownloadFile(strArray[index], updateStream, timeout);
        if (flag)
          break;
      }
      if (!flag)
      {
        BandCloudException e = new BandCloudException(string.Format(CommonSR.FirmwareUpdateDownloadError, new object[1]
        {
          (object) stringBuilder
        }));
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
    }

    internal CloudProfile GetUserProfile(CancellationToken cancellationToken)
    {
      string responseContent = string.Empty;
      Logger.Log(LogLevel.Info, "Getting user profile from the cloud");
      TimeSpan timeSpan = TimeSpan.FromMinutes(1.0);
      using (HttpClient httpClient = this.CreateHttpClient(this.authorizationHeader))
      {
        httpClient.Timeout = timeSpan;
        using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, this.profileUri))
        {
          using (HttpResponseMessage responseMessage = this.Send(httpClient, requestMessage, cancellationToken))
          {
            if (responseMessage.Content != null)
              responseContent = responseMessage.Content.ReadAsStringAsync().Result;
            if (responseMessage.StatusCode != HttpStatusCode.OK)
            {
              this.LogRequestAndResponseMessages(LogLevel.Warning, requestMessage, responseMessage, responseContent);
              throw CloudProvider.CreateAppropriateException(responseMessage, responseContent, CommonSR.ReadProfileFailed);
            }
            this.LogRequestAndResponseMessages(LogLevel.Verbose, requestMessage, responseMessage, responseContent);
            using (Stream result = responseMessage.Content.ReadAsStreamAsync().Result)
              return CargoClient.DeserializeJson<CloudProfile>(result);
          }
        }
      }
    }

    internal void SaveUserProfile(
      CloudProfile profile,
      bool createNew,
      CancellationToken cancellationToken)
    {
      string responseContent = string.Empty;
      Logger.Log(LogLevel.Info, "Saving user profile to the cloud");
      TimeSpan timeSpan = TimeSpan.FromMinutes(1.0);
      if (profile == null)
      {
        ArgumentNullException e = new ArgumentNullException(nameof (profile));
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      string json = CargoClient.SerializeJson((object) profile);
      using (HttpClient httpClient = this.CreateHttpClient(this.authorizationHeader))
      {
        httpClient.Timeout = timeSpan;
        using (HttpRequestMessage requestMessage = new HttpRequestMessage(createNew ? HttpMethod.Post : HttpMethod.Put, string.Format("{0}/{1}", new object[2]
        {
          (object) this.profileUri,
          createNew ? (object) "post" : (object) "put"
        })))
        {
          requestMessage.Content = CloudProvider.CreateJsonContent(json);
          using (HttpResponseMessage responseMessage = this.Send(httpClient, requestMessage, cancellationToken))
          {
            if (responseMessage.Content != null)
              responseContent = responseMessage.Content.ReadAsStringAsync().Result;
            if (responseMessage.StatusCode != HttpStatusCode.OK)
            {
              this.LogRequestAndResponseMessages(LogLevel.Warning, requestMessage, responseMessage, responseContent);
              throw CloudProvider.CreateAppropriateException(responseMessage, responseContent, CommonSR.WriteProfileFailed);
            }
            this.LogRequestAndResponseMessages(LogLevel.Verbose, requestMessage, responseMessage, responseContent);
          }
        }
      }
    }

    internal void SaveDeviceLinkToUserProfile(
      CloudProfileDeviceLink profile,
      CancellationToken cancellationToken)
    {
      string responseContent = string.Empty;
      Logger.Log(LogLevel.Info, "Saving device link to user profile");
      TimeSpan timeSpan = TimeSpan.FromMinutes(1.0);
      if (profile == null)
      {
        ArgumentNullException e = new ArgumentNullException(nameof (profile));
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      string json = CargoClient.SerializeJson((object) profile);
      using (HttpClient httpClient = this.CreateHttpClient(this.authorizationHeader))
      {
        httpClient.Timeout = timeSpan;
        using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Put, string.Format("{0}/put", new object[1]
        {
          (object) this.profileUri
        })))
        {
          requestMessage.Content = CloudProvider.CreateJsonContent(json);
          using (HttpResponseMessage responseMessage = this.Send(httpClient, requestMessage, cancellationToken))
          {
            if (responseMessage.Content != null)
              responseContent = responseMessage.Content.ReadAsStringAsync().Result;
            if (responseMessage.StatusCode != HttpStatusCode.OK)
            {
              this.LogRequestAndResponseMessages(LogLevel.Warning, requestMessage, responseMessage, responseContent);
              throw CloudProvider.CreateAppropriateException(responseMessage, responseContent, CommonSR.WriteProfileFailed);
            }
            this.LogRequestAndResponseMessages(LogLevel.Verbose, requestMessage, responseMessage, responseContent);
          }
        }
      }
    }

    internal void SaveUserProfileFirmware(byte[] firmwareBytes, CancellationToken cancellationToken)
    {
      string responseContent = string.Empty;
      Logger.Log(LogLevel.Info, "Saving device firmware bytes to user profile");
      TimeSpan timeSpan = TimeSpan.FromMinutes(1.0);
      string json = CargoClient.SerializeJson((object) new CloudProfileFirmwareBytes()
      {
        DeviceSettings = new CloudDeviceSettingsFirmwareBytes()
        {
          FirmwareByteArray = Convert.ToBase64String(firmwareBytes)
        }
      });
      using (HttpClient httpClient = this.CreateHttpClient(this.authorizationHeader))
      {
        httpClient.Timeout = timeSpan;
        using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Put, string.Format("{0}/put", new object[1]
        {
          (object) this.profileUri
        })))
        {
          requestMessage.Content = CloudProvider.CreateJsonContent(json);
          using (HttpResponseMessage responseMessage = this.Send(httpClient, requestMessage, cancellationToken))
          {
            if (responseMessage.Content != null)
              responseContent = responseMessage.Content.ReadAsStringAsync().Result;
            if (responseMessage.StatusCode != HttpStatusCode.OK)
            {
              this.LogRequestAndResponseMessages(LogLevel.Warning, requestMessage, responseMessage, responseContent);
              throw CloudProvider.CreateAppropriateException(responseMessage, responseContent, CommonSR.WriteProfileFailed);
            }
            this.LogRequestAndResponseMessages(LogLevel.Verbose, requestMessage, responseMessage, responseContent);
          }
        }
      }
    }

    private IEnumerable<HttpHeader> GetLocalizationHeadersFromProfile(
      IUserProfile profile)
    {
      yield return new HttpHeader("Accept-Language", profile.DeviceSettings.LocaleSettings.Language.ToLanguageCultureName());
      yield return new HttpHeader("Region", profile.DeviceSettings.LocaleSettings.LocaleId.ToRegionName());
    }

    private HttpClient CreateHttpClient(params HttpHeader[] additionalHeaders) => this.CreateHttpClient((IEnumerable<HttpHeader>) additionalHeaders);

    private HttpClient CreateHttpClient(IEnumerable<HttpHeader> additionalHeaders)
    {
      HttpClient httpClient = new HttpClient();
      httpClient.DefaultRequestHeaders.Add(this.userAgentHeader.Name, this.userAgentHeader.Value);
      if (additionalHeaders != null)
      {
        foreach (HttpHeader additionalHeader in additionalHeaders)
        {
          if (string.Compare(this.userAgentHeader.Name, additionalHeader.Name, StringComparison.OrdinalIgnoreCase) != 0)
            httpClient.DefaultRequestHeaders.Add(additionalHeader.Name, additionalHeader.Value);
        }
      }
      return httpClient;
    }

    private HttpResponseMessage Send(
      HttpClient client,
      HttpRequestMessage requestMessage,
      CancellationToken cancellationToken)
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
            TimeoutException e = new TimeoutException();
            Logger.LogException(LogLevel.Error, (Exception) e);
            throw e;
          }
          Logger.LogException(LogLevel.Error, ex.InnerException);
          throw ex.InnerException;
        }
        throw;
      }
    }

    private static HttpContent CreateJsonContent(string json) => (HttpContent) new StringContent(json, Encoding.UTF8, "application/json");

    private static BandHttpException CreateAppropriateException(
      HttpResponseMessage responseMessage,
      string responseContent,
      string message,
      Exception innerException = null)
    {
      BandHttpException appropriateException;
      switch (responseMessage.StatusCode)
      {
        case HttpStatusCode.Unauthorized:
        case HttpStatusCode.Forbidden:
          appropriateException = (BandHttpException) new BandHttpSecurityException(responseContent, CloudProvider.FormatMessage(responseMessage, responseContent, message), innerException);
          break;
        default:
          appropriateException = new BandHttpException(responseContent, CloudProvider.FormatMessage(responseMessage, responseContent, message), innerException);
          break;
      }
      return appropriateException;
    }

    private static string FormatMessage(
      HttpResponseMessage responseMessage,
      string responseContent,
      string message)
    {
      StringWriter stringWriter = new StringWriter();
      stringWriter.WriteLine(message);
      if (string.IsNullOrWhiteSpace(responseMessage.ReasonPhrase))
        stringWriter.WriteLine(" {0}: {1} {2}", new object[3]
        {
          (object) CommonSR.HttpExceptionStatusLineLabel,
          (object) (int) responseMessage.StatusCode,
          (object) responseMessage.StatusCode
        });
      else
        stringWriter.WriteLine(" {0}: {1} {2} {3}", (object) CommonSR.HttpExceptionStatusLineLabel, (object) (int) responseMessage.StatusCode, (object) responseMessage.StatusCode, (object) responseMessage.ReasonPhrase);
      stringWriter.Write(" {0}: {1} {2}", new object[3]
      {
        (object) CommonSR.HttpExceptionRequestLineLabel,
        (object) responseMessage.RequestMessage.Method,
        (object) responseMessage.RequestMessage.RequestUri
      });
      if (!string.IsNullOrWhiteSpace(responseContent) && responseContent.Trim() != string.Empty)
      {
        stringWriter.WriteLine();
        stringWriter.Write(" {0}: {1}", new object[2]
        {
          (object) CommonSR.HttpExceptionResponseContentLabel,
          (object) responseContent
        });
      }
      return stringWriter.ToString();
    }

    private void LogRequestAndResponseMessages(
      LogLevel level,
      HttpRequestMessage requestMessage,
      HttpResponseMessage responseMessage)
    {
      lock (this.debugLock)
      {
        Logger.Log(level, "Request: {0} {1}", (object) requestMessage.Method, (object) requestMessage.RequestUri);
        Logger.Log(level, "Response StatusCode: {0} ({1})", (object) responseMessage.StatusCode, (object) (int) responseMessage.StatusCode);
      }
    }

    private void LogRequestAndResponseMessages(
      LogLevel level,
      HttpRequestMessage requestMessage,
      HttpResponseMessage responseMessage,
      string responseContent)
    {
      lock (this.debugLock)
        this.LogRequestAndResponseMessages(level, requestMessage, responseMessage);
    }

    private void LogRequestAndResponseMessages(
      LogLevel level,
      HttpRequestMessage requestMessage,
      HttpResponseMessage responseMessage,
      Stream responseStream)
    {
      lock (this.debugLock)
        this.LogRequestAndResponseMessages(level, requestMessage, responseMessage);
    }
  }
}
