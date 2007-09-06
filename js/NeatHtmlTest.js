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

NeatHtmlTest = {};

NeatHtmlTest.AppendTestStatusElement = function() {
	var scriptElems = document.getElementsByTagName("script");
	var callingScriptElem = scriptElems[scriptElems.length - 1];
	NeatHtmlTest.DetailsLink = document.createElement("a");
	NeatHtmlTest.DetailsLink.setAttribute("href", "#");
	callingScriptElem.parentNode.appendChild(NeatHtmlTest.DetailsLink);
	NeatHtmlTest.StatusDiv = document.createElement("blockquote");
	callingScriptElem.parentNode.appendChild(NeatHtmlTest.StatusDiv);
	NeatHtmlTest.ShowDetails();
};

NeatHtmlTest.ShowDetails = function()
{
	NeatHtmlTest.StatusDiv.style.display = "block";
	NeatHtmlTest.DetailsLink.innerHTML = "Hide details";
	NeatHtmlTest.DetailsLink.onclick = NeatHtmlTest.HideDetails;
}

NeatHtmlTest.HideDetails = function()
{
	NeatHtmlTest.StatusDiv.style.display = "none";
	NeatHtmlTest.DetailsLink.innerHTML = "Show details";
	NeatHtmlTest.DetailsLink.onclick = NeatHtmlTest.ShowDetails;
}

NeatHtmlTest.GetMode = function()
{
	if (!NeatHtmlTest._Mode)
	{
		NeatHtmlTest._Mode = "normal";
		var paramName = "NeatHtmlTestMode=";
		var queryParams=window.location.search.substring(1).split("&");
		for (var i = 0; i < queryParams.length; i++)
		{
			if (queryParams[i].substring(0, paramName.length) == paramName)
			{
				NeatHtmlTest._Mode = queryParams[i].substring(paramName.length, queryParams[i].length);
			}
		}
	}
	return NeatHtmlTest._Mode;
};

NeatHtmlTest.HtmlEncode = function(s) { return NeatHtml.Filter.prototype.HtmlEncode(s); };

NeatHtmlTest.RunTests = function(topSuite) {
	NeatHtmlTest.AppendOutput("<em>Please send bugs, comments, and questions to dean at brettle dot com</em><br/>");
	var statusCounts = {FAILED: 0, PASSED: 0};
	var statusNames = ["FAILED", "PASSED"];
	var statusColors = { FAILED: "#FF0000", PASSED: "#00FF00" };
	var numTests = 0;
	
	RunTestSuite(topSuite);
		
	var summary = document.createElement("div");
	summary.style.fontSize="18pt";
	NeatHtmlTest.StatusDiv.parentNode.insertBefore(summary, NeatHtmlTest.DetailsLink);
	var color;
	if (statusCounts["PASSED"] == numTests)
	{
		NeatHtmlTest.HideDetails();
		color = statusColors["PASSED"];
	}
	else if (statusCounts["FAILED"] > 0)
	{
		NeatHtmlTest.ShowDetails();
		color = statusColors["FAILED"];
	}
	else
	{
		NeatHtmlTest.HideDetails();
		color = "#FF9900";
	}
	var msg = "";	
	for (var i = 0; i < statusNames.length; i ++)
	{
		var name = statusNames[i];
		var count = statusCounts[name];
		if (count > 0)
		{
			if (msg.length > 0) 
				msg += ", ";
			msg += count + "/" + numTests + " " + NeatHtmlTest.HtmlEncode(name);
		}
	}
	summary.innerHTML = "<span style='background-color: " + color + ";'>" + msg + "</span>";
	
	function RunTestSuite(tests)
	{
		for (var i = 0; i < tests.length; i++)
		{
			if (tests[i] == null)
				continue;
			if (typeof(tests[i][1]) != "function")
			{
				RunTestSuite(tests[i][1]);
				continue;
			}
			numTests++;
			NeatHtmlTest.Status = "PASSED"
			NeatHtmlTest.StatusDetails = "";
			try
			{
				tests[i][1].call(window);
			}
			catch (ex)
			{
				if (NeatHtmlTest.Status == "PASSED")
					NeatHtmlTest.Status = "FAILED";
				NeatHtmlTest.StatusDetails += ex;
			}
			if (typeof(statusCounts[NeatHtmlTest.Status]) == "undefined")
			{
				statusNames.push(NeatHtmlTest.Status);
				statusCounts[NeatHtmlTest.Status] = 1;
			}
			else
			{
				statusCounts[NeatHtmlTest.Status]++;
			}
			var color = statusColors[NeatHtmlTest.Status] || "#FF9900";
			NeatHtmlTest.AppendOutput("<span style='color: " + color + ";'>" 
				+ NeatHtmlTest.HtmlEncode(NeatHtmlTest.Status) + ": "
				+ NeatHtmlTest.HtmlEncode(tests[i][0]) + "</span><br />");
			if (NeatHtmlTest.StatusDetails.length > 0)
			{
				NeatHtmlTest.AppendOutput("" 
					+ "<blockquote style='color: " + color + ";'>"
					+ "<pre>"
					+ NeatHtmlTest.HtmlEncode(NeatHtmlTest.StatusDetails)
					+ "</pre>"
					+ "</blockquote>" 
					+ "<br />");
			}
		}
	}
};

NeatHtmlTest.AppendOutput = function(html) {
	NeatHtmlTest.StatusDiv.innerHTML += html;
};

