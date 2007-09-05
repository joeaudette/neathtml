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

#noCounterDisplay:after {
	content: "";
}
</style>
</head>
<body>
<form runat="server">
	<a href="?NeatHtmlTestMode=normal">Test with javascript</a>
	<a href="?NeatHtmlTestMode=noscript">Test without javascript (simulated)</a>
	<a href="?NeatHtmlTestMode=unsafe">Test without client-side NeatHtml</a>
	<br />

	<script type="text/javascript">
	// <![CDATA[
NeatHtmlTest.AppendTestStatusElement();	
	// ]]></script>

	<p class="Trusted" id="noCounterDisplay">

	<span id="trustedLinkBeforeParent" style="display: none;"><a id="trustedLinkBefore" href="#">This link</a> is for the ID spoofing test.  We don't normally display it because the tests automatically detect spoofing.</span>

	<a href="http://www.brettle.com/neathtml">NeatHtml&trade;</a> is displaying untrusted content in the box below
	</p>
	
	<div style="border: solid red 2px;">
	<NeatHtml:UntrustedContent id="untrustedContent" runat="server" ClientSideFilterName="NeatHtmlTest.DefaultFilter">
			<p>Here is a table with lots of XSS attacks and tag soup markup:</p>
			<table border=1>
				<tr>
					<th>Script attacks
					<td>script element<br/>
						<script>
						window.alert('XSS from script element');
						</script>
						script elem in CDATA<br/>
						<![CDATA[
						<script>
						window.alert('XSS from script element in CDATA section');
						</script>
						]]>						
					<td>
						<a onclick=window.alert('XSS_on_click') href='http://www.google.com/'>on* attr</a><br />
						<a href=javascript:alert('XSS_on_link')>javascript href</a><br />
					<td id='styleXss' style='nonstandard-attribute1: expression(alert(&quot;XSS from style&quot;)); nonstandard-attribute2: expr/**/ession(alert(&quot;XSS from style with comment&quot;)); nonstandard-attribute3: expres\000073ion(alert(&quot;XSS from style with escape&quot;)); background-color: rgb(192,255,192);'>
						green despite script in style
					<td>
						<a id='trustedLinkBefore' href='http://www.google.com/'>spoof existing ID</a><br />
						<a id='trustedLinkAfter' href='http://www.google.com/'>spoof future ID</a><br />
				<tr>
					<th>Tag soup
					<td><ul><li>no &lt;/li> 1/2<li>no &lt;/li> 2/2</ul>
					<td>unmatched &lt;/em> </em>
					<td><B>varying</B> <i>case</I> <U>tags</u>
					<td><p>line<br>break with &lt;br&gt;
				<tr>
					<th>Special characters and attributes
					<td><a implicit_attr href=http://www.google.com/search?hl=en&q=neathtml&btnG=Search>unquoted and unencoded link</a>
					<td><font face='&#9;&#0;
'>non-printable</font> and <font face='&#xdead;'>non-ascii</font> attr values
					<td>A B C with entities: &#65;&nbsp;&#x42;&nbsp;&#X43;
					<td>Unencoded <, and &
			</table>
			<br/>
			<table border=1 style="border-spacing: 0;">
				<tr>
					<td style="counter-increment: trusted-num;">Increment a CSS counter.  For result, see just under the
						untrusted content box.
						This table has a nested table.  It will be split in noscript mode (known limitation).
					</td>
					<td id="innerTableContainer">
						<table id="innerTable" border=1>
							<tr>
								<td style="background-image: url(http://www.brettle.com/Data/Sites/1/logos/deanatwork_sidesmall.jpg)">style="background-image: url(...)"</td>
								<td style="background-color: rgb(0,255,0);">style="background-color: rgb(...);"</td>
							</tr>
						</table>
					</td>
					<td>
					Try to break out of the layout jail
					<div style="position: absolute; top: 0; right: 0; color: red;">Let me out!</div>
					<div style="position: absolute; top: -100px; right: 0; color: red;">Let me out with a negative top property!</div>					</td>
				</tr>
			</table>

			Try to break out of the markup jail...
			</table>
            </div>
            </div>
            </div>
			"Help! Let me out of this box!"
			Try to pull trusted content into the box...
			<plaintext><iframe><object><script>
	</NeatHtml:UntrustedContent>
	</div>
	<p id="trustedLinkAfterParent" style="display: none;"><a id="trustedLinkAfter" href="#">Another link</a> for
	the ID spoofing test that is not displayed because the test automatically detects spoofing.
	</p>

	<p class="Trusted" style="font-color: #FF3333">Known limitation: This should not say "#3" but does in noscript mode if CSS counters
	and :after elements are supported --> </p>

	<p>
	Think you can break it? Enter some untrusted content in the area below and click the submit button.  Please
	email me (dean at brettle dot com) if you can make a test fail.  A test will fail if you can get your content
	to call window.alert(), spoof the trustedLinkBefore or trustedLinkAfter IDs, or	escape from the markup jail.
	I'm also interested in any other failure mode you find.  Thanks!
	</p>
	<textarea id="textarea" runat="server" rows=25 cols=120></textarea>
	<br/>
	<asp:Button id="submitButton" runat="server" Text="Submit" />

  	<script type="text/javascript">
	// <![CDATA[
	window.onload = function() 
	{
		NeatHtmlTest.RunTests([
			// The DefaultTests will detect:
			//    any calls to window.alert()
			//    any spoofing of the trustedLinkBefore or trustedLinkAfter IDs
			//    any escape from the markup jail
			["Default test suite", NeatHtmlTest.DefaultTests],  
			
			// Add more tests like the example below (see NeatHtmlTest.DefaultTests for examples):
/*
			["Test name", function () {
				AssertEquals(expected, actual);
				AssertMatches(regex, actual);
			}],
*/
			null
		]);
	}
	// ]]>
	</script>
</form>
</body>
</html>