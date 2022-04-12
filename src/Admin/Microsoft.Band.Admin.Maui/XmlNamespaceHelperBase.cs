using Windows.Data.Xml.Dom;

namespace Microsoft.Band.Admin;

internal abstract class XmlNamespaceHelperBase
{
    protected const string NamespacePrefix = "xmlns";

    protected const string DefaultNamespace = "xmlns=\"";

    protected const string WebTileNamespace = "xmlns:msbwt=\"";

    public abstract string ResolveNodeWithNamespace(IXmlNode node, string xpath);

    public abstract void RemoveDefaultNamespace(XmlDocument doc);
}
