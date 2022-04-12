// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.XmlNamespaceHelperBase
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

using Windows.Data.Xml.Dom;

namespace Microsoft.Band.Admin
{
  internal abstract class XmlNamespaceHelperBase
  {
    protected const string NamespacePrefix = "xmlns";
    protected const string DefaultNamespace = "xmlns=\"";
    protected const string WebTileNamespace = "xmlns:msbwt=\"";

    public abstract string ResolveNodeWithNamespace(IXmlNode node, string xpath);

    public abstract void RemoveDefaultNamespace(XmlDocument doc);
  }
}
