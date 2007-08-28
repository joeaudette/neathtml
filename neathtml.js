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

NeatHtml = {};
NeatHtml.BeginUntrusted = function() {
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

NeatHtml.ProcessUntrusted = function() {
	var my = this;
	var containingDiv = FindContainingDiv(this.BeginUntrustedScript);
	try
	{
		var xmlStr = GetUntrustedXml();
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

	function GetUntrustedXml() 
	{
		// The untrusted content is in the node that immediately follows the script element that called BeginUntrusted. 
		var n = my.BeginUntrustedScript.nextSibling;
		var xmlStr;
		if (n.nodeType == 8 /* Node.COMMENT_NODE */)
		{
			xmlStr = n.data;
		}
		else if (n.tagName == "XMP")
		{
	 		xmlStr = n.innerHTML;
	 		// Unquote the HTML special characters.
			xmlStr = xmlStr.replace(/&lt;/gm, "<").replace(/&gt;/gm, ">").replace(/&amp;/gm, "&");
		}
		
		// If we don't see the ending </div> tag, the content was probably not escaped properly.
		var endTag = "</div>";
		var endTagIndex = xmlStr.lastIndexOf(endTag);
		if (endTagIndex == -1)
		{
			throw "Untrusted HTML is invalid.  It probably contains a '--' or '</xmp>'.";
		}
		
		// Anything after the </div> is extraneous, so remove it.  For example Firefox's parser leaves 
		xmlStr = xmlStr.substring(0, endTagIndex + endTag.length);
		return xmlStr;
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
			xmlStr = new XMLSerializer().serializeToString(xmlDoc.firstChild);
			if (!xmlStr)
			{
				// Safari < 2.0 only accepts XML documents, not nodes.
				// It also does not correctly serialize attributes that have been accessed with elem.attributes.item(i).
				// So, we serialize manually.
				xmlStr = XmlElemToString(xmlDoc.firstChild);
  			}
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
		if (elem.tagName.toLowerCase() in { script: 1, object: 1, embed: 1, iframe: 1, frame: 1, frameset: 1, xml: 1 })
		{
			elem.parentNode.removeChild(elem);
			return;
		}
		if (elem.attributes.length > 0)
		{
			ProcessXmlAttrs(elem);
		}
		var nextSibling = null;
		for (var n = elem.firstChild; n != null; n = nextSibling)
		{
			nextSibling = n.nextSibling; // Remember the nextSibling in case n gets removed.
			switch (n.nodeType)
			{
				case 1: // ELEMENT_NODE
					ProcessXmlElem(n);
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
	}

	function ProcessXmlAttrs(elem)
	{
		var attrs = elem.attributes;
		var attrsToRemove = [];
		var styleValue = null;
		for (var i = 0; i < attrs.length; i++)
		{
			var attr = attrs.item(i);
			var name = attr.name;
			var val = attr.value;
			if (/^on.*/i.test(name)                                     // Event handler attributes
				|| (name.toLowerCase() in { href: 1, src: 1, url: 1}       // URL attribute with...
					&& /^(http:|https:|ftp:|mailto:)/i.test(val) == false)   // ... invalid protocol
				|| name == "neathtml_style")                               // Reserved attribute name
			{
				attrsToRemove.push(attr);
			}
			else if (name.toLowerCase() == "style")
			{
				// Remember the style value so we can put it in a private attribute
				styleValue = val;
				attrsToRemove.push(attr);
			}
		}

		// Remove the unwanted attributes
		for (var i = 0; i < attrsToRemove.length; i++)
		{
			elem.removeAttributeNode(attrsToRemove[i]);
		}
		// If there was a style attribute, sanitize it's value and put it in our private attribute.
		// We must sanitize the style before we attempt to access any style properties (e.g. elem.style.overflow) in IE.
		// There doesn't seem to be a way to access the styles in IE after they have been parsed but before the 
		// script has been run.  For example, if you do:
		//
		// elem.style.cssText = "attr: expression(window.alert('XSS'));";
		//
		// the script will not run yet.  But, as soon as you access any of the style properties the script will run. 
		if (styleValue != null)
		{
			styleValue = SanitizeStyle(styleValue);
			elem.setAttribute("neathtml_style", styleValue);
		}
	}
	
	// Removes Javascript from style.
	function SanitizeStyle(s)
	{
		// Remove comments
		s = s.replace(/\x2F\*[^*]*\*+([^\x2F][^*]*\*+)*\x2F/gm, "");
		// Remove any left over comment markers
		s = s.replace(/\x2F\*|\*\x2F/, "");
		// Remove Unicode escapes
		s = s.replace(/\\[0-9a-f]{1,6}[ \n\r\t\f]?/g, "");
		// Remove all other escapes
		s = s.replace(/\\./g, "");
		
		// Rename all function calls except those to rgb()
		s = s.replace(/([A-Za-z]|[^\0-\177])([A-Za-z0-9-]|[^\0-\177])*\(/g, function (match) {
			if (match != "rgb(")
			{
				match = match.substring(0, match.length-1) + "NotAllowedByNeatHtml(";
			}
			return match;
		});
		return s;
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
	
	var testElem;
	function ProcessHtmlAttrs(elem)
	{
		var attrs = elem.attributes;
		var attrsToRemove = [];
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
			else if (name.toLowerCase() == "neathtml_style")
			{
				attrsToRemove.push(attr);
				if (!testElem)
				{
				 	testElem = document.createElement("A");
				}
				testElem.style.cssText = val;
				CopyStyleProperties(testElem, elem);
			}
		}
		// Remove the unwanted attributes
		for (var i = 0; i < attrsToRemove.length; i++)
		{
			elem.removeAttributeNode(attrsToRemove[i]);
		}
	}
	
	function CopyStyleProperties(src, dest)
	{
		// TODO: Filter properties when copying them.

		if (src.style.length == 0)
		{
			return;
		}
		// Copy the style properties
		if (src.style.getPropertyValue && dest.style.setProperty)
		{
			var propNames = [];
			if (src.style.item(0) !== null)
			{
				// The standard way to get the property names...
				for (var i = 0; i < src.style.length; i++)
				{
					propNames.push(src.style.item(i));
				}
			}
			else
			{
				// The Safari hack...
				var styles = src.style.cssText.split(/;/g);
				for (var i = 0; i < styles.length; i++)
				{
					var name = styles[i].replace(/[ \t\n\r]/, "").split(/:/g)[0];
					propNames.push(name);
				}
			}
			for (var i = 0; i < propNames.length; i++)
			{
				var p = propNames[i];
				var propValue = src.style.getPropertyValue(p);
				if (propValue != "")
				{
					dest.style.setProperty(p, propValue, null);
				}
			}
		}
		else
		{
			// The IE way
			for (var p in src.style)
			{
				if (p == "cssText" || p == "length") continue;
				var propValue = null;
				var propType = null;
				propValue = src.style[p];
				propType = typeof(propValue);
				if((propType != "string" && propType != "number" && propType != "boolean") 
					|| propValue == null || propValue == "")
				{
					continue;
				}
				dest.style[p] = propValue;
	 		}
	 	}
	}
};

