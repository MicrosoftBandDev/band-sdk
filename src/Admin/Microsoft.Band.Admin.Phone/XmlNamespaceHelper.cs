// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.XmlNamespaceHelper
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.Data.Html;
using Windows.Data.Xml.Dom;

namespace Microsoft.Band.Admin
{
  internal class XmlNamespaceHelper : XmlNamespaceHelperBase
  {
    private List<string> namespaces;

    private XmlNamespaceHelper()
    {
    }

    public XmlNamespaceHelper(XmlDocument doc) => this.namespaces = this.GenerateNamespaceList(doc);

    public override string ResolveNodeWithNamespace(IXmlNode node, string xpath)
    {
      StringBuilder stringBuilder = new StringBuilder();
      string str1 = (string) null;
      foreach (string str2 in this.namespaces)
        stringBuilder.AppendFormat("{0}{1}", new object[2]
        {
          stringBuilder.Length > 0 ? (object) " " : (object) "",
          (object) str2
        });
      IXmlNode ixmlNode = ((IXmlNodeSelector) node).SelectSingleNodeNS(xpath, (object) stringBuilder.ToString());
      if (ixmlNode != null)
        str1 = HtmlUtilities.ConvertToText(((IXmlNodeSerializer) ixmlNode).InnerText);
      return str1;
    }

    public override void RemoveDefaultNamespace(XmlDocument doc) => doc.LoadXml(doc.GetXml().Replace("xmlns=\"", "xmlns:msbwt=\""));

    private List<string> GenerateNamespaceList(XmlDocument doc)
    {
      HashSet<string> source = new HashSet<string>();
      Queue<XmlElement> xmlElementQueue = new Queue<XmlElement>();
      xmlElementQueue.Enqueue(doc.DocumentElement);
      while (xmlElementQueue.Count != 0)
      {
        IXmlNode ixmlNode = (IXmlNode) xmlElementQueue.Dequeue();
        foreach (XmlAttribute attribute in (IEnumerable<IXmlNode>) ixmlNode.Attributes)
        {
          if ((string) attribute.Prefix == "xmlns")
            source.Add(attribute.GetXml());
        }
        foreach (IXmlNode childNode in (IEnumerable<IXmlNode>) ixmlNode.ChildNodes)
        {
          if ((object) ((object) childNode).GetType() == (object) typeof (XmlElement))
            xmlElementQueue.Enqueue(childNode as XmlElement);
        }
      }
      return source.ToList<string>();
    }
  }
}
