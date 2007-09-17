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
			return ScriptJail.Jail(untrusted, ClientSideName, NoScriptDownlevelIEWidth, NoScriptDownlevelIEHeight);
		}
	}
	
	internal class ScriptJail
	{
		internal static string Jail(string untrusted, string clientSideName, string noScriptDownlevelIEWidth, string noScriptDownlevelIEHeight)
		{
			ScriptJail jail = new ScriptJail();
			string jailed = JailRE.Replace(untrusted, new MatchEvaluator(jail.GuardJail));

			// If any untrusted tables are still open, close any open attributes and tags
			// and then close all the tables.
			if (jail.UntrustedTables > 0)
			{
				jailed += ParserResetString;
				while (jail.UntrustedTables-- > 0)
					jailed += "</table>";
			}

			jailed = AttributeRE.Replace(jailed, new MatchEvaluator(GuardAttributeJail));

			return String.Format(Format, clientSideName, jailed, noScriptDownlevelIEWidth, noScriptDownlevelIEHeight);
		}

		private static string GuardAttributeJail(Match m)
		{
			if (m.Groups[1].Success		// safe style attribute 
				|| m.Groups[3].Success	// or other allowed attribute name
				)
			{
				// So leave it unchanged
				return m.Value;
			}
			
			// Otherwise, it is suspicious, so encode the "=" as "&#61;" to disable it.
			return m.Value.Substring(0,m.Value.Length - 1) + "&#61;";
		}

		private static string[] propsAllowedWhenNoScript
			= {"azimuth","background-attachment","background-color","background-image","background-position",
				"background-repeat","background","border-collapse","border-color","border-spacing","border-style",
				"border-top","border-right","border-bottom","border-left","border-top-color","border-right-color",
				"border-bottom-color","border-left-color","border-top-style","border-right-style","border-bottom-style",
				"border-left-style","border-top-width","border-right-width","border-bottom-width","border-left-width",
				"border-width","border","bottom","caption-side","clear","clip","color","content",
				/* "counter-increment","counter-reset", */ // Don"t allow messing with the counters 
				"cue-after","cue-before","cue","cursor","direction","display","elevation","empty-cells",
				"float","font-family","font-size","font-style","font-variant","font-weight","font","height","left",
				"letter-spacing","line-height","list-style-image","list-style-position","list-style-type","list-style",
				"margin-right","margin-left","margin-top","margin-bottom","margin","max-height","max-width","min-height",
				"min-width","orphans","outline-color","outline-style","outline-width","outline","overflow","padding-top",
				"padding-right","padding-bottom","padding-left","padding","page-break-after","page-break-before",
				"page-break-inside","pause-after","pause-before","pause","pitch-range","pitch","play-during","position",
				"quotes","richness","right","speak-header","speak-numeral","speak-punctuation","speak","speech-rate",
				"stress","table-layout","text-align","text-decoration","text-indent","text-transform","top",
				"unicode-bidi","vertical-align","visibility","voice-family","volume","white-space","widows","width",
				"word-spacing","z-index"
			};
		
		private static string[] attrsAllowedWhenNoScript
			= { "abbr", "accept", "accesskey", "align", "alt", "axis", "bgcolor", "border", "cellpadding",
				"cellspacing", "cite", "class", "classid", "clear", "code", "color", "cols", "colspan", "compact", 
				"datetime", "dir", "disabled", "face", "frame", "frameborder", "headers", "height", "href", "hreflang", "id", 
				"label", "lang", "language", "longdesc", "marginheight", "marginwidth", "name", "noshade", "nowrap", 
				"readonly", "rel", "rev", "rows", "rowspan", "rules", "scope", "size", "span", "start", "summary", 
				"tabindex", "title", "type", "value", "width", "xml:lang", "xml:space",
				"s", "d" // Used by <NeatHtmlParserReset> and <NeatHtmlEndUntrusted> 
				};
		
		// Style property value whitelist.  Note: '&' '\' and '(' [except 'rgb('] are not on it.
		private static string StylePropValueREString = "\\((?<=rgb\\()|[ !#$%)-9<-[\\]-~]";
		private static Regex AttributeRE
			= new Regex("([ \\r\\n\\t]style *=(?=(?:[ \\r\\n\\t]*(?:" // "style=" followed by
								// "safe value"
								+ "\"(?:"
									+ " *(?:" + String.Join("|", propsAllowedWhenNoScript) + ") *"
									+ ":"
									+ "(?:'|" + StylePropValueREString + ")*(?:;|(?=\")))*\"" 
								// 'safe value'
								+ "|'(?:"
									+ " *(?:" + String.Join("|", propsAllowedWhenNoScript) + ") *"
									+ ":"
									+ "(?:\"" + StylePropValueREString + ")*(?:;|(?=')))*'" 
							+ ")(?:[ \\r\\n\\t]|/?>))))" // followed by whitespace or end of tag  
							// or an "=" optionally preceded by an allowed attribute name
						+ "|(([ \\r\\n\\t](?:" + String.Join("|", attrsAllowedWhenNoScript) + ") *)?=)",
						RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);
			
						
		private static string ParserResetString = "<NeatHtmlParserReset s='' d=\"\"></NeatHtmlParserReset><script></script>";

		private static string Format 
			= "\n"
			+ "<![if gte IE 7]>\n"
			+ "<div class='NeatHtml' style='overflow: hidden; position: relative; border: none; padding: 0; margin: 0;'>\n"
			+ "<![endif]>\n"
			+ "<!--[if lt IE 7]>\n"
			+ "<div class='NeatHtml' style='width: {2}; height: {3}; overflow: auto; position: relative; border: none; padding: 0; margin: 0;'>\n"
			+ "<![endif]-->\n"
			+ "<table style='border-spacing: 0;'><tr><td style='padding: 0;'><!-- test comment --><script type='text/javascript'>\n"
			+ "try {{ {0}.BeginUntrusted(); }} catch (ex) {{ document.writeln('NeatHtml not found\\074!-' + '-'); }}</script>"
			+ "<div>{1}</div>"
			+ "<NeatHtmlEndUntrusted s='' d=\"\"></NeatHtmlEndUntrusted><script></script><!-- > --><xmp></xmp></td></tr></table><script type='text/javascript'>\n"
			+ "{0}.ProcessUntrusted();\n"
			+ "</script>\n"
			+ "</div><script type='text/javascript'>\n"
			+ "{0}.ResizeContainer();\n"
			+ "</script>\n";

		private static string[] tagsAllowedWhenNoScript
			= { "a", "abbr", "acronym", "address", "b", "basefont", "bdo", "big", "blockquote", "br",
		 		"caption", "center", "cite", "code", "col", "colgroup", "dd", "del", "dfn", "dir", "div", "dl", "dt", 
		 		"em", "font", "h1", "h2", "h3", "h4", "h5", "h6", "hr", "i", "ins", "kbd", "li", "ol", "p", "pre", "q",
		 		"s", "samp", "small", "span", "strike", "strong", "sub", "sup", "table", "tbody", "td", "tfoot", "th",
		 		"thead", "tr", "tt", "u", "ul", "var",
		 		"script" // OK when script is disabled.  Hides script source from user.
		 		// Do NOT allow "iframe" or "object", unless you are willing to track them like with tables.
		 		// Do NOT allow "xmp" -- it is used to hold the untrusted content on some browsers 
		 		// (eg. Safari/Konqueror).
			};
		
		private static Regex JailRE 
			= new Regex(							// 1: Matches any potential table-related start tag
						"(<(?:table|caption|thead|tfoot|tbody|colgroup|col|tr|td|th)"
													// 2: Matches if this is a valid table-related start tag
							+ "((?:[ \\t\\n\\r]+[_:a-z][_:a-z0-9.]*[ \\t\\n\\r]*(?:=(?:\"[^<\"]*\"|'[^<']*'))?)*[ \\t\\n\\r]*>)?"
						+ ")"
													// 3: Matches any potential table-related end tag
						+ "|(</(?:table|caption|thead|tfoot|tbody|colgroup|col|tr|td|th)"
							+ "([ \\t\\n\\r]*>)?"   // 4: Matches if valid table end tag
						+ ")"
						+ "|(<!--[^-]*(?:-[^-]+)*-->)" // 5: HTML Comment (without embedded "--")
						+ "|(?:<!\\[CDATA\\[([^\\]]*(?:\\][^\\]]+)*)\\]\\]>)" // 6: Complete CDATA section
						+ "|(<"                     // 7: Matches tags "<" when followed by:
													// 8: a tag name that is not allowed
							+ "(?!/?(" + String.Join("|", tagsAllowedWhenNoScript) + ")[ \\t\\n\\r/>])"
							+ "([!\\?/])?"			// 9: starting with ! ? or / (ie. <!, <?, or </)
							+ "([a-z])?"			// 10: and/or a letter (eg. <X, </X)
						+ ")"
						+ "|(--)",                  // 12: "--" we need to encode to prevent ending a comment prematurely						,
						RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

		private string GuardJail(Match m)
		{
			if (m.Groups[1].Success)
			{
				string tagName 
					= m.Groups[1].Value.Substring(1, m.Groups[1].Value.Length - 1
													- (m.Groups[2].Success ? m.Groups[2].Value.Length : 0));
				string lcTagName = tagName.ToLower();
				// Hide suspicious table-related start tags
				if (!m.Groups[2].Success || (lcTagName == "table" && !IsTableAllowed)) 
				{
					// Hide it from the browser's parser so our markup jail is not affected.
					// It will be recovered by NeatHtml.js.
					return "<NeatHtmlReplace_" 
						+ m.Groups[1].Value.Substring(1, m.Groups[1].Value.Length-1); // Preserve case
				}
				
				if (lcTagName == "table")
				{
					IsTableAllowed = false;
					UntrustedTables++;
					// Close any open attribute values and tags and then start the table as requested
					return ParserResetString + m.Groups[1].Value;
				}
				if ((lcTagName == "td" || lcTagName == "th") && !IsTableAllowed)
				{
					IsTableAllowed = true;
					// Close any open attribute values and tags and then start the cell as requested
					return ParserResetString + m.Groups[1].Value;
				}					
				return m.Groups[1].Value;
			}
			else if (m.Groups[3].Success)
			{
				string tagName 
					= m.Groups[3].Value.Substring(2, m.Groups[3].Value.Length - 2
													- (m.Groups[4].Success ? m.Groups[4].Value.Length : 0));
				if (!m.Groups[4].Success || UntrustedTables <= 0) // suspicious table-related end tag
				{
					// Hide it from the browser's parser so our markup jail is not affected.
					// It will be recovered by NeatHtml.js.
					return "</NeatHtmlReplace_" + tagName + (m.Groups[4].Success ? ">" : "");
				}
				string lcTagName = tagName.ToLower();
				if (lcTagName == "table")
				{
					IsTableAllowed = true;
					UntrustedTables--;
					// Close any open attribute values and tags and then end the table as requested
					return ParserResetString + m.Groups[3].Value;
				}
				else
				{
					IsTableAllowed = false;
					return m.Groups[3].Value;
				}
			}
			else if (m.Groups[5].Success) // HTML comment (not containing "--", so it isn't ambiguous)
			{
				return "";
			}
			else if (m.Groups[6].Success) // CDATA Section.
			{
				return HttpUtility.HtmlEncode(m.Groups[6].Value);
			}
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
					// Not a tag, HTML encode it to be sure it doesn't confuse the browser's parser.
					return HttpUtility.HtmlEncode(m.Groups[7].Value);
				}
			}
			else if (m.Groups[11].Success)
				return "&#45;&#45;";
			else
			{
				return ""; // Should never get here			
			}
		}

		private int UntrustedTables = 0;
		private bool IsTableAllowed = true;
	}
}
