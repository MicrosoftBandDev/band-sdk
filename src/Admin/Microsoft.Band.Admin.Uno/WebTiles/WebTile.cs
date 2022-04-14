using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Band.Tiles;
using Microsoft.Band.Tiles.Pages;
using Newtonsoft.Json.Linq;
using Windows.Data.Xml.Dom;
using Windows.Security.Credentials;

namespace Microsoft.Band.Admin.WebTiles;

[DataContract]
public class WebTile : IWebTile
{
    private const string LastSyncFileName = "LastSync.json";

    private const string CacheFileName = "ResourceCache.json";

    private const string UserSettingsFileName = "UserSettings.json";

    private const int TileIconWidthAndHeight = 46;

    private const int BadgeIconWidthAndHeight = 24;

    private string name;

    private string description;

    private uint manifestVersion;

    private uint version;

    private string versionString;

    private string author;

    private string organization;

    private string contactEmail;

    private Dictionary<int, string> tileIcon;

    private Dictionary<int, string> badgeIcon;

    private Dictionary<string, string> additionalIcons;

    private WebTileTheme tileTheme;

    private uint refreshIntervalMinutes;

    private WebTileResource[] resources;

    private WebTilePage[] pages;

    private WebTileNotification[] notifications;

    private BandIcon tileBandIcon;

    private BandIcon badgeBandIcon;

    private BandIcon[] additionalBandIcons;

    internal List<string> iconFilenames;

    internal Dictionary<string, int> iconIndices;

    private Dictionary<string, int> layoutIndices;

    internal Dictionary<string, string> variableMappings;

    private bool lastUpdateError;

    private bool currentUpdateError;

    private HeaderNameValuePair[] requestHeaders;

    private WebTileUserSettings userSettings;

    private WebTileSyncInfo lastSyncInfo;

    public const uint MinManifestVersion = 1u;

    public const uint MaxManifestVersion = 1u;

    public const uint MinVersion = 1u;

    public const int MinNameLength = 1;

    public const int MaxNameLength = 21;

    public const int MaxDescriptionLength = 100;

    public const int MaxVersionStringLength = 10;

    public const int MaxAuthorLength = 50;

    public const int MaxOrganizationLength = 100;

    public const int MaxContactEmailLength = 100;

    public const uint MinRefreshIntervalMinutes = 15u;

    public const uint MaxRefreshIntervalMinutes = 180u;

    public const int MaxAdditionalIcons = 8;

    public const int MaxPages = 7;

    public const int ErrorLayoutIndex = 0;

    public const int FirstWebTileLayoutIndex = 1;

    public const int MaxLayouts = 4;

    public const int ScrollingTextLayoutTitleElementId = 1;

    public const int ScrollingTextLayoutBodyElementId = 2;

    public const int ScrollingTextLayoutLocalTimeElementId = 3;

    public const string ErrorPageTimeStampFormat = "{0:MM/dd - hh:mm tt}";

    public const string WebTileVariableNamePattern = "([A-Za-z_]\\w*)";

    public const string WebTileVariablePattern = "\\{\\{([A-Za-z_]\\w*)\\}\\}";

    private WebTilePropertyValidator validator;

    private const string SimplePageBaseGuid = "A5EECE73496945D1A863CFA76DC485";

    private const string ErrorPageGuid = "A5EECE73496945D1A863CFA76DC485FF";

    public bool AllowInvalidValues
    {
        get
        {
            return validator.AllowInvalidValues;
        }
        set
        {
            validator.AllowInvalidValues = value;
        }
    }

    public Dictionary<string, string> PropertyErrors => validator.PropertyErrors;

    public Guid TileId { get; set; }

    public IImageProvider ImageProvider { get; set; }

    public string PackageFolderPath { get; set; }

    public string DataFolderPath { get; set; }

    public IStorageProvider StorageProvider { get; set; }

    public HeaderNameValuePair[] RequestHeaders
    {
        get
        {
            return requestHeaders;
        }
        set
        {
            requestHeaders = value;
            if (Resources != null)
            {
                IWebTileResource[] array = Resources;
                for (int i = 0; i < array.Length; i++)
                {
                    array[i].RequestHeaders = requestHeaders;
                }
            }
        }
    }

    public Dictionary<string, int> LayoutIndices => layoutIndices;

    public Dictionary<string, int> IconIndices => iconIndices;

    [DataMember(Name = "name", IsRequired = true)]
    public string Name
    {
        get
        {
            return name;
        }
        set
        {
            validator.CheckProperty("Name", value != null, CommonSR.WTNameCannotBeNull);
            validator.SetStringProperty(ref name, value, "Name", 1, 21);
        }
    }

    [DataMember(Name = "description", EmitDefaultValue = false)]
    public string Description
    {
        get
        {
            return description;
        }
        set
        {
            validator.SetStringProperty(ref description, value, "Description", 0, 100);
        }
    }

    [DataMember(Name = "manifestVersion", IsRequired = true)]
    public uint ManifestVersion
    {
        get
        {
            return manifestVersion;
        }
        set
        {
            validator.SetUintProperty(ref manifestVersion, value, "ManifestVersion", 1u, 1u);
        }
    }

