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
	NeatHtmlTest.StatusDiv = document.createElement("div");
	callingScriptElem.parentNode.appendChild(NeatHtmlTest.StatusDiv);
};

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

NeatHtmlTest.HtmlEncode = NeatHtml.Filter.prototype.HtmlEncode;

NeatHtmlTest.RunTests = function(tests) {
	NeatHtmlTest.Log("Starting tests");
	var statusCounts = {FAILED: 0, PASSED: 0};
	var statusNames = ["FAILED", "PASSED"];
	var statusColors = { FAILED: "#FF0000", PASSED: "#00FF00" };
	var numTests = 0;
	for (var i = 0; i < tests.length; i++)
	{
		if (tests[i] == null)
			continue;
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
	var summary = document.createElement("h2");
	NeatHtmlTest.StatusDiv.parentNode.insertBefore(summary, NeatHtmlTest.StatusDiv);
	var color;
	if (statusCounts["PASSED"] == numTests)
		color = statusColors["PASSED"];
	else if (statusCounts["FAILED"] > 0)
		color = statusColors["FAILED"];
	else
		color = "#FF9900";

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
	summary.innerHTML = "<span style='color: " + color + ";'>" + msg + "</span>";
};

NeatHtmlTest.AppendOutput = function(html) {
	NeatHtmlTest.StatusDiv.innerHTML += html;
};

NeatHtmlTest.Log = function(msg) {
	NeatHtmlTest.AppendOutput('<span>' + NeatHtmlTest.HtmlEncode(msg) + '</span><br />');
};

NeatHtmlTest.AlertCalls = 0;
NeatHtmlTest.AlertFromScript = function(msg) {
	if (NeatHtmlTest.GetMode() == "noscript") {
		return;
	}
	NeatHtmlTest.AlertCalls++;
	NeatHtmlTest.AppendOutput('<span style="color: red;">' + NeatHtmlTest.HtmlEncode(msg) + '</span><br />');
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


NeatHtml.DefaultFilter.BeginUntrusted = function() {
	if (NeatHtmlTest.GetMode() == "normal")
		NeatHtml.Filter.prototype.BeginUntrusted.call(this);
};

NeatHtml.DefaultFilter.ProcessUntrusted = function() {
	if (NeatHtmlTest.GetMode() == "normal")
		NeatHtml.Filter.prototype.ProcessUntrusted.call(this);
};

window.alert = function(msg)
{
	NeatHtmlTest.AlertFromScript(msg);
};
