using System.Linq;
using System.Text.RegularExpressions;
using SystemProcessingInstruction = System.Xml.XmlProcessingInstruction;
using SystemXmlNode = System.Xml.XmlNode;

namespace Windows.Data.Xml.Dom
{
	public partial class XmlProcessingInstruction : IXmlNode, IXmlNodeSerializer, IXmlNodeSelector
	{
		private readonly XmlDocument _owner;
		internal readonly SystemProcessingInstruction _backingProcessingInstruction = null;

		public XmlProcessingInstruction(XmlDocument owner, SystemProcessingInstruction backingProcessingInstruction)
		{
			_owner = owner;
			_backingProcessingInstruction = backingProcessingInstruction;
		}

		public object Prefix
		{
			get => _backingProcessingInstruction.Prefix;
			set => _backingProcessingInstruction.Prefix = value?.ToString();
		}

		public object NodeValue
		{
			get => _backingProcessingInstruction.Value;
			set => _backingProcessingInstruction.Value = value?.ToString();
		}

		public XmlNamedNodeMap Attributes => (XmlNamedNodeMap)_owner.Wrap(_backingProcessingInstruction.Attributes);

		public IXmlNode FirstChild => (IXmlNode)_owner.Wrap(_backingProcessingInstruction.FirstChild);

		public XmlNodeList ChildNodes => (XmlNodeList)_owner.Wrap(_backingProcessingInstruction.ChildNodes);

		public IXmlNode LastChild => (IXmlNode)_owner.Wrap(_backingProcessingInstruction.LastChild);

		public object LocalName => _backingProcessingInstruction.LocalName;

		public object NamespaceUri => _backingProcessingInstruction.NamespaceURI;

		public IXmlNode NextSibling => (IXmlNode)_owner.Wrap(_backingProcessingInstruction.NextSibling);

		public string NodeName => _owner.NodeName;

		public NodeType NodeType => Dom.NodeType.ProcessingInstructionNode;

		public XmlDocument OwnerDocument => _owner;

		public IXmlNode ParentNode => (IXmlNode)_owner.Wrap(_backingProcessingInstruction.ParentNode);

		public IXmlNode PreviousSibling => (IXmlNode)_owner.Wrap(_backingProcessingInstruction.PreviousSibling);

		public string InnerText
		{
			get => _backingProcessingInstruction.InnerText;
			set => _backingProcessingInstruction.InnerText = value;
		}

		public string Data
		{
			get => _backingProcessingInstruction.Data;
			set => _backingProcessingInstruction.Data = value;
		}

		public string Target => _backingProcessingInstruction.Target;

		public bool HasChildNodes() => _backingProcessingInstruction.HasChildNodes;

		public IXmlNode InsertAfter(IXmlNode newChild, IXmlNode referenceChild) =>
			(IXmlNode)_owner.Wrap(
				_backingProcessingInstruction.InsertAfter(
					(SystemXmlNode)_owner.Unwrap(newChild),
					(SystemXmlNode)_owner.Unwrap(referenceChild)));

		public IXmlNode InsertBefore(IXmlNode newChild, IXmlNode referenceChild) =>
			(IXmlNode)_owner.Wrap(
				_backingProcessingInstruction.InsertBefore(
					(SystemXmlNode)_owner.Unwrap(newChild),
					(SystemXmlNode)_owner.Unwrap(referenceChild)));

		public IXmlNode ReplaceChild(IXmlNode newChild, IXmlNode referenceChild) =>
			(IXmlNode)_owner.Wrap(
				_backingProcessingInstruction.ReplaceChild(
					(SystemXmlNode)_owner.Unwrap(newChild),
					(SystemXmlNode)_owner.Unwrap(referenceChild)));

		public IXmlNode RemoveChild(IXmlNode childNode) =>
			(IXmlNode)_owner.Wrap(
				_backingProcessingInstruction.RemoveChild(
					(SystemXmlNode)_owner.Unwrap(childNode)));

		public IXmlNode AppendChild(IXmlNode newChild) =>
			(IXmlNode)_owner.Wrap(
				_backingProcessingInstruction.AppendChild(
					(SystemXmlNode)_owner.Unwrap(newChild)));

		public IXmlNode CloneNode(bool deep) => (IXmlNode)_owner.Wrap(_backingProcessingInstruction.CloneNode(deep));

		public void Normalize() => _owner.Normalize();

		public IXmlNode SelectSingleNode(string xpath) => (IXmlNode)_owner.Wrap(_backingProcessingInstruction.SelectSingleNode(xpath));

		public XmlNodeList SelectNodes(string xpath) => (XmlNodeList)_owner.Wrap(_backingProcessingInstruction.SelectNodes(xpath));

		public IXmlNode SelectSingleNodeNS(string xpath, params string[] namespaces)
		{
			// Extract namespace URIs from XML namespace definitions
			Regex rx = new("xmlns:([A-z]+)=\"(.+)\"", RegexOptions.Compiled);
			var uris = namespaces.Select(ns => rx.Match(ns))
				.Where(m => m.Success)
				.Select(m => m.Groups[2].Value);

			return SelectNodes(xpath).Where(n => uris.Contains(n.NamespaceUri.ToString())).FirstOrDefault();
		}

		public string GetXml() => _backingProcessingInstruction.OuterXml;
	}
}
