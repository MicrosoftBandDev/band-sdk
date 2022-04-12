using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.Data.Xml.Dom;
#if WINDOWS
using Windows.Data.Html;
#endif

namespace Microsoft.Band.Admin;

internal class XmlNamespaceHelper : XmlNamespaceHelperBase
{
    private List<string> namespaces;

    private XmlNamespaceHelper()
    {
    }

    public XmlNamespaceHelper(XmlDocument doc)
    {
        namespaces = GenerateNamespaceList(doc);
    }

    public override string ResolveNodeWithNamespace(IXmlNode node, string xpath)
    {
        string result = null;
#if !WINDOWS
        string[] nss = namespaces.ToArray();
#else
        string nss = string.Join(' ', namespaces);
#endif

        IXmlNode val = node.SelectSingleNodeNS(xpath, nss);
        if (val != null)
        {
#if WINDOWS
            result = HtmlUtilities.ConvertToText(val.InnerText);
#else
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(val.InnerText);
            StringBuilder sb = new();
            foreach (HtmlAgilityPack.HtmlTextNode htmlNode in doc.DocumentNode.SelectNodes("//text()"))
            {
                sb.AppendLine(htmlNode.Text);
            }
            result = sb.ToString();
#endif
        }
        return result;
    }

    public override void RemoveDefaultNamespace(XmlDocument doc)
    {
        doc.LoadXml(doc.GetXml().Replace("xmlns=\"", "xmlns:msbwt=\""));
    }

    private List<string> GenerateNamespaceList(XmlDocument doc)
    {
        //IL_0038: Unknown result type (might be due to invalid IL or missing references)
        //IL_003f: Expected O, but got Unknown
        HashSet<string> hashSet = new HashSet<string>();
        Queue<XmlElement> queue = new Queue<XmlElement>();
        queue.Enqueue(doc.DocumentElement);
        while (queue.Count != 0)
        {
            IXmlNode val = (IXmlNode)(object)queue.Dequeue();
            foreach (XmlAttribute item in (IEnumerable<IXmlNode>)val.Attributes)
            {
                XmlAttribute val2 = item;
                if ((string)val2.Prefix == "xmlns")
                {
                    hashSet.Add(val2.GetXml());
                }
            }
            foreach (IXmlNode item2 in (IEnumerable<IXmlNode>)val.ChildNodes)
            {
                if ((object)((object)item2).GetType() == typeof(XmlElement))
                {
                    queue.Enqueue((XmlElement)(object)((item2 is XmlElement) ? item2 : null));
                }
            }
        }
        return hashSet.ToList();
    }
}
