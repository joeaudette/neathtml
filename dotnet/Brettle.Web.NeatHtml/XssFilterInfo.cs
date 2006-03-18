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
	internal class XssFilterInfo
	{
		internal XssFilterInfo(string schemaLocation)
		{
			XmlDocument schemaDoc = GetSchemaDoc(schemaLocation);
			XmlReader schemaReader = new XmlNodeReader(schemaDoc);
			try
			{
				Schema = XmlSchema.Read(schemaReader, null);
			}
			finally
			{
				schemaReader.Close();
			}
			
			Schema.Compile(null);
            if (Type.GetType("Mono.Runtime", false) != null)
            {
                SetUpUriRegexAndXPath(schemaDoc);
            }
        }

        private void SetUpUriRegexAndXPath(XmlDocument schemaDoc)
        {
            XmlSchemaSimpleType uriType = Schema.SchemaTypes[new XmlQualifiedName("URI", "http://www.w3.org/1999/xhtml")] as XmlSchemaSimpleType;
			XmlSchemaSimpleTypeRestriction uriTypeRestriction = uriType.Content as XmlSchemaSimpleTypeRestriction;
			XmlSchemaPatternFacet uriPattern = uriTypeRestriction.Facets[0] as XmlSchemaPatternFacet;
			UriRegex = new Regex(uriPattern.Value);
			
			XmlNodeList uriAttributeNameNodes = schemaDoc.SelectNodes("//*[@type='URI']/@name");
			string xpathPredicateOfUriAttributes = null;
			Hashtable includedAttributeNames = new Hashtable();
			for (int i = 0; i < uriAttributeNameNodes.Count; i++)
			{
				string attrName = uriAttributeNameNodes.Item(i).Value;
				if (includedAttributeNames[attrName] != null)
				{
					continue;
				}
				else
				{
					includedAttributeNames[attrName] = attrName;
				}
				
				if (i == 0)
				{
					xpathPredicateOfUriAttributes = "";
				}
				else
				{
					xpathPredicateOfUriAttributes += " or ";
				}
				xpathPredicateOfUriAttributes += "local-name() = '" + attrName + "'";
			}
			XPathOfUriAttributes = "/" + "/@*[" + xpathPredicateOfUriAttributes + "]";
			
			// NOTE: We don't bother with attributes of type "URIs" (plural) because only <object> has such an
			// attribute and we don't allow that element.
		}
		
		internal XmlSchema Schema;
		internal string XPathOfUriAttributes;
		internal Regex UriRegex;
		
		private static XmlDocument GetSchemaDoc(string schemaLocation)
		{
			XmlDocument schema = new XmlDocument();
			XmlTextReader schemaReader = new XmlTextReader(schemaLocation);
			try
			{
				schema.Load(schemaReader);
			}
			finally
			{
				schemaReader.Close();
			}
            if (Type.GetType("Mono.Runtime", false) != null)
            {
                PreProcessSchemaDoc(schema, schemaLocation);
            }
            return schema;
        }

        private static void PreProcessSchemaDoc(XmlDocument schema, string schemaLocation)
        {
            Hashtable includedFileNames = new Hashtable();
            while (true)
			{
				XmlNamespaceManager nsMgr = new System.Xml.XmlNamespaceManager(schema.NameTable);
				nsMgr.AddNamespace("xs", "http://www.w3.org/2001/XMLSchema");
                XmlNodeList imports = schema.GetElementsByTagName("xs:import");
				if (imports.Count > 0)
				{
					XmlElement import = imports[0] as XmlElement;
					import.ParentNode.RemoveChild(import);
					continue;
				}
				XmlNodeList nodes = schema.SelectNodes("//xs:include|//xs:redefine", nsMgr);
				
				Debug.WriteLine("nodes.Count == " + nodes.Count);
				XmlElement elem = null;
				for (int i = 0; i < nodes.Count; i++)
				{
					string inclSchemaLocation = nodes[i].Attributes["schemaLocation"].Value;
					if (includedFileNames[inclSchemaLocation] == null)
					{
						includedFileNames[inclSchemaLocation] = inclSchemaLocation;
						elem = nodes[i] as XmlElement;
						break;
					}
					else
					{
						nodes[i].ParentNode.RemoveChild(nodes[i]);
					}
				}
					
				if (elem != null)
				{
					string inclSchemaLocation = elem.Attributes["schemaLocation"].Value;
					inclSchemaLocation = Path.Combine(Path.GetDirectoryName(schemaLocation), inclSchemaLocation);
					XmlDocument inclSchema = new XmlDocument();
					Debug.WriteLine("Including " + inclSchemaLocation);
                    XmlTextReader schemaReader = new XmlTextReader(inclSchemaLocation);
					try
					{
						inclSchema.Load(schemaReader);
					}
					finally
					{
						schemaReader.Close();
					}
					
					foreach (XmlNode contentNode in inclSchema.DocumentElement.ChildNodes)
					{
						elem.ParentNode.InsertBefore(schema.ImportNode(contentNode, true), elem);
					}
					if (elem.Name == "xs:redefine")
					{
						Debug.WriteLine("Redefining " + inclSchemaLocation);
						foreach (XmlNode contentNode in elem.ChildNodes)
						{
							string xpath = null;
							XmlNode existingNode = null;
							if (contentNode.Name != null && contentNode.Attributes != null
							    && contentNode.Attributes["name"] != null && contentNode.Attributes["name"].Value != null)
							{
								xpath = "/" + "/" + contentNode.Name
									+ "[@name=\"" + contentNode.Attributes["name"].Value + "\"]";
								existingNode = schema.SelectSingleNode(xpath, nsMgr);
							}
							if (existingNode != null)
							{
								Debug.WriteLine("Replacing " + xpath);
								existingNode.ParentNode.ReplaceChild(contentNode.CloneNode(true), existingNode);
							}
							else
							{
								Debug.WriteLine("Adding " + xpath);
								elem.ParentNode.InsertBefore(contentNode.CloneNode(true), elem);
							}
						}
					}
						
					elem.ParentNode.RemoveChild(elem);
					continue;
				}
				break;
			}
/*				
			XmlTextWriter writer = new XmlTextWriter(new StreamWriter("Inline" + schemaLocation));
			try
			{
				schema.WriteTo(writer);
			}
			finally
			{
				writer.Close();
			}
*/			
		}	
	}
}
