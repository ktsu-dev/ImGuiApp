// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp.Test;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

[TestClass]
public class FontHelperTests
{
	[TestMethod]
	public void FontHelper_CleanupCustomFonts_DoesNotThrow()
	{
		// This method should be safe to call even without initialization
		FontHelper.CleanupCustomFonts();
		Assert.IsTrue(true); // If we get here, it didn't throw
	}

	[TestMethod]
	public void FontHelper_CleanupGlyphRanges_DoesNotThrow()
	{
		// This method should be safe to call even without initialization
		FontHelper.CleanupGlyphRanges();
		Assert.IsTrue(true); // If we get here, it didn't throw
	}

	[TestMethod]
	public void FontHelper_IsStaticClass()
	{
		Type fontHelperType = typeof(FontHelper);

		// Verify it's a static class (abstract and sealed)
		Assert.IsTrue(fontHelperType.IsAbstract && fontHelperType.IsSealed);
	}
}