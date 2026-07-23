// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Markdown.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class LinkPolicyTests
{
	[TestMethod]
	public void ShouldAutoOpen_HttpHttpsMailto_AreTrue()
	{
		Assert.IsTrue(LinkPolicy.ShouldAutoOpen("http://example.com"));
		Assert.IsTrue(LinkPolicy.ShouldAutoOpen("https://example.com"));
		Assert.IsTrue(LinkPolicy.ShouldAutoOpen("HTTPS://EXAMPLE.COM"));
		Assert.IsTrue(LinkPolicy.ShouldAutoOpen("mailto:a@b.com"));
	}

	[TestMethod]
	public void ShouldAutoOpen_OtherSchemes_AreFalse()
	{
		Assert.IsFalse(LinkPolicy.ShouldAutoOpen("file:///etc/passwd"));
		Assert.IsFalse(LinkPolicy.ShouldAutoOpen("javascript:alert(1)"));
		Assert.IsFalse(LinkPolicy.ShouldAutoOpen("./relative/path"));
		Assert.IsFalse(LinkPolicy.ShouldAutoOpen(""));
		Assert.IsFalse(LinkPolicy.ShouldAutoOpen(null!));
	}

	[TestMethod]
	public void Activate_PrefersCallback_OverAutoOpen()
	{
		string? captured = null;
		LinkPolicy.Activate("file:///danger", url => captured = url);
		Assert.AreEqual("file:///danger", captured);
	}
}