NeatHtmlTest.Log = function(msg) {
	NeatHtmlTest.AppendOutput('<span>' + NeatHtmlTest.HtmlEncode(msg) + '</span><br />');
	NeatHtmlTest.ShowDetails();
};

NeatHtmlTest.AlertCalls = 0;
NeatHtmlTest.AlertFromScript = function(msg) {
	if (NeatHtmlTest.GetMode() == "noscript") {
		return;
	}
	NeatHtmlTest.AlertCalls++;
	NeatHtmlTest.AppendOutput('<span style="color: red;">' + NeatHtmlTest.HtmlEncode(msg) + '</span><br />');
	NeatHtmlTest.ShowDetails();
};

NeatHtmlTest.AssertEquals = function (expected, actual, msg)
{
	if (typeof(msg) == "undefined")
		msg = "";
	else
		msg += ": ";
	if (typeof(expected) != typeof(actual)) {
		throw msg + "AssertEquals() Failed: types unequal: " + typeof(expected) + "!=" + typeof(actual);
	}
	if (expected != actual) {
		throw msg + "AssertEquals() Failed: " + NeatHtmlTest.QuoteString(expected) + "!=" + NeatHtmlTest.QuoteString(actual);
	}
};

NeatHtmlTest.AssertEqualsIgnoreWhitespace = function (expected, actual, msg)
{
	if (typeof(msg) == "undefined")
		msg = "";
	else
		msg += ": ";
	if (typeof(expected) != typeof(actual)) {
		throw msg + "AssertEquals() Failed: types unequal: " + typeof(expected) + "!=" + typeof(actual);
	}
	if (expected.replace(/[\r\n\t ]+/gm, " ") != actual.replace(/[\r\n\t ]+/gm, " ")) {
		throw msg + "AssertEquals() Failed: " + NeatHtmlTest.QuoteString(expected) + "!=" + NeatHtmlTest.QuoteString(actual);
	}
};

NeatHtmlTest.AssertMatches = function (re, actual, msg)
{
	if (typeof(msg) == "undefined")
		msg = "";
	else
		msg += ": ";
	if (! re.test(actual)) {
		throw msg + "AssertMatches() Failed: " + NeatHtmlTest.QuoteString(re) + "!=" + NeatHtmlTest.QuoteString(actual);
	}
};

NeatHtmlTest.QuoteString = function(s)
{
	if (typeof(s) != "string") return s;
	return "'" + s.replace(/'/g, "\\'").replace(/\n/g, "\\n'\n+'") + "'";    // " '
}

NeatHtmlTest.DefaultFilter = {};

NeatHtmlTest.Container = null;
NeatHtmlTest.AfterContainer = null;

NeatHtmlTest.DefaultFilter.BeginUntrusted = function() {
	var scriptElems = document.getElementsByTagName("script");
	var callingScriptElem = scriptElems[scriptElems.length - 1];
	NeatHtmlTest.Container = callingScriptElem;
	while (NeatHtmlTest.Container.tagName.toLowerCase() != "div")
		NeatHtmlTest.Container = NeatHtmlTest.Container.parentNode;
	if (NeatHtmlTest.GetMode() == "normal")
		NeatHtml.DefaultFilter.BeginUntrusted();
};

NeatHtmlTest.DefaultFilter.ProcessUntrusted = function() {
	if (NeatHtmlTest.GetMode() == "normal")
		NeatHtml.DefaultFilter.ProcessUntrusted();
};

NeatHtmlTest.DefaultFilter.ResizeContainer = function() {
	var scriptElems = document.getElementsByTagName("script");
	NeatHtmlTest.AfterContainer = scriptElems[scriptElems.length - 1];
	if (NeatHtmlTest.GetMode() == "normal")
		NeatHtml.DefaultFilter.ResizeContainer();
};

// Override the functions called by test XSS vectors so that we can detect that they
// were called and pretend that they weren't in noscript mode 
window.alert = function(msg)
{
	NeatHtmlTest.AlertFromScript(msg);
};

window.resizeTo = function(w, h)
{
	// Just count it as another alert() call. 
	alert("resizeTo(" + w + "," + h + ") called");
};

NeatHtmlTest.DefaultTests = [
			["Markup invasion blocked", function () {
				NeatHtmlTest.AssertEquals(NeatHtmlTest.AfterContainer, NeatHtmlTest.Container.nextSibling);
			}],
			["XSS blocked", function () {
				if (NeatHtmlTest.GetMode() == "noscript")
				{
					NeatHtmlTest.Status = "disabled in noscript mode";
					return;
				}
				NeatHtmlTest.AssertEquals(NeatHtmlTest.AlertCalls, 0);
			}],
			["ID spoofing blocked", function () {
				if (NeatHtmlTest.GetMode() == "noscript")
				{
					NeatHtmlTest.Status = "disabled in noscript mode";
					return;
				}
				function BeforeClicked()
				{
					NeatHtmlTest.Log("Trusted link before untrusted content clicked."); 
					return false;
				}
				function AfterClicked()
				{
					NeatHtmlTest.Log("Trusted link before untrusted content clicked."); 
					return false;
				}
				document.getElementById("trustedLinkBefore").onclick = BeforeClicked;
				document.getElementById("trustedLinkAfter").onclick = AfterClicked;
				NeatHtmlTest.AssertEquals(BeforeClicked, document.getElementById("trustedLinkBeforeParent").firstChild.onclick);
				NeatHtmlTest.AssertEquals(AfterClicked, document.getElementById("trustedLinkAfterParent").firstChild.onclick);			
			}],
			null
];