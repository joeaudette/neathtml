/*

NeatUpload - an HttpModule and User Controls for uploading large files
Copyright (C) 2005  Dean Brettle

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
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using System.IO;
using System.Text.RegularExpressions;

namespace Brettle.Web.NeatHtml
{
	public class Demo : System.Web.UI.Page
	{	
		protected HtmlTextArea actualFilteredContentTextarea;
		protected HtmlTextArea expectedFilteredContentTextarea;
		protected HtmlTextArea testContentTextarea;
		protected Button submitButton;
		protected UntrustedContent untrustedContent;
		protected HtmlSelect selectedTest;
		protected HtmlInputCheckBox checkFilteredContent;
		
		protected override void OnInit(EventArgs e)
		{
			InitializeComponent();
			base.OnInit(e);
		}
		
		private void InitializeComponent()
		{
			this.Load += new System.EventHandler(this.Page_Load);
		}
		
		private void Page_Load(object sender, EventArgs e)
		{
			string testName = Request.Params["selectedTest"];
			if (testName == null && testContentTextarea.Value.Length == 0)
			{
				testName = "Default Test";
			}
			if (testName != null)
			{
				testName = Regex.Replace(testName, "[^A-Za-z0-9 ]", "_");
				string testPath = Path.Combine("tests", testName);
				if (File.Exists(testPath))
					testContentTextarea.InnerText = testContentTextarea.Value = ReadAllText(testPath);
				string expectedPath = Path.Combine("tests", testName + ".expected");
				if (Request.Params["NoScript"] == "true")
				{
					string noscriptExpectedPath = Path.Combine("tests", testName + ".noscript.expected");
					if (File.Exists(noscriptExpectedPath))
						expectedPath = noscriptExpectedPath;
				}
				if (File.Exists(expectedPath))
				{
					expectedFilteredContentTextarea.InnerText = expectedFilteredContentTextarea.Value 
						= ReadAllText(expectedPath);
					checkFilteredContent.Checked = true;
				}
				else
				{
					expectedFilteredContentTextarea.InnerText = expectedFilteredContentTextarea.Value = "";
					checkFilteredContent.Checked = false;
				}
			}

			string html = testContentTextarea.Value;
			untrustedContent.Controls.Clear();
			untrustedContent.Controls.Add(new LiteralControl(html));

			StringWriter sw = new StringWriter();
			HtmlTextWriter htw = new HtmlTextWriter(sw);
			untrustedContent.RenderControl(htw);
			htw.Close();
			string actualFilteredContent = sw.ToString();
			
			// For consistency with what NeatHtml.js will produce, we only want the <div> and it's contents.
			int startOfDiv = actualFilteredContent.IndexOf("<div>");
			int endOfDiv = actualFilteredContent.LastIndexOf("</div>", actualFilteredContent.LastIndexOf("</div>")) + 6;
			actualFilteredContent = actualFilteredContent.Substring(startOfDiv, endOfDiv - startOfDiv);
			
			actualFilteredContentTextarea.InnerText = actualFilteredContentTextarea.Value = actualFilteredContent;			

			if (!IsPostBack)
			{
				string[] testFilePaths = Directory.GetFiles("tests");
				Array.Sort(testFilePaths);
				for (int i = 0; i < testFilePaths.Length; i++)
				{
					if (testFilePaths[i].EndsWith(".expected"))
					{
						continue;
					}
					selectedTest.Items.Insert(selectedTest.Items.Count - 1, 
											new ListItem(Path.GetFileName(testFilePaths[i])));
				}
				selectedTest.SelectedIndex = 0;
			}
		}

		private string ReadAllText(string path)
		{
			StreamReader r = File.OpenText(path);
			string s = r.ReadToEnd();
			r.Close();
			return s;
		}
	}
}