    [DataMember(Name = "version", EmitDefaultValue = true)]
    public uint Version
    {
        get
        {
            return version;
        }
        set
        {
            validator.SetProperty(ref version, value, "Version", 1 <= value, string.Format(CommonSR.WTVersionTooSmall, new object[1] { 1u }));
        }
    }

    [DataMember(Name = "versionString", EmitDefaultValue = false)]
    public string VersionString
    {
        get
        {
            if (versionString == null)
            {
                return version.ToString();
            }
            return versionString;
        }
        set
        {
            validator.SetStringProperty(ref versionString, value, "VersionString", 0, 10);
        }
    }

    [DataMember(Name = "author", EmitDefaultValue = false)]
    public string Author
    {
        get
        {
            return author;
        }
        set
        {
            validator.SetStringProperty(ref author, value, "Author", 0, 50);
        }
    }

    [DataMember(Name = "organization", EmitDefaultValue = false)]
    public string Organization
    {
        get
        {
            return organization;
        }
        set
        {
            validator.SetStringProperty(ref organization, value, "Organization", 0, 100);
        }
    }

    [DataMember(Name = "contactEmail", EmitDefaultValue = false)]
    public string ContactEmail
    {
        get
        {
            return contactEmail;
        }
        set
        {
            validator.SetStringProperty(ref contactEmail, value, "ContactEmail", 0, 100);
        }
    }

    [DataMember(Name = "tileIcon", IsRequired = true)]
    public Dictionary<int, string> TileIcons
    {
        get
        {
            return tileIcon;
        }
        set
        {
            validator.SetProperty(ref tileIcon, value, "TileIcons", value != null, CommonSR.WTTileIconRequired);
            GenerateIconNameMappings();
        }
    }

    [DataMember(Name = "badgeIcon", EmitDefaultValue = false)]
    public Dictionary<int, string> BadgeIcons
    {
        get
        {
            return badgeIcon;
        }
        set
        {
            badgeIcon = value;
            GenerateIconNameMappings();
        }
    }

    [DataMember(Name = "icons", EmitDefaultValue = false)]
    public Dictionary<string, string> AdditionalIcons
    {
        get
        {
            return additionalIcons;
        }
        set
        {
            additionalIcons = value;
            GenerateIconNameMappings();
        }
    }

    [DataMember(Name = "tileTheme", EmitDefaultValue = false)]
    public WebTileTheme TileTheme
    {
        get
        {
            return tileTheme;
        }
        set
        {
            tileTheme = value;
        }
    }

    [DataMember(Name = "refreshIntervalMinutes", EmitDefaultValue = false)]
    public uint RefreshIntervalMinutes
    {
        get
        {
            return refreshIntervalMinutes;
        }
        set
        {
            validator.SetUintProperty(ref refreshIntervalMinutes, value, "RefreshIntervalMinutes", 15u, 180u);
        }
    }

    public IWebTileResource[] Resources
    {
        get
        {
            return resources;
        }
        set
        {
            resources = value as WebTileResource[];
        }
    }

    [DataMember(Name = "resources", IsRequired = true)]
    public WebTileResource[] PrivateResources
    {
        get
        {
            return resources;
        }
        set
        {
            resources = value;
        }
    }

    [DataMember(Name = "pages", IsRequired = true)]
    public WebTilePage[] Pages
    {
        get
        {
            return pages;
        }
        set
        {
            validator.SetProperty(ref pages, value, "Pages", value.Length <= 7, string.Format(CommonSR.WTTooManyPages, new object[1] { 7 }));
            LayoutIndices.Clear();
            if (pages != null)
            {
                int num = 1;
                WebTilePage[] array = pages;
                foreach (WebTilePage webTilePage in array)
                {
                    if (!LayoutIndices.ContainsKey(webTilePage.LayoutName))
                    {
                        LayoutIndices.Add(webTilePage.LayoutName, num++);
                    }
                }
            }
            validator.CheckProperty("LayoutIndices", LayoutIndices.Count <= 4, CommonSR.WTTooManyLayouts);
        }
    }

    [DataMember(Name = "Notifications")]
    public WebTileNotification[] Notifications
    {
        get
        {
            return notifications;
        }
        set
        {
            notifications = value;
        }
    }

    public bool HasNotifications
    {
        get
        {
            if (notifications != null)
            {
                return notifications.Length != 0;
            }
            return false;
        }
    }

    public bool NotificationEnabled
    {
        get
        {
            if (HasNotifications)
            {
                return userSettings.NotificationEnabled;
            }
            return false;
        }
        set
        {
            if (value && !HasNotifications)
            {
                throw new ArgumentException("value");
            }
            userSettings.NotificationEnabled = value;
        }
    }

    public BandIcon TileBandIcon => tileBandIcon;

    public BandIcon BadgeBandIcon => badgeBandIcon;

    public BandIcon[] AdditionalBandIcons => additionalBandIcons;

