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
using System.Web;
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
			
			XmlTextReader reader = new XmlTextReader(new StringReader(page));
			XmlDocument origDoc = new XmlDocument();
			origDoc.PreserveWhitespace = true;
			try
			{
				origDoc.Load(reader);
			
				if (FilterInfo.XPathOfUriAttributes != null)
				{
					Debug.WriteLine("XPath = " + FilterInfo.XPathOfUriAttributes);
					XmlNodeList uriAttributes = origDoc.SelectNodes(FilterInfo.XPathOfUriAttributes);
					foreach (XmlAttribute attr in uriAttributes)
					{
						Debug.WriteLine("Checking " + attr.Value + " against " + FilterInfo.UriRegex.ToString());
						if (!FilterInfo.UriRegex.IsMatch(attr.Value))
						{
	                        attr.OwnerElement.Attributes.RemoveNamedItem(attr.LocalName, attr.NamespaceURI);
	                    }
					}
				}
							
				XPathNavigator nav = origDoc.CreateNavigator();
				
				lock (this)
				{
					IsValid = false;
					// We limit the number of errors we find to be sure we don't end up
					// in an infinite loop.  There is no way there could be more than 1 error for every
					// 2 characters in the fragment.
					for (int errors = 0; errors < htmlFragment.Length/2 + 1 && !IsValid; errors++)
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
					if (!IsValid)
					{
						throw new XmlException("Found too many errors in html fragment");
					}
				}

	            XmlElement bodyElem = origDoc.GetElementsByTagName("div")[0] as XmlElement;
				return bodyElem.InnerXml;
			}
			catch (XmlException)
			{
				return HttpUtility.HtmlEncode(htmlFragment);
			}
			catch (XmlSchemaException)
			{
				return HttpUtility.HtmlEncode(htmlFragment);
			}
		}
		
		private void OnValidationError(object sender, ValidationEventArgs args)
		{
			IsValid = false;
            XmlNode node = ((IHasXmlNode)NavReader.CreateNavigator()).GetNode();
			Debug.WriteLine("Name = " + node.Name + ", NodeType = " + node.NodeType);
            Debug.WriteLine(args.Message);
			
			if (node.NodeType == XmlNodeType.Element)
			{
				if (node.ParentNode == null)
				{
					// This occurs when a second error is thrown for an element that has already been replaced/removed.
					return;
				}
				if (node.Name == "span")
				{
					// If it's already a "span", replacing it with a "span" will just cause an infinite loop.
					// So throw an exception to force validation to stop.
					throw args.Exception;
				}
				
				
                XmlElement replacementElem = node.OwnerDocument.CreateElement("span", "http://www.w3.org/1999/xhtml");
				foreach (XmlNode contentNode in node.ChildNodes)
				{
					replacementElem.AppendChild(contentNode.CloneNode(true));
				}
				Debug.WriteLine("node.ParentNode = " + node.ParentNode);
				node.ParentNode.ReplaceChild(replacementElem, node);
			}
			else if (node.NodeType == XmlNodeType.Attribute)
			{
				XmlAttribute attr = (XmlAttribute)node;
				attr.OwnerElement.Attributes.RemoveNamedItem(attr.LocalName, attr.NamespaceURI);
			}
			else
			{
				node.ParentNode.RemoveChild(node);
			}
		}
	}
}
