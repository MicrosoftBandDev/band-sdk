using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Band.Admin.Phone;
using Newtonsoft.Json.Linq;
using Windows.Data.Xml.Dom;

namespace Microsoft.Band.Admin.WebTiles;

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

    private static IPlatformProvider platformProvider = new PhoneProvider();

    public IWebTileResourceCacheInfo CacheInfo
    {
        get
        {
            return cacheInfo;
        }
        set
        {
            cacheInfo = (WebTileResourceCacheInfo)value;
        }
    }

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

    public HeaderNameValuePair[] RequestHeaders { get; set; }

    [DataMember(Name = "url", IsRequired = true)]
    public string Url
    {
        get
        {
            return url;
        }
        set
        {
            validator.SetProperty(ref url, value, "Url", value != null && Uri.IsWellFormedUriString(value, UriKind.Absolute), CommonSR.WTBadUrl);
        }
    }

    [DataMember(Name = "style")]
    public ResourceStyle Style
    {
        get
        {
            return style;
        }
        set
        {
            style = value;
        }
    }

    [DataMember(Name = "content", IsRequired = true)]
    public Dictionary<string, string> Content
    {
        get
        {
            return content;
        }
        set
        {
            validator.ClearPropertyError("Content");
            validator.CheckProperty("Content", value != null && value.Count > 0, CommonSR.WTMissingVariableDefinitions);
            if (value != null)
            {
                foreach (KeyValuePair<string, string> item in value)
                {
                    bool flag = ValidateVariableName(item.Key);
                    if (!flag)
                    {
                        validator.CheckProperty("Content", flag, string.Format(CommonSR.WTInvalidVariableName, new object[1] { item.Key }));
                        break;
                    }
                    flag = ValidateVariableExpression(item.Value);
                    if (!flag)
                    {
                        validator.CheckProperty("Content", flag, string.Format(CommonSR.WTInvalidVariableExpression, new object[1] { item.Key }));
                        break;
                    }
                }
            }
            content = value;
        }
    }

    public string Username { get; set; }

    public string Password { get; set; }

    private string AuthenticationHeader
    {
        get
        {
            if (Username != null && Password != null)
            {
                return Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Format("{0}:{1}", new object[2] { Username, Password })));
            }
            return null;
        }
    }

    public WebTileResource()
    {
        style = ResourceStyle.Simple;
        validator = new WebTilePropertyValidator();
        cacheInfo = new WebTileResourceCacheInfo();
    }

    private bool ValidateVariableName(string variableName)
    {
        if (!string.IsNullOrEmpty(variableName))
        {
            return AllowedVariableName.IsMatch(variableName);
        }
        return false;
    }

    private bool ValidateVariableExpression(string variableExpression)
    {
        return !string.IsNullOrWhiteSpace(variableExpression);
    }

    public async Task<List<Dictionary<string, string>>> ResolveFeedContentMappingsAsync()
    {
        object obj = await DownloadResourceAsync(requiresXml: true);
        XmlDocument val = (XmlDocument)((obj is XmlDocument) ? obj : null);
        if (val == null)
        {
            return new List<Dictionary<string, string>>();
        }
        return GetFeedContentMappings(val);
    }

    internal async Task<object> DownloadResourceAsync(bool requiresXml = false)
    {
        string urlContent = null;
        XmlDocument xdc = new XmlDocument();
        object result = null;
        try
        {
            using HttpResponseMessage responseMessage = await GetResourceResponseAsync();
            if (responseMessage.Content != null)
            {
                if (responseMessage.Content.Headers != null && responseMessage.Content.Headers.ContentType != null && responseMessage.Content.Headers.ContentType.CharSet != null)
                {
                    string charSet = responseMessage.Content.Headers.ContentType.CharSet.Replace("\"", "");
                    responseMessage.Content.Headers.ContentType.CharSet = charSet;
                }
                urlContent = await responseMessage.Content.ReadAsStringAsync();
            }
            try
            {
                if (responseMessage.StatusCode == HttpStatusCode.NotModified)
                {
                    Logger.Log(LogLevel.Info, "No new data for resource {0}", Url);
                    return null;
                }
                responseMessage.EnsureSuccessStatusCode();
                if (responseMessage.Headers.ETag != null)
                {
                    cacheInfo.ETag = responseMessage.Headers.ETag.Tag;
                }
                cacheInfo.LastModified = null;
                if (responseMessage.Content.Headers.LastModified.HasValue)
                {
                    DateTimeOffset value = responseMessage.Content.Headers.LastModified.Value;
                    cacheInfo.LastModified = value.ToString("r");
                }
            }
            catch (Exception innerException)
            {
                throw new BandHttpException(urlContent, CommonSR.WTFailedToFetchResourceData, innerException);
            }
        }
        catch (Exception e)
        {
            Logger.LogException(LogLevel.Warning, e);
            throw;
        }
        if (urlContent == null)
        {
            return null;
        }
        try
        {
            xdc.LoadXml(urlContent);
            result = xdc;
        }
        catch
        {
        }
        if (result == null && !requiresXml)
        {
            try
            {
                result = JToken.Parse(urlContent);
            }
            catch
            {
            }
        }
        if (result == null)
        {
            Exception ex = new Exception("Url content not recognized");
            Logger.LogException(LogLevel.Info, ex);
            throw ex;
        }
        return result;
    }

    public bool IsSecure()
    {
        return new Uri(Url).Scheme == "https";
    }

    public async Task<bool> AuthenticateAsync()
    {
        using (HttpResponseMessage responseMessage = await GetResourceResponseAsync())
        {
            try
            {
                responseMessage.EnsureSuccessStatusCode();
            }
            catch (Exception e)
            {
                if (responseMessage.StatusCode == HttpStatusCode.Unauthorized)
                {
                    if (!IsSecure())
                    {
                        throw new WebTileException(CommonSR.WTAuthenticationNeedsHttpsUri, e);
                    }
                    Logger.LogException(LogLevel.Warning, e, "Failed to authenticate");
                    return false;
                }
                throw new BandHttpException(await responseMessage.Content.ReadAsStringAsync(), CommonSR.WTFailedToFetchResourceData, e);
            }
        }
        return true;
    }

    private async Task<HttpResponseMessage> GetResourceResponseAsync()
    {
        using HttpClient client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(30.0);
        using HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, Url);
        requestMessage.Headers.CacheControl = new CacheControlHeaderValue
        {
            NoCache = true
        };
        requestMessage.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.2)");
        string authenticationHeader = AuthenticationHeader;
        if (authenticationHeader != null)
        {
            requestMessage.Headers.Add("Authorization", $"Basic {authenticationHeader}");
        }
        if (cacheInfo.ETag != null)
        {
            requestMessage.Headers.TryAddWithoutValidation("If-None-Match", cacheInfo.ETag);
        }
        if (cacheInfo.LastModified != null)
        {
            requestMessage.Headers.TryAddWithoutValidation("If-Modified-Since", cacheInfo.LastModified);
        }
        if (RequestHeaders != null)
        {
            HeaderNameValuePair[] requestHeaders = RequestHeaders;
            for (int i = 0; i < requestHeaders.Length; i++)
            {
                HeaderNameValuePair headerNameValuePair = requestHeaders[i];
                try
                {
                    requestMessage.Headers.Add(headerNameValuePair.name, headerNameValuePair.value);
                }
                catch (Exception innerException)
                {
                    throw new BandHttpException(Url, string.Format(CommonSR.WTBadHTTPRequestHeader, new object[2] { headerNameValuePair.name, headerNameValuePair.value }), innerException);
                }
            }
        }
        return await client.SendAsync(requestMessage);
    }

    internal string GetUniqueItemId(XmlNamespaceHelper xmlnshelper, bool isRSS, IXmlNode node)
    {
        string text = null;
        string[] array = new string[2] { "guid", "pubDate" };
        string[] array2 = new string[3] { "id", "updated", "published" };
        string[] array3 = (isRSS ? array : array2);
        foreach (string xpath in array3)
        {
            text = xmlnshelper.ResolveNodeWithNamespace(node, xpath);
            if (text != null)
            {
                break;
            }
        }
        if (text == null)
        {
            string text2 = xmlnshelper.ResolveNodeWithNamespace(node, isRSS ? "description" : "summary");
            if (text2 == null)
            {
                text2 = ((IXmlNodeSerializer)node).GetXml();
            }
            if (text2 != null)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(text2);
                text = BandBitConverter.ToString(platformProvider.ComputeHashMd5(bytes));
            }
        }
        return text;
    }

    internal bool ItemIdIsInCache(string id)
    {
        bool result = false;
        if (cacheInfo.FeedItemIds != null)
        {
            foreach (string feedItemId in cacheInfo.FeedItemIds)
            {
                if (string.Compare(id, feedItemId) == 0)
                {
                    return true;
                }
            }
            return result;
        }
        return result;
    }

    internal void AddItemIdsToCache(List<string> ids)
    {
        if (cacheInfo.FeedItemIds == null)
        {
            cacheInfo.FeedItemIds = ids;
        }
        else
        {
            foreach (string id in ids)
            {
                cacheInfo.FeedItemIds.Add(id);
            }
        }
        if (cacheInfo.FeedItemIds.Count > 8)
        {
            cacheInfo.FeedItemIds = cacheInfo.FeedItemIds.GetRange(cacheInfo.FeedItemIds.Count - 8, 8);
        }
    }

    internal List<Dictionary<string, string>> GetFeedContentMappings(XmlDocument data)
    {
        List<Dictionary<string, string>> list = new List<Dictionary<string, string>>();
        Dictionary<string, string> dictionary = new Dictionary<string, string>();
        List<string> list2 = new List<string>();
        XmlNamespaceHelper xmlNamespaceHelper = new XmlNamespaceHelper(data);
        xmlNamespaceHelper.RemoveDefaultNamespace(data);
        int num = 0;
        foreach (KeyValuePair<string, string> item in Content)
        {
            if (!item.Value.StartsWith("/"))
            {
                continue;
            }
            num++;
            if (!dictionary.ContainsKey(item.Key))
            {
                string text = xmlNamespaceHelper.ResolveNodeWithNamespace((IXmlNode)(object)data.DocumentElement, item.Value);
                if (text != null)
                {
                    dictionary.Add(item.Key, text);
                }
            }
        }
        if (num == Content.Count)
        {
            list.Add(dictionary);
            return list;
        }
        XmlNodeList val;
        bool isRSS;
        if (((string)data.DocumentElement.LocalName).ToLower() == "rss")
        {
            val = data.DocumentElement.SelectNodes("/rss/channel/item");
            isRSS = true;
        }
        else
        {
            val = data.DocumentElement.SelectNodes("/feed/entry");
            isRSS = false;
        }
        int num2 = Math.Min(((IReadOnlyCollection<IXmlNode>)val).Count, 8);
        for (int num3 = 0; num3 < num2; num3++)
        {
            string uniqueItemId = GetUniqueItemId(xmlNamespaceHelper, isRSS, val[num3]);
            if (uniqueItemId != null)
            {
                if (ItemIdIsInCache(uniqueItemId))
                {
                    break;
                }
                list2.Add(uniqueItemId);
            }
            Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> item2 in dictionary)
            {
                dictionary2.Add(item2.Key, item2.Value);
            }
            foreach (KeyValuePair<string, string> item3 in Content)
            {
                if (!dictionary2.ContainsKey(item3.Key))
                {
                    string text2 = null;
                    text2 = xmlNamespaceHelper.ResolveNodeWithNamespace(val[num3], item3.Value);
                    if (text2 != null)
                    {
                        dictionary2.Add(item3.Key, text2);
                    }
                }
            }
            list.Add(dictionary2);
        }
        AddItemIdsToCache(list2);
        return list;
    }

    internal void ResolveContentMappings(Dictionary<string, string> mappings, XmlDocument document)
    {
        XmlNamespaceHelper xmlNamespaceHelper = new XmlNamespaceHelper(document);
        xmlNamespaceHelper.RemoveDefaultNamespace(document);
        foreach (KeyValuePair<string, string> item in Content)
        {
            string text = xmlNamespaceHelper.ResolveNodeWithNamespace((IXmlNode)(object)document.DocumentElement, item.Value);
            if (text != null)
            {
                mappings[item.Key] = text;
            }
        }
    }

    internal void ResolveContentMappings(Dictionary<string, string> mappings, JToken j)
    {
        foreach (KeyValuePair<string, string> item in Content)
        {
            string text = (string)j.SelectToken(item.Value);
            if (text != null)
            {
                mappings[item.Key] = text;
            }
        }
    }
}
