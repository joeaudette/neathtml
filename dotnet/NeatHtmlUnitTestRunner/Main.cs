// project created on 5/27/2006 at 12:05 AM
using System;
using System.IO;
using System.Web;
using System.Xml;
using System.Xml.Schema;
using Brettle.Web.NeatHtml;

namespace NeatHtmlUnitTestRunner
{
	class MainClass
	{
		public static bool RunTest(FileInfo testFile, bool exceptExpection)
		{
			bool exceptionExpected = false;
			try
			{
				DirectoryInfo currentDir = new DirectoryInfo(System.Environment.CurrentDirectory);
				string schemaLocation = Path.Combine(currentDir.Parent.Parent.Parent.Parent.FullName, "schema");
				schemaLocation = Path.Combine(schemaLocation, "NeatHtml.xsd");
				XssFilter filter = XssFilter.GetForSchema(schemaLocation);
				
				string fragment = null;
				string expected = null;
				string actual = null;
				
				using (TextReader reader = testFile.OpenText())
				{
					fragment = reader.ReadToEnd();
				}
										
				string expectedFileName = testFile.FullName + ".expected";
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
				
				exceptionExpected = exceptExpection;
				actual = filter.FilterFragment(fragment);
				using (StreamWriter writer = File.CreateText(testFile.FullName + ".actual"))
				{
					writer.Write(actual);
				}
				
				if (exceptionExpected)
				{
					exceptionExpected = false;
					Console.Error.WriteLine();
					Console.Error.WriteLine(testFile.FullName + " FAILED");
					Console.Error.WriteLine("Expected exception not thrown.  Full actual = " + actual);
					return false;
				}
					
				if (expected != actual)
				{
					Console.Error.WriteLine();
					Console.Error.WriteLine(testFile.FullName + " FAILED");
					Console.Error.WriteLine("Expected:");
					Console.Error.WriteLine(expected);
					Console.Error.WriteLine("Actual:");
					Console.Error.WriteLine(actual);
					return false;
				}
				return true;
			}
			catch (Exception ex)
			{
				if (exceptionExpected)
				{
					return true;
				}
				else
				{
					Console.Error.WriteLine();
					Console.Error.WriteLine(testFile.FullName + " FAILED");
					Console.Error.WriteLine(ex.Message);
					Console.Error.WriteLine(ex.StackTrace);
					return false;
				}
			}
		}
				
		private static FileInfo[] GetTestFiles(string testsLocation)
		{
			DirectoryInfo testsDir = new DirectoryInfo(testsLocation);
			return testsDir.GetFiles("*.txt");
		}
	
		public static void Main(string[] args)
		{
			DirectoryInfo currentDir = new DirectoryInfo(System.Environment.CurrentDirectory);
			string testsLocation = Path.Combine(currentDir.Parent.Parent.Parent.Parent.FullName, "tests");
			FileInfo[] validTestFiles = GetTestFiles(Path.Combine(testsLocation, "valid"));
			int numTestsPassed = 0;
			int numTestsFailed = 0;
			foreach (FileInfo testFile in validTestFiles)
			{
				if (RunTest(testFile, false))
				{
					numTestsPassed++;
				}
				else
				{
					numTestsFailed++;
				}
			}
			FileInfo[] invalidTestFiles = GetTestFiles(Path.Combine(testsLocation, "invalid"));
			foreach (FileInfo testFile in invalidTestFiles)
			{
				if (RunTest(testFile, true))
				{
					numTestsPassed++;
				}
				else
				{
					numTestsFailed++;
				}
			}
			Console.WriteLine(numTestsFailed + " tests failed, " + numTestsPassed + " tests passed");
			if (numTestsFailed != 0) 
			{
				System.Environment.Exit(-1);
			}
		}
	}
}