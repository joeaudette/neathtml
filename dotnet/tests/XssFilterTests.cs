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
using System.Xml;
using System.Xml.Schema;

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
			AssertFilterDoesNotChange(@"test text");
		}
		
		[Test]
		public void TestBR()
		{
			AssertFilterDoesNotChange(@"test<br />text");
		}
		
		[Test]
		public void TestFromJoe()
		{
			AssertFilterDoesNotChange(@"<h3 id=""target"">Why was the target attribute removed from XHTML 1.1?</h3>

<p>It wasn't. XHTML 1.0 comes in three versions: strict, transitional, and frameset. All three of these were deliberately kept as close as possible to HTML 4.01 as XML would allow. XHTML 1.1 is an updated version of XHTML 1.0 <em>strict</em>, and no version of HTML strict has ever included the <code>target</code> attribute. The other two versions, transitional and frameset, were not updated, because there was nothing to update. If you want to use the <code>target</code> attribute, use XHTML 1.0 transitional.<br /><br />I took this text from W3C website about XHTML 1.1<br />so I think you should keep your target attributes and just convert to XHTML transitional.</p>
						    <br /><br />");
		}
		
		[Test]
		public void TestBareAmpersandsEncoded()
		{
			AssertFilteredIsEqual(@"<a href=""http://mywebsite.com/GalleryImageEdit.aspx?mid=9&pageindex=3&pageid=6"">http://mywebsite.com/GalleryImageEdit.aspx?mid=9&pageindex=3&pageid=6</a>.",
			                         @"<a href=""http://mywebsite.com/GalleryImageEdit.aspx?mid=9&amp;pageindex=3&amp;pageid=6"">http://mywebsite.com/GalleryImageEdit.aspx?mid=9&amp;pageindex=3&amp;pageid=6</a>.");
		}
		
		[Test]
		public void TestEntitiesLeftAlone()
		{
			AssertFilterDoesNotChange(@"&quot;This&#x20;&amp;&#32;that&nbsp;and that&quot;");
		}
		
		[Test]
		public void TestTagsToLowercase()
		{
			AssertFilteredIsEqual(@"<SPAN>Test</SPAN><Span>Test2</Span>",
			                         @"<span>Test</span><span>Test2</span>");
		}
		
		[Test]
		public void TestFixUnclosedBR()
		{
			AssertFilteredIsEqual(@"line 1<br>line2<br >line3<br class=""BR"">line4<br/>line5<br />line6",
			                         @"line 1<br/>line2<br />line3<br class=""BR""/>line4<br/>line5<br />line6");
		}
		
		[Test]
		public void TestHref()
		{
			AssertFilterThrowsXmlSchemaException(@"<a href='javascript:alert(""TestHref"");'>test link</a>");
		}
		
		[Test]
        public void TestObject()
		{
            AssertFilterThrowsXmlSchemaException(@"<object>fallback content</object>");
		}
		
		[Test]
		[ExpectedException(typeof(XmlException))]
		public void TestMalformed()
		{
			Filter.FilterFragment(@"<span id='x'>missing close tag");
		}
		
		[Test]
		public void TestPreserveWhitespace()
		{
			AssertFilterDoesNotChange("<pre>\n\ttab indent\n      6 space indent\n</pre>");
		}
		
		[Test]
		public void TestFont()
		{
			AssertFilterDoesNotChange(@"<basefont size=""3""/><font color=""#ff0000"" size=""-1"" face=""Arial, Helvetica, Geneva, SunSans-Regular, sans-serif "">test</font>");
		}
		
		[Test]
		public void TestParagraphInFont()
		{
			AssertFilterDoesNotChange(@"<font color=""#ff0000""><p>test</p></font>");
		}
		
		[Test]
		public void TestCommonAttribs()
		{
			AssertFilterDoesNotChange(@"<table bgcolor=""#ff0000""><tr><td></td></tr></table>");
		}
		
		[Test]
		public void TestInlineTextStyles()
		{
			AssertFilterDoesNotChange(@"<span style=""font-weight: bold;"">Bold,</span> <span style=""font-style: italic;"">italic,</span> <span style=""text-decoration: underline;"">underline,</span> <span style=""font-weight: bold; font-style: italic; text-decoration: underline;"">bold-italic-underline</span>.");
		}
		
		[Test]
		public void TestInlineMoreStyles()
		{
			AssertFilterDoesNotChange(@"<span style=""width: 100%; height: 20px; vertical-align: top; mso-fareast-font-family: 'Times New Roman'; mso-ansi-language: EN-US; mso-fareast-language: EN-US; mso-bidi-language: AR-SA"">test</span>");
		}
		
		[Test]
		public void TestStylesAreCaseInsensitive()
		{
			AssertFilterDoesNotChange(@"<span style=""FONT-WEIGHT: bold;"">Bold,</span> <span style=""font-style: italic;"">italic,</span> <span style=""text-decoration: underline;"">underline,</span> <span style=""font-weight: bold; font-style: italic; text-decoration: underline;"">bold-italic-underline</span>.");
		}
		
		[Test]
		public void TestFontFamily()
		{
			AssertFilterDoesNotChange(@"<span style='FONT-FAMILY: ""Arial, sans-serif"";'>Arial</span>");
		}
		
		[Test]
		public void TestMarginStyleNoSemi()
		{
			AssertFilterDoesNotChange(@"<p style=""MARGIN: 0in 0in 0pt"">test</p>");
		}
		
		[Test]
		public void TestDuplicateIdAllowed()
		{
			AssertFilterDoesNotChange(@"<a id=""_ctrl5a_myDataList__ctrl0a_Description"" href=""/foo?id=bar"">test</a><span id=""_ctrl5a_myDataList__ctrl0a_Description"">test2</span>");
		}
		
		[Test]
		public void TestRelativeUrl()
		{
			AssertFilterDoesNotChange(@"<a href=""/foo?bar=spam"">test</a>");
		}
		
		[Test]
		public void TestEmptyStyle()
		{
			AssertFilterDoesNotChange(@"<span style="""">test</span>");
		}
				
		[Test]
		public void TestXmp()
		{
			AssertFilterDoesNotChange(@"<xmp class=""Example"">An example</xmp>");
		}
		
		[Test]
		public void TestHRInH1()
		{
			AssertFilterDoesNotChange(@"<h1>Header followed by horizontal rule<hr/></h1>");
		}
		
		[Test]
		public void TestImgSrc()
		{
            AssertFilterThrowsXmlSchemaException(@"<img src=""&#x6A;&#x61;&#x76;&#x61;&#x73;&#x63;&#x72;&#x69;&#x70;&#x74;:alert('TestImgSrc');""/>");
		}
				
		[Test]
		public void TestMissingRequiredAttribute()
		{
            AssertFilterThrowsXmlSchemaException(@"<img />");
		}
		
		[Test]
		public void TestOnClick()
		{
            AssertFilterThrowsXmlSchemaException(@"<a href=""#"" onclick=""&#x6A;&#x61;&#x76;&#x61;&#x73;&#x63;&#x72;&#x69;&#x70;&#x74;:document.body.appendChild(document.createTextNode('${xssTestId}_A_ONCLICK_HEX.'));"">TestOnClick</a>");
		}
		
		[Test]
		public void TestSpanNotAllowed()
		{
            AssertFilterThrowsXmlSchemaException(@"<hr><span>span not allowed here</span></hr>");
		}
		
		[Test]
		public void TestOtherNamespacesNotAllowed()
		{
            AssertFilterThrowsXmlSchemaException(@"<span xmlns:xs=""http://www.w3.org/2001/XMLSchema""><xs:element name=""rt"" type=""rt.type""/></span>");
		}
		
		[Test]
		[ExpectedException(typeof(XmlException))]
		public void TestDeclarationsNotAllowed()
		{
			Filter.FilterFragment(@"<!ENTITY % HTMLlat1 PUBLIC
			                         ""-//W3C//ENTITIES Latin 1 for XHTML//EN""
			                         ""http://www.w3.org/MarkUp/DTD/xhtml-lat1.ent"">");
		}
		
		[Test]
		public void TestExternalSchemasNotAllowed()
		{
            AssertFilterThrowsXmlSchemaException(@"<test:elem xmlns:test=""urn:test"" xsi:schemaLocation=""urn:test http://www.brettle.com/Data/Sites/1/test.xsd"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">Test</test:elem>");
		}
		
		[Test]
		public void TestInternalSchemasNotAllowed()
		{
            AssertFilterThrowsXmlSchemaException(@"<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""urn:test"" xmlns=""urn:test""><xs:element name=""elem"" type=""xs:string"" /></xs:schema><test:elem xmlns:test=""urn:test"">Test</test:elem>");
		}
		
		private void AssertFilteredIsEqual(string fragment, string expected)
		{
			string actual = Filter.FilterFragment(fragment);
			Assert.AreEqual(expected, actual, "Full actual = " + actual);
		}			
		
		private void AssertFilterDoesNotChange(string fragment)
		{
			string actual = Filter.FilterFragment(fragment);
			Assert.AreEqual(fragment, actual, "Full actual = " + actual);
		}			
		
        private void AssertFilterThrowsXmlSchemaException(string fragment)
        {
            try
            {
                Filter.FilterFragment(fragment);
                Assert.Fail("XmlSchemaException not thrown");
            }
            catch (XmlSchemaException)
            {
                // Expected
            }
        }
    }
}
