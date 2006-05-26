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
using NUnit.Core;
using System.IO;
using System.Web;
using System.Xml;
using System.Xml.Schema;

namespace Brettle.Web.NeatHtml.UnitTests
{
	public class XssFilterTest : NUnit.Core.TestCase
	{
		public XssFilterTest(FileInfo testFile, bool exceptionExpected)
			: base("Brettle.Web.NeatHtml.UnitTests.XssTest", testFile.Name)
		{
			TestFile = testFile;
			ExceptionExpected = exceptionExpected;
		}
		
		private FileInfo TestFile;
		private bool ExceptionExpected;
		private new XssFilter Filter;
				
		public override void Run(TestCaseResult result)
		{
			bool exceptionExpected = false;
			try
			{
				DirectoryInfo currentDir = new DirectoryInfo(System.Environment.CurrentDirectory);
				string schemaLocation = Path.Combine(currentDir.Parent.Parent.Parent.Parent.FullName, "schema");
				schemaLocation = Path.Combine(schemaLocation, "NeatHtml.xsd");
				Filter = XssFilter.GetForSchema(schemaLocation);
				
				string fragment = null;
				string expected = null;
				string actual = null;
				
				using (TextReader reader = TestFile.OpenText())
				{
					fragment = reader.ReadToEnd();
				}
										
				string expectedFileName = TestFile.FullName + ".expected";
				if (File.Exists(expectedFileName))
				{
					using (TextReader reader = File.OpenText(expectedFileName))
					{
						expected = reader.ReadToEnd();
					}
				}
				else
				{
					expected = fragment;
				}
				
				exceptionExpected = ExceptionExpected;
				actual = Filter.FilterFragment(fragment);
				using (StreamWriter writer = File.CreateText(TestFile.FullName + ".actual"))
				{
					writer.Write(actual);
				}
				
				if (exceptionExpected)
				{
					exceptionExpected = false;
					NUnit.Framework.Assert.Fail("expected exception not thrown.  Full actual = " + actual);
				}
					
				NUnit.Framework.Assert.AreEqual(expected, actual, "Full actual = " + actual);
				result.Success();
			}
			catch (Exception ex)
			{
				if (exceptionExpected)
				{
					result.Success();
				}
				else
				{
					if (ex is NunitException)
					{
						ex = ex.InnerException;
					}
					result.Failure(ex.Message, ex.StackTrace);
				}
			}
		}
		
		[Suite]
		public static TestSuite Suite
		{
			get
			{
				DirectoryInfo currentDir = new DirectoryInfo(System.Environment.CurrentDirectory);
				string testsLocation = Path.Combine(currentDir.Parent.Parent.Parent.Parent.FullName, "tests");
				TestSuite suite = new TestSuite("XssTestSuite");
				FileInfo[] validTestFiles = GetTestFiles(Path.Combine(testsLocation, "valid"));
				foreach (FileInfo testFile in validTestFiles)
				{
					suite.Add(new XssFilterTest(testFile, false));
				}
				FileInfo[] invalidTestFiles = GetTestFiles(Path.Combine(testsLocation, "invalid"));
				foreach (FileInfo testFile in invalidTestFiles)
				{
					suite.Add(new XssFilterTest(testFile, true));
				}
				return suite;
			}
		}
		
		private static FileInfo[] GetTestFiles(string testsLocation)
		{
			DirectoryInfo testsDir = new DirectoryInfo(testsLocation);
			return testsDir.GetFiles("*.txt");
		}
	}
}
