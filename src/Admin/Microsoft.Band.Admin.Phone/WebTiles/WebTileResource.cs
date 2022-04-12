// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.WebTiles.WebTileResource
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

using Microsoft.Band.Admin.Phone;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;

namespace Microsoft.Band.Admin.WebTiles
{
  [DataContract]
  public class WebTileResource : IWebTileResource
  {
    private string url;
    private ResourceStyle style;
    private Dictionary<string, string> content;
    private WebTilePropertyValidator validator;
    private WebTileResourceCacheInfo cacheInfo;
    internal const int MaxFeed = 8;
    private static readonly Regex AllowedVariableName = new Regex("^([A-Za-z_]\\w*)$");
    private static IPlatformProvider platformProvider = (IPlatformProvider) new PhoneProvider();

    public IWebTileResourceCacheInfo CacheInfo
    {
      get => (IWebTileResourceCacheInfo) this.cacheInfo;
      set => this.cacheInfo = (WebTileResourceCacheInfo) value;
    }

    public WebTileResource()
    {
      this.style = ResourceStyle.Simple;
      this.validator = new WebTilePropertyValidator();
      this.cacheInfo = new WebTileResourceCacheInfo();
    }

    public bool AllowInvalidValues
    {
      get => this.validator.AllowInvalidValues;
      set => this.validator.AllowInvalidValues = value;
    }

    public Dictionary<string, string> PropertyErrors => this.validator.PropertyErrors;

    public HeaderNameValuePair[] RequestHeaders { get; set; }

    [DataMember(IsRequired = true, Name = "url")]
    public string Url
    {
      get => this.url;
      set => this.validator.SetProperty<string>(ref this.url, value, nameof (Url), value != null && Uri.IsWellFormedUriString(value, UriKind.Absolute), CommonSR.WTBadUrl);
    }

    [DataMember(Name = "style")]
    public ResourceStyle Style
    {
      get => this.style;
      set => this.style = value;
    }

    [DataMember(IsRequired = true, Name = "content")]
    public Dictionary<string, string> Content
    {
      get => this.content;
      set
      {
        this.validator.ClearPropertyError(nameof (Content));
        this.validator.CheckProperty(nameof (Content), value != null && value.Count > 0, CommonSR.WTMissingVariableDefinitions);
        if (value != null)
        {
          foreach (KeyValuePair<string, string> keyValuePair in value)
          {
            bool flag1 = this.ValidateVariableName(keyValuePair.Key);
            if (!flag1)
            {
              this.validator.CheckProperty(nameof (Content), (flag1 ? 1 : 0) != 0, string.Format(CommonSR.WTInvalidVariableName, new object[1]
              {
                (object) keyValuePair.Key
              }));
              break;
            }
            bool flag2 = this.ValidateVariableExpression(keyValuePair.Value);
            if (!flag2)
            {
              this.validator.CheckProperty(nameof (Content), (flag2 ? 1 : 0) != 0, string.Format(CommonSR.WTInvalidVariableExpression, new object[1]
              {
                (object) keyValuePair.Key
              }));
              break;
            }
          }
        }
        this.content = value;
      }
    }

    public string Username { get; set; }

    public string Password { get; set; }

