<%@ Page language="c#" Src="Demo.aspx.cs" AutoEventWireup="false" Inherits="Brettle.Web.NeatHtml.Demo" ValidateRequest="false"%>
<%@ Register TagPrefix="NeatHtml" Namespace="Brettle.Web.NeatHtml" Assembly="Brettle.Web.NeatHtml" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.01//EN" "http://www.w3.org/TR/html4/strict.dtd">
<html>
<head runat="server">
<title>NeatHtml Test</title>
<meta http-equiv="content-type" content="text/html; charset=UTF-8">
<meta http-equiv="content-style-type" content="text/css">
<meta http-equiv="expires" content="0">
<script type="text/javascript" src="/NeatHtml/NeatHtmlTest.js"></script>
<style>
.Trusted {
	counter-increment: trusted-num;
}

.Trusted:after {
	content: " #" counter(trusted-num);
}

label { font-weight: bold; }

#noCounterDisplay:after {
	content: "";
}
</style>
</head>
<body>
<form runat="server">
	<script type="text/javascript">
	// <![CDATA[
<%= Request.Params["NoScript"] == "true" ? "NeatHtmlTest.NoScript = true;" 
	: (Request.Params["NoScript"] == "false" ? "NeatHtmlTest.NoScript = false;" : "") %>
<%= Request.Params["NoNeatHtml"] == "true" ? "NeatHtmlTest.NoNeatHtml = true;" 
	: (Request.Params["NoNeatHtml"] == "false" ? "NeatHtmlTest.NoNeatHtml = false;" : "") %>
