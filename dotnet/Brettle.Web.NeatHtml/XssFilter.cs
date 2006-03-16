/*

NeatHtml- Helps prevent XSS attacks by validating HTML against a subset of XHTML.
Copyright (C) 2006  Dean Brettle

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
*/

using System;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Schema;
using System.Collections;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace Brettle.Web.NeatHtml
{
	public class XssFilter
	{
		public static XssFilter GetForSchema(string schemaLocation)
		{
			lock (FilterInfoTable.SyncRoot)
			{
				XssFilterInfo filterInfo = FilterInfoTable[schemaLocation] as XssFilterInfo;
				if (filterInfo == null)
				{
					filterInfo = new XssFilterInfo(schemaLocation);
					FilterInfoTable[schemaLocation] = filterInfo;
				}
				return new XssFilter(filterInfo);
			}
		}
		
		private static Hashtable FilterInfoTable = new Hashtable();
		
		private XssFilter(XssFilterInfo filterInfo)
		{
			FilterInfo = filterInfo;
		}
		
		private XssFilterInfo FilterInfo;
		private XPathNavigatorReader NavReader;
		private bool IsValid = false;
		
		public string FilterFragment(string htmlFragment)
		{
			string page ="<html xmlns=\"http://www.w3.org/1999/xhtml\">\n"
			+ "<head><title>title</title></head>\n"
			+ "<body>"
			+ "<div>" + htmlFragment + "</div>"
			+ "</body>\n"
			+ "</html>";
			
			// XmlParserContext parserContext = new XmlParserContext(null, null, "", XmlSpace.None);
			
			// XmlValidatingReader validator
			//			= new System.Xml.XmlValidatingReader(page, XmlNodeType.Element, parserContext);
			XmlTextReader reader = new XmlTextReader(new StringReader(page));
			XmlDocument origDoc = new XmlDocument();
			origDoc.Load(reader);
						
			XPathNavigator nav = origDoc.CreateNavigator();
			
			lock (this)
			{
				IsValid = false;
				// TODO: limit number of errors
				while (!IsValid)
				{
					nav.MoveToRoot();
					NavReader = new XPathNavigatorReader(nav);
					XmlValidatingReader validator = new System.Xml.XmlValidatingReader(NavReader);
					validator.ValidationEventHandler += new ValidationEventHandler(OnValidationError);
					
					validator.Schemas.Add(FilterInfo.Schema);
					validator.ValidationType = ValidationType.Schema;
					IsValid = true;
					while (IsValid && validator.Read())
					{
					}
				}
			}
						
			if (FilterInfo.XPathOfUriAttributes != null)
			{
				XmlNodeList uriAttributes = origDoc.SelectNodes(FilterInfo.XPathOfUriAttributes);
				foreach (XmlNode attr in uriAttributes)
				{
					if (!FilterInfo.UriRegex.IsMatch(attr.Value))
					{
						attr.Value = "";
					}
				}
			}
			
			XmlElement bodyElem = origDoc.GetElementsByTagName("div")[0] as XmlElement;
			return bodyElem.InnerXml;
		}
		
		private void OnValidationError(object sender, ValidationEventArgs args)
		{
			IsValid = false;
			Console.WriteLine(args.Message);
			XmlNode node = ((IHasXmlNode)NavReader.CreateNavigator()).GetNode();
			Console.WriteLine("Name = " + node.Name + ", NodeType = " + node.NodeType);
			if (node.ParentNode == null)
			{
				return;
			}
			
			if (node.NodeType == XmlNodeType.Element)
			{
				XmlElement replacementElem = node.OwnerDocument.CreateElement("span");
				foreach (XmlNode contentNode in node.ChildNodes)
				{
					replacementElem.AppendChild(contentNode.CloneNode(true));
				}
				Console.WriteLine("node.ParentNode = " + node.ParentNode);
				node.ParentNode.ReplaceChild(replacementElem, node);
			}
			else
			{
				node.ParentNode.RemoveChild(node);
			}
		}
	}
}