    private string UserSettingsFilePath
    {
        get
        {
            CheckStorageProviderAndDataFolderPath();
            return Path.Combine(new string[2] { DataFolderPath, "UserSettings.json" });
        }
    }

    internal WebTile()
    {
        validator = new WebTilePropertyValidator();
        layoutIndices = new Dictionary<string, int>();
        iconFilenames = new List<string>();
        iconIndices = new Dictionary<string, int>();
        userSettings = new WebTileUserSettings
        {
            NotificationEnabled = true
        };
        Version = 1u;
        RefreshIntervalMinutes = 30u;
    }

    private void GenerateIconNameMappings()
    {
        iconFilenames.Clear();
        iconIndices.Clear();
        string item = null;
        if (TileIcons != null)
        {
            item = TileIcons[46];
        }
        iconFilenames.Add(item);
        string item2 = null;
        if (BadgeIcons != null)
        {
            item2 = BadgeIcons[24];
        }
        iconFilenames.Add(item2);
        if (AdditionalIcons == null)
        {
            return;
        }
        foreach (KeyValuePair<string, string> additionalIcon in AdditionalIcons)
        {
            int num = iconFilenames.IndexOf(additionalIcon.Value);
            if (num < 0)
            {
                validator.CheckProperty("AdditionalIcons", iconFilenames.Count - 2 < 8, string.Format(CommonSR.WTMaxIconsExceeded, new object[1] { 8 }));
                iconFilenames.Add(additionalIcon.Value);
                num = iconFilenames.Count() - 1;
            }
            iconIndices[additionalIcon.Key] = num;
        }
    }

    private async Task<BandIcon> GetIconAsync(string iconName, string iconFilename, int pixelSize)
    {
        if (ImageProvider == null)
        {
            throw new InvalidDataException(CommonSR.WTImageProviderNotSet);
        }
        if (PackageFolderPath == null)
        {
            throw new InvalidDataException(CommonSR.WTPackageFolderPathNotSet);
        }
        if (iconFilename == null)
        {
            throw new ArgumentNullException("iconFilename");
        }
        try
        {
            string text = Path.Combine(new string[2] { PackageFolderPath, iconFilename });
            text = text.Replace("/", "\\");
            BandIcon bandIcon = await ImageProvider.GetBandIconFromFileAsync(text);
            if (pixelSize > 0 && (bandIcon.Width != pixelSize || bandIcon.Height != pixelSize))
            {
                throw new InvalidDataException(string.Format(CommonSR.WTInvalidIconDimensions, new object[1] { iconName }));
            }
            return bandIcon;
        }
        catch (BandException innerException)
        {
            throw new InvalidDataException(string.Format(CommonSR.WTInvalidIconFile, new object[1] { iconFilename }), innerException);
        }
    }

    public async Task LoadIconsAsync()
    {
        if (iconFilenames.Count < 2)
        {
            throw new InvalidDataException(CommonSR.WTMissingIconFilenames);
        }
        string text = iconFilenames[0];
        if (text == null)
        {
            throw new InvalidDataException("tileIconFilename");
        }
        WebTile webTile = this;
        _ = webTile.tileBandIcon;
        BandIcon bandIcon = (webTile.tileBandIcon = await GetIconAsync("tileIcon", text, 46));
        string text2 = iconFilenames[1];
        if (text2 != null)
        {
            webTile = this;
            _ = webTile.badgeBandIcon;
            bandIcon = (webTile.badgeBandIcon = await GetIconAsync("badgeIcon", text2, 24));
        }
        additionalBandIcons = null;
        if (iconFilenames.Count > 2)
        {
            additionalBandIcons = new BandIcon[iconFilenames.Count - 2];
            for (int iFilename = 2; iFilename < iconFilenames.Count(); iFilename++)
            {
                BandIcon[] array = additionalBandIcons;
                int num = iFilename - 2;
                _ = array[num];
                bandIcon = (array[num] = await GetIconAsync(iconFilenames[iFilename], iconFilenames[iFilename], 0));
            }
        }
    }

    public Task<TileLayout[]> GetLayoutsAsync(BandClass bandClass)
    {
        return Task.Run(delegate
        {
            TileLayout[] array = new TileLayout[1 + LayoutIndices.Count];
            array[0] = new TileLayout(WebTilePage.GetLayoutBlob("MSBand_ScrollingText", bandClass));
            foreach (KeyValuePair<string, int> layoutIndex in LayoutIndices)
            {
                array[layoutIndex.Value] = new TileLayout(WebTilePage.GetLayoutBlob(layoutIndex.Key, bandClass));
            }
            return array;
        });
    }

    public static string ResolveTextBindingExpression(string input, Dictionary<string, string> mappings)
    {
        return new Regex("\\{\\{([A-Za-z_]\\w*)\\}\\}").Replace(input, (Match match) => (mappings == null || !mappings.ContainsKey(match.Groups[1].Value)) ? "--" : mappings[match.Groups[1].Value]);
    }

