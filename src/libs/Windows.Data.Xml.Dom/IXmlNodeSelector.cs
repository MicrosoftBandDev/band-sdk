namespace Windows.Data.Xml.Dom
{
	public partial interface IXmlNodeSelector
	{
		IXmlNode SelectSingleNode(string xpath);

		XmlNodeList SelectNodes(string xpath);

		IXmlNode SelectSingleNodeNS(string xpath, params string[] namespaces);
	}
}
