using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.Data.Html;
using Windows.Data.Xml.Dom;

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
        StringBuilder stringBuilder = new StringBuilder();
        string result = null;
        foreach (string @namespace in namespaces)
        {
            stringBuilder.AppendFormat("{0}{1}", new object[2]
            {
                (stringBuilder.Length > 0) ? " " : "",
                @namespace
            });
        }
        IXmlNode val = ((IXmlNodeSelector)node).SelectSingleNodeNS(xpath, (object)stringBuilder.ToString());
        if (val != null)
        {
            result = HtmlUtilities.ConvertToText(((IXmlNodeSerializer)val).get_InnerText());
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
        queue.Enqueue(doc.get_DocumentElement());
        while (queue.Count != 0)
        {
            IXmlNode val = (IXmlNode)(object)queue.Dequeue();
            foreach (XmlAttribute item in (IEnumerable<IXmlNode>)val.get_Attributes())
            {
                XmlAttribute val2 = item;
                if ((string)val2.get_Prefix() == "xmlns")
                {
                    hashSet.Add(val2.GetXml());
                }
            }
            foreach (IXmlNode item2 in (IEnumerable<IXmlNode>)val.get_ChildNodes())
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
