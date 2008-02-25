/*

NeatHtml- Fighting XSS with JavaScript Judo
Copyright (C) 2007  Dean Brettle

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
using System.Web.UI.Design;
using System.ComponentModel;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using System.IO;
using System.Security.Permissions;
using System.Drawing.Design;

namespace Brettle.Web.NeatHtml
{	
	/// <summary>
	/// Renders it's content using NeatHtml.js to fight XSS and other attacks.
	/// </summary>
	/// <remarks>
	/// Tables that are not at the top-level of the content may cause the content to not display properly
	/// for users without javascript.
	/// </remarks>
	[AspNetHostingPermissionAttribute (SecurityAction.LinkDemand, Level = AspNetHostingPermissionLevel.Minimal)]
	[AspNetHostingPermissionAttribute (SecurityAction.InheritanceDemand, Level = AspNetHostingPermissionLevel.Minimal)]
	[ParseChildren(false)]
	[PersistChildren(true)]
	public class UntrustedContent : System.Web.UI.WebControls.WebControl
	{
		/// <summary>
		/// The name of the filter in client-side script.
		/// </summary>
		/// <remarks>
		/// You can create and configure multiple filters in client-side script.
		/// </remarks>
		[DefaultValue("NeatHtml.DefaultFilter")]
		public string ClientSideFilterName
		{
			get { return (ViewState["ClientSideFilterName"] != null 
				              ? (string)ViewState["ClientSideFilterName"] : "NeatHtml.DefaultFilter"); }
			set { ViewState["ClientSideFilterName"] = value; }
		}
		
		/// <summary>
		/// Enables/disables support for displaying TABLE elements to users that have JavaScript disabled.
		/// </summary>
		/// <remarks>
		/// Due to the complexity of supporting tables for no-script users, enabling this support could be a
		/// security risk.  Only set this to true if you need well-formed tables in the untrusted content to
		/// be displayed properly to no-script users.
		/// </remarks>
		[DefaultValue(false)]
		public bool SupportNoScriptTables
		{
			get { return (ViewState["SupportNoScriptTables"] != null && (bool)ViewState["SupportNoScriptTables"]); }
			set { ViewState["SupportNoScriptTables"] = value; }
		}
		
		/// <summary>
		/// The maximum number of "<" characters (including in tags), "&" characters (including entities), 
		/// attributes, and style properties, combined, that are allowed in the untrusted content.
		/// </summary>
		/// <remarks>
		/// This limits the effectiveness of Denial of Service attacks that use pathological untrusted content
		/// to increase processing time.  If you expect benign content to contain many entities,
		/// or complex markup, you might want to increase this value.  If you expect benign content to contain 
		/// relatively few entities and little markup, you might want to decrease this value.
		/// </remarks>
		[DefaultValue(10000)]
		public int MaxComplexity
		{
			get { return (ViewState["MaxComplexity"] != null ? (int)ViewState["MaxComplexity"] : 10000); }
			set { ViewState["MaxComplexity"] = value; }
		}

		private bool IsDesignTime = (HttpContext.Current == null);

		// This is used to ensure that the browser gets the latest NeatHtml.js each time this assembly is
		// reloaded.  Strictly speaking the browser only needs to get the latest when NeatHtml.js changes,
		// but computing a hash on that file everytime this assembly is loaded strikes me as overkill.
		private static Guid CacheBustingGuid = System.Guid.NewGuid();

		private string AppPath
		{
			get 
			{
				string appPath = Context.Request.ApplicationPath;
				if (appPath == "/")
				{
					appPath = "";
				}
				return appPath;
			}
		}
				
		/// <summary>
		/// URL of NeatHtml.js.</summary>
		/// <remarks>
		/// If the URL starts with "~", the "~" will be replaced with the web application root as returned by
		/// <see cref="HttpRequest.ApplicationPath"/>.  By default, "~/NeatHtml/NeatHtml.js" will be used.</remarks>
		[Editor(typeof(UrlEditor), typeof(UITypeEditor)), Bindable(true), DefaultValue("~/NeatHtml/NeatHtml.js")]
		public string ClientScriptUrl
		{
			get { return (string)ViewState["ClientScriptUrl"]; }
			set { ViewState["ClientScriptUrl"] = value; }
		}
				
		internal static string ApplyAppPathModifier(string url)
		{
			string appPath = HttpContext.Current.Request.ApplicationPath;
			if (appPath == "/")
			{
				appPath = "";
			}
			string requestUrl = HttpContext.Current.Request.RawUrl;
			string result = HttpContext.Current.Response.ApplyAppPathModifier(url);
			
			// Workaround Mono XSP bug where ApplyAppPathModifier() doesn't add the session id
			if (requestUrl.StartsWith(appPath + "/(") && !result.StartsWith(appPath + "/("))
			{
				if (url.StartsWith("/") && url.StartsWith(appPath))
				{
					url = "~" + url.Remove(0, appPath.Length);
				}
				if (url.StartsWith("~/"))
				{
					string[] compsOfPathWithinApp = requestUrl.Substring(appPath.Length).Split('/');
					url = appPath + "/" + compsOfPathWithinApp[1] + "/" + url.Substring(2);
				}
				result = url;
			}
			return result;
		}
		
		protected override void OnPreRender (EventArgs e)
		{
			if (!IsDesignTime)
			{
				if (ClientScriptUrl == null)
				{
					ClientScriptUrl = "~/NeatHtml/NeatHtml.js";
				}
				if (!Page.IsClientScriptBlockRegistered("NeatHtmlJs"))
				{
					Page.RegisterClientScriptBlock("NeatHtmlJs", @"
	<script type='text/javascript' language='javascript' src='" + ApplyAppPathModifier(ClientScriptUrl) + @"?guid=" 
		+ CacheBustingGuid + @"'></script>");
				}
			}
			base.OnPreRender(e);
		}


		protected override void Render(HtmlTextWriter writer)
		{
			// Render the content of this control to a string
			StringWriter sw = new StringWriter();
			System.Reflection.ConstructorInfo constructor 
				= writer.GetType().GetConstructor(new Type[] { sw.GetType() });
			HtmlTextWriter htw = constructor.Invoke(new object[] {sw}) as HtmlTextWriter;
			base.RenderChildren(htw);
			htw.Close();

			// Filter the string and write the result
			Filter f = new Filter();
			f.ClientSideFilterName = ClientSideFilterName;
			f.SupportNoScriptTables = SupportNoScriptTables;
			f.MaxComplexity = MaxComplexity;
			writer.Write(f.FilterUntrusted(sw.ToString()));
		}
	}
}
