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
using Microsoft.Band.Admin.WebTiles;
using Microsoft.Band.Tiles;
using Newtonsoft.Json;
using Windows.Foundation;
using Windows.Security.Credentials;

namespace Microsoft.Band.Admin;

internal class WebTileManager : IWebTileManager
{
    public const string WebTileTempFolder = "Temp_WebTile";

    public const string WebTileFolder = "WebTiles";

    public static readonly string WebTileInstalledFolder = Path.Combine(new string[2] { "WebTiles", "Installed" });

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
        IWebTile webTile = null;
        string url = GetWebTileUrlFromUri(uri);
        string zipPath = default(string);
        _ = zipPath;
        zipPath = await DownloadWebTileAsync(url);
        try
        {
            using (Stream zipStream = await Task.Run(() => storageProvider.OpenFileForRead(StorageProviderRoot.App, zipPath, -1)))
            {
                webTile = await GetWebTilePackageAsync(zipStream, url);
                webTile.RequestHeaders = WebTileAgentHelper.GetAgentHeadersForUrl(url, webTile.Organization);
            }
            return webTile;
        }
        finally
        {
            TryDeleteFolder(StorageProviderRoot.App, "Temp_Zip", "the temporary webtile file");
        }
    }

    public async Task<IWebTile> GetWebTilePackageAsync(Stream source, string sourceFileName)
    {
        IWebTile webTile = await Task.Run(delegate
        {
            storageProvider.DeleteFolder(StorageProviderRoot.App, "Temp_WebTile");
            ZipUtils.Unzip(storageProvider, StorageProviderRoot.App, source, "Temp_WebTile");
            try
            {
                IWebTile webTile2 = DeserializeWebTilePackageFromJson(Path.Combine(new string[2] { "Temp_WebTile", "manifest.json" }));
                webTile2.Validate();
                return webTile2;
            }
            catch
            {
                TryDeleteFolder(StorageProviderRoot.App, "Temp_WebTile", "temp folder after failed JSON deserialize");
                throw;
            }
        });
        webTile.StorageProvider = storageProvider;
        webTile.ImageProvider = imageProvider;
        webTile.PackageFolderPath = "Temp_WebTile";
        await webTile.LoadIconsAsync();
        return webTile;
    }

    public Task InstallWebTileAsync(IWebTile webTile)
    {
        return Task.Run(delegate
        {
            InstallWebTile(webTile);
        });
    }

    private void InstallWebTile(IWebTile webTile)
    {
        if (webTile == null)
        {
            throw new ArgumentNullException("webTile");
        }
        webTile.TileId = Guid.NewGuid();
        string text = Path.Combine(new string[2]
        {
            WebTileInstalledFolder,
            webTile.TileId.ToString()
        });
        string text2 = Path.Combine(new string[2] { text, "Package" });
        storageProvider.MoveFolder(StorageProviderRoot.App, "Temp_WebTile", StorageProviderRoot.App, text2);
        webTile.PackageFolderPath = text2;
        webTile.DataFolderPath = Path.Combine(new string[2] { text, "Data" });
        webTile.SaveResourceAuthentication();
        if (!storageProvider.DirectoryExists(StorageProviderRoot.App, webTile.DataFolderPath))
        {
            storageProvider.CreateFolder(StorageProviderRoot.App, webTile.DataFolderPath);
        }
        webTile.SaveUserSettings();
        if (webTile.RequestHeaders != null && webTile.RequestHeaders.Count() > 0)
        {
            string relativePath = Path.Combine(new string[2] { webTile.DataFolderPath, "headers.json" });
            using Stream outputStream = storageProvider.OpenFileForWrite(StorageProviderRoot.App, relativePath, append: false);
            CargoClient.SerializeJson(outputStream, webTile.RequestHeaders);
        }
    }

    public Task UninstallWebTileAsync(Guid tileId)
    {
        return Task.Run(delegate
        {
            UninstallWebTile(tileId);
        });
    }

    private void UninstallWebTile(Guid tileId)
    {
        GetWebTile(tileId)?.DeleteStoredResourceCredentials();
        storageProvider.DeleteFolder(StorageProviderRoot.App, Path.Combine(new string[2]
        {
            WebTileInstalledFolder,
            tileId.ToString()
        }));
    }

    public Task<IList<IWebTile>> GetInstalledWebTilesAsync(bool loadTileDisplayIcons)
    {
        throw new NotImplementedException();
    }

    public async Task<AdminBandTile> CreateAdminBandTileAsync(IWebTile webTile, BandClass bandClass)
    {
        AdminTileSettings adminTileSettings = AdminTileSettings.None;
        List<BandIcon> list = new List<BandIcon>();
        bool flag = false;
        if (webTile.BadgeIcons != null)
        {
            adminTileSettings |= AdminTileSettings.EnableBadging;
        }
        AdminBandTile bandTile = new AdminBandTile(webTile.TileId, webTile.Name, adminTileSettings)
        {
            OwnerId = AdminBandTile.WebTileOwnerId
        };
        list.Add(webTile.TileBandIcon);
        if (webTile.BadgeBandIcon != null)
        {
            flag = true;
            list.Add(webTile.BadgeBandIcon);
        }
        else
        {
            list.Add(webTile.TileBandIcon);
        }
        if (webTile.AdditionalBandIcons != null)
        {
            list.AddRange(webTile.AdditionalBandIcons);
        }
        bandTile.SetImageList(webTile.TileId, list, 0u, flag ? new uint?(1u) : null);
        TileLayout[] array = await webTile.GetLayoutsAsync(bandClass);
        for (int i = 0; i < array.Length; i++)
        {
            bandTile.Layouts.Add((uint)i, array[i]);
        }
        if (webTile.TileTheme != null)
        {
            bandTile.Theme = GetBandTheme(webTile);
        }
        return bandTile;
    }

    private BandTheme GetBandTheme(IWebTile webTile)
    {
        return new BandTheme
        {
            Base = GetBandColor(webTile.TileTheme.Base),
            Highlight = GetBandColor(webTile.TileTheme.Highlight),
            Lowlight = GetBandColor(webTile.TileTheme.Lowlight),
            SecondaryText = GetBandColor(webTile.TileTheme.SecondaryText),
            HighContrast = GetBandColor(webTile.TileTheme.HighContrast),
            Muted = GetBandColor(webTile.TileTheme.Muted)
        };
    }

    private BandColor GetBandColor(string color)
    {
        if (color == null)
        {
            throw new ArgumentNullException("color");
        }
        return new BandColor(uint.Parse(color, NumberStyles.HexNumber));
    }

    public IList<Guid> GetInstalledWebTileIds()
    {
        IList<Guid> list = new List<Guid>();
        if (!storageProvider.DirectoryExists(StorageProviderRoot.App, "WebTiles") || !storageProvider.DirectoryExists(StorageProviderRoot.App, WebTileInstalledFolder))
        {
            return list;
        }
        string[] folders = storageProvider.GetFolders(StorageProviderRoot.App, WebTileInstalledFolder);
        for (int i = 0; i < folders.Length; i++)
        {
            if (Guid.TryParse(Path.GetFileName(folders[i]), out var result))
            {
                list.Add(result);
            }
        }
        return list;
    }

    public IWebTile GetWebTile(Guid tileId)
    {
        string text = Path.Combine(new string[2]
        {
            WebTileInstalledFolder,
            tileId.ToString()
        });
        if (!storageProvider.DirectoryExists(StorageProviderRoot.App, text))
        {
            return null;
        }
        string text2 = Path.Combine(new string[2] { text, "Package" });
        if (!storageProvider.DirectoryExists(StorageProviderRoot.App, text2))
        {
            TryDeleteFolder(StorageProviderRoot.App, text, "folder for installed webtile with missing package path");
            return null;
        }
        try
        {
            IWebTile webTile = DeserializeWebTilePackageFromJson(Path.Combine(new string[2] { text2, "manifest.json" }));
            webTile.TileId = tileId;
            webTile.StorageProvider = storageProvider;
            webTile.PackageFolderPath = text2;
            webTile.DataFolderPath = Path.Combine(new string[2] { text, "Data" });
            webTile.LoadResourceAuthentication();
            webTile.LoadUserSettings();
            string relativePath = Path.Combine(new string[2] { webTile.DataFolderPath, "headers.json" });
            if (storageProvider.FileExists(StorageProviderRoot.App, relativePath))
            {
                using Stream inputStream = storageProvider.OpenFileForRead(StorageProviderRoot.App, relativePath);
                webTile.RequestHeaders = CargoClient.DeserializeJson<HeaderNameValuePair[]>(inputStream);
            }
            return webTile;
        }
        catch
        {
            TryDeleteFolder(StorageProviderRoot.App, text, "folder for installed webtile after failed JSON deserialize");
            throw;
        }
    }

    public Task DeleteAllStoredResourceCredentialsAsync()
    {
        return Task.Run(delegate
        {
            DeleteAllStoredResourceCredentials();
        });
    }

    private void DeleteAllStoredResourceCredentials()
    {
        //IL_0000: Unknown result type (might be due to invalid IL or missing references)
        //IL_0006: Expected O, but got Unknown
        PasswordVault val = new PasswordVault();
        foreach (PasswordCredential item in val.RetrieveAll())
        {
            val.Remove(item);
        }
    }

    private IWebTile DeserializeWebTilePackageFromJson(string filePath)
    {
        using Stream stream = storageProvider.OpenFileForRead(StorageProviderRoot.App, filePath, -1);
        return DeserializeWebTilePackageFromJson(stream);
    }

    private IWebTile DeserializeWebTilePackageFromJson(Stream stream, int bufferSize = 8192)
    {
        using StreamReader reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize, leaveOpen: true);
        using JsonTextReader reader2 = new JsonTextReader(reader);
        return JsonSerializer.Create().Deserialize<WebTile>(reader2);
    }

    private Task<IWebTile> DeserializeWebTilePackageFromJsonAsync(Guid tileId)
    {
        throw new NotImplementedException();
    }

    private Task SerializeWebTiletoJsonManifestFileAsync(IWebTile tile, string manifestFilePath)
    {
        throw new NotImplementedException();
    }

    private bool TryDeleteFolder(StorageProviderRoot root, string folderRelativePath, string description)
    {
        try
        {
            storageProvider.DeleteFolder(root, folderRelativePath);
        }
        catch (Exception e)
        {
            Logger.LogException(LogLevel.Warning, e, $"Error occurred while deleting {description}.");
            return false;
        }
        return true;
    }

    public Task<string> DownloadWebTileAsync(string url)
    {
        return DownloadWebTileAsync(url, CancellationToken.None);
    }

    public async Task<string> DownloadWebTileAsync(string url, CancellationToken cancellationToken)
    {
        if (url == null)
        {
            throw new ArgumentNullException("url");
        }
        Stopwatch transferTimer = Stopwatch.StartNew();
        string text = Guid.NewGuid().ToString() + ".webtile";
        string localWebTileTempFileRelativePath = Path.Combine(new string[2] { "Temp_Zip", text });
        await Task.Run(delegate
        {
            storageProvider.CreateFolder(StorageProviderRoot.App, "Temp_Zip");
        });
        Stream updateStream;
        try
        {
            updateStream = await Task.Run(() => storageProvider.OpenFileForWrite(StorageProviderRoot.App, localWebTileTempFileRelativePath, append: false));
        }
        catch (Exception innerException)
        {
            BandException ex = new BandException(string.Format(CommonSR.WebTileDownloadTempFileOpenError, new object[1] { localWebTileTempFileRelativePath }), innerException);
            Logger.LogException(LogLevel.Error, ex);
            throw ex;
        }
        try
        {
            using (updateStream)
            {
                await DownloadFileAsync(url, updateStream, TimeSpan.FromSeconds(60.0), cancellationToken);
            }
        }
        catch
        {
            TryDeleteFolder(StorageProviderRoot.App, "Temp_Zip", "the temporary webtile file, after an error downloading it");
            throw;
        }
        transferTimer.Stop();
        Logger.Log(LogLevel.Info, "Time to get web tile: {0}", transferTimer.Elapsed);
        return localWebTileTempFileRelativePath;
    }

    private async Task DownloadFileAsync(string downloadFileUrl, Stream updateStream, TimeSpan timeout, CancellationToken cancellationToken)
    {
        string responseContent = string.Empty;
        Logger.Log(LogLevel.Info, "Downloading file using the URL: {0}", downloadFileUrl);
        using HttpClient client = new HttpClient();
        client.Timeout = timeout;
        using HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, downloadFileUrl);
        using HttpResponseMessage responseMessage = await SendAsync(client, requestMessage, cancellationToken);
        if (responseMessage.StatusCode == HttpStatusCode.OK)
        {
            LogRequestAndResponseMessages(LogLevel.Verbose, requestMessage, responseMessage);
            await responseMessage.Content.CopyToAsync(updateStream);
            return;
        }
        if (responseMessage.Content != null)
        {
            responseContent = responseMessage.Content.ReadAsStringAsync().Result;
        }
        throw CreateAppropriateException(responseMessage, responseContent, CommonSR.WebTileDownloadError);
    }

    private Task<HttpResponseMessage> SendAsync(HttpClient client, HttpRequestMessage requestMessage, CancellationToken cancellationToken)
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

    private static BandHttpException CreateAppropriateException(HttpResponseMessage responseMessage, string responseContent, string message, Exception innerException = null)
    {
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

    private string GetWebTileUrlFromUri(Uri uri)
    {
        //IL_0006: Unknown result type (might be due to invalid IL or missing references)
        //IL_000c: Expected O, but got Unknown
        WwwFormUrlDecoder val = new WwwFormUrlDecoder(uri.Query);
        string firstValueByName = val.GetFirstValueByName("action");
        if (firstValueByName != null && firstValueByName.Equals("download-manifest", StringComparison.OrdinalIgnoreCase))
        {
            string firstValueByName2 = val.GetFirstValueByName("url");
            if (firstValueByName2 != null)
            {
                return firstValueByName2;
            }
        }
        throw new BandException(CommonSR.WTUnableToParseUrlFromUri);
    }
}
