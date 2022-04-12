// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.WebTiles.WebTile
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

using Microsoft.Band.Tiles;
using Microsoft.Band.Tiles.Pages;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.Security.Credentials;

namespace Microsoft.Band.Admin.WebTiles
{
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
    public const uint MinManifestVersion = 1;
    public const uint MaxManifestVersion = 1;
    public const uint MinVersion = 1;
    public const int MinNameLength = 1;
    public const int MaxNameLength = 21;
    public const int MaxDescriptionLength = 100;
    public const int MaxVersionStringLength = 10;
    public const int MaxAuthorLength = 50;
    public const int MaxOrganizationLength = 100;
    public const int MaxContactEmailLength = 100;
    public const uint MinRefreshIntervalMinutes = 15;
    public const uint MaxRefreshIntervalMinutes = 180;
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
      get => this.validator.AllowInvalidValues;
      set => this.validator.AllowInvalidValues = value;
    }

    public Dictionary<string, string> PropertyErrors => this.validator.PropertyErrors;

    internal WebTile()
    {
      this.validator = new WebTilePropertyValidator();
      this.layoutIndices = new Dictionary<string, int>();
      this.iconFilenames = new List<string>();
      this.iconIndices = new Dictionary<string, int>();
      this.userSettings = new WebTileUserSettings()
      {
        NotificationEnabled = true
      };
      this.Version = 1U;
      this.RefreshIntervalMinutes = 30U;
    }

    public Guid TileId { get; set; }

    public IImageProvider ImageProvider { get; set; }

    public string PackageFolderPath { get; set; }

    public string DataFolderPath { get; set; }

    public IStorageProvider StorageProvider { get; set; }

    public HeaderNameValuePair[] RequestHeaders
    {
      get => this.requestHeaders;
      set
      {
        this.requestHeaders = value;
        if (this.Resources == null)
          return;
        foreach (IWebTileResource resource in this.Resources)
          resource.RequestHeaders = this.requestHeaders;
      }
    }

    public Dictionary<string, int> LayoutIndices => this.layoutIndices;

    public Dictionary<string, int> IconIndices => this.iconIndices;

    [DataMember(IsRequired = true, Name = "name")]
    public string Name
    {
      get => this.name;
      set
      {
        this.validator.CheckProperty(nameof (Name), value != null, CommonSR.WTNameCannotBeNull);
        this.validator.SetStringProperty(ref this.name, value, nameof (Name), 1, 21);
      }
    }

    [DataMember(EmitDefaultValue = false, Name = "description")]
    public string Description
    {
      get => this.description;
      set => this.validator.SetStringProperty(ref this.description, value, nameof (Description), 0, 100);
    }

    [DataMember(IsRequired = true, Name = "manifestVersion")]
    public uint ManifestVersion
    {
      get => this.manifestVersion;
      set => this.validator.SetUintProperty(ref this.manifestVersion, value, nameof (ManifestVersion), 1U, 1U);
    }

    [DataMember(EmitDefaultValue = true, Name = "version")]
    public uint Version
    {
      get => this.version;
      set => this.validator.SetProperty<uint>(ref this.version, value, nameof (Version), (1U <= value ? 1 : 0) != 0, string.Format(CommonSR.WTVersionTooSmall, new object[1]
      {
        (object) 1U
      }));
    }

    [DataMember(EmitDefaultValue = false, Name = "versionString")]
    public string VersionString
    {
      get => this.versionString == null ? this.version.ToString() : this.versionString;
      set => this.validator.SetStringProperty(ref this.versionString, value, nameof (VersionString), 0, 10);
    }

    [DataMember(EmitDefaultValue = false, Name = "author")]
    public string Author
    {
      get => this.author;
      set => this.validator.SetStringProperty(ref this.author, value, nameof (Author), 0, 50);
    }

    [DataMember(EmitDefaultValue = false, Name = "organization")]
    public string Organization
    {
      get => this.organization;
      set => this.validator.SetStringProperty(ref this.organization, value, nameof (Organization), 0, 100);
    }

    [DataMember(EmitDefaultValue = false, Name = "contactEmail")]
    public string ContactEmail
    {
      get => this.contactEmail;
      set => this.validator.SetStringProperty(ref this.contactEmail, value, nameof (ContactEmail), 0, 100);
    }

    private void GenerateIconNameMappings()
    {
      this.iconFilenames.Clear();
      this.iconIndices.Clear();
      string str1 = (string) null;
      if (this.TileIcons != null)
        str1 = this.TileIcons[46];
      this.iconFilenames.Add(str1);
      string str2 = (string) null;
      if (this.BadgeIcons != null)
        str2 = this.BadgeIcons[24];
      this.iconFilenames.Add(str2);
      if (this.AdditionalIcons == null)
        return;
      foreach (KeyValuePair<string, string> additionalIcon in this.AdditionalIcons)
      {
        int num = this.iconFilenames.IndexOf(additionalIcon.Value);
        if (num < 0)
        {
          this.validator.CheckProperty("AdditionalIcons", (this.iconFilenames.Count - 2 < 8 ? 1 : 0) != 0, string.Format(CommonSR.WTMaxIconsExceeded, new object[1]
          {
            (object) 8
          }));
          this.iconFilenames.Add(additionalIcon.Value);
          num = this.iconFilenames.Count<string>() - 1;
        }
        this.iconIndices[additionalIcon.Key] = num;
      }
    }

    [DataMember(IsRequired = true, Name = "tileIcon")]
    public Dictionary<int, string> TileIcons
    {
      get => this.tileIcon;
      set
      {
        this.validator.SetProperty<Dictionary<int, string>>(ref this.tileIcon, value, nameof (TileIcons), value != null, CommonSR.WTTileIconRequired);
        this.GenerateIconNameMappings();
      }
    }

    [DataMember(EmitDefaultValue = false, Name = "badgeIcon")]
    public Dictionary<int, string> BadgeIcons
    {
      get => this.badgeIcon;
      set
      {
        this.badgeIcon = value;
        this.GenerateIconNameMappings();
      }
    }

    [DataMember(EmitDefaultValue = false, Name = "icons")]
    public Dictionary<string, string> AdditionalIcons
    {
      get => this.additionalIcons;
      set
      {
        this.additionalIcons = value;
        this.GenerateIconNameMappings();
      }
    }

    [DataMember(EmitDefaultValue = false, Name = "tileTheme")]
    public WebTileTheme TileTheme
    {
      get => this.tileTheme;
      set => this.tileTheme = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "refreshIntervalMinutes")]
    public uint RefreshIntervalMinutes
    {
      get => this.refreshIntervalMinutes;
      set => this.validator.SetUintProperty(ref this.refreshIntervalMinutes, value, nameof (RefreshIntervalMinutes), 15U, 180U);
    }

    public IWebTileResource[] Resources
    {
      get => (IWebTileResource[]) this.resources;
      set => this.resources = value as WebTileResource[];
    }

    [DataMember(IsRequired = true, Name = "resources")]
    public WebTileResource[] PrivateResources
    {
      get => this.resources;
      set => this.resources = value;
    }

    [DataMember(IsRequired = true, Name = "pages")]
    public WebTilePage[] Pages
    {
      get => this.pages;
      set
      {
        this.validator.SetProperty<WebTilePage[]>(ref this.pages, value, nameof (Pages), (value.Length <= 7 ? 1 : 0) != 0, string.Format(CommonSR.WTTooManyPages, new object[1]
        {
          (object) 7
        }));
        this.LayoutIndices.Clear();
        if (this.pages != null)
        {
          int num = 1;
          foreach (WebTilePage page in this.pages)
          {
            if (!this.LayoutIndices.ContainsKey(page.LayoutName))
              this.LayoutIndices.Add(page.LayoutName, num++);
          }
        }
        this.validator.CheckProperty("LayoutIndices", this.LayoutIndices.Count <= 4, CommonSR.WTTooManyLayouts);
      }
    }

    [DataMember(Name = "Notifications")]
    public WebTileNotification[] Notifications
    {
      get => this.notifications;
      set => this.notifications = value;
    }

    public bool HasNotifications => this.notifications != null && (uint) this.notifications.Length > 0U;

    public bool NotificationEnabled
    {
      get => this.HasNotifications && this.userSettings.NotificationEnabled;
      set => this.userSettings.NotificationEnabled = !value || this.HasNotifications ? value : throw new ArgumentException(nameof (value));
    }

    public BandIcon TileBandIcon => this.tileBandIcon;

    public BandIcon BadgeBandIcon => this.badgeBandIcon;

    public BandIcon[] AdditionalBandIcons => this.additionalBandIcons;

    private async Task<BandIcon> GetIconAsync(
      string iconName,
      string iconFilename,
      int pixelSize)
    {
      if (this.ImageProvider == null)
        throw new InvalidDataException(CommonSR.WTImageProviderNotSet);
      if (this.PackageFolderPath == null)
        throw new InvalidDataException(CommonSR.WTPackageFolderPathNotSet);
      if (iconFilename == null)
        throw new ArgumentNullException(nameof (iconFilename));
      BandIcon iconAsync;
      try
      {
        BandIcon iconFromFileAsync = await this.ImageProvider.GetBandIconFromFileAsync(Path.Combine(new string[2]
        {
          this.PackageFolderPath,
          iconFilename
        }).Replace("/", "\\"));
        if (pixelSize > 0 && (iconFromFileAsync.Width != pixelSize || iconFromFileAsync.Height != pixelSize))
          throw new InvalidDataException(string.Format(CommonSR.WTInvalidIconDimensions, new object[1]
          {
            (object) iconName
          }));
        iconAsync = iconFromFileAsync;
      }
      catch (BandException ex)
      {
        throw new InvalidDataException(string.Format(CommonSR.WTInvalidIconFile, new object[1]
        {
          (object) iconFilename
        }), (Exception) ex);
      }
      return iconAsync;
    }

    public async Task LoadIconsAsync()
    {
      string iconFilename1 = this.iconFilenames.Count >= 2 ? this.iconFilenames[0] : throw new InvalidDataException(CommonSR.WTMissingIconFilenames);
      if (iconFilename1 == null)
        throw new InvalidDataException("tileIconFilename");
      WebTile webTile = this;
      BandIcon tileBandIcon = webTile.tileBandIcon;
      BandIcon iconAsync1 = await this.GetIconAsync("tileIcon", iconFilename1, 46);
      webTile.tileBandIcon = iconAsync1;
      webTile = (WebTile) null;
      string iconFilename2 = this.iconFilenames[1];
      if (iconFilename2 != null)
      {
        webTile = this;
        BandIcon badgeBandIcon = webTile.badgeBandIcon;
        BandIcon iconAsync2 = await this.GetIconAsync("badgeIcon", iconFilename2, 24);
        webTile.badgeBandIcon = iconAsync2;
        webTile = (WebTile) null;
      }
      this.additionalBandIcons = (BandIcon[]) null;
      if (this.iconFilenames.Count <= 2)
        return;
      this.additionalBandIcons = new BandIcon[this.iconFilenames.Count - 2];
      for (int iFilename = 2; iFilename < this.iconFilenames.Count<string>(); ++iFilename)
      {
        BandIcon[] bandIconArray = this.additionalBandIcons;
        int index = iFilename - 2;
        BandIcon bandIcon = bandIconArray[index];
        BandIcon iconAsync3 = await this.GetIconAsync(this.iconFilenames[iFilename], this.iconFilenames[iFilename], 0);
        bandIconArray[index] = iconAsync3;
        bandIconArray = (BandIcon[]) null;
      }
    }

    public Task<TileLayout[]> GetLayoutsAsync(BandClass bandClass) => Task.Run<TileLayout[]>((Func<TileLayout[]>) (() =>
    {
      TileLayout[] layoutsAsync = new TileLayout[1 + this.LayoutIndices.Count];
      layoutsAsync[0] = new TileLayout(WebTilePage.GetLayoutBlob("MSBand_ScrollingText", bandClass));
      foreach (KeyValuePair<string, int> layoutIndex in this.LayoutIndices)
        layoutsAsync[layoutIndex.Value] = new TileLayout(WebTilePage.GetLayoutBlob(layoutIndex.Key, bandClass));
      return layoutsAsync;
    }));

    public static string ResolveTextBindingExpression(
      string input,
      Dictionary<string, string> mappings)
    {
      return new Regex("\\{\\{([A-Za-z_]\\w*)\\}\\}").Replace(input, (MatchEvaluator) (match => mappings == null || !mappings.ContainsKey(match.Groups[1].Value) ? "--" : mappings[match.Groups[1].Value]));
    }

    private string[] FindVariableNames(string input)
    {
      if (input == null)
        return new string[0];
      HashSet<string> source = new HashSet<string>();
      foreach (Match match in Regex.Matches(input, "\\{\\{([A-Za-z_]\\w*)\\}\\}"))
        source.Add(match.Groups[1].Value);
      return source.ToArray<string>();
    }

    public List<PageData> Refresh(
      out bool clearPages,
      out bool sendAsMessage,
      out NotificationDialog notificationDialog)
    {
      List<PageData> pageDataList = (List<PageData>) null;
      clearPages = false;
      sendAsMessage = false;
      notificationDialog = (NotificationDialog) null;
      this.currentUpdateError = false;
      try
      {
        if (this.Resources.Length < 1)
          throw new InvalidDataException(CommonSR.WTNoResources);
        this.ReadResourceCache();
        if (this.Resources[0].Style == ResourceStyle.Feed)
        {
          sendAsMessage = this.Pages[0].LayoutName == "MSBand_ScrollingText";
          pageDataList = this.EnsureNewFeedPageDataList(out notificationDialog);
        }
        else
        {
          if (this.Resources[0].Style != ResourceStyle.Simple)
            throw new InvalidDataException("Style");
          clearPages = this.lastUpdateError;
          int num = this.ResolveContentMappingsAsync().Result ? 1 : 0;
          if (num != 0 || this.lastUpdateError)
            pageDataList = this.GetSimplePageDataList(this.variableMappings);
          if (num != 0)
          {
            if (this.NotificationEnabled)
              notificationDialog = this.GetNotificationDialog(this.variableMappings);
          }
        }
      }
      catch (Exception ex)
      {
        Logger.LogException(LogLevel.Error, ex, "Error occurred while refreshing Webtile, using error page instead.");
        pageDataList = new List<PageData>()
        {
          this.CreateErrorPage()
        };
        this.currentUpdateError = true;
        clearPages = false;
      }
      return pageDataList;
    }

    public NotificationDialog GetNotificationDialog(
      Dictionary<string, string> variableMappings)
    {
      if (this.Notifications != null)
      {
        try
        {
          WebTileCondition webTileCondition = new WebTileCondition(variableMappings);
          foreach (WebTileNotification notification in this.Notifications)
          {
            if (webTileCondition.ComputeValue(notification.Condition))
            {
              NotificationDialog notificationDialog = new NotificationDialog();
              notificationDialog.Title = WebTile.ResolveTextBindingExpression(notification.Title, variableMappings);
              if (notification.Body != null)
                notificationDialog.Body = WebTile.ResolveTextBindingExpression(notification.Body, variableMappings);
              return notificationDialog;
            }
          }
        }
        catch (Exception ex)
        {
          Logger.Log(LogLevel.Error, string.Format("Exception {0} processing WebTile notification", new object[1]
          {
            (object) ex
          }));
        }
      }
      return (NotificationDialog) null;
    }

    private PageData CreateErrorPage()
    {
      PageData errorPage = new PageData(new Guid("A5EECE73496945D1A863CFA76DC485FF"), 0, new PageElementData[0]);
      errorPage.Values.Add((PageElementData) new TextBlockData((short) 1, "Data fetch error"));
      errorPage.Values.Add((PageElementData) new TextBlockData((short) 2, "There seems to be something wrong with the data for this tile...check back in a few."));
      errorPage.Values.Add((PageElementData) new TextBlockData((short) 3, string.Format("{0:MM/dd - hh:mm tt}", new object[1]
      {
        (object) DateTimeOffset.Now
      })));
      return errorPage;
    }

    private List<PageData> EnsureNewFeedPageDataList(out NotificationDialog dialog)
    {
      if (this.Resources.Length != 1)
        throw new InvalidDataException(CommonSR.WTFeedMustHaveExactlyOneResource);
      if (this.Pages.Length != 1)
        throw new InvalidDataException(CommonSR.WTFeedMustHaveExactlyOnePage);
      dialog = (NotificationDialog) null;
      List<PageData> pageDataList = (List<PageData>) null;
      List<Dictionary<string, string>> result = this.Resources[0].ResolveFeedContentMappingsAsync().Result;
      this.EnsureLastSyncInfoLoaded();
      if (!this.lastSyncInfo.HasSameLastSyncMappings(result))
      {
        pageDataList = new List<PageData>(result.Count);
        foreach (Dictionary<string, string> mappings in result.Reverse<Dictionary<string, string>>())
          pageDataList.Add(this.GetPageData(-1, mappings));
        if (this.NotificationEnabled)
        {
          foreach (Dictionary<string, string> variableMappings in result)
          {
            dialog = this.GetNotificationDialog(variableMappings);
            if (dialog != null)
              break;
          }
        }
        this.lastSyncInfo.LastSyncMappings = result;
      }
      return pageDataList;
    }

    private List<PageData> GetSimplePageDataList(Dictionary<string, string> mappings)
    {
      List<PageData> simplePageDataList = new List<PageData>();
      for (int pageIndex = this.pages.Length - 1; pageIndex >= 0; --pageIndex)
        simplePageDataList.Add(this.GetPageData(pageIndex, mappings));
      return simplePageDataList;
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
        pageId = new Guid(string.Format("{0}0{1}", new object[2]
        {
          (object) "A5EECE73496945D1A863CFA76DC485",
          (object) pageIndex
        }));
      if (pageIndex >= this.Pages.Length)
        throw new ArgumentOutOfRangeException(nameof (pageIndex));
      WebTilePage page = this.Pages[pageIndex];
      int layoutIndex = this.layoutIndices[page.LayoutName];
      PageData pageData = new PageData(pageId, layoutIndex, new PageElementData[0]);
      if (page.LayoutName == "MSBand_SingleMetricWithSecondary")
        pageData.Values.Add((PageElementData) new TextBlockData((short) 13, "l"));
      if (page.TextBindings != null)
      {
        foreach (WebTileTextBinding textBinding in page.TextBindings)
        {
          int length = textBinding.ElementId != (short) 2 || !(page.LayoutName == "MSBand_ScrollingText") ? 20 : 160;
          string text = WebTile.ResolveTextBindingExpression(textBinding.TextValue, mappings).TruncateTrimDanglingHighSurrogate(length);
          pageData.Values.Add((PageElementData) new TextBlockData(textBinding.ElementId, text));
        }
      }
      if (page.IconBindings != null)
      {
        foreach (WebTileIconBinding iconBinding in page.IconBindings)
        {
          int iconIndex = this.IconIndices[iconBinding.Conditions[0].IconName];
          pageData.Values.Add((PageElementData) new IconData(iconBinding.ElementId, (ushort) iconIndex));
        }
      }
      return pageData;
    }

    public async Task<bool> ResolveContentMappingsAsync()
    {
      if (this.variableMappings == null)
        this.variableMappings = new Dictionary<string, string>();
      bool oneOrMoreResourcesChanged = false;
      WebTileResource[] webTileResourceArray = this.resources;
      for (int index = 0; index < webTileResourceArray.Length; ++index)
      {
        WebTileResource iwtr = webTileResourceArray[index];
        object obj = await iwtr.DownloadResourceAsync();
        if (obj != null)
        {
          oneOrMoreResourcesChanged = true;
          if (obj is XmlDocument)
            iwtr.ResolveContentMappings(this.variableMappings, obj as XmlDocument);
          else if (obj is JToken)
            iwtr.ResolveContentMappings(this.variableMappings, obj as JToken);
        }
        iwtr = (WebTileResource) null;
      }
      webTileResourceArray = (WebTileResource[]) null;
      return oneOrMoreResourcesChanged;
    }

    private void CheckStorageProviderAndDataFolderPath()
    {
      if (this.StorageProvider == null)
        throw new InvalidOperationException(CommonSR.WTStorageProviderNotSet);
      if (this.DataFolderPath == null)
        throw new InvalidOperationException(CommonSR.WTDataFolderPathNotSet);
    }

    public bool HasRefreshIntervalElapsed(DateTimeOffset time)
    {
      this.EnsureLastSyncInfoLoaded();
      return this.lastSyncInfo.LastSyncTime.AddMinutes((double) this.RefreshIntervalMinutes) < time;
    }

    internal void ReadResourceCache()
    {
      this.CheckStorageProviderAndDataFolderPath();
      string relativePath = Path.Combine(new string[2]
      {
        this.DataFolderPath,
        "ResourceCache.json"
      });
      WebTileCacheInfo webTileCacheInfo;
      try
      {
        using (Stream inputStream = this.StorageProvider.OpenFileForRead(StorageProviderRoot.App, relativePath, 4096))
          webTileCacheInfo = CargoClient.DeserializeJson<WebTileCacheInfo>(inputStream);
      }
      catch (Exception ex)
      {
        if (ex is FileNotFoundException || ex.InnerException is FileNotFoundException)
        {
          webTileCacheInfo = (WebTileCacheInfo) null;
        }
        else
        {
          Logger.Log(LogLevel.Info, "Unexpected error opening resource cache file {0}", (object) ex);
          throw;
        }
      }
      if (webTileCacheInfo == null)
        return;
      foreach (WebTileResource resource in this.Resources)
        resource.CacheInfo = (IWebTileResourceCacheInfo) webTileCacheInfo.ResourceCacheInfo[resource.Url];
      this.variableMappings = webTileCacheInfo.VariableMappings;
      this.lastUpdateError = webTileCacheInfo.LastUpdateError;
    }

    internal void WriteResourceCache()
    {
      this.CheckStorageProviderAndDataFolderPath();
      WebTileCacheInfo webTileCacheInfo = new WebTileCacheInfo();
      webTileCacheInfo.ResourceCacheInfo = new Dictionary<string, WebTileResourceCacheInfo>();
      foreach (WebTileResource resource in this.Resources)
      {
        if (resource.CacheInfo != null)
          webTileCacheInfo.ResourceCacheInfo.Add(resource.Url, (WebTileResourceCacheInfo) resource.CacheInfo);
      }
      if (((IEnumerable<IWebTileResource>) this.Resources).Count<IWebTileResource>() > 1 && this.Resources[0].Style == ResourceStyle.Simple)
        webTileCacheInfo.VariableMappings = this.variableMappings;
      webTileCacheInfo.LastUpdateError = this.currentUpdateError;
      using (Stream outputStream = this.StorageProvider.OpenFileForWrite(StorageProviderRoot.App, Path.Combine(new string[2]
      {
        this.DataFolderPath,
        "ResourceCache.json"
      }), false))
        CargoClient.SerializeJson(outputStream, (object) webTileCacheInfo);
    }

    public void SaveLastSync(DateTimeOffset time)
    {
      this.EnsureLastSyncInfoLoaded();
      this.lastSyncInfo.LastSyncTime = time;
      using (Stream outputStream = this.StorageProvider.OpenFileForWrite(StorageProviderRoot.App, Path.Combine(new string[2]
      {
        this.DataFolderPath,
        "LastSync.json"
      }), false))
        CargoClient.SerializeJson(outputStream, (object) this.lastSyncInfo);
      this.WriteResourceCache();
    }

    private void EnsureLastSyncInfoLoaded()
    {
      if (this.lastSyncInfo != null)
        return;
      try
      {
        this.CheckStorageProviderAndDataFolderPath();
        using (Stream inputStream = this.StorageProvider.OpenFileForRead(StorageProviderRoot.App, Path.Combine(new string[2]
        {
          this.DataFolderPath,
          "LastSync.json"
        }), 4096))
          this.lastSyncInfo = CargoClient.DeserializeJson<WebTileSyncInfo>(inputStream);
      }
      catch (Exception ex)
      {
        Logger.LogException(LogLevel.Info, ex, "Unexpected error reading file {0}", (object) "LastSync.json");
        this.lastSyncInfo = new WebTileSyncInfo();
      }
    }

    private string UserSettingsFilePath
    {
      get
      {
        this.CheckStorageProviderAndDataFolderPath();
        return Path.Combine(new string[2]
        {
          this.DataFolderPath,
          "UserSettings.json"
        });
      }
    }

    public Task SetNotificationEnabledAsync(bool enabled)
    {
      this.NotificationEnabled = enabled;
      return this.SaveUserSettingsAsync();
    }

    public Task SaveUserSettingsAsync() => Task.Run((Action) (() => this.SaveUserSettings()));

    public void SaveUserSettings()
    {
      using (Stream outputStream = this.StorageProvider.OpenFileForWrite(StorageProviderRoot.App, this.UserSettingsFilePath, false))
        CargoClient.SerializeJson(outputStream, (object) this.userSettings);
    }

    public void LoadUserSettings()
    {
      if (this.StorageProvider.FileExists(StorageProviderRoot.App, this.UserSettingsFilePath))
      {
        using (Stream inputStream = this.StorageProvider.OpenFileForRead(StorageProviderRoot.App, this.UserSettingsFilePath, 4096))
          this.userSettings = CargoClient.DeserializeJson<WebTileUserSettings>(inputStream);
      }
      else
        this.userSettings.NotificationEnabled = true;
    }

    public void Validate()
    {
      if (this.Resources.Length < 1)
        throw new InvalidDataException(CommonSR.WTNoResources);
      if (this.Resources[0].Style == ResourceStyle.Feed && this.Resources.Length != 1)
        throw new InvalidDataException(CommonSR.WTFeedMustHaveExactlyOneResource);
      this.ValidateTextBindings();
      this.ValidateIconBindings();
      this.ValidateAllPagesElementIds();
    }

    internal void ValidateTextBindings()
    {
      HashSet<string> referencedVariableNames1 = this.GetTextReferencedVariableNames();
      HashSet<string> definedVariableNames = this.GetTextDefinedVariableNames();
      HashSet<string> referencedVariableNames2 = this.GetNotificationsReferencedVariableNames();
      foreach (string str in referencedVariableNames1)
      {
        if (!definedVariableNames.Contains(str))
          throw new InvalidDataException(string.Format(CommonSR.WTUndefinedVariableReferencedInTextBindings, new object[1]
          {
            (object) str
          }));
      }
      foreach (string str in referencedVariableNames2)
      {
        if (!definedVariableNames.Contains(str))
          throw new InvalidDataException(string.Format(CommonSR.WTUndefinedVariableReferencedInNotifications, new object[1]
          {
            (object) str
          }));
      }
      foreach (object obj in definedVariableNames.Except<string>((IEnumerable<string>) referencedVariableNames1).Except<string>((IEnumerable<string>) referencedVariableNames2))
        Logger.Log(LogLevel.Warning, string.Format("Resource variable {0} not used", new object[1]
        {
          obj
        }));
    }

    private HashSet<string> GetTextDefinedVariableNames()
    {
      HashSet<string> definedVariableNames = new HashSet<string>();
      if (this.PrivateResources != null)
      {
        foreach (WebTileResource privateResource in this.PrivateResources)
        {
          if (privateResource != null && privateResource.Content != null)
          {
            foreach (KeyValuePair<string, string> keyValuePair in privateResource.Content)
            {
              string key = keyValuePair.Key;
              if (definedVariableNames.Contains(key))
                throw new InvalidDataException(string.Format(CommonSR.WTVariableNameNotUnique, new object[1]
                {
                  (object) key
                }));
              definedVariableNames.Add(key);
            }
          }
        }
      }
      return definedVariableNames;
    }

    private HashSet<string> GetTextReferencedVariableNames()
    {
      HashSet<string> referencedVariableNames = new HashSet<string>();
      if (this.pages != null)
      {
        foreach (WebTilePage page in this.pages)
        {
          WebTileTextBinding[] textBindings = page.TextBindings;
          if (textBindings != null)
          {
            foreach (WebTileTextBinding webTileTextBinding in textBindings)
            {
              if (webTileTextBinding != null && webTileTextBinding.TextValue != null)
              {
                foreach (string variableName in this.FindVariableNames(webTileTextBinding.TextValue))
                  referencedVariableNames.Add(variableName);
              }
            }
          }
        }
      }
      return referencedVariableNames;
    }

    internal void ValidateIconBindings()
    {
      HashSet<string> referencedIconNames = this.GetReferencedIconNames();
      HashSet<string> definedIconNames = this.GetDefinedIconNames();
      foreach (string str in referencedIconNames)
      {
        if (!definedIconNames.Contains(str))
          throw new InvalidDataException(string.Format(CommonSR.WTIconNotDefined, new object[1]
          {
            (object) str
          }));
      }
    }

    private HashSet<string> GetDefinedIconNames()
    {
      HashSet<string> definedIconNames = new HashSet<string>();
      if (this.additionalIcons != null)
      {
        foreach (KeyValuePair<string, string> additionalIcon in this.additionalIcons)
          definedIconNames.Add(additionalIcon.Key);
      }
      return definedIconNames;
    }

    private HashSet<string> GetReferencedIconNames()
    {
      HashSet<string> referencedIconNames = new HashSet<string>();
      if (this.pages != null)
      {
        foreach (WebTilePage page in this.pages)
        {
          WebTileIconBinding[] iconBindings = page.IconBindings;
          if (iconBindings != null)
          {
            foreach (WebTileIconBinding webTileIconBinding in iconBindings)
            {
              if (webTileIconBinding != null && webTileIconBinding.Conditions != null)
              {
                foreach (WebTileIconCondition condition in webTileIconBinding.Conditions)
                  referencedIconNames.Add(condition.IconName);
              }
            }
          }
        }
      }
      return referencedIconNames;
    }

    private HashSet<string> GetNotificationsReferencedVariableNames()
    {
      HashSet<string> referencedVariableNames = new HashSet<string>();
      if (this.notifications != null)
      {
        foreach (WebTileNotification notification in this.notifications)
        {
          if (notification.Condition != null)
          {
            foreach (string variableName in this.FindVariableNames(notification.Condition))
              referencedVariableNames.Add(variableName);
          }
        }
      }
      return referencedVariableNames;
    }

    private static PageLayout DeserializeLayout(string layoutName)
    {
      PageLayout pageLayout = (PageLayout) null;
      byte[] layoutBlob = WebTilePage.GetLayoutBlob(layoutName, BandClass.Unknown);
      if (layoutBlob != null)
      {
        using (MemoryStream input = new MemoryStream(layoutBlob))
        {
          using (CargoReaderOnBinaryReader reader = new CargoReaderOnBinaryReader(new BinaryReader((Stream) input)))
            pageLayout = PageLayout.DeserializeFromBand((ICargoReader) reader);
        }
      }
      return pageLayout;
    }

    internal void ValidateAllPagesElementIds()
    {
      if (this.pages == null)
        return;
      foreach (WebTilePage page in this.pages)
      {
        PageLayout pageLayout = WebTile.DeserializeLayout(page.LayoutName);
        if (pageLayout != null)
        {
          this.ValidateTextBindingElementIds(page, pageLayout);
          this.ValidateIconBindingElementIds(page, pageLayout);
        }
      }
    }

    private void ValidateTextBindingElementIds(WebTilePage page, PageLayout pageLayout)
    {
      HashSet<short> shortSet = new HashSet<short>();
      if (page.TextBindings == null)
        return;
      foreach (WebTileTextBinding textBinding in page.TextBindings)
      {
        textBinding.Validator.ClearPropertyError("ElementId");
        if (shortSet.Contains(textBinding.ElementId))
          page.Validator.CheckProperty("TextBindings", false, string.Format(CommonSR.WTMultipleTextBindingsWithElementId, new object[1]
          {
            (object) textBinding.ElementId
          }));
        else
          shortSet.Add(textBinding.ElementId);
        PageElement element = pageLayout.Root.FindElement(textBinding.ElementId);
        switch (element)
        {
          case null:
            textBinding.Validator.CheckProperty("ElementId", false, string.Format(CommonSR.WTElementIDNotValidForLayout, new object[2]
            {
              (object) textBinding.ElementId,
              (object) page.LayoutName
            }));
            continue;
          case TextBlock _:
          case WrappedTextBlock _:
          case Barcode _:
            continue;
          default:
            textBinding.Validator.CheckProperty("ElementId", false, string.Format(CommonSR.WTElementIDDoesNotSupportText, new object[2]
            {
              (object) textBinding.ElementId,
              (object) element.ToString()
            }));
            continue;
        }
      }
    }

    private void ValidateIconBindingElementIds(WebTilePage page, PageLayout pageLayout)
    {
      if (page.IconBindings == null)
        return;
      foreach (WebTileIconBinding iconBinding in page.IconBindings)
      {
        PageElement element = pageLayout.Root.FindElement(iconBinding.ElementId);
        if (element == null)
          iconBinding.Validator.CheckProperty("ElementId", false, string.Format(CommonSR.WTElementIDNotValidForLayout, new object[2]
          {
            (object) iconBinding.ElementId,
            (object) page.LayoutName
          }));
        else if (!(element is Icon))
          iconBinding.Validator.CheckProperty("ElementId", false, string.Format(CommonSR.WTElementIDIsNotAnIconInLayout, new object[2]
          {
            (object) iconBinding.ElementId,
            (object) page.LayoutName
          }));
      }
    }

    public Task SetAuthenticationHeaderAsync(
      IWebTileResource resource,
      string userName,
      string password)
    {
      return Task.Run((Action) (() => this.SetAuthenticationHeader(resource, userName, password)));
    }

    public void SetAuthenticationHeader(
      IWebTileResource resource,
      string userName,
      string password)
    {
      if (resource == null)
        throw new ArgumentNullException(nameof (resource));
      resource.Username = userName;
      resource.Password = password;
    }

    public async Task<bool> AuthenticateResourceAsync(IWebTileResource resource)
    {
      if (resource == null)
        throw new ArgumentNullException(nameof (resource));
      return await resource.AuthenticateAsync();
    }

    public void SaveResourceAuthentication()
    {
      PasswordVault vault = new PasswordVault();
      if (this.PrivateResources == null)
        return;
      foreach (WebTileResource privateResource in this.PrivateResources)
      {
        if (privateResource.Username != null && privateResource.Password != null)
        {
          string str = privateResource.Url + this.TileId.ToString();
          vault.Add(new PasswordCredential(str, privateResource.Username, privateResource.Password));
        }
        else
          this.RemoveResourceCredentials(vault, (IWebTileResource) privateResource);
      }
    }

    public void LoadResourceAuthentication()
    {
      PasswordVault vault = new PasswordVault();
      if (this.PrivateResources == null)
        return;
      foreach (WebTileResource privateResource in this.PrivateResources)
      {
        string str = privateResource.Url + this.TileId.ToString();
        IReadOnlyList<PasswordCredential> resourceCredentialsList = this.GetResourceCredentialsList(vault, (IWebTileResource) privateResource);
        if (resourceCredentialsList != null && ((IReadOnlyCollection<PasswordCredential>) resourceCredentialsList).Count > 0)
        {
          PasswordCredential passwordCredential = vault.Retrieve(str, resourceCredentialsList[0].UserName);
          privateResource.Username = passwordCredential.UserName;
          privateResource.Password = passwordCredential.Password;
        }
        else
        {
          privateResource.Username = (string) null;
          privateResource.Password = (string) null;
        }
      }
    }

    public void DeleteStoredResourceCredentials()
    {
      PasswordVault vault = new PasswordVault();
      if (this.PrivateResources == null)
        return;
      foreach (WebTileResource privateResource in this.PrivateResources)
        this.RemoveResourceCredentials(vault, (IWebTileResource) privateResource);
    }

    private void RemoveResourceCredentials(PasswordVault vault, IWebTileResource resource)
    {
      IReadOnlyList<PasswordCredential> resourceCredentialsList = this.GetResourceCredentialsList(vault, resource);
      if (resourceCredentialsList == null)
        return;
      foreach (PasswordCredential passwordCredential in (IEnumerable<PasswordCredential>) resourceCredentialsList)
        vault.Remove(passwordCredential);
    }

    private IReadOnlyList<PasswordCredential> GetResourceCredentialsList(
      PasswordVault vault,
      IWebTileResource resource)
    {
      string str = resource.Url + this.TileId.ToString();
      try
      {
        return vault.FindAllByResource(str);
      }
      catch
      {
        return (IReadOnlyList<PasswordCredential>) null;
      }
    }
  }
}