    private string[] FindVariableNames(string input)
    {
        if (input == null)
        {
            return new string[0];
        }
        HashSet<string> hashSet = new HashSet<string>();
        foreach (Match item in Regex.Matches(input, "\\{\\{([A-Za-z_]\\w*)\\}\\}"))
        {
            hashSet.Add(item.Groups[1].Value);
        }
        return hashSet.ToArray();
    }

    public List<PageData> Refresh(out bool clearPages, out bool sendAsMessage, out NotificationDialog notificationDialog)
    {
        List<PageData> result = null;
        clearPages = false;
        sendAsMessage = false;
        notificationDialog = null;
        currentUpdateError = false;
        try
        {
            if (Resources.Length < 1)
            {
                throw new InvalidDataException(CommonSR.WTNoResources);
            }
            ReadResourceCache();
            if (Resources[0].Style == ResourceStyle.Feed)
            {
                sendAsMessage = Pages[0].LayoutName == "MSBand_ScrollingText";
                return EnsureNewFeedPageDataList(out notificationDialog);
            }
            if (Resources[0].Style == ResourceStyle.Simple)
            {
                clearPages = lastUpdateError;
                bool result2 = ResolveContentMappingsAsync().Result;
                if (result2 || lastUpdateError)
                {
                    result = GetSimplePageDataList(variableMappings);
                }
                if (result2)
                {
                    if (NotificationEnabled)
                    {
                        notificationDialog = GetNotificationDialog(variableMappings);
                        return result;
                    }
                    return result;
                }
                return result;
            }
            throw new InvalidDataException("Style");
        }
        catch (Exception e)
        {
            Logger.LogException(LogLevel.Error, e, "Error occurred while refreshing Webtile, using error page instead.");
            result = new List<PageData> { CreateErrorPage() };
            currentUpdateError = true;
            clearPages = false;
            return result;
        }
    }

