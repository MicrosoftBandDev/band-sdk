// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.WebTileManager
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

using Microsoft.Band.Admin.WebTiles;
using Microsoft.Band.Tiles;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Security.Credentials;

namespace Microsoft.Band.Admin
{
  internal class WebTileManager : IWebTileManager
  {
    public const string WebTileTempFolder = "Temp_WebTile";
    public const string WebTileFolder = "WebTiles";
    public static readonly string WebTileInstalledFolder = Path.Combine(new string[2]
    {
      "WebTiles",
      "Installed"
    });
    private const string PackageFolderName = "Package";
    private const string DataFolderName = "Data";
    private const string ManifestFileName = "manifest.json";
    private const string ZipTempFolder = "Temp_Zip";
    private const string HeadersFileName = "headers.json";
    private IStorageProvider storageProvider;
    private IImageProvider imageProvider;
    private object debugLock = new object();

    internal WebTileManager(IStorageProvider storageProvider, IImageProvider imageProvider)
    {
      this.storageProvider = storageProvider;
      this.imageProvider = imageProvider;
    }

    public async Task<IWebTile> GetWebTilePackageAsync(Uri uri)
    {
      IWebTile tilePackageAsync = (IWebTile) null;
      string url = this.GetWebTileUrlFromUri(uri);
      string zipPath;
      string str = zipPath;
      zipPath = await this.DownloadWebTileAsync(url);
      try
      {
        using (Stream zipStream = await Task.Run<Stream>((Func<Stream>) (() => this.storageProvider.OpenFileForRead(StorageProviderRoot.App, zipPath, -1))))
        {
          tilePackageAsync = await this.GetWebTilePackageAsync(zipStream, url);
          tilePackageAsync.RequestHeaders = WebTileAgentHelper.GetAgentHeadersForUrl(url, tilePackageAsync.Organization);
        }
      }
      finally
      {
        this.TryDeleteFolder(StorageProviderRoot.App, "Temp_Zip", "the temporary webtile file");
      }
      return tilePackageAsync;
    }

    public async Task<IWebTile> GetWebTilePackageAsync(
      Stream source,
      string sourceFileName)
    {
      IWebTile webTile = await Task.Run<IWebTile>((Func<IWebTile>) (() =>
      {
        this.storageProvider.DeleteFolder(StorageProviderRoot.App, "Temp_WebTile");
        ZipUtils.Unzip(this.storageProvider, StorageProviderRoot.App, source, "Temp_WebTile");
        try
        {
          IWebTile tilePackageAsync = this.DeserializeWebTilePackageFromJson(Path.Combine(new string[2]
          {
            "Temp_WebTile",
            "manifest.json"
          }));
          tilePackageAsync.Validate();
          return tilePackageAsync;
        }
        catch
        {
          this.TryDeleteFolder(StorageProviderRoot.App, "Temp_WebTile", "temp folder after failed JSON deserialize");
          throw;
        }
      }));
      webTile.StorageProvider = this.storageProvider;
      webTile.ImageProvider = this.imageProvider;
      webTile.PackageFolderPath = "Temp_WebTile";
      await webTile.LoadIconsAsync();
      return webTile;
    }

    public Task InstallWebTileAsync(IWebTile webTile) => Task.Run((Action) (() => this.InstallWebTile(webTile)));

