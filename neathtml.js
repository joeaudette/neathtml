/*
NeatHtml - for preventing XSS in untrusted HTML
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

NeatHtml.EndUntrusted = function() {
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
			xmlDoc = (new DOMParser()).parseFromString(xmlStr, "text/xml");
		}
		else
		{
			try
			{
				xmlDoc = document.implementation.createDocument("", null, null);
			}
			catch (ex) 
			{
				xmlDoc = new ActiveXObject("Microsoft.XMLDOM");
			}
		
			xmlDoc.async = false;
			xmlDoc.loadXML(xmlStr);
		}
		if ((xmlDoc.parseError && xmlDoc.parseError.errorCode)
			|| xmlDoc.documentElement.tagName == "parsererror"
			|| xmlDoc.documentElement.tagName == "html" // Konqueror returns an error HTML document
			|| xmlDoc.getElementsByTagName("parsererror").length > 0)
		{
	  		throw "Untrusted HTML could not be parsed."
		}
		return xmlDoc;
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
				xmlStr = new XMLSerializer().serializeToString(xmlDoc);
  			}
  		} 
  		else
  		{
  			xmlStr = xmlDoc.firstChild.xml;
  		}
		return xmlStr;
	}
	
	function ProcessXmlElem(elem)
	{
		if (elem.tagName in { SCRIPT: 1, OBJECT: 1, EMBED: 1, IFRAME: 1, FRAME: 1, FRAMESET: 1, XML: 1 })
		{
			elem.parentNode.removeNode(elem);
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
				default: // Remove everything else, including comments and CDATA sections
					elem.removeNode(n);
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
		// If there was a style attribute, put it's value in our private attribute.
		if (styleValue != null)
		{
			elem.setAttribute("neathtml_style", styleValue);
		}
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
			parent.style.height = parent.firstChild.scrollHeight;
			parent.style.width = parent.firstChild.scrollWidth;
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
				CopySafeStyleProperties(testElem, elem);
			}
		}
		// Remove the unwanted attributes
		for (var i = 0; i < attrsToRemove.length; i++)
		{
			elem.removeAttributeNode(attrsToRemove[i]);
		}
	}
	
	function CopySafeStyleProperties(src, dest)
	{
		// Copy the style properties that don't contain script
		for (var p in src.style)
		{
			var propValue = src.style[p];
			var propType = typeof(propValue);
			if (p == "cssText" || p == "length"          
				|| (propType != "string" && propType != "number" && propType != "boolean") 
				|| propValue == null || propValue == ""
				|| propValue.match(/expression|url/i)) 
			{
				continue;
			}
			dest.style[p] = propValue;
 		}			
	}
};

