/*
NeatHtml - for preventing XSS in untrusted HTML
Copyright (C) 2007  Dean Brettle

GetParseErrorText() based on code from LGPLed sarissa.js which is Copyright 2004-2007 Emmanouil Batsis.

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

/*

Simplest usage (note that comments and absence of whitespace between tags can be significant):
	
<![if gte IE 7]>
	<div style="overflow: hidden; position: relative; border: none; padding: 0; margin: 0;">
<![endif]>
<!--[if lt IE 7]>
	<div style="width: NOSCRIPT_IE6_WIDTH; height: NOSCRIPT_IE6_HEIGHT; overflow: auto; position: relative; border: none; padding: 0; margin: 0;">	
<![endif]-->
		<table><tr><td><!-- test comment --><script type="text/javascript">// <![CDATA[
			NeatHtml.DefaultFilter.BeginUntrusted(); // ]]></script><div>
				PREPROCESSED_UNTRUSTED_CONTENT
		<xmp></xmp><!-- ' " > --></td></tr></table>
	</div>
	<script type="text/javascript">// <![CDATA[
		NeatHtml.DefaultFilter.ProcessUntrusted();
	// ]]></script>
	
where:

	PREPROCESSED_UNTRUSTED_CONTENT has had the following preprocessing done on the server:
		1. Remove all comments (i.e. anything matching this regex: "<!--([^-]*-)+-[^>]*>")
		2. Replace all remaining "--" with "&#45;&#45;"
		3. Replace all "<xmp>" and "</xmp>" with "&lt;xmp&gt;" and "&lt;/xmp&gt;".  
		4. Either ensure that the untrusted content is well-formed XML 
			or replace all "<table>" and "</table>" with "&lt;table&gt;" and "&lt;/table&gt;".
	NOSCRIPT_IE6_WIDTH and NOSCRIPT_IE6_HEIGHT are the desired dimensions of the div that will display the
		untrusted content in IE6 and earlier when script is disabled.  If the untrusted content is larger, 
		scrollbars will be added to the div.  These values have no effect on other browsers nor do they have
		an effect when script is enabled.
		
To change the way various tags and attributes are handled:

	1. Create your own filter:
			var filter = new NeatHtml.Filter();
	2. Configure it by modifying or replacing the default ElemActions, AttrActions, or StyleRe members.  
		ElemActions and AttrActions are associative arrays which map element or attribute names to functions which
		manipulate elements or attributes with those names.  The following functions are predefined:
			NeatHtml.Filter.prototype.AllowElem - Allows the element and filters it's content.
			NeatHtml.Filter.prototype.RemoveElem - Removes the element and all of it's content.
			NeatHtml.Filter.prototype.AllowAttr - Allows the attribute unchanged.
			NeatHtml.Filter.prototype.HandleUrl - Removes the attribute if the value doesn't start with one of
							"http:", "https", "ftp:", or "mailto:".
			NeatHtml.Filter.prototype.HandleStyle - Removes comments, escapes, and calls to functions other than rgb()
							from the value, compares the result against the StyleRe member, and removes the attribute if
							it doesn't match.
		For example, you could strip all tags and attributes except for bold and italic by doing:
			filter.ElemActions = { b: filter.AllowElem, i: filter.AllowElem };
			filter.AttrActions = {};
		Or you could add support for <img> elements to the default filter by doing:
			filter.ElemActions.img = filter.AllowElem;
			filter.AttrActions.src = filter.AllowAttr;
	3. Change "NeatHtml.DefaultFilter" to "filter" in both places in the simple usage example above.
*/


NeatHtml = {};

