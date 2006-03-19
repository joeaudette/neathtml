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
		public void TestNormalText()
		{
			AssertFilteredIsEqual(@"test text",
			                         @"test text");
		}
		
		[Test]
		public void TestBR()
		{
			AssertFilteredIsEqual(@"test<br />text",
			                         @"test<br xmlns=""http://www.w3.org/1999/xhtml"" />text");
		}
		
		[Test]
		public void TestFromJoe()
		{
			AssertFilteredIsEqual(@"<h3 id=""target"">Why was the target attribute removed from XHTML 1.1?</h3>

<p>It wasn't. XHTML 1.0 comes in three versions: strict, transitional, and frameset. All three of these were deliberately kept as close as possible to HTML 4.01 as XML would allow. XHTML 1.1 is an updated version of XHTML 1.0 <em>strict</em>, and no version of HTML strict has ever included the <code>target</code> attribute. The other two versions, transitional and frameset, were not updated, because there was nothing to update. If you want to use the <code>target</code> attribute, use XHTML 1.0 transitional.<br /><br />I took this text from W3C website about XHTML 1.1<br />so I think you should keep your target attributes and just convert to XHTML transitional.</p>
						    <br /><br />",
			                         @"<h3 id=""target"" xmlns=""http://www.w3.org/1999/xhtml"">Why was the target attribute removed from XHTML 1.1?</h3>

<p xmlns=""http://www.w3.org/1999/xhtml"">It wasn't. XHTML 1.0 comes in three versions: strict, transitional, and frameset. All three of these were deliberately kept as close as possible to HTML 4.01 as XML would allow. XHTML 1.1 is an updated version of XHTML 1.0 <em>strict</em>, and no version of HTML strict has ever included the <code>target</code> attribute. The other two versions, transitional and frameset, were not updated, because there was nothing to update. If you want to use the <code>target</code> attribute, use XHTML 1.0 transitional.<br /><br />I took this text from W3C website about XHTML 1.1<br />so I think you should keep your target attributes and just convert to XHTML transitional.</p>
						    <br xmlns=""http://www.w3.org/1999/xhtml"" /><br xmlns=""http://www.w3.org/1999/xhtml"" />");
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
			AssertFilteredIsEqual(@"<basefont size=""3""/><font color=""#ff0000"" size=""-1"" face=""Arial, Helvetica, Geneva, SunSans-Regular, sans-serif "">test</font>",
			                         @"<basefont size=""3"" xmlns=""http://www.w3.org/1999/xhtml"" /><font color=""#ff0000"" size=""-1"" face=""Arial, Helvetica, Geneva, SunSans-Regular, sans-serif "" xmlns=""http://www.w3.org/1999/xhtml"">test</font>");
		}
		
		[Test]
		public void TestImgSrc()
		{
			AssertFilteredIsEqual(@"<img src=""&#x6A;&#x61;&#x76;&#x61;&#x73;&#x63;&#x72;&#x69;&#x70;&#x74;:alert('TestImgSrc');""/>",
			                         @"<span xmlns=""http://www.w3.org/1999/xhtml"" />");
		}
				
		[Test]
		public void TestMissingRequiredAttribute()
		{
			AssertFilteredIsEqual(@"<img />",
			                         @"<span xmlns=""http://www.w3.org/1999/xhtml"" />");
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
