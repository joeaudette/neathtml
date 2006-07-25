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
using System.Xml;
using System.Xml.Schema;
using System.IO;

namespace Brettle.Web.NeatHtml
{
	public class Demo : System.Web.UI.Page
	{	
		protected HtmlForm form;
		protected HtmlTextArea textarea;
		protected Button submitButton;
		protected Button cancelButton;
		protected HtmlGenericControl outputDiv;
		
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
			submitButton.Click += new System.EventHandler(this.Button_Clicked);
			// Works around a Mono bug (http://bugzilla.ximian.com/show_bug.cgi?id=78948) which causes
			// the textarea content to render without being encoded.
			textarea.InnerText = textarea.Value;
		}

		private void Button_Clicked(object sender, EventArgs e)
		{
			FileInfo currentFile = new FileInfo(Request.PhysicalPath);
			string schemaLocation = Path.Combine(currentFile.Directory.Parent.FullName, "schema");
			schemaLocation = Path.Combine(schemaLocation, "NeatHtml.xsd");
			XssFilter filter = XssFilter.GetForSchema(schemaLocation);
			
			try
			{
				string html = textarea.Value;
				if (Environment.Version.Major < 2)
				{
					html = HttpUtility.HtmlDecode(html);
				}
				filter.FilterFragment(html);
				outputDiv.InnerHtml = html;
			}
			catch (Exception ex)
			{
				outputDiv.InnerHtml = "<span style='color: red;'>" + ex.Message + "</span><br/>";
				
				int lineNumber = -1;
				int linePosition = -1;
				XmlException xmlEx = ex as XmlException;
				if (xmlEx != null)
				{
					lineNumber = xmlEx.LineNumber-1;
					linePosition = xmlEx.LinePosition;
				}
				XmlSchemaException schemaEx = ex as XmlSchemaException;
				if (schemaEx != null)
				{
					lineNumber = schemaEx.LineNumber-1;
					linePosition = schemaEx.LinePosition;
				}
				if (lineNumber > 0)
				{
					int position = 0;
					for (int i = 0; i < lineNumber - 1; i++)
					{
						position = textarea.Value.IndexOf("\n", position);
						position = position + 1;
					}
					position -= (lineNumber - 1); // Because newlines aren't counted when javascript moves to the position
					position += linePosition - 1;
					RegisterStartupScript("moveto-error", "<script>NeatHtml_MoveTo('textarea', " + position + ");</script>");
				}				
			}
		}
	}
}