    private void InstallWebTile(IWebTile webTile)
    {
      if (webTile == null)
        throw new ArgumentNullException(nameof (webTile));
      webTile.TileId = Guid.NewGuid();
      string str = Path.Combine(new string[2]
      {
        WebTileManager.WebTileInstalledFolder,
        webTile.TileId.ToString()
      });
      string relativeDestFolder = Path.Combine(new string[2]
      {
        str,
        "Package"
      });
      this.storageProvider.MoveFolder(StorageProviderRoot.App, "Temp_WebTile", StorageProviderRoot.App, relativeDestFolder);
      webTile.PackageFolderPath = relativeDestFolder;
      webTile.DataFolderPath = Path.Combine(new string[2]
      {
        str,
        "Data"
      });
      webTile.SaveResourceAuthentication();
      if (!this.storageProvider.DirectoryExists(StorageProviderRoot.App, webTile.DataFolderPath))
        this.storageProvider.CreateFolder(StorageProviderRoot.App, webTile.DataFolderPath);
      webTile.SaveUserSettings();
      if (webTile.RequestHeaders == null || ((IEnumerable<HeaderNameValuePair>) webTile.RequestHeaders).Count<HeaderNameValuePair>() <= 0)
        return;
      using (Stream outputStream = this.storageProvider.OpenFileForWrite(StorageProviderRoot.App, Path.Combine(new string[2]
      {
        webTile.DataFolderPath,
        "headers.json"
      }), false))
        CargoClient.SerializeJson(outputStream, (object) webTile.RequestHeaders);
    }

    public Task UninstallWebTileAsync(Guid tileId) => Task.Run((Action) (() => this.UninstallWebTile(tileId)));

    private void UninstallWebTile(Guid tileId)
    {
      this.GetWebTile(tileId)?.DeleteStoredResourceCredentials();
      this.storageProvider.DeleteFolder(StorageProviderRoot.App, Path.Combine(new string[2]
      {
        WebTileManager.WebTileInstalledFolder,
        tileId.ToString()
      }));
    }

    public Task<IList<IWebTile>> GetInstalledWebTilesAsync(
      bool loadTileDisplayIcons)
    {
      throw new NotImplementedException();
    }

    public async Task<AdminBandTile> CreateAdminBandTileAsync(
      IWebTile webTile,
      BandClass bandClass)
    {
      AdminTileSettings tileSettings = AdminTileSettings.None;
      List<BandIcon> icons = new List<BandIcon>();
      bool flag = false;
      if (webTile.BadgeIcons != null)
        tileSettings |= AdminTileSettings.EnableBadging;
      AdminBandTile bandTile = new AdminBandTile(webTile.TileId, webTile.Name, tileSettings);
      bandTile.OwnerId = AdminBandTile.WebTileOwnerId;
      icons.Add(webTile.TileBandIcon);
      if (webTile.BadgeBandIcon != null)
      {
        flag = true;
        icons.Add(webTile.BadgeBandIcon);
      }
      else
        icons.Add(webTile.TileBandIcon);
      if (webTile.AdditionalBandIcons != null)
        icons.AddRange((IEnumerable<BandIcon>) webTile.AdditionalBandIcons);
      bandTile.SetImageList(webTile.TileId, (IList<BandIcon>) icons, 0U, flag ? new uint?(1U) : new uint?());
      TileLayout[] layoutsAsync = await webTile.GetLayoutsAsync(bandClass);
      for (int key = 0; key < layoutsAsync.Length; ++key)
        bandTile.Layouts.Add((uint) key, layoutsAsync[key]);
      if (webTile.TileTheme != null)
        bandTile.Theme = this.GetBandTheme(webTile);
      return bandTile;
    }

    private BandTheme GetBandTheme(IWebTile webTile) => new BandTheme()
    {
      Base = this.GetBandColor(webTile.TileTheme.Base),
      Highlight = this.GetBandColor(webTile.TileTheme.Highlight),
      Lowlight = this.GetBandColor(webTile.TileTheme.Lowlight),
      SecondaryText = this.GetBandColor(webTile.TileTheme.SecondaryText),
      HighContrast = this.GetBandColor(webTile.TileTheme.HighContrast),
      Muted = this.GetBandColor(webTile.TileTheme.Muted)
    };

    private BandColor GetBandColor(string color) => color != null ? new BandColor(uint.Parse(color, NumberStyles.HexNumber)) : throw new ArgumentNullException(nameof (color));

