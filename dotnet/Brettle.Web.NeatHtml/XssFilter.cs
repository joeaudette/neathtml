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
		
		public string FilterFragment(string htmlFragment)
		{
			htmlFragment = FixAmpersandsAndCase(htmlFragment);
			
			string page = @"<html xmlns=""" + FilterInfo.Schema.TargetNamespace + @"""><head><title>title</title></head>"
			+ "<body>\n"
			+ "<div>" + htmlFragment + "</div>\n"
			+ "</body></html>";
			XmlTextReader reader = new XmlTextReader(new StringReader(page));
			XmlDocument doc = new XmlDocument();
			doc.PreserveWhitespace = true;
			doc.Load(reader);
			if (FilterInfo.UriAndStyleValidator != null)
			{
				FilterInfo.UriAndStyleValidator.Validate(doc);
			}
			
			StringWriter sw = new StringWriter();
			doc.Save(sw);
			reader = new XmlTextReader(new StringReader(sw.ToString()));
			try
			{
				XmlValidatingReader validator = new System.Xml.XmlValidatingReader(reader);
				validator.ValidationEventHandler += new ValidationEventHandler(OnValidationError);
				
				validator.Schemas.Add(FilterInfo.Schema);
				validator.ValidationType = ValidationType.Schema;
				while (validator.Read())
				{
				}
			}
			finally
			{
				reader.Close();
			}
            XmlElement bodyElem = doc.GetElementsByTagName("div")[0] as XmlElement;
			return bodyElem.InnerXml;
		}
		
		// Replace ampersands with "&amp;" if they are not followed by either:
			// a. alphanumerics and a semi
			// b. "#" and decimal digits and a semi
			// c. "#x" or "#X" and hex digits and a semi
		// And force all tag names to lowercase.
		private static readonly Regex FixAmpersandsAndCaseRegex
			= new Regex("(</?[a-zA-Z]+)|(&(?!([A-Za-z0-9]+|#[0-9]+|#[xX][0-9A-Fa-f]+);))");
		private string FixAmpersandsAndCase(string htmlFragment)
		{
			// Replace ampersands with "&amp;" if they are not followed by either:
				// a. alphanumerics and a semi
				// b. "#" and decimal digits and a semi
				// c. "#x" or "#X" and hex digits and a semi
			// And force all tag names to lowercase.
			return FixAmpersandsAndCaseRegex.Replace(htmlFragment, new MatchEvaluator(FixMatch));
		}
		
		private string FixMatch(Match m)
		{
			if (m.Groups[1].Success)
			{
				return m.Groups[1].Value.ToLower();
			}
			else if (m.Groups[2].Success)
			{
				return "&amp;";
			}
			else
			{
				return null;
			}
		}
				
		private void OnValidationError(object sender, ValidationEventArgs args)
		{
			if (args.Exception != null)
			{
				throw args.Exception;
			}
			else
			{
				throw new XmlException("Validation error: " + args.Message);
			}
		}
	}
}
