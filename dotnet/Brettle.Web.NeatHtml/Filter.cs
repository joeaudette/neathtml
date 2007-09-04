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
using System.Text.RegularExpressions;
using System.Web;

namespace Brettle.Web.NeatHtml
{
	public class Filter
	{
		public static Filter GetByName(string clientSideName)
		{
			return new Filter(clientSideName);
		}

		public static Filter DefaultFilter = GetByName("NeatHtml.DefaultFilter");
		
		private Filter(string clientSideName)
		{
			ClientSideName = clientSideName;
		}
		
		public string ClientSideName;
		public string NoScriptDownlevelIEWidth = "100%";
		public string NoScriptDownlevelIEHeight = "400px";
		
		public string FilterFragment(string origHtmlFragment)
		{
			string preprocessedFragment = PreprocessingRE.Replace(origHtmlFragment, new MatchEvaluator(Preprocess));
			return String.Format(Format, ClientSideName, preprocessedFragment, NoScriptDownlevelIEWidth, NoScriptDownlevelIEHeight);
		} 

		private static string[] tagsAllowedWhenScriptDisabled
			= { "a", "abbr", "acronym", "address", "b", "basefont", "bdo", "big", "blockquote", "br",
		 		"caption", "center", "cite", "code", "col", "colgroup", "dd", "del", "dfn", "dir", "div", "dl", "dt", 
		 		"em", "font", "h1", "h2", "h3", "h4", "h5", "h6", "hr", "i", "ins", "kbd", "li", "ol", "p", "pre", "q",
		 		"s", "samp", "small", "span", "strike", "strong", "sub", "sup", "table", "tbody", "td", "tfoot", "th",
		 		"thead", "tr", "tt", "u", "ul", "var"
			};
			
		private static Regex PreprocessingRE 
			= new Regex("(<table)|(</table)|(<!--([^-]*-)+-[^>]*>)|(--)|((</?)(?!(" 
							+ String.Join("|", tagsAllowedWhenScriptDisabled) + ")[ \t\r\n/>])([_a-z]))",
						RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

		private string Preprocess(Match m)
		{
			if (m.Groups[1].Success)
				return @"<NeatHtmlParserReset single='' double=""""></NeatHtmlParserReset></td></tr></table><table";
			else if (m.Groups[2].Success)
				return @"<NeatHtmlParserReset single='' double=""""></table><table style='border-spacing: 0;'><tr><td style='padding: 0;'";
			else if (m.Groups[3].Success)
				return "";
			else if (m.Groups[5].Success)
				return "&#45;&#45;";
			else if (m.Groups[6].Success)
				return m.Groups[7].Value + "NeatHtmlReplace_" + m.Groups[9].Value;
			else
				return ""; // Should never get here			
		}

		private static string Format = @"<![if gte IE 7]>
	<div class='NeatHtml' style='overflow: hidden; position: relative; border: none; padding: 0; margin: 0;'>
<![endif]>
<!--[if lt IE 7]>
	<div class='NeatHtml' style='width: {2}; height: {3}; overflow: auto; position: relative; border: none; padding: 0; margin: 0;'>	
<![endif]-->
		<table style='border-spacing: 0;'><tr><td style='padding: 0;'><!-- test comment --><script type='text/javascript'>// <![CDATA[
			try {{ {0}.BeginUntrusted(); }} catch (ex) {{ document.writeln('NeatHtml not found<!-' + '-'); }} // ]]></script><div>
			{1}
			<xmp></xmp><!-- ' "" ]]> --></td></tr></table>
	</div><script type='text/javascript'>// <![CDATA[
		{0}.ProcessUntrusted();
	// ]]></script>";
		
	}
}