NeatHtml.Filter = function(elemActions, attrActions)
{
	this.ElemActions = elemActions || GetDefaultElemActions();
	this.AttrActions = attrActions || GetDefaultAttrActions();
	this.StyleRe = new RegExp("^(\s*((vertical-align|VERTICAL-ALIGN):\s*((text-)?(top|bottom)|middle|baseline|sub|super)|(text-align|TEXT-ALIGN):\s*(left|right|center|justify)|(text-decoration|TEXT-DECORATION):\s*(blink|line-through|overline|underline|none)|(font-style|FONT-STYLE):\s*(normal|oblique|italic)|(font-weight|FONT-WEIGHT):\s*(normal|bold)|((mso-|MSO-)(fareast-|FAREAST-|ansi-|ANSI-|bidi-|BIDI-))?(font-family|FONT-FAMILY|language|LANGUAGE):\s*[a-zA-Z, &quot;'\-]+|(font-size|FONT-SIZE):\s*(([1-9]|1[0-8])pt|([1-3]|[0-2]\.[0-9]*)e[mx]|xx-small|x-small|small|medium|large)|(margin|MARGIN|padding|PADDING)(-(top|TOP|left|LEFT|bottom|BOTTOM|right|RIGHT))?:(\s*(0|[1-9][0-9]*)(.[0-9]*)?(pt|em|ex|in|px|cm)?)+|(width|WIDTH|height|HEIGHT):\s*(0|[1-9][0-9]*)(.[0-9]*)?(pt|em|ex|in|px|cm|%)?|(text-indent|TEXT-INDENT):\s*(0|[1-9][0-9]*)(.[0-9]*)?(pt|em|ex|in|cm)|(mso-spacerun|MSO-SPACERUN):\s*(yes|no)|((background|BACKGROUND)-)?(color|COLOR):\s*((-|[a-zA-Z])+|#[0-9a-fA-F]+|rgb\([0-9, ]+\)))\s*;?)*$");

	function GetDefaultElemActions()
	{
		// These tags and their content is allowed.
		var allowedTags 
			= [ "a", "abbr", "acronym", "address", "b", "basefont", "bdo", "big", "blockquote", "br",
		 		"caption", "center", "cite", "code", "col", "colgroup", "dd", "del", "dfn", "dir", "div", "dl", "dt", 
		 		"em", "font", "h1", "h2", "h3", "h4", "h5", "h6", "hr", "i", "ins", "kbd", "li", "ol", "p", "pre", "q",
		 		"s", "samp", "small", "span", "strike", "strong", "sub", "sup", "table", "tbody", "td", "tfoot", "th",
		 		"thead", "tr", "tt", "u", "ul", "var" ];

		// These tags are removed along with their content
		var prohibitedTags = [ "script", "style" ];

		var elemActions = {};
		for (var i = 0; i < allowedTags.length ; i++)
		{
			var t = allowedTags[i];
			elemActions[t] = NeatHtml.Filter.prototype.AllowElem; 
		}
	
		for (var i = 0; i < prohibitedTags.length ; i++)
		{
			var t = prohibitedTags[i];
			elemActions[t] = NeatHtml.Filter.prototype.RemoveElem; 
		}
		
		return elemActions;
	}
	
	function GetDefaultAttrActions()
	{
		// These attributes are allowed
		var allowedAttrs 
			= [ "abbr", "accept", "accesskey", "align", "alt", "axis", "bgcolor", "border", "cellpadding",
				"cellspacing", "cite", "class", "classid", "clear", "code", "color", "cols", "colspan", "compact", 
				"datetime", "dir", "disabled", "face", "frame", "frameborder", "headers", "height", "hreflang", "id", 
				"label", "lang", "language", "longdesc", "marginheight", "marginwidth", "name", "noshade", "nowrap", 
				"readonly", "rel", "rev", "rows", "rowspan", "rules", "scope", "size", "span", "start", "summary", 
				"tabindex", "title", "type", "value", "width", "xml:lang", "xml:space" ];

		var attrActions = {};
		for (var i = 0; i < allowedAttrs.length ; i++)
		{
			var a = allowedAttrs[i];
			attrActions[a] = NeatHtml.Filter.prototype.AllowAttribute; 
		}
	
		attrActions["href"] = NeatHtml.Filter.prototype.HandleUrl; 
		attrActions["style"] = NeatHtml.Filter.prototype.HandleStyle;
		
		return attrActions;
	}
};

NeatHtml.Filter.prototype.AllowAttribute = function(elem, attr, attrsToRemove, attrsToAdd)
{
	// Do nothing
};
		
