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
			return LayoutJail.Jail(ScriptJail.Jail(MarkupJail.Jail(untrusted), ClientSideName), 
									ClientSideName, NoScriptDownlevelIEWidth, NoScriptDownlevelIEHeight);
		}
	}
	
	internal class LayoutJail
	{
		internal static string Jail(string untrusted, string clientSideName, string noScriptDownlevelIEWidth, string noScriptDownlevelIEHeight)
		{
			string layoutJailed = LayoutJailRE.Replace(untrusted, new MatchEvaluator(LayoutJail.GuardJail));
			return String.Format(Format, clientSideName, layoutJailed, noScriptDownlevelIEWidth, noScriptDownlevelIEWidth);
		}

		// Style value whitelist.  Note: '&' '\' and '(' [except 'rgb('] are not on it.[\\r\\n\\t !#$%)-~]]
		// We blacklist "counter-increment:" and "counter-reset:" instead of trying to whitelist all
		// propnames and values.
		private static string StyleValueREString = ":(?<!counter-(?:increment|reset)[\\r\\n\\t ]*:)|\\((?<=rgb\\()|[\\r\\n\\t !#$%)-9;-[\\]-~]"; 
		private static Regex LayoutJailRE
			= new Regex("style[^=a-z0-9]*=(?!([ \\r\\n\\t]*(" // "style=" (roughly) followed by something other than
								+ "\"('|" + StyleValueREString + ")*\"" // "value"
								+ "|'(\"|" + StyleValueREString + ")*'" // or 'value'
							+ ")([ \\r\\n\\t]|/?>)))", // value must be followed by whitespace or end of tag  
						RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);
						
		private static string GuardJail(Match m)
		{
			return m.Value.Substring(0,3) + "&#" + ((int)m.Value[3]) + ";" + m.Value.Substring(4, m.Value.Length - 4);
		}

		private static string Format = "\n"
			+ "<![if gte IE 7]>\n"
			+ "<div class='NeatHtml' style='overflow: hidden; position: relative; border: none; padding: 0; margin: 0;'>\n"
			+ "<![endif]>\n"
			+ "<!--[if lt IE 7]>\n"
			+ "<div class='NeatHtml' style='width: {2}; height: {3}; overflow: auto; position: relative; border: none; padding: 0; margin: 0;'>\n"
			+ "<![endif]-->\n"
			+ "{1}\n"
			+ "</div><script type='text/javascript'>// <![CDATA[ \n"
			+ "{0}.ResizeContainer();\n"
			+ "// ]]></script>\n";
	}
	
	internal class ScriptJail
	{
		internal static string Jail(string untrusted, string clientSideName)
		{
			string scriptJailed = ScriptJailRE.Replace(untrusted, new MatchEvaluator(GuardJail));
			return String.Format(Format, clientSideName, scriptJailed);
		}
		
		private static Regex ScriptJailRE 
			= new Regex(  "(<!--(?:[^-]*-)+-[^>]*>)"  // 1: HTML Comment
						+ "|(--)"                   // 2: "--" we need to encode to prevent ending a comment prematurely
						+ "|(<(/)?(xmp))",           // 3: xmp tag
						RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

		private static string GuardJail(Match m)
		{
			if (m.Groups[1].Success)
				return "";
			else if (m.Groups[2].Success)
				return "&#45;&#45;";
			else if (m.Groups[3].Success)
				return "<" + m.Groups[4].Value + "NeatHtmlReplace_" + m.Groups[5].Value;
			else
				return "";  // Should never get here
		}

		private static string Format = "<!-- test comment --><script type='text/javascript'>// <![CDATA[ \n"
			+ "try {{ {0}.BeginUntrusted(); }} catch (ex) {{ document.writeln('NeatHtml not found<!-' + '-'); }} // ]]></script>\n"
			+ "{1}\n"
			+ "<xmp></xmp><!-- ' \" ]]> --><script type='text/javascript'>// <![CDATA[ \n"
			+ "{0}.ProcessUntrusted();\n"
			+ "// ]]></script>";
	}
	
	internal class MarkupJail
	{
	
		internal static string Jail(string untrusted)
		{
			MarkupJail jail = new MarkupJail();
			string markupJailed = MarkupJailRE.Replace(untrusted, new MatchEvaluator(jail.GuardJail));
			// If any untrusted tables are still open, close any open attributes and tags
			// and then close all the tables.
			if (jail.UntrustedTables > 0)
			{
				markupJailed += ParserResetString;
				while (jail.UntrustedTables-- > 0)
					markupJailed += "</table>";
			}
			return String.Format(Format, markupJailed);
		}
		
		private static string ParserResetString = "<NeatHtmlParserReset s='' d=\"\" /><script></script>";

		private static string Format 
			= "<table style='border-spacing: 0;'><tr><td style='padding: 0;'>{0}" + ParserResetString + "</td></tr></table>";

		private static string[] tagsAllowedWhenNoScript
			= { "a", "abbr", "acronym", "address", "b", "basefont", "bdo", "big", "blockquote", "br",
		 		"caption", "center", "cite", "code", "col", "colgroup", "dd", "del", "dfn", "dir", "div", "dl", "dt", 
		 		"em", "font", "h1", "h2", "h3", "h4", "h5", "h6", "hr", "i", "ins", "kbd", "li", "ol", "p", "pre", "q",
		 		"s", "samp", "small", "span", "strike", "strong", "sub", "sup", "table", "tbody", "td", "tfoot", "th",
		 		"thead", "tr", "tt", "u", "ul", "var",
		 		"script" // OK when script is disabled.  Hides script source from user.
		 		// Do NOT allow "iframe" or "object", unless you are willing to track them like with tables.
			};
		
		private static Regex MarkupJailRE 
			= new Regex("(<table"                  // 1: Matches any potential table start tag
							                       // 2: The following line will only match if this is a valid table start tag
							+ "((?:[ \\t\\n\\r]+[A-Z:_a-z][A-Z:_a-z0-9._]*[ \\t\\n\\r]*(?:=(?:\"[^<\"]*\"|'[^<']*'))?)*[ \\t\\n\\r]*>)?"
						+ ")"
						+ "|(</table"               // 3: Matches any potential table end tag
							+ "([ \\t\\n\\r]*>)?"   // 4: Matches if valid table end tag
						+ ")"
						+ "|(<!--(?:[^-]*-)+-[^>]*>)" // 5: HTML Comment
						+ "|(?:<!\\[CDATA\\[((?:[^\\]]*\\])+)\\]>)" // 6: CDATA section
						+ "|(<"                     // 7: Matches tags "<" when followed by:
													// 8: a tag name that is not allowed
							+ "(?!/?(" + String.Join("|", tagsAllowedWhenNoScript) + ")[ \t\r\n/>])"
							+ "([!\\?/])?"			// 9: starting with ! ? or / (ie. <!, <?, or </)
							+ "([a-z])?"			// 10: or a letter (ie. <X)
						+ ")",
						RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

		private string GuardJail(Match m)
		{
			if (m.Groups[1].Success)
			{
				if (m.Groups[2].Success) // valid table start tag
				{
					UntrustedTables++;
					// Close any open attribute values and tags and then start the table as requested
					return ParserResetString + m.Groups[1].Value;
				}
				else // suspicious table start tag
				{
					// Hide it from the browser's parser so our markup jail is not affected.
					// It will be recovered by NeatHtml.js.
					return "<NeatHtmlReplace_" 
						+ m.Groups[1].Value.Substring(1, "table".Length); // Preserve case
				}
			}
			else if (m.Groups[3].Success)
			{
				if (m.Groups[4].Success && UntrustedTables > 0) // valid table end tag and some tables to close
				{
					UntrustedTables--;
					// Close any open attribute values and tags and then end the table as requested
					return ParserResetString + m.Groups[3].Value;
				}
				else // suspicious table end tag
				{
					// Hide it from the browser's parser so our markup jail is not affected.
					// It will be recovered by NeatHtml.js.
					return "</NeatHtmlReplace_" 
						+ m.Groups[3].Value.Substring(2, "table".Length) // Preserve case
						+ (m.Groups[4].Success? ">" : "");
				}
			}
			else if (m.Groups[5].Success)
				return "";
			else if (m.Groups[6].Success) // CDATA Section (with trailing ']').
				return HttpUtility.HtmlEncode(m.Groups[6].Value.Substring(0, m.Groups[6].Value.Length - 1));
			else if (m.Groups[7].Success)
			{
				if (m.Groups[10].Success)
				{
					// Looks like the start of a tag.  It might be safe when javascript
					// is enabled.  Prepend NeatHtmlReplace_ to the tag name so that NeatHtml.js can still see it.
					return "<" + m.Groups[9].Value + "NeatHtmlReplace_" + m.Groups[10].Value;
				}
				else
				{
					// Not a tag, so let it go.
					return m.Groups[7].Value;
				}
			}
			else
				return ""; // Should never get here			
		}

		private int UntrustedTables = 0;
	}
}
