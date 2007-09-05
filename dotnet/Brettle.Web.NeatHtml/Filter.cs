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
		
		public string FilterUntrusted(string untrusted)
		{
			string markupJailed = MarkupJailRE.Replace(untrusted, new MatchEvaluator(GuardMarkupJail));
			// To prevent url() in inline styles (a potential CSRF vector), do this:
			string preprocessed = StyleAttributeRE.Replace(markupJailed, new MatchEvaluator(DisableSuspiciousStyles));
			return String.Format(Format, ClientSideName, preprocessed, NoScriptDownlevelIEWidth, NoScriptDownlevelIEHeight);
		}
		
		private static string[] tagsAllowedWhenNoScript
			= { "a", "abbr", "acronym", "address", "b", "basefont", "bdo", "big", "blockquote", "br",
		 		"caption", "center", "cite", "code", "col", "colgroup", "dd", "del", "dfn", "dir", "div", "dl", "dt", 
		 		"em", "font", "h1", "h2", "h3", "h4", "h5", "h6", "hr", "i", "ins", "kbd", "li", "ol", "p", "pre", "q",
		 		"s", "samp", "small", "span", "strike", "strong", "sub", "sup", "table", "tbody", "td", "tfoot", "th",
		 		"thead", "tr", "tt", "u", "ul", "var",
		 		"script" // OK when script is disabled.  Hides script source from user.
			};
		
		private static Regex MarkupJailRE 
			= new Regex("(<table)|(</table)|(<!--([^-]*-)+-[^>]*>)|(--)|(<(?!/?(" 
							+ String.Join("|", tagsAllowedWhenNoScript) + ")[ \t\r\n/>])([!\\?/])?([a-z])?)",
						RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

		private string GuardMarkupJail(Match m)
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
			{
				if (m.Groups[9].Success)
				{
					// Looks like the start of a tag.  It might be safe when javascript
					// is enabled.  Prepend NeatHtmlReplace_ to the tag name so that NeatHtml.js can still see it.
					return "<" + m.Groups[8].Value + "NeatHtmlReplace_" + m.Groups[9].Value;
				}
				else
				{
					// Not a tag, so let it go.
					return m.Groups[6].Value;
				}
			}
			else
				return ""; // Should never get here			
		}

		// Style value whitelist.  Note: '&' '\' and '(' [except 'rgb('] are not on it.[\\r\\n\\t !#$%)-~]]
		private static string StyleValueREString = "\\((?<=rgb\\()|[\\r\\n\\t !#$%)-[\\]-~]"; 
		private static Regex StyleAttributeRE
			= new Regex("style[^=a-z0-9 \\t\\r\\n]*=(?!(" // "style=" (roughly) followed by something other than
								+ "\"('|" + StyleValueREString + ")*\"" // "value"
								+ "|'(\"|" + StyleValueREString + ")*'" // or 'value'
							+ ")([ \\r\\n\\t]|/?>))", // value must be followed by whitespace or end of tag  
						RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);
						
		private string DisableSuspiciousStyles(Match m)
		{
			return m.Value.Substring(0,3) + "&#" + ((int)m.Value[3]) + ";" + m.Value.Substring(4, m.Value.Length - 4);
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
			<xmp></xmp><!-- ' "" ]]> --><script></script></td></tr></table>
	</div><script type='text/javascript'>// <![CDATA[
		{0}.ProcessUntrusted();
	// ]]></script>";
		
	}
}
