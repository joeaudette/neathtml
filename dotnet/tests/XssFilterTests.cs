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
		public void TestJavaScriptUri()
		{
			Assert.AreEqual(@"<a href="""" xmlns=""http://www.w3.org/1999/xhtml"">test link</a>",
			                Filter.FilterFragment("<a href='javascript:alert(\"Hello\");'>test link</a>"));
		}
		
		[Test]
		public void TestObject()
		{
			string actual = Filter.FilterFragment("<object>fallback content</object>");
			Assert.AreEqual(@"<span>fallback content</span>", actual, actual);
		}
	}
}