    private string AuthenticationHeader
    {
      get
      {
        if (this.Username == null || this.Password == null)
          return (string) null;
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Format("{0}:{1}", new object[2]
        {
          (object) this.Username,
          (object) this.Password
        })));
      }
    }

    private bool ValidateVariableName(string variableName) => !string.IsNullOrEmpty(variableName) && WebTileResource.AllowedVariableName.IsMatch(variableName);

    private bool ValidateVariableExpression(string variableExpression) => !string.IsNullOrWhiteSpace(variableExpression);

    public async Task<List<Dictionary<string, string>>> ResolveFeedContentMappingsAsync() => await this.DownloadResourceAsync(true) is XmlDocument data ? this.GetFeedContentMappings(data) : new List<Dictionary<string, string>>();

    internal async Task<object> DownloadResourceAsync(bool requiresXml = false)
    {
      string urlContent = (string) null;
      XmlDocument xdc = new XmlDocument();
      object result = (object) null;
      try
      {
        using (HttpResponseMessage responseMessage = await this.GetResourceResponseAsync())
        {
          if (responseMessage.Content != null)
          {
            if (responseMessage.Content.Headers != null && responseMessage.Content.Headers.ContentType != null && responseMessage.Content.Headers.ContentType.CharSet != null)
              responseMessage.Content.Headers.ContentType.CharSet = responseMessage.Content.Headers.ContentType.CharSet.Replace("\"", "");
            urlContent = await responseMessage.Content.ReadAsStringAsync();
          }
          try
          {
            if (responseMessage.StatusCode == HttpStatusCode.NotModified)
            {
              Logger.Log(LogLevel.Info, "No new data for resource {0}", (object) this.Url);
              return (object) null;
            }
            responseMessage.EnsureSuccessStatusCode();
            if (responseMessage.Headers.ETag != null)
              this.cacheInfo.ETag = responseMessage.Headers.ETag.Tag;
            this.cacheInfo.LastModified = (string) null;
            if (responseMessage.Content.Headers.LastModified.HasValue)
              this.cacheInfo.LastModified = responseMessage.Content.Headers.LastModified.Value.ToString("r");
          }
          catch (Exception ex)
          {
            throw new BandHttpException(urlContent, CommonSR.WTFailedToFetchResourceData, ex);
          }
        }
      }
      catch (Exception ex)
      {
        Logger.LogException(LogLevel.Warning, ex);
        throw;
      }
      if (urlContent == null)
        return (object) null;
      try
      {
        xdc.LoadXml(urlContent);
        result = (object) xdc;
      }
      catch
      {
      }
      if (result == null)
      {
        if (!requiresXml)
        {
          try
          {
            result = (object) JToken.Parse(urlContent);
          }
          catch
          {
          }
        }
      }
      if (result == null)
      {
        Exception e = new Exception("Url content not recognized");
        Logger.LogException(LogLevel.Info, e);
        throw e;
      }
      return result;
    }

    public bool IsSecure() => new Uri(this.Url).Scheme == "https";

    public async Task<bool> AuthenticateAsync()
    {
      using (HttpResponseMessage responseMessage = await this.GetResourceResponseAsync())
      {
        try
        {
          responseMessage.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
          if (responseMessage.StatusCode != HttpStatusCode.Unauthorized)
            throw new BandHttpException(await responseMessage.Content.ReadAsStringAsync(), CommonSR.WTFailedToFetchResourceData, ex);
          if (!this.IsSecure())
            throw new WebTileException(CommonSR.WTAuthenticationNeedsHttpsUri, ex);
          Logger.LogException(LogLevel.Warning, ex, "Failed to authenticate");
          return false;
        }
      }
      return true;
    }

    private async Task<HttpResponseMessage> GetResourceResponseAsync()
    {
      HttpResponseMessage resourceResponseAsync;
      using (HttpClient client = new HttpClient())
      {
        client.Timeout = TimeSpan.FromSeconds(30.0);
        using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, this.Url))
        {
          requestMessage.Headers.CacheControl = new CacheControlHeaderValue()
          {
            NoCache = true
          };
          requestMessage.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.2)");
          string authenticationHeader = this.AuthenticationHeader;
          if (authenticationHeader != null)
            requestMessage.Headers.Add("Authorization", string.Format("Basic {0}", new object[1]
            {
              (object) authenticationHeader
            }));
          if (this.cacheInfo.ETag != null)
            requestMessage.Headers.TryAddWithoutValidation("If-None-Match", this.cacheInfo.ETag);
          if (this.cacheInfo.LastModified != null)
            requestMessage.Headers.TryAddWithoutValidation("If-Modified-Since", this.cacheInfo.LastModified);
          if (this.RequestHeaders != null)
          {
            foreach (HeaderNameValuePair requestHeader in this.RequestHeaders)
            {
              try
              {
                requestMessage.Headers.Add(requestHeader.name, requestHeader.value);
              }
              catch (Exception ex)
              {
                throw new BandHttpException(this.Url, string.Format(CommonSR.WTBadHTTPRequestHeader, new object[2]
                {
                  (object) requestHeader.name,
                  (object) requestHeader.value
                }), ex);
              }
            }
          }
          resourceResponseAsync = await client.SendAsync(requestMessage);
        }
      }
      return resourceResponseAsync;
    }

    internal string GetUniqueItemId(XmlNamespaceHelper xmlnshelper, bool isRSS, IXmlNode node)
    {
      string uniqueItemId = (string) null;
      string[] strArray1 = new string[2]
      {
        "guid",
        "pubDate"
      };
      string[] strArray2 = new string[3]
      {
        "id",
        "updated",
        "published"
      };
      foreach (string xpath in isRSS ? strArray1 : strArray2)
      {
        uniqueItemId = xmlnshelper.ResolveNodeWithNamespace(node, xpath);
        if (uniqueItemId != null)
          break;
      }
      if (uniqueItemId == null)
      {
        string s = xmlnshelper.ResolveNodeWithNamespace(node, isRSS ? "description" : "summary") ?? ((IXmlNodeSerializer) node).GetXml();
        if (s != null)
        {
          byte[] bytes = Encoding.UTF8.GetBytes(s);
          uniqueItemId = BandBitConverter.ToString(WebTileResource.platformProvider.ComputeHashMd5(bytes));
        }
      }
      return uniqueItemId;
    }

    internal bool ItemIdIsInCache(string id)
    {
      bool flag = false;
      if (this.cacheInfo.FeedItemIds != null)
      {
        foreach (string feedItemId in this.cacheInfo.FeedItemIds)
        {
          if (string.Compare(id, feedItemId) == 0)
          {
            flag = true;
            break;
          }
        }
      }
      return flag;
    }

    internal void AddItemIdsToCache(List<string> ids)
    {
      if (this.cacheInfo.FeedItemIds == null)
      {
        this.cacheInfo.FeedItemIds = ids;
      }
      else
      {
        foreach (string id in ids)
          this.cacheInfo.FeedItemIds.Add(id);
      }
      if (this.cacheInfo.FeedItemIds.Count <= 8)
        return;
      this.cacheInfo.FeedItemIds = this.cacheInfo.FeedItemIds.GetRange(this.cacheInfo.FeedItemIds.Count - 8, 8);
    }

    internal List<Dictionary<string, string>> GetFeedContentMappings(XmlDocument data)
    {
      List<Dictionary<string, string>> feedContentMappings = new List<Dictionary<string, string>>();
      Dictionary<string, string> dictionary1 = new Dictionary<string, string>();
      List<string> ids = new List<string>();
      XmlNamespaceHelper xmlnshelper = new XmlNamespaceHelper(data);
      xmlnshelper.RemoveDefaultNamespace(data);
      int num1 = 0;
      foreach (KeyValuePair<string, string> keyValuePair in this.Content)
      {
        if (keyValuePair.Value.StartsWith("/"))
        {
          ++num1;
          if (!dictionary1.ContainsKey(keyValuePair.Key))
          {
            string str = xmlnshelper.ResolveNodeWithNamespace((IXmlNode) data.DocumentElement, keyValuePair.Value);
            if (str != null)
              dictionary1.Add(keyValuePair.Key, str);
          }
        }
      }
      if (num1 == this.Content.Count)
      {
        feedContentMappings.Add(dictionary1);
        return feedContentMappings;
      }
      XmlNodeList xmlNodeList;
      bool isRSS;
      if (((string) data.DocumentElement.LocalName).ToLower() == "rss")
      {
        xmlNodeList = data.DocumentElement.SelectNodes("/rss/channel/item");
        isRSS = true;
      }
      else
      {
        xmlNodeList = data.DocumentElement.SelectNodes("/feed/entry");
        isRSS = false;
      }
      int num2 = Math.Min(((IReadOnlyCollection<IXmlNode>) xmlNodeList).Count, 8);
      for (uint index = 0; (long) index < (long) num2; ++index)
      {
        string uniqueItemId = this.GetUniqueItemId(xmlnshelper, isRSS, xmlNodeList.Item(index));
        if (uniqueItemId != null)
        {
          if (!this.ItemIdIsInCache(uniqueItemId))
            ids.Add(uniqueItemId);
          else
            break;
        }
        Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
        foreach (KeyValuePair<string, string> keyValuePair in dictionary1)
          dictionary2.Add(keyValuePair.Key, keyValuePair.Value);
        foreach (KeyValuePair<string, string> keyValuePair in this.Content)
        {
          if (!dictionary2.ContainsKey(keyValuePair.Key))
          {
            string str = xmlnshelper.ResolveNodeWithNamespace(xmlNodeList.Item(index), keyValuePair.Value);
            if (str != null)
              dictionary2.Add(keyValuePair.Key, str);
          }
        }
        feedContentMappings.Add(dictionary2);
      }
      this.AddItemIdsToCache(ids);
      return feedContentMappings;
    }

    internal void ResolveContentMappings(Dictionary<string, string> mappings, XmlDocument document)
    {
      XmlNamespaceHelper xmlNamespaceHelper = new XmlNamespaceHelper(document);
      xmlNamespaceHelper.RemoveDefaultNamespace(document);
      foreach (KeyValuePair<string, string> keyValuePair in this.Content)
      {
        string str = xmlNamespaceHelper.ResolveNodeWithNamespace((IXmlNode) document.DocumentElement, keyValuePair.Value);
        if (str != null)
          mappings[keyValuePair.Key] = str;
      }
    }

    internal void ResolveContentMappings(Dictionary<string, string> mappings, JToken j)
    {
      foreach (KeyValuePair<string, string> keyValuePair in this.Content)
      {
        string str = (string) j.SelectToken(keyValuePair.Value);
        if (str != null)
          mappings[keyValuePair.Key] = str;
      }
    }
  }
}