NeatHtmlTest.AppendTestStatusElement();	
	// ]]></script>

	<p class="Trusted" id="noCounterDisplay">

	<span id="trustedLinkBeforeParent" style="display: none;"><a id="trustedLinkBefore" href="#">This link</a> is for the ID spoofing test.  We don't normally display it because the tests automatically detect spoofing.</span>

	<a href="http://www.brettle.com/neathtml">NeatHtml&trade;</a> is displaying untrusted content in the box below.
	</p>
	
	<div style="border: solid red 2px;">
	<NeatHtml:UntrustedContent id="untrustedContent" runat="server" ClientSideFilterName="NeatHtmlTest.DefaultFilter">
	</NeatHtml:UntrustedContent>
	</div>
	<p id="trustedLinkAfterParent" style="display: none;"><a id="trustedLinkAfter" href="#">Another link</a> for
	the ID spoofing test that is not displayed because the test automatically detects spoofing.
	</p>

	<p class="Trusted" style="font-color: #FF3333">If the browser supports the CSS :after pseudo-element and the counter() function, then "#2" should appear to the right --> </p>

	<div id="showFilteredContentDiv" style="display:block;"><a href="javascript:void(0)" onclick="ShowActualFilteredContentTextarea();">View HTML source of filtered content displayed above</a></div>
	<div id="filteredContentDiv" style="display:none;">
	<p><a href="javascript:void(0)" onclick="HideActualFilteredContentTextarea();">Hide HTML source of filtered content displayed above</a></p>
	<label for="actualFilteredContentTextarea">Filtered Untrusted Content<br/></label>
		<textarea id="actualFilteredContentTextarea" runat="server" rows="25" cols="120" readonly="readonly"></textarea>
	</div>
	<script type="text/javascript">
	// <![CDATA[
	function ShowActualFilteredContentTextarea()
	{
		document.getElementById('filteredContentDiv').style.display = "block";
		document.getElementById('showFilteredContentDiv').style.display = "none";
		return false;
	}
	function HideActualFilteredContentTextarea()
	{
		document.getElementById('showFilteredContentDiv').style.display = "block";
		document.getElementById('filteredContentDiv').style.display = "none";
		return false;
	}
	// ]]>
	</script>	
	
	<h3>Try Different Test Content</h3>
	<p>
	<select id="selectedTest" runat="server">
		<option>Create Your Own Test...</option>
	</select>
	</p>
	<div id="customTestContentDiv">
	<p>
	Think you can break it? Fill out the form below and click the submit button.  The test page automatically runs
	some or all of the following tests:</p>
	<ul>
		<li>The "Markup invasion blocked" test fails if your untrusted content is able to affect the DOM in a way that changes the <code>nextSibling</code> of the DIV in which NeatHtml displays the filtered content.</li>
		<li>The "XSS blocked" test fails if your content calls <code>window.alert()</code> or <code>window.resizeTo()</code>. I'm obviously interested in stopping any XSS, but if you want to see the test fail you should try to get untrusted content to call one of those functions. This test is not run if you check the "Simulate browser with scripting disabled" checkbox.</li>
		<li>The "ID spoofing blocked" test fails if your content can cause <code>document.getElementById()</code> to return the wrong element for an ID of "trustedLinkBefore" or "trustedLinkAfter". There are links with those IDs before and after the DIV that displays the untrusted content. They are hidden (display:none) just to reduce clutter on the page. This test is not run if you check the "Simulate browser with scripting disabled" checkbox. 
		<li>The "Filtered content is correct" test will check the filtered content against a value that you specify.  This test is only run when the "Compare filtered content against expected value" checkbox is checked.  <strong>Note: The test content and expected result are initialized to whatever test you last ran.  If you modify the test content, you either need to modify the expected result or uncheck the "Compare filtered content against expected value" checkbox.</strong></li>
	</ul>
	<p>
	If you check the "Simulate browser with scripting disabled" checkbox, the test page intercepts and ignores all calls to NeatHtml's JavaScript functions so that the filtered content displayed is what a no-script user would see. Checking that box also causes all calls to <code>window.alert()</code> and <code>window.resizeTo()</code> to be ignored since XSS attacks would not run for no-script users. 
	</p>
	<p>
	Please email me (dean at brettle dot com) if you can make a test fail.
	I'm also interested in any other failure mode you find.  Thanks!
	</p>
	<label for="testContentTextarea">Test Untrusted Content<br/></label>
	<textarea id="testContentTextarea" runat="server" rows="25" cols="120"></textarea>
	
	
	<p><input id="checkFilteredContent" runat="server" type="checkbox" onchange="CheckFilteredContentChanged();"/> Compare filtered content against expected value
	<div id="expectedFilteredContent">
	<label for="expectedFilteredContentTextarea">Expected Filtered Untrusted Content<br/></label>
	<textarea id="expectedFilteredContentTextarea" name="expectedFilteredContentTextarea" rows="25" cols="120"></textarea>
	</div>
	
	<script type="text/javascript">
	// <![CDATA[
	CheckFilteredContentChanged();
	
	function CheckFilteredContentChanged()
	{
		if (document.getElementById('checkFilteredContent').checked)
			document.getElementById('expectedFilteredContent').style.display = "block";
		else
			document.getElementById('expectedFilteredContent').style.display = "none";
		return false;
	}
	// ]]>
	</script>	
	</div>
	
	<p>
		<input id="noScript" runat="server" type="checkbox" name="NoScript" value="true"/> Simulate browser with scripting disabled <br />
		<input id="noNeatHtml" runat="server" type="checkbox" name="NoNeatHtml" value="true"/> Disable NeatHtml (to see tests fail) <br />
	</p>
	<asp:Button id="submitButton" runat="server" Text="Submit" />

  	<script type="text/javascript">
	// <![CDATA[
	function SelectedTestChanged() {
		var selectedTest = document.getElementById('selectedTest');
		var customTestContentDiv = document.getElementById('customTestContentDiv');
		if (selectedTest.selectedIndex == selectedTest.options.length - 1)
			customTestContentDiv.style.display = "block";
		else
			customTestContentDiv.style.display = "none";
		return true;
	}
	document.getElementById('selectedTest').onchange = SelectedTestChanged;
	SelectedTestChanged();
	
	var expectedFilteredContent = <%= ToJsString(expectedFilteredContent) %>;
	var actualFilteredContent = <%= ToJsString(actualFilteredContent) %>;
	
	window.onload = function() 
	{
		if (typeof(NeatHtml.DefaultFilter.FilteredContent) != "undefined")
		{
			actualFilteredContent = NeatHtml.DefaultFilter.FilteredContent;
		}
		
		document.getElementById("expectedFilteredContentTextarea").value = expectedFilteredContent;
		document.getElementById("actualFilteredContentTextarea").value = actualFilteredContent;

		var tests = NeatHtmlTest.DefaultTests;
		if (NeatHtmlTest.NoScript)
		{
			tests = NeatHtmlTest.DefaultNoScriptTests
		}

		if (expectedFilteredContent.length > 0 && document.getElementById('checkFilteredContent').checked)
		{
			tests.push(["Filtered content is correct", function() {
				NeatHtmlTest.AssertEqualsCompressWhitespace(expectedFilteredContent + "\n", actualFilteredContent + "\n");
			}]);
		}
		
		NeatHtmlTest.RunTests(tests);
	}
	// ]]>
	</script>
</form>
</body>
</html>