    public IList<Guid> GetInstalledWebTileIds()
    {
      IList<Guid> installedWebTileIds = (IList<Guid>) new List<Guid>();
      if (!this.storageProvider.DirectoryExists(StorageProviderRoot.App, "WebTiles") || !this.storageProvider.DirectoryExists(StorageProviderRoot.App, WebTileManager.WebTileInstalledFolder))
        return installedWebTileIds;
      foreach (string folder in this.storageProvider.GetFolders(StorageProviderRoot.App, WebTileManager.WebTileInstalledFolder))
      {
        Guid result;
        if (Guid.TryParse(Path.GetFileName(folder), out result))
          installedWebTileIds.Add(result);
      }
      return installedWebTileIds;
    }

    public IWebTile GetWebTile(Guid tileId)
    {
      string str = Path.Combine(new string[2]
      {
        WebTileManager.WebTileInstalledFolder,
        tileId.ToString()
      });
      if (!this.storageProvider.DirectoryExists(StorageProviderRoot.App, str))
        return (IWebTile) null;
      string relativePath1 = Path.Combine(new string[2]
      {
        str,
        "Package"
      });
      if (!this.storageProvider.DirectoryExists(StorageProviderRoot.App, relativePath1))
      {
        this.TryDeleteFolder(StorageProviderRoot.App, str, "folder for installed webtile with missing package path");
        return (IWebTile) null;
      }
      try
      {
        IWebTile webTile = this.DeserializeWebTilePackageFromJson(Path.Combine(new string[2]
        {
          relativePath1,
          "manifest.json"
        }));
        webTile.TileId = tileId;
        webTile.StorageProvider = this.storageProvider;
        webTile.PackageFolderPath = relativePath1;
        webTile.DataFolderPath = Path.Combine(new string[2]
        {
          str,
          "Data"
        });
        webTile.LoadResourceAuthentication();
        webTile.LoadUserSettings();
        string relativePath2 = Path.Combine(new string[2]
        {
          webTile.DataFolderPath,
          "headers.json"
        });
        if (this.storageProvider.FileExists(StorageProviderRoot.App, relativePath2))
        {
          using (Stream inputStream = this.storageProvider.OpenFileForRead(StorageProviderRoot.App, relativePath2))
            webTile.RequestHeaders = CargoClient.DeserializeJson<HeaderNameValuePair[]>(inputStream);
        }
        return webTile;
      }
      catch
      {
        this.TryDeleteFolder(StorageProviderRoot.App, str, "folder for installed webtile after failed JSON deserialize");
        throw;
      }
    }

    public Task DeleteAllStoredResourceCredentialsAsync() => Task.Run((Action) (() => this.DeleteAllStoredResourceCredentials()));

    private void DeleteAllStoredResourceCredentials()
    {
      PasswordVault passwordVault = new PasswordVault();
      foreach (PasswordCredential passwordCredential in (IEnumerable<PasswordCredential>) passwordVault.RetrieveAll())
        passwordVault.Remove(passwordCredential);
    }

    private IWebTile DeserializeWebTilePackageFromJson(string filePath)
    {
      using (Stream stream = this.storageProvider.OpenFileForRead(StorageProviderRoot.App, filePath, -1))
        return this.DeserializeWebTilePackageFromJson(stream);
    }

    private IWebTile DeserializeWebTilePackageFromJson(Stream stream, int bufferSize = 8192)
    {
      using (StreamReader streamReader = new StreamReader(stream, Encoding.UTF8, false, bufferSize, true))
      {
        using (JsonTextReader reader = new JsonTextReader((TextReader) streamReader))
          return (IWebTile) JsonSerializer.Create().Deserialize<WebTile>((JsonReader) reader);
      }
    }

    private Task<IWebTile> DeserializeWebTilePackageFromJsonAsync(Guid tileId) => throw new NotImplementedException();

    private Task SerializeWebTiletoJsonManifestFileAsync(IWebTile tile, string manifestFilePath) => throw new NotImplementedException();

    private bool TryDeleteFolder(
      StorageProviderRoot root,
      string folderRelativePath,
      string description)
    {
      try
      {
        this.storageProvider.DeleteFolder(root, folderRelativePath);
      }
      catch (Exception ex)
      {
        Logger.LogException(LogLevel.Warning, ex, string.Format("Error occurred while deleting {0}.", new object[1]
        {
          (object) description
        }));
        return false;
      }
      return true;
    }

