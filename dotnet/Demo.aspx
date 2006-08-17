<%@ Page language="c#" Src="Demo.aspx.cs" AutoEventWireup="false" Inherits="Brettle.Web.NeatHtml.Demo" ValidateRequest="false"%>
<Html>
	<Head>
		<Title>NeatHtml Demo</Title>
		<style type="text/css">
<!--
		.ProgressBar {
			margin: 0px;
			border: 0px;
			padding: 0px;
			width: 100%;
			height: 2em;
		}
-->
		</style>
	</Head>
	<Body>
		<script>
function NeatHtml_MoveTo(id, position)
{
	var textarea = document.getElementById(id);
	if (textarea.setSelectionRange)
	{
		textarea.focus();
		textarea.setSelectionRange(position, position);
	}
}
		</script>
		<form id="form" runat="server">
			<h1>NeatHtml Demo</h1>
			<p>
			This page demonstrates the basic functionality of NeatHtml under ASP.NET <%= System.Environment.Version %>.
			Enter some HTML source in the area below and click the submit button.
			</p>
			<textarea id="textarea" runat="server" rows=25 cols=80></textarea>
			<br/>
			<asp:Button id="submitButton" runat="server" Text="Submit" />
			</span>
			<br/>
			If NeatHtml accepts your HTML, it will be rendered in the red box below.  The red box will expand up to 800
			pixels wide and 
			500 pixels high.  If your HTML requires more space than that, you should see 
			scrollbars.  You should not be able to cause any script to execute and you should not be able to make
			anything appear outside the red box.  If you can, please email me (dean at brettle dot com) with the HTML
			you used and the browser you used.  Thanks!
			<br/>
			<div id="outputDiv" style="border: 2px solid red; max-width: 800px; max-height: 500px; min-height: 2em; overflow: auto; width: expression(Math.min(parseInt(this.offsetWidth), 800 ) + 'px'); height: expression(Math.min(parseInt(this.offsetHeight), 500 ) + 'px');" runat="server">
			</div>
		</form>
	</Body>
</Html>
