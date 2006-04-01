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
			This page demonstrates the basic functionality of NeatHtml.
			Enter some HTML source in the area below and click submit.
			</p>
			<textarea id="textarea" runat="server" rows=25 cols=80></textarea>
			<br/>
			<asp:Button id="submitButton" runat="server" Text="Submit" />
			</span>
			<div id="outputDiv" runat="server">
			</div>
		</form>
	</Body>
</Html>