    public Task<string> DownloadWebTileAsync(string url) => this.DownloadWebTileAsync(url, CancellationToken.None);

    public async Task<string> DownloadWebTileAsync(
      string url,
      CancellationToken cancellationToken)
    {
      if (url == null)
        throw new ArgumentNullException(nameof (url));
      Stopwatch transferTimer = Stopwatch.StartNew();
      Stream updateStream = (Stream) null;
      string localWebTileTempFileRelativePath = Path.Combine(new string[2]
      {
        "Temp_Zip",
        Guid.NewGuid().ToString() + ".webtile"
      });
      await Task.Run((Action) (() => this.storageProvider.CreateFolder(StorageProviderRoot.App, "Temp_Zip")));
      try
      {
        updateStream = await Task.Run<Stream>((Func<Stream>) (() => this.storageProvider.OpenFileForWrite(StorageProviderRoot.App, localWebTileTempFileRelativePath, false)));
      }
      catch (Exception ex)
      {
        BandException e = new BandException(string.Format(CommonSR.WebTileDownloadTempFileOpenError, new object[1]
        {
          (object) localWebTileTempFileRelativePath
        }), ex);
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      try
      {
        using (updateStream)
          await this.DownloadFileAsync(url, updateStream, TimeSpan.FromSeconds(60.0), cancellationToken);
      }
      catch
      {
        this.TryDeleteFolder(StorageProviderRoot.App, "Temp_Zip", "the temporary webtile file, after an error downloading it");
        throw;
      }
      transferTimer.Stop();
      Logger.Log(LogLevel.Info, "Time to get web tile: {0}", (object) transferTimer.Elapsed);
      return localWebTileTempFileRelativePath;
    }

    private async Task DownloadFileAsync(
      string downloadFileUrl,
      Stream updateStream,
      TimeSpan timeout,
      CancellationToken cancellationToken)
    {
      string responseContent = string.Empty;
      Logger.Log(LogLevel.Info, "Downloading file using the URL: {0}", (object) downloadFileUrl);
      using (HttpClient client = new HttpClient())
      {
        client.Timeout = timeout;
        using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, downloadFileUrl))
        {
          using (HttpResponseMessage responseMessage = await this.SendAsync(client, requestMessage, cancellationToken))
          {
            if (responseMessage.StatusCode == HttpStatusCode.OK)
            {
              this.LogRequestAndResponseMessages(LogLevel.Verbose, requestMessage, responseMessage);
              await responseMessage.Content.CopyToAsync(updateStream);
            }
            else
            {
              if (responseMessage.Content != null)
                responseContent = responseMessage.Content.ReadAsStringAsync().Result;
              throw WebTileManager.CreateAppropriateException(responseMessage, responseContent, CommonSR.WebTileDownloadError);
            }
          }
        }
      }
    }

    private Task<HttpResponseMessage> SendAsync(
      HttpClient client,
      HttpRequestMessage requestMessage,
      CancellationToken cancellationToken)
    {
      try
      {
        return client.SendAsync(requestMessage, cancellationToken);
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

    private static BandHttpException CreateAppropriateException(
      HttpResponseMessage responseMessage,
      string responseContent,
      string message,
      Exception innerException = null)
    {
      return new BandHttpException(responseContent, WebTileManager.FormatMessage(responseMessage, responseContent, message), innerException);
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

    private string GetWebTileUrlFromUri(Uri uri)
    {
      WwwFormUrlDecoder wwwFormUrlDecoder = new WwwFormUrlDecoder(uri.Query);
      string firstValueByName1 = wwwFormUrlDecoder.GetFirstValueByName("action");
      if (firstValueByName1 != null && firstValueByName1.Equals("download-manifest", StringComparison.OrdinalIgnoreCase))
      {
        string firstValueByName2 = wwwFormUrlDecoder.GetFirstValueByName("url");
        if (firstValueByName2 != null)
          return firstValueByName2;
      }
      throw new BandException(CommonSR.WTUnableToParseUrlFromUri);
    }
  }
}