NeatHtml.Filter.prototype.HandleUrl = function(elem, attr, attrsToRemove, attrsToAdd)
{
	if (attr.value == null) return;
	if (/^(http:|https:|ftp:|mailto:)/i.test(attr.value) == false)   // ... invalid protocol
	{
		attrsToRemove.push(attr);
	}
};
		
NeatHtml.Filter.prototype.HandleStyle = function(elem, attr, attrsToRemove, attrsToAdd)
{
	// If there was a style attribute, sanitize it's value and then check it against a whitelist regex.
	// We must sanitize the style before we attempt to access any style properties (e.g. elem.style.overflow) in IE.
	// There doesn't seem to be a way to access the styles in IE after they have been parsed but before the 
	// script has been run.  For example, if you do:
	//
	// elem.style.cssText = "attr: expression(window.alert('XSS'));";
	//
	// the script will not run yet.  But, as soon as you access any of the style properties the script will run. 
	if (attr.value == null) return;

	s = attr.value;			
	// Remove comments
	s = s.replace(/\x2F\*[^*]*\*+([^\x2F][^*]*\*+)*\x2F/gm, "");
	// Remove any left over comment markers
	s = s.replace(/\x2F\*|\*\x2F/, "");
	// Remove Unicode escapes
	s = s.replace(/\\[0-9a-f]{1,6}[ \n\r\t\f]?/g, "");
	// Remove all other escapes
	s = s.replace(/\\./g, "");
	
	// Remove all function calls except those to rgb()
	s = s.replace(/([A-Za-z]|[^\0-\177])([A-Za-z0-9-]|[^\0-\177])*\(/g, function (match) {
		if (/rgb\(/.test(match) == false)
		{
			removedCall = true;
			match = " (";
		}
		return match;
	});
	attr.value = s;

	if (this.StyleRe.test(attr.value) == false) // Still looks a little fishy, so delete it to be safe... 
	{
		attrsToRemove.push(attr);
	}
};

NeatHtml.Filter.prototype.RemoveTag = function(elem)
{
	// move all the children out of the element and then delete the empty element.
	var nextSibling = null;
	for (var n = elem.firstChild; n != null; n = nextSibling)
	{
		nextSibling = n.nextSibling;
		elem.removeChild(n);
		elem.parentNode.insertBefore(n, elem.nextSibling);
	}
	this.RemoveElem(elem);
};

NeatHtml.Filter.prototype.AllowElem = function(elem)
{
	var my = this;
	ProcessAttributes(elem);
	var nextSibling = null;
	for (var n = elem.firstChild; n != null; n = nextSibling)
	{
		nextSibling = n.nextSibling; // Remember the nextSibling in case n gets removed.
		switch (n.nodeType)
		{
			case 1: // ELEMENT_NODE
				var action = this.ElemActions[n.tagName] 			// Use the case-sensitive name if available,
					|| this.ElemActions[n.tagName.toLowerCase()] // otherwise, check for the lowercase version,
					|| this.RemoveTag;									// otherwise, remove the tag and process the children
				nextSibling = action.call(this, n);
				break;
			case 3: // TEXT_NODE
			case 5: // ENTITY_REFERENCE_NODE
				break;
			case 4: // CDATA_SECTION_NODE
			case 8: // COMMENT_NODE
			default: // Remove everything else, including comments and CDATA sections
				elem.removeChild(n);
		}
	}

	return elem.nextSibling;
	
	function ProcessAttributes(elem)
	{
		var attrs = elem.attributes;
		var attrsToRemove = [];
		var attrsToAdd = [];
		for (var i = 0; i < attrs.length; i++)
		{
			var attr = attrs.item(i);
			var action = my.AttrActions[attr.name] || my.AttrActions[attr.name.toLowerCase()];
			if (action)
			{
				action.call(my, elem, attr, attrsToRemove, attrsToAdd);
			}
			else
			{
				attrsToRemove.push(attr);
			}
		}

		// Remove the unwanted attributes
		for (var i = 0; i < attrsToRemove.length; i++)
		{
			elem.removeAttributeNode(attrsToRemove[i]);
		}
		// Add new attributes
		for (var i = 0; i < attrsToAdd.length; i++)
		{
			elem.setAttribute(attrsToAdd[i].name, attrsToAdd[i].value);
		}
	}
};
		
NeatHtml.Filter.prototype.RemoveElem = function(elem)
{
	var nextSibling = elem.nextSibling;
	elem.parentNode.removeChild(elem);
	return nextSibling;
};
	
NeatHtml.Filter.prototype.BeginUntrusted = function() {
	// Inject markup to prevent the untrusted content from being parsed as HTML.  
	// If we are able to extract content from comments, start a comment.
	// Otherwise (e.g. Safari and Konqueror), start an <xmp> element.

	// Find the calling script element and remember it so we can use it to find the untrusted content.
	var scriptElems = document.getElementsByTagName("script");
	this.BeginUntrustedScript = scriptElems[scriptElems.length - 1];

	// The calling script element must be preceded by an HTML comment (i.e. <!-- something -->).  
	if (this.BeginUntrustedScript.previousSibling 
		&& this.BeginUntrustedScript.previousSibling.nodeType == 8 /* Node.COMMENT_NODE */)
	{
		document.write("<!--");
	}
	else
	{
		document.write("<xmp>");
	}
};

NeatHtml.Filter.prototype.ProcessUntrusted = function() {
	var my = this;
	var containingDiv = FindContainingDiv(this.BeginUntrustedScript);
	try
	{
		var untrustedContent = GetUntrustedContent();
		var xmlStr = TagSoupToXml(untrustedContent);
		xmlStr = ProcessXml(xmlStr);
		containingDiv.innerHTML = xmlStr;
		ProcessHtmlElem(containingDiv);
	}
	catch (ex)
	{
		containingDiv.innerHTML = "<pre>" + ex.toString().replace("<", "&lt;").replace("&", "&amp;") + "</pre>";
	}
	/***** Local Functions ******/
	
	function FindContainingDiv(n) 
	{
		while (n.tagName != "DIV")
		{
			n = n.parentNode;
		}
		return n;
	}

	function GetUntrustedContent() 
	{
		// The untrusted content is in the node that immediately follows the script element that called BeginUntrusted. 
		var n = my.BeginUntrustedScript.nextSibling;
		var s;
		if (n.nodeType == 8 /* Node.COMMENT_NODE */)
		{
			s = n.data;
		}
		else if (n.tagName == "XMP")
		{
	 		s = n.innerHTML;
	 		// Unquote the HTML special characters.
			s = s.replace(/&lt;/gm, "<").replace(/&gt;/gm, ">").replace(/&amp;/gm, "&");
		}
//		alert(s);
		return s;
	}
	
	function TagSoupToXml(s)
	{
		// According to HTML 3.2
		var entityCharCodes = { 
			nbsp:160,iexcl:161,cent:162,pound:163,curren:164,yen:165,brvbar:166,
			sect:167,uml:168,copy:169,ordf:170,laquo:171,not:172,shy:173,reg:174,
			macr:175,deg:176,plusmn:177,sup2:178,sup3:179,acute:180,micro:181,
			para:182,middot:183,cedil:184,sup1:185,ordm:186,raquo:187,frac14:188,
			frac12:189,frac34:190,iquest:191,Agrave:192,Aacute:193,Acirc:194,
			Atilde:195,Auml:196,Aring:197,AElig:198,Ccedil:199,Egrave:200,Eacute:201,
			Ecirc:202,Euml:203,Igrave:204,Iacute:205,Icirc:206,Iuml:207,ETH:208,
			Ntilde:209,Ograve:210,Oacute:211,Ocirc:212,Otilde:213,Ouml:214,times:215,
			Oslash:216,Ugrave:217,Uacute:218,Ucirc:219,Uuml:220,Yacute:221,
			THORN:222,szlig:223,agrave:224,aacute:225,acirc:226,atilde:227,
			auml:228,aring:229,aelig:230,ccedil:231,egrave:232,eacute:233,
			ecirc:234,euml:235,igrave:236,iacute:237,icirc:238,iuml:239,eth:240,
			ntilde:241,ograve:242,oacute:243,ocirc:244,otilde:245,ouml:246,
			divide:247,oslash:248,ugrave:249,uacute:250,ucirc:251,uuml:252,yacute:253,
			thorn:254,yuml:255,fnof:402,Alpha:913,Beta:914,Gamma:915,Delta:916,
			Epsilon:917,Zeta:918,Eta:919,Theta:920,Iota:921,Kappa:922,Lambda:923,
			Mu:924,Nu:925,Xi:926,Omicron:927,Pi:928,Rho:929,Sigma:931,Tau:932,
			Upsilon:933,Phi:934,Chi:935,Psi:936,Omega:937,alpha:945,beta:946,
			gamma:947,delta:948,epsilon:949,zeta:950,eta:951,theta:952,iota:953,
			kappa:954,lambda:955,mu:956,nu:957,xi:958,omicron:959,pi:960,rho:961,
			sigmaf:962,sigma:963,tau:964,upsilon:965,phi:966,chi:967,psi:968,
			omega:969,thetasym:977,upsih:978,piv:982,bull:8226,hellip:8230,prime:8242,
			Prime:8243,oline:8254,frasl:8260,weierp:8472,image:8465,real:8476,
			trade:8482,alefsym:8501,larr:8592,uarr:8593,rarr:8594,darr:8595,harr:8596,
			crarr:8629,lArr:8656,uArr:8657,rArr:8658,dArr:8659,hArr:8660,
			forall:8704,part:8706,exist:8707,empty:8709,nabla:8711,isin:8712,
			notin:8713,ni:8715,prod:8719,sum:8721,minus:8722,lowast:8727,radic:8730,
			prop:8733,infin:8734,ang:8736,"and":8743,"or":8744,cap:8745,cup:8746,"int":8747,
			there4:8756,sim:8764,cong:8773,asymp:8776,ne:8800,equiv:8801,le:8804,
			ge:8805,sub:8834,sup:8835,nsub:8836,sube:8838,supe:8839,oplus:8853,otimes:8855,
			perp:8869,sdot:8901,lceil:8968,rceil:8969,lfloor:8970,rfloor:8971,
			lang:9001,rang:9002,loz:9674,spades:9824,clubs:9827,hearts:9829,diams:9830,
			OElig:338,oelig:339,Scaron:352,scaron:353,Yuml:376,circ:710,tilde:732,
			ensp:8194,emsp:8195,thinsp:8201,zwnj:8204,zwj:8205,lrm:8206,rlm:8207,
			ndash:8211,mdash:8212,lsquo:8216,rsquo:8217,sbquo:8218,ldquo:8220,rdquo:8221,
			bdquo:8222,dagger:8224,Dagger:8225,permil:8240,lsaquo:8249,rsaquo:8250,euro:8364 
		};
		var endTagsForbidden = { br:1, hr:1, meta:1, col:1, isindex:1, img:1, link:1, area:1, basefont:1, param:1, input:1, base:1 };
		var endTagsOptional = { li:1, p:1, dt:1, dd:1, thead:1, tfoot:1, tbody:1, colgroup:1, tr:1, th:1, td:1, plaintext:1, option:1 };
		var openTagNames = [];
		var lengthToIgnoreAtEnd = 0;
		var lengthToIgnoreAtBeginning = 0;
		var ignoreUntilOffset = 0;
		s = s.replace(/(&((#[0-9]{1,10};|#x[0-9a-fA-F]{1,8};|amp;|lt;|gt;|quot;|apos;)|([A-Z:_a-z][A-Z:_a-z0-9._]{0,10};|(#X[0-9a-fA-F]{1,8};|))))|(<(\/?)(([!\?A-Z:_a-z][^ \t\n\r>]*)([^>]*)>|([^!\?A-Z:_a-z])))/gm, 
			function(match, isAmp, afterAmp, isValidXmlEntityRef, charEntityRef, upperCaseHexEntityRef, isOpenAngle, isEndTag, raw, tagName, attrs, isNotEncoded, offset) {
			// If we already set the length to ignore at end, then we are already done.
			if (lengthToIgnoreAtEnd || offset < ignoreUntilOffset) return match;
			if (isAmp) return HandleAmpersand(match, isValidXmlEntityRef, afterAmp, charEntityRef, upperCaseHexEntityRef);
			if (isNotEncoded)	return HtmlEncode(match);
			if (isOpenAngle) return HandleOpenAngle(match, isEndTag, tagName, attrs, offset);
		});
		
		// Ignore anything before/after the first elem
		s = s.substring(lengthToIgnoreAtBeginning, s.length - lengthToIgnoreAtEnd);

		// Close any remaining open tags
		while (openTagNames.length)
		{
		 	var openTagName = openTagNames.pop();
			s += "</" + openTagName + ">";
		}
// alert(s);
		return s;

		/* Local functions */
		function HandleAmpersand(match, isValidXmlEntityRef, afterAmp, charEntityRef, upperCaseHexEntityRef)
		{
			if (isValidXmlEntityRef)
				return match;
			if (!charEntityRef)
				return HtmlEncode(match);
			if (upperCaseHexEntityRef)
				return match.toLowerCase();
			// It is a character entity reference but it isn't supported in XML
			if (charEntityRef)
			{
				// Strip the trailing semicolon
				charEntityRef = charEntityRef.substring(0, charEntityRef.length-1);
			}
			var charCode = entityCharCodes[charEntityRef];
			if (!charCode)
				return HtmlEncode(match);
			return "&#" + charCode + ";";
		}

		function HandleOpenAngle(match, isEndTag, tagName, attrs, offset)
		{
			if (/^!--.*$/.test(tagName))
			{
				ignoreUntilOffset = s.indexOf("--", offset + "<!--".length);
				if (ignoreUntilOffset == -1) return HtmlEncode(match);
				ignoreUntilOffset = s.indexOf(">", ignoreUntilOffset);
				if (ignoreUntilOffset == -1) return HtmlEncode(match);
				return match;
			}
			if (/^!\[CDATA\[.*$/.test(tagName))
			{
				ignoreUntilOffset = s.indexOf("]]>", offset + "<![CDATA[".length);
				if (ignoreUntilOffset == -1) return HtmlEncode(match);
				return match;
			}
			if (/^\?.*$/.test(tagName))
			{
				ignoreUntilOffset = s.indexOf("?>", offset + "<?".length);
				if (ignoreUntilOffset == -1) return HtmlEncode(match);
				return match;
			}
			// If it doesn't look like a tag then it is probably an unencoded '<'.
			if (! /^[A-Z:_a-z][A-Z:_a-z0-9._]*$/.test(tagName) || isEndTag && openTagNames.length == 0) 
				return HtmlEncode(match);
			// Otherwise it is a begin or end tag
			if (isEndTag)
				return HandleEndTag(match, tagName, offset);
			else
				return HandleBeginTag(match, tagName, attrs, offset);
		}
			
		function HandleBeginTag(match, tagName, attrs, offset)
		{
			var lcTagName = tagName.toLowerCase();
			// Ignore everything before the first open tag.
			if (openTagNames.length == 0)
				lengthToIgnoreAtBeginning = offset;
			// Cleanup the attributes
			var newAttrs = "";
			attrs = attrs.replace(/[ \t\n\r]+([A-Z:_a-z][A-Z:_a-z0-9._]*)(=("[^"]*"|'[^']*'|[^"'][^ \t\r\n]*))?/gm, 
										function(attrMatch, attrName, hasValue, attrValue) {
											newAttrs += HandleAttr(attrName, hasValue, attrValue);
										});
			var closeTag = "";
			// If this tag has an optional end tag and the current open elem has the same tag name,
			// close the one that is open
			if (endTagsOptional[lcTagName] && openTagNames.length)
			{
				var openTagName = openTagNames[openTagNames.length-1];
				if (openTagName.toLowerCase() == lcTagName)
				{
					closeTag = "</" + openTagName + ">"
					openTagNames.pop();
					// If there aren't any open tags left, ignore everything else
					if (openTagNames.length == 0)
						lengthToIgnoreAtEnd = s.length - offset - match.length;
				}
			}
			var newTag = closeTag + "<" + tagName + newAttrs;
			if (attrs.charAt(attrs.length-1) == "/" || endTagsForbidden[lcTagName])
			{
				newTag += " />";
			}
			else
			{
				newTag += ">";
				openTagNames.push(tagName);
			}
			return newTag;
		}

		function HandleAttr(attrName, hasValue, attrValue)
		{
			if (!hasValue)
			{
				attrValue = attrName;
			}
			attrValue = attrValue.replace(/</gm, "&lt;");
			attrValue = attrValue.replace(/&((#[0-9]{1,10};|#x[0-9a-fA-F]{1,8};|amp;|lt;|gt;|quot;|apos;)|([A-Z:_a-z][A-Z:_a-z0-9._]{0,10};|(#X[0-9a-fA-F]{1,8};|)))/gm,
													HandleAmpersand);
			var firstChar = attrValue.charAt(0)
			
			if ( firstChar != '"' && firstChar != "'")
			{
				attrValue = '"' + HtmlEncode(attrValue) + '"';
			}
			return " " + attrName + "=" + attrValue;
		}
		
		function HandleEndTag(match, tagName, offset)
		{
			var lcTagName = tagName.toLowerCase();
			var result = null;
			var tagIndex = openTagNames.length - 1;
			for (var closeTags = ""; tagIndex >= 0; tagIndex--)
			{
			 	var openTagName = openTagNames[tagIndex];
				lcOpenTagName = openTagName.toLowerCase();
				closeTags += "</" + openTagName + ">";
				if (lcOpenTagName == lcTagName)
				{
					result = closeTags;
					break;
				}
				// If the open tag must be explicitly closed, ignore those close tag.
				if (!endTagsOptional[lcOpenTagName])
				{
					break;
				}
			}
			// If we didn't find a matching open tag, then remove the close tag
			if (result == null)
			{
				return "";
			}
			// Remove the tags from the stack of open tags
			openTagNames.splice(tagIndex, openTagNames.length - tagIndex);
			// If there aren't any open tags left, ignore everything else
			if (openTagNames.length == 0)
			{
				lengthToIgnoreAtEnd = s.length - offset - match.length;
			}
			return result;
		}
	}

	function ProcessXml(xmlStr)
	{
		// Parse the XML string.
		var xmlDoc = ParseXml(xmlStr);
	
		// Process the document.
		ProcessXmlElem(xmlDoc.documentElement);
		
		// Serialize the XML document back to a string
		return XmlDocToString(xmlDoc);
	}
	
	function ParseXml(xmlStr)
	{
		var xmlDoc;
		if (window.DOMParser)
		{
			// Mozilla...
			xmlDoc = (new DOMParser()).parseFromString(xmlStr, "text/xml");
		}
		else
		{
			try
			{
				// Safari/Konqueror...
				xmlDoc = document.implementation.createDocument("", null, null);
			}
			catch (ex) 
			{
				// IE...
				xmlDoc = new ActiveXObject("Microsoft.XMLDOM");
			}
		
			xmlDoc.async = false;
			xmlDoc.loadXML(xmlStr);
		}
		var parseErrorText = GetParseErrorText(xmlDoc);
      if (parseErrorText)
      {
	  		throw "Untrusted HTML could not be parsed: " + parseErrorText;
		}
		return xmlDoc;
	}
	
	function GetParseErrorText(xmlDoc)
	{
		var parseErrorText = null;
		if (xmlDoc.parseError && xmlDoc.parseError.errorCode)
		{
			parseErrorText = xmlDoc.parseError.reason + 
				"\nLocation: " + xmlDoc.parseError.url + 
				"\nLine Number " + xmlDoc.parseError.line + ", Column " + 
			xmlDoc.parseError.linepos + 
				":\n" + xmlDoc.parseError.srcText +
				"\n";
			for(var i = 0;  i < xmlDoc.parseError.linepos;i++)
			{
				parseErrorText += "-";
			};
			parseErrorText +=  "^\n";
		}
		else if (!xmlDoc.documentElement)
		{
			parseErrorText = "Unknown reason";
		}
		else if (xmlDoc.documentElement.tagName == "parsererror"
					|| xmlDoc.documentElement.tagName == "html" // Konqueror returns an error HTML document
					|| xmlDoc.getElementsByTagName("parsererror").length > 0)
		{
			parseErrorText = "Unknown reason";
			if (xmlDoc.documentElement.firstChild && xmlDoc.documentElement.firstChild.data)
			{ 
				parseErrorText = xmlDoc.documentElement.firstChild.data;
				if (xmlDoc.documentElement.firstChild.nextSibling
					&& xmlDoc.documentElement.firstChild.nextSibling.firstChild)
				{
      			parseErrorText += "\n" +  xmlDoc.documentElement.firstChild.nextSibling.firstChild.data;
      		}
      	}
      }
      return parseErrorText;
	}
	
	function XmlDocToString(xmlDoc)
	{
		var xmlStr;
		if (window.XMLSerializer)
		{
			// Can't use serializeToString() because Safari 2 expands entities like "&lt;".
			// Also, Safari < 2.0 does not correctly serialize attributes that have been 
			// accessed with elem.attributes.item(i).
			// xmlStr = new XMLSerializer().serializeToString(xmlDoc.firstChild);
			xmlStr = XmlElemToString(xmlDoc.firstChild);
  		} 
  		else
  		{
  			xmlStr = xmlDoc.documentElement.xml;
  		}
		return xmlStr;
	}
	
	function XmlElemToString(elem)
	{
		var xmlStr = "";
		
		xmlStr = "<" + elem.tagName;
		var attrs = elem.attributes;
		for (var i = 0; i < attrs.length; i++)
		{
			var attr = attrs.item(i);
			xmlStr += " " + attr.name + '="' + HtmlEncode(attr.value) + '"';
		}
		xmlStr += ">";
		var nextSibling = null;
		for (var n = elem.firstChild; n != null; n = nextSibling)
		{
			nextSibling = n.nextSibling; // Remember the nextSibling in case n gets removed.
			switch (n.nodeType)
			{
				case 1: // ELEMENT_NODE
					xmlStr += XmlElemToString(n);
					break;
				case 3: // TEXT_NODE
					xmlStr += HtmlEncode(n.nodeValue);
					break;
				default:
					// There shouldn't be anything else.  We removed comments and CDATA sections.
					// Character entity references should have been expanded by the parser.
			}
		}
		xmlStr += "</" + elem.tagName + ">";
		return xmlStr;
	}
	
	function HtmlEncode(s)
	{
		return s.replace(/[<>&"']/g, function (c) { 
			switch (c)
			{
				case '<': return "&lt;";  
				case '>': return "&gt;";  
				case '&': return "&amp;";  
				case '"': return "&quot;";  
				case "'": return "&apos;";
			}
		});  
	}
	
	function ProcessXmlElem(elem)
	{
		my.ElemActions[elem.tagName].call(my, elem);
		return;
	}

	
	function ProcessHtmlElem(parent)
	{
		var elems = parent.getElementsByTagName("*");
		for (var i = 0; i < elems.length; i++)
		{
			var e = elems[i];
			if (e.attributes.length > 0)
			{
				ProcessHtmlAttrs(e);
			}
		}

		// Set the dimensions of the div based on the new content.  This allows IE6 to hide overflow that was
		// absolutely positioned.
		if (parent.firstChild.scrollHeight)
		{
			// Firefox (at least 1.5) computes an incorrect scrollHeight there is no padding and no border.
			// If there is no padding, temporarily add padding so FF will calculate scrollHeight properly.
			var extraPadding = 0;
			if (!parent.firstChild.style.padding)
			{
				parent.firstChild.style.padding = "1px";
				extraPadding = 1;
			}
			// Don't count extraPadding in height and width because we will be removing it.
			parent.style.height = (parent.firstChild.scrollHeight-2*extraPadding) + "px";
			parent.style.width = (parent.firstChild.scrollWidth-2*extraPadding) + "px";
			if (extraPadding)
			{
				parent.firstChild.style.padding = "0px";
			}
			parent.style.overflow = "hidden";
		}
	}
	
	function ProcessHtmlAttrs(elem)
	{
		var attrs = elem.attributes;
		for (var i = 0; i < attrs.length; i++)
		{
			var attr = attrs.item(i);
			var name = attr.name;
			var val = attr.value;
			if (name.toLowerCase() == "id")
			{
				newIdValue = "NeatHtml_" + val;
				elem.setAttribute(name, "NeatHtml_" + val);
			}
		}
	}
};

NeatHtml.DefaultFilter = new NeatHtml.Filter();

