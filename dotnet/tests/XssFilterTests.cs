/*

NeatHtml - Helps prevent XSS attacks by validating HTML against a subset of XHTML.
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
using NUnit.Framework;
using System.IO;
using System.Web;

namespace Brettle.Web.NeatHtml.UnitTests
{
	[TestFixture]
	public class XssFilterTests
	{
		private XssFilter Filter;
		
		[SetUp]
		public void SetUp()
		{
			DirectoryInfo currentDir = new DirectoryInfo(System.Environment.CurrentDirectory);
			string schemaLocation = Path.Combine(currentDir.Parent.Parent.Parent.Parent.FullName, "schema");
			schemaLocation = Path.Combine(schemaLocation, "NeatHtml.xsd");
			Filter = XssFilter.GetForSchema(schemaLocation);
		}
		
		[Test]
		public void TestHref()
		{
			AssertFilteredIsEqual(@"<a href='javascript:alert(""TestHref"");'>test link</a>",
			                         @"<a xmlns=""http://www.w3.org/1999/xhtml"">test link</a>");
		}
		
		[Test]
		public void TestObject()
		{
			AssertFilteredIsEqual(@"<object>fallback content</object>",
                                     @"<span xmlns=""http://www.w3.org/1999/xhtml"">fallback content</span>");
		}
		
		[Test]
		public void TestMalformed()
		{
			AssertFilteredIsEncoded(@"<span id='x'>missing close tag");
		}
		
		[Test]
		public void TestPreserveWhitespace()
		{
			AssertFilteredIsEqual("<pre>\n\ttab indent\n      6 space indent\n</pre>",
			                         "<pre xmlns=\"http://www.w3.org/1999/xhtml\">\n\ttab indent\n      6 space indent\n</pre>");
		}
		
		[Test]
		public void TestFont()
		{
			AssertFilteredIsEqual(@"<font color=""#ff0000"" size=""2"" face=""Arial, Helvetica, Geneva, SunSans-Regular, sans-serif "">test</font>",
			                         @"<font color=""#ff0000"" size=""2"" face=""Arial, Helvetica, Geneva, SunSans-Regular, sans-serif "" xmlns=""http://www.w3.org/1999/xhtml"">test</font>");
		}
		
		[Test]
		public void TestImgSrc()
		{
			AssertFilteredIsEqual(@"<img src=""&#x6A;&#x61;&#x76;&#x61;&#x73;&#x63;&#x72;&#x69;&#x70;&#x74;:alert('TestImgSrc');""/>", @"<span xmlns=""http://www.w3.org/1999/xhtml"" />");
		}
				
		[Test]
		public void TestMissingRequiredAttribute()
		{
			AssertFilteredIsEqual(@"<img />", @"<span xmlns=""http://www.w3.org/1999/xhtml"" />");
		}
		
		[Test]
		public void TestOnClick()
		{
			AssertFilteredIsEqual(@"<a href=""#"" onclick=""&#x6A;&#x61;&#x76;&#x61;&#x73;&#x63;&#x72;&#x69;&#x70;&#x74;:document.body.appendChild(document.createTextNode('${xssTestId}_A_ONCLICK_HEX.'));"">TestOnClick</a>",
			                         @"<a href=""#"" xmlns=""http://www.w3.org/1999/xhtml"">TestOnClick</a>");
		}
		
		[Test]
		public void TestSpanNotAllowed()
		{
			AssertFilteredIsEncoded(@"<br><span>span not allowed here</span></br>");
		}
		
		private void AssertFilteredIsEqual(string fragment, string expected)
		{
			string actual = Filter.FilterFragment(fragment);
			Assert.AreEqual(expected, actual, "Full actual = " + actual);
		}			
		
		private void AssertFilteredIsEncoded(string fragment)
		{
			string actual = Filter.FilterFragment(fragment);
			Assert.AreEqual(HttpUtility.HtmlEncode(fragment), actual, "Full actual = " + actual);
		}			
	}
}
