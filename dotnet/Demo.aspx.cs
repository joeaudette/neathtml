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

namespace Brettle.Web.NeatHtml
{
	public class Demo : System.Web.UI.Page
	{	
		protected HtmlTextArea textarea;
		protected Button submitButton;
		protected UntrustedContent untrustedContent;
		
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
			if (!IsPostBack || textarea.Value.Length == 0)
			{
				StringWriter sw = new StringWriter();
				HtmlTextWriter htw = new HtmlTextWriter(sw);
				for (int i = 0; i < untrustedContent.Controls.Count; i++)
				{
					untrustedContent.Controls[i].RenderControl(htw);
				}
				htw.Close();
				textarea.Value = sw.ToString();
			}
			submitButton.Click += new System.EventHandler(this.Button_Clicked);
			// Works around a Mono bug (http://bugzilla.ximian.com/show_bug.cgi?id=78948) which causes
			// the textarea content to render without being encoded.
			textarea.InnerText = textarea.Value;
		}

		private void Button_Clicked(object sender, EventArgs e)
		{
			string html = textarea.Value;
			if (Environment.Version.Major < 2)
			{
				html = HttpUtility.HtmlDecode(html);
			}
			untrustedContent.Controls.Clear();
			untrustedContent.Controls.Add(new LiteralControl(html));
		}
	}
}