    public NotificationDialog GetNotificationDialog(Dictionary<string, string> variableMappings)
    {
        if (Notifications != null)
        {
            try
            {
                WebTileCondition webTileCondition = new WebTileCondition(variableMappings);
                WebTileNotification[] array = Notifications;
                foreach (WebTileNotification webTileNotification in array)
                {
                    if (webTileCondition.ComputeValue(webTileNotification.Condition))
                    {
                        NotificationDialog notificationDialog = new NotificationDialog();
                        notificationDialog.Title = ResolveTextBindingExpression(webTileNotification.Title, variableMappings);
                        if (webTileNotification.Body != null)
                        {
                            notificationDialog.Body = ResolveTextBindingExpression(webTileNotification.Body, variableMappings);
                        }
                        return notificationDialog;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, $"Exception {ex} processing WebTile notification");
            }
        }
        return null;
    }

    private PageData CreateErrorPage()
    {
        PageData pageData = new PageData(new Guid("A5EECE73496945D1A863CFA76DC485FF"), 0);
        pageData.Values.Add(new TextBlockData(1, "Data fetch error"));
        pageData.Values.Add(new TextBlockData(2, "There seems to be something wrong with the data for this tile...check back in a few."));
        pageData.Values.Add(new TextBlockData(3, $"{DateTimeOffset.Now:MM/dd - hh:mm tt}"));
        return pageData;
    }

    private List<PageData> EnsureNewFeedPageDataList(out NotificationDialog dialog)
    {
        if (Resources.Length != 1)
        {
            throw new InvalidDataException(CommonSR.WTFeedMustHaveExactlyOneResource);
        }
        if (Pages.Length != 1)
        {
            throw new InvalidDataException(CommonSR.WTFeedMustHaveExactlyOnePage);
        }
        dialog = null;
        List<PageData> list = null;
        List<Dictionary<string, string>> result = Resources[0].ResolveFeedContentMappingsAsync().Result;
        EnsureLastSyncInfoLoaded();
        if (!lastSyncInfo.HasSameLastSyncMappings(result))
        {
            list = new List<PageData>(result.Count);
            foreach (Dictionary<string, string> item in Enumerable.Reverse(result))
            {
                list.Add(GetPageData(-1, item));
            }
            if (NotificationEnabled)
            {
                foreach (Dictionary<string, string> item2 in result)
                {
                    dialog = GetNotificationDialog(item2);
                    if (dialog != null)
                    {
                        break;
                    }
                }
            }
            lastSyncInfo.LastSyncMappings = result;
        }
        return list;
    }

    private List<PageData> GetSimplePageDataList(Dictionary<string, string> mappings)
    {
        List<PageData> list = new List<PageData>();
        for (int num = pages.Length - 1; num >= 0; num--)
        {
            list.Add(GetPageData(num, mappings));
        }
        return list;
    }

    private PageData GetPageData(int pageIndex, Dictionary<string, string> mappings)
    {
        Guid pageId;
        if (pageIndex == -1)
        {
            pageIndex = 0;
            pageId = Guid.NewGuid();
        }
        else
        {
            pageId = new Guid(string.Format("{0}0{1}", new object[2] { "A5EECE73496945D1A863CFA76DC485", pageIndex }));
        }
        if (pageIndex >= Pages.Length)
        {
            throw new ArgumentOutOfRangeException("pageIndex");
        }
        WebTilePage webTilePage = Pages[pageIndex];
        int pageLayoutIndex = layoutIndices[webTilePage.LayoutName];
        PageData pageData = new PageData(pageId, pageLayoutIndex);
        if (webTilePage.LayoutName == "MSBand_SingleMetricWithSecondary")
        {
            pageData.Values.Add(new TextBlockData(13, "l"));
        }
        if (webTilePage.TextBindings != null)
        {
            WebTileTextBinding[] textBindings = webTilePage.TextBindings;
            foreach (WebTileTextBinding webTileTextBinding in textBindings)
            {
                int length = ((webTileTextBinding.ElementId != 2 || !(webTilePage.LayoutName == "MSBand_ScrollingText")) ? 20 : 160);
                string s = ResolveTextBindingExpression(webTileTextBinding.TextValue, mappings);
                s = s.TruncateTrimDanglingHighSurrogate(length);
                pageData.Values.Add(new TextBlockData(webTileTextBinding.ElementId, s));
            }
        }
        if (webTilePage.IconBindings != null)
        {
            WebTileIconBinding[] iconBindings = webTilePage.IconBindings;
            foreach (WebTileIconBinding webTileIconBinding in iconBindings)
            {
                string iconName = webTileIconBinding.Conditions[0].IconName;
                int num = IconIndices[iconName];
                pageData.Values.Add(new IconData(webTileIconBinding.ElementId, (ushort)num));
            }
        }
        return pageData;
    }

    public async Task<bool> ResolveContentMappingsAsync()
    {
        if (variableMappings == null)
        {
            variableMappings = new Dictionary<string, string>();
        }
        bool oneOrMoreResourcesChanged = false;
        WebTileResource[] array = resources;
        foreach (WebTileResource iwtr in array)
        {
            object obj = await iwtr.DownloadResourceAsync();
            if (obj != null)
            {
                oneOrMoreResourcesChanged = true;
                if (obj is XmlDocument)
                {
                    iwtr.ResolveContentMappings(variableMappings, (XmlDocument)((obj is XmlDocument) ? obj : null));
                }
                else if (obj is JToken)
                {
                    iwtr.ResolveContentMappings(variableMappings, obj as JToken);
                }
            }
        }
        return oneOrMoreResourcesChanged;
    }

    private void CheckStorageProviderAndDataFolderPath()
    {
        if (StorageProvider == null)
        {
            throw new InvalidOperationException(CommonSR.WTStorageProviderNotSet);
        }
        if (DataFolderPath == null)
        {
            throw new InvalidOperationException(CommonSR.WTDataFolderPathNotSet);
        }
    }

    public bool HasRefreshIntervalElapsed(DateTimeOffset time)
    {
        EnsureLastSyncInfoLoaded();
        return lastSyncInfo.LastSyncTime.AddMinutes(RefreshIntervalMinutes) < time;
    }

    internal void ReadResourceCache()
    {
        CheckStorageProviderAndDataFolderPath();
        string relativePath = Path.Combine(new string[2] { DataFolderPath, "ResourceCache.json" });
        WebTileCacheInfo webTileCacheInfo;
        try
        {
            using Stream inputStream = StorageProvider.OpenFileForRead(StorageProviderRoot.App, relativePath, 4096);
            webTileCacheInfo = CargoClient.DeserializeJson<WebTileCacheInfo>(inputStream);
        }
        catch (Exception ex)
        {
            if (!(ex is FileNotFoundException) && !(ex.InnerException is FileNotFoundException))
            {
                Logger.Log(LogLevel.Info, "Unexpected error opening resource cache file {0}", ex);
                throw;
            }
            webTileCacheInfo = null;
        }
        if (webTileCacheInfo != null)
        {
            IWebTileResource[] array = Resources;
            for (int i = 0; i < array.Length; i++)
            {
                WebTileResource webTileResource = (WebTileResource)array[i];
                webTileResource.CacheInfo = webTileCacheInfo.ResourceCacheInfo[webTileResource.Url];
            }
            variableMappings = webTileCacheInfo.VariableMappings;
            lastUpdateError = webTileCacheInfo.LastUpdateError;
        }
    }

    internal void WriteResourceCache()
    {
        CheckStorageProviderAndDataFolderPath();
        WebTileCacheInfo webTileCacheInfo = new WebTileCacheInfo();
        webTileCacheInfo.ResourceCacheInfo = new Dictionary<string, WebTileResourceCacheInfo>();
        IWebTileResource[] array = Resources;
        for (int i = 0; i < array.Length; i++)
        {
            WebTileResource webTileResource = (WebTileResource)array[i];
            if (webTileResource.CacheInfo != null)
            {
                webTileCacheInfo.ResourceCacheInfo.Add(webTileResource.Url, (WebTileResourceCacheInfo)webTileResource.CacheInfo);
            }
        }
        if (Resources.Count() > 1 && Resources[0].Style == ResourceStyle.Simple)
        {
            webTileCacheInfo.VariableMappings = variableMappings;
        }
        webTileCacheInfo.LastUpdateError = currentUpdateError;
        string relativePath = Path.Combine(new string[2] { DataFolderPath, "ResourceCache.json" });
        using Stream outputStream = StorageProvider.OpenFileForWrite(StorageProviderRoot.App, relativePath, append: false);
        CargoClient.SerializeJson(outputStream, webTileCacheInfo);
    }

    public void SaveLastSync(DateTimeOffset time)
    {
        EnsureLastSyncInfoLoaded();
        lastSyncInfo.LastSyncTime = time;
        string relativePath = Path.Combine(new string[2] { DataFolderPath, "LastSync.json" });
        using (Stream outputStream = StorageProvider.OpenFileForWrite(StorageProviderRoot.App, relativePath, append: false))
        {
            CargoClient.SerializeJson(outputStream, lastSyncInfo);
        }
        WriteResourceCache();
    }

    private void EnsureLastSyncInfoLoaded()
    {
        if (lastSyncInfo != null)
        {
            return;
        }
        try
        {
            CheckStorageProviderAndDataFolderPath();
            string relativePath = Path.Combine(new string[2] { DataFolderPath, "LastSync.json" });
            using Stream inputStream = StorageProvider.OpenFileForRead(StorageProviderRoot.App, relativePath, 4096);
            lastSyncInfo = CargoClient.DeserializeJson<WebTileSyncInfo>(inputStream);
        }
        catch (Exception e)
        {
            Logger.LogException(LogLevel.Info, e, "Unexpected error reading file {0}", "LastSync.json");
            lastSyncInfo = new WebTileSyncInfo();
        }
    }

    public Task SetNotificationEnabledAsync(bool enabled)
    {
        NotificationEnabled = enabled;
        return SaveUserSettingsAsync();
    }

    public Task SaveUserSettingsAsync()
    {
        return Task.Run(delegate
        {
            SaveUserSettings();
        });
    }

    public void SaveUserSettings()
    {
        using Stream outputStream = StorageProvider.OpenFileForWrite(StorageProviderRoot.App, UserSettingsFilePath, append: false);
        CargoClient.SerializeJson(outputStream, userSettings);
    }

    public void LoadUserSettings()
    {
        if (StorageProvider.FileExists(StorageProviderRoot.App, UserSettingsFilePath))
        {
            using (Stream inputStream = StorageProvider.OpenFileForRead(StorageProviderRoot.App, UserSettingsFilePath, 4096))
            {
                userSettings = CargoClient.DeserializeJson<WebTileUserSettings>(inputStream);
                return;
            }
        }
        userSettings.NotificationEnabled = true;
    }

    public void Validate()
    {
        if (Resources.Length < 1)
        {
            throw new InvalidDataException(CommonSR.WTNoResources);
        }
        if (Resources[0].Style == ResourceStyle.Feed && Resources.Length != 1)
        {
            throw new InvalidDataException(CommonSR.WTFeedMustHaveExactlyOneResource);
        }
        ValidateTextBindings();
        ValidateIconBindings();
        ValidateAllPagesElementIds();
    }

    internal void ValidateTextBindings()
    {
        HashSet<string> textReferencedVariableNames = GetTextReferencedVariableNames();
        HashSet<string> textDefinedVariableNames = GetTextDefinedVariableNames();
        HashSet<string> notificationsReferencedVariableNames = GetNotificationsReferencedVariableNames();
        foreach (string item in textReferencedVariableNames)
        {
            if (!textDefinedVariableNames.Contains(item))
            {
                throw new InvalidDataException(string.Format(CommonSR.WTUndefinedVariableReferencedInTextBindings, new object[1] { item }));
            }
        }
        foreach (string item2 in notificationsReferencedVariableNames)
        {
            if (!textDefinedVariableNames.Contains(item2))
            {
                throw new InvalidDataException(string.Format(CommonSR.WTUndefinedVariableReferencedInNotifications, new object[1] { item2 }));
            }
        }
        foreach (string item3 in textDefinedVariableNames.Except(textReferencedVariableNames).Except(notificationsReferencedVariableNames))
        {
            Logger.Log(LogLevel.Warning, $"Resource variable {item3} not used");
        }
    }

    private HashSet<string> GetTextDefinedVariableNames()
    {
        HashSet<string> hashSet = new HashSet<string>();
        if (PrivateResources != null)
        {
            WebTileResource[] privateResources = PrivateResources;
            foreach (WebTileResource webTileResource in privateResources)
            {
                if (webTileResource == null || webTileResource.Content == null)
                {
                    continue;
                }
                foreach (KeyValuePair<string, string> item in webTileResource.Content)
                {
                    string key = item.Key;
                    if (hashSet.Contains(key))
                    {
                        throw new InvalidDataException(string.Format(CommonSR.WTVariableNameNotUnique, new object[1] { key }));
                    }
                    hashSet.Add(key);
                }
            }
        }
        return hashSet;
    }

    private HashSet<string> GetTextReferencedVariableNames()
    {
        HashSet<string> hashSet = new HashSet<string>();
        if (pages != null)
        {
            WebTilePage[] array = pages;
            for (int i = 0; i < array.Length; i++)
            {
                WebTileTextBinding[] textBindings = array[i].TextBindings;
                if (textBindings == null)
                {
                    continue;
                }
                WebTileTextBinding[] array2 = textBindings;
                foreach (WebTileTextBinding webTileTextBinding in array2)
                {
                    if (webTileTextBinding != null && webTileTextBinding.TextValue != null)
                    {
                        string[] array3 = FindVariableNames(webTileTextBinding.TextValue);
                        foreach (string item in array3)
                        {
                            hashSet.Add(item);
                        }
                    }
                }
            }
        }
        return hashSet;
    }

    internal void ValidateIconBindings()
    {
        HashSet<string> referencedIconNames = GetReferencedIconNames();
        HashSet<string> definedIconNames = GetDefinedIconNames();
        foreach (string item in referencedIconNames)
        {
            if (!definedIconNames.Contains(item))
            {
                throw new InvalidDataException(string.Format(CommonSR.WTIconNotDefined, new object[1] { item }));
            }
        }
    }

    private HashSet<string> GetDefinedIconNames()
    {
        HashSet<string> hashSet = new HashSet<string>();
        if (additionalIcons != null)
        {
            foreach (KeyValuePair<string, string> additionalIcon in additionalIcons)
            {
                hashSet.Add(additionalIcon.Key);
            }
            return hashSet;
        }
        return hashSet;
    }

    private HashSet<string> GetReferencedIconNames()
    {
        HashSet<string> hashSet = new HashSet<string>();
        if (pages != null)
        {
            WebTilePage[] array = pages;
            for (int i = 0; i < array.Length; i++)
            {
                WebTileIconBinding[] iconBindings = array[i].IconBindings;
                if (iconBindings == null)
                {
                    continue;
                }
                WebTileIconBinding[] array2 = iconBindings;
                foreach (WebTileIconBinding webTileIconBinding in array2)
                {
                    if (webTileIconBinding != null && webTileIconBinding.Conditions != null)
                    {
                        WebTileIconCondition[] conditions = webTileIconBinding.Conditions;
                        foreach (WebTileIconCondition webTileIconCondition in conditions)
                        {
                            hashSet.Add(webTileIconCondition.IconName);
                        }
                    }
                }
            }
        }
        return hashSet;
    }

    private HashSet<string> GetNotificationsReferencedVariableNames()
    {
        HashSet<string> hashSet = new HashSet<string>();
        if (notifications != null)
        {
            WebTileNotification[] array = notifications;
            foreach (WebTileNotification webTileNotification in array)
            {
                if (webTileNotification.Condition != null)
                {
                    string[] array2 = FindVariableNames(webTileNotification.Condition);
                    foreach (string item in array2)
                    {
                        hashSet.Add(item);
                    }
                }
            }
        }
        return hashSet;
    }

    private static PageLayout DeserializeLayout(string layoutName)
    {
        PageLayout result = null;
        byte[] layoutBlob = WebTilePage.GetLayoutBlob(layoutName, BandClass.Unknown);
        if (layoutBlob != null)
        {
            using (MemoryStream input = new MemoryStream(layoutBlob))
            {
                using CargoReaderOnBinaryReader reader = new CargoReaderOnBinaryReader(new BinaryReader(input));
                return PageLayout.DeserializeFromBand(reader);
            }
        }
        return result;
    }

    internal void ValidateAllPagesElementIds()
    {
        if (pages == null)
        {
            return;
        }
        WebTilePage[] array = pages;
        foreach (WebTilePage webTilePage in array)
        {
            PageLayout pageLayout = DeserializeLayout(webTilePage.LayoutName);
            if (pageLayout != null)
            {
                ValidateTextBindingElementIds(webTilePage, pageLayout);
                ValidateIconBindingElementIds(webTilePage, pageLayout);
            }
        }
    }

    private void ValidateTextBindingElementIds(WebTilePage page, PageLayout pageLayout)
    {
        HashSet<short> hashSet = new HashSet<short>();
        if (page.TextBindings == null)
        {
            return;
        }
        WebTileTextBinding[] textBindings = page.TextBindings;
        foreach (WebTileTextBinding webTileTextBinding in textBindings)
        {
            webTileTextBinding.Validator.ClearPropertyError("ElementId");
            if (hashSet.Contains(webTileTextBinding.ElementId))
            {
                page.Validator.CheckProperty("TextBindings", valid: false, string.Format(CommonSR.WTMultipleTextBindingsWithElementId, new object[1] { webTileTextBinding.ElementId }));
            }
            else
            {
                hashSet.Add(webTileTextBinding.ElementId);
            }
            PageElement pageElement = pageLayout.Root.FindElement(webTileTextBinding.ElementId);
            if (pageElement == null)
            {
                webTileTextBinding.Validator.CheckProperty("ElementId", valid: false, string.Format(CommonSR.WTElementIDNotValidForLayout, new object[2] { webTileTextBinding.ElementId, page.LayoutName }));
            }
            else if (!(pageElement is TextBlock) && !(pageElement is WrappedTextBlock) && !(pageElement is Barcode))
            {
                webTileTextBinding.Validator.CheckProperty("ElementId", valid: false, string.Format(CommonSR.WTElementIDDoesNotSupportText, new object[2]
                {
                    webTileTextBinding.ElementId,
                    pageElement.ToString()
                }));
            }
        }
    }

    private void ValidateIconBindingElementIds(WebTilePage page, PageLayout pageLayout)
    {
        if (page.IconBindings == null)
        {
            return;
        }
        WebTileIconBinding[] iconBindings = page.IconBindings;
        foreach (WebTileIconBinding webTileIconBinding in iconBindings)
        {
            PageElement pageElement = pageLayout.Root.FindElement(webTileIconBinding.ElementId);
            if (pageElement == null)
            {
                webTileIconBinding.Validator.CheckProperty("ElementId", valid: false, string.Format(CommonSR.WTElementIDNotValidForLayout, new object[2] { webTileIconBinding.ElementId, page.LayoutName }));
            }
            else if (!(pageElement is Icon))
            {
                webTileIconBinding.Validator.CheckProperty("ElementId", valid: false, string.Format(CommonSR.WTElementIDIsNotAnIconInLayout, new object[2] { webTileIconBinding.ElementId, page.LayoutName }));
            }
        }
    }

    public Task SetAuthenticationHeaderAsync(IWebTileResource resource, string userName, string password)
    {
        return Task.Run(delegate
        {
            SetAuthenticationHeader(resource, userName, password);
        });
    }

    public void SetAuthenticationHeader(IWebTileResource resource, string userName, string password)
    {
        if (resource == null)
        {
            throw new ArgumentNullException("resource");
        }
        resource.Username = userName;
        resource.Password = password;
    }

    public async Task<bool> AuthenticateResourceAsync(IWebTileResource resource)
    {
        if (resource == null)
        {
            throw new ArgumentNullException("resource");
        }
        return await resource.AuthenticateAsync();
    }

    public void SaveResourceAuthentication()
    {
        //IL_0000: Unknown result type (might be due to invalid IL or missing references)
        //IL_0006: Expected O, but got Unknown
        //IL_005e: Unknown result type (might be due to invalid IL or missing references)
        //IL_0068: Expected O, but got Unknown
        PasswordVault val = new PasswordVault();
        if (PrivateResources == null)
        {
            return;
        }
        WebTileResource[] privateResources = PrivateResources;
        foreach (WebTileResource webTileResource in privateResources)
        {
            if (webTileResource.Username != null && webTileResource.Password != null)
            {
                string text = webTileResource.Url + TileId.ToString();
                val.Add(new PasswordCredential(text, webTileResource.Username, webTileResource.Password));
            }
            else
            {
                RemoveResourceCredentials(val, webTileResource);
            }
        }
    }

    public void LoadResourceAuthentication()
    {
        //IL_0000: Unknown result type (might be due to invalid IL or missing references)
        //IL_0006: Expected O, but got Unknown
        PasswordVault val = new PasswordVault();
        if (PrivateResources == null)
        {
            return;
        }
        WebTileResource[] privateResources = PrivateResources;
        foreach (WebTileResource webTileResource in privateResources)
        {
            string text = webTileResource.Url + TileId.ToString();
            IReadOnlyList<PasswordCredential> resourceCredentialsList = GetResourceCredentialsList(val, webTileResource);
            if (resourceCredentialsList != null && resourceCredentialsList.Count > 0)
            {
                PasswordCredential val2 = val.Retrieve(text, resourceCredentialsList[0].UserName);
                webTileResource.Username = val2.UserName;
                webTileResource.Password = val2.Password;
            }
            else
            {
                webTileResource.Username = null;
                webTileResource.Password = null;
            }
        }
    }

    public void DeleteStoredResourceCredentials()
    {
        //IL_0000: Unknown result type (might be due to invalid IL or missing references)
        //IL_0006: Expected O, but got Unknown
        PasswordVault vault = new PasswordVault();
        if (PrivateResources != null)
        {
            WebTileResource[] privateResources = PrivateResources;
            foreach (WebTileResource resource in privateResources)
            {
                RemoveResourceCredentials(vault, resource);
            }
        }
    }

    private void RemoveResourceCredentials(PasswordVault vault, IWebTileResource resource)
    {
        IReadOnlyList<PasswordCredential> resourceCredentialsList = GetResourceCredentialsList(vault, resource);
        if (resourceCredentialsList == null)
        {
            return;
        }
        foreach (PasswordCredential item in resourceCredentialsList)
        {
            vault.Remove(item);
        }
    }

    private IReadOnlyList<PasswordCredential> GetResourceCredentialsList(PasswordVault vault, IWebTileResource resource)
    {
        string text = resource.Url + TileId.ToString();
        try
        {
            return vault.FindAllByResource(text);
        }
        catch
        {
            return null;
        }
    }
}
