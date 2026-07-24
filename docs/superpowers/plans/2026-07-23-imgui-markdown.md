# ktsu.ImGui.Markdown Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a new `ktsu.ImGui.Markdown` package that renders CommonMark markdown inside Dear ImGui, parsing with Markdig and owning only the immediate-mode rendering layer.

**Architecture:** Markdig parses source text into an AST; a block renderer walks blocks and an inline renderer builds styled runs that a pure `InlineLayout` engine word-wraps before drawing. The package is layered like `ImGui.Widgets` (no dependency on `ImGui.App`); heading sizes derive from the live `ImGui.GetFontSize()` so DPI and accessibility scaling are respected without an App reference. Fonts for bold/italic/headings resolve through an optional config delegate with a faux-styling fallback.

**Tech Stack:** C#, .NET (net10.0/net9.0/net8.0), Hexa.NET.ImGui bindings, Markdig, MSTest, ktsu.Sdk.

## Global Constraints

- File header on every `.cs` file, verbatim:
  ```
  // Copyright (c) ktsu.dev
  // All rights reserved.
  // Licensed under the MIT license.
  ```
- Tabs for indentation. CRLF line endings. File-scoped namespaces with `using` directives inside the namespace.
- Explicit types (no `var`). No `this.` qualifier. Always use braces. Primary constructors when appropriate. Explicit accessibility modifiers everywhere.
- Nullable reference types enabled. Warnings as errors.
- Root namespace `ktsu.ImGui.Markdown`. Package id `ktsu.ImGui.Markdown`.
- Target frameworks `net10.0;net9.0;net8.0` for the library. Test and example projects target `net10.0` only.
- Do NOT create a manual `AssemblyInfo.cs` — the ktsu.Sdk generates assembly attributes (siblings have none).
- No global warning suppressions. Use targeted `[SuppressMessage]` with justification, or a documented preprocessor fallback, only when necessary.
- Central package management: package versions live in `Directory.Packages.props`; project files reference packages without versions.
- Use `Ensure.NotNull()` (from Polyfill) for public-method argument null checks.
- MSTest with semantic asserts (`Assert.AreEqual`, `Assert.IsTrue`, `CollectionAssert`, etc.), never bare `Assert.IsTrue(a == b)`.
- Prefer `ktsu.Semantics` and `ImGui.Color`/`ImGui.Styler` types already used across the suite; color math delegates to `ImGui.Color`.

---

## File Structure

```
ImGui.Markdown/
  ImGui.Markdown.csproj          # project + Markdig + Color/Styler refs
  MarkdownConfig.cs              # MarkdownConfig record, MarkdownFontRole, MarkdownImageResult
  LinkPolicy.cs                  # pure: which schemes auto-open; OS-open wrapper
  InlineLayout.cs                # pure: word-wrap runs into lines (injected measure)
  MarkdownParser.cs              # Markdig pipeline + bounded source-keyed parse cache
  MarkdownDocument.cs            # public instance: parse once, render many
  ImGuiMarkdown.cs               # public static entry: Render(string, config)
  Rendering/
    MarkdownSizing.cs            # pure: heading pixel sizes; font role selection
    ListMarker.cs                # pure: bullet/number/checkbox marker text
    InlineBuilder.cs             # pure: AST ContainerInline -> IReadOnlyList<InlineRun>
    InlineRenderer.cs            # ImGui draw: lay out + draw runs, links, inline code, images
    MarkdownColors.cs            # theme color resolution + overrides
    BlockRenderer.cs             # ImGui draw: walk blocks -> inline renderer
tests/ImGui.Markdown.Tests/
  ImGui.Markdown.Tests.csproj
  LinkPolicyTests.cs
  InlineLayoutTests.cs
  MarkdownParserTests.cs
  ListMarkerTests.cs
  MarkdownSizingTests.cs
  InlineBuilderTests.cs
  MarkdownConfigTests.cs
examples/ImGuiMarkdownDemo/
  ImGuiMarkdownDemo.csproj
  ImGuiMarkdownDemo.cs
  README.md (optional)
ImGui.Markdown/README.md         # package readme
```

---

### Task 1: Package scaffold, Markdig dependency, solution wiring

Creates the library and test projects, adds Markdig via central package management, registers both projects in the solution, and proves the build/test harness is green with one trivial test.

**Files:**
- Create: `ImGui.Markdown/ImGui.Markdown.csproj`
- Create: `tests/ImGui.Markdown.Tests/ImGui.Markdown.Tests.csproj`
- Create: `tests/ImGui.Markdown.Tests/HarnessSmokeTest.cs`
- Modify: `Directory.Packages.props` (add Markdig version)
- Modify: `ImGui.sln` (add both projects)

**Interfaces:**
- Consumes: nothing.
- Produces: buildable `ktsu.ImGui.Markdown` assembly and `ktsu.ImGui.Markdown.Tests` test assembly; `InternalsVisibleTo` grants the test project access to `internal` members.

- [ ] **Step 1: Create the library csproj**

Create `ImGui.Markdown/ImGui.Markdown.csproj`:

```xml
<Project>
  <Sdk Name="Microsoft.NET.Sdk" />
  <Sdk Name="ktsu.Sdk" />

  <PropertyGroup>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <TargetFrameworks>net10.0;net9.0;net8.0</TargetFrameworks>
  </PropertyGroup>

  <ItemGroup>
    <InternalsVisibleTo Include="ktsu.ImGui.Markdown.Tests" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Hexa.NET.ImGui" />
    <PackageReference Include="Markdig" />
    <PackageReference Include="ktsu.ScopedAction" />
    <PackageReference Include="ktsu.Semantics.Color" />
    <ProjectReference Include="..\ImGui.Color\ImGui.Color.csproj" />
    <ProjectReference Include="..\ImGui.Styler\ImGui.Styler.csproj" />
    <PackageReference Include="Polyfill" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Add Markdig to central package management**

Run (adds a `PackageVersion` entry pinned to the latest stable Markdig):

```bash
dotnet add ImGui.Markdown/ImGui.Markdown.csproj package Markdig
```

Then confirm `Directory.Packages.props` now contains a `<PackageVersion Include="Markdig" Version="..." />` line. If `dotnet add` instead wrote a versioned `<PackageReference>` into the csproj, move the version to `Directory.Packages.props` and strip the version from the csproj (central management requires unversioned references).

- [ ] **Step 3: Create the test csproj**

Create `tests/ImGui.Markdown.Tests/ImGui.Markdown.Tests.csproj` (mirrors `tests/ImGui.Widgets.Tests`):

```xml
<Project>
  <Sdk Name="MSTest.Sdk" />
  <Sdk Name="ktsu.Sdk" />

  <PropertyGroup>
    <IsTestProject>true</IsTestProject>
    <TargetFramework>net10.0</TargetFramework>
    <TargetFrameworks></TargetFrameworks>
    <RootNamespace>ktsu.ImGui.Markdown.Tests</RootNamespace>
    <AssemblyName>$(RootNamespace)</AssemblyName>
    <EnableSourceLink>false</EnableSourceLink>
    <NoWarn>CA1051;CA1002;CA1062;CA1515;CA1707;CA1815;CA1819;CA1822;CA2227;CS8604;IDE0060;MSTEST0039</NoWarn>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\ImGui.Markdown\ImGui.Markdown.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 4: Write the harness smoke test**

Create `tests/ImGui.Markdown.Tests/HarnessSmokeTest.cs`:

```csharp
// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Markdown.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class HarnessSmokeTest
{
	[TestMethod]
	public void MarkdigResolvesAndParses()
	{
		Markdig.Syntax.MarkdownDocument document = Markdig.Markdown.Parse("# Hello");
		Assert.AreEqual(1, document.Count);
	}
}
```

- [ ] **Step 5: Register both projects in the solution**

Run:

```bash
dotnet sln ImGui.sln add ImGui.Markdown/ImGui.Markdown.csproj
dotnet sln ImGui.sln add tests/ImGui.Markdown.Tests/ImGui.Markdown.Tests.csproj
```

- [ ] **Step 6: Restore, build, and run the smoke test**

Run:

```bash
dotnet build ImGui.Markdown/ImGui.Markdown.csproj
dotnet test tests/ImGui.Markdown.Tests/ImGui.Markdown.Tests.csproj --filter "FullyQualifiedName~MarkdigResolvesAndParses"
```
Expected: build succeeds; test PASSES (document has one heading block).

- [ ] **Step 7: Commit**

```bash
git add ImGui.Markdown/ImGui.Markdown.csproj tests/ImGui.Markdown.Tests/ Directory.Packages.props ImGui.sln
git commit -m "feat(markdown): scaffold ktsu.ImGui.Markdown package with Markdig"
```

---

### Task 2: Configuration surface

Defines the public config record, the font-role enum, and the image-result struct with their defaults.

**Files:**
- Create: `ImGui.Markdown/MarkdownConfig.cs`
- Test: `tests/ImGui.Markdown.Tests/MarkdownConfigTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces:
  - `enum MarkdownFontRole { Body, Bold, Italic, BoldItalic, Code, H1, H2, H3, H4, H5, H6 }`
  - `readonly record struct MarkdownImageResult(nint TextureId, System.Numerics.Vector2 Size)`
  - `sealed record MarkdownConfig` with init properties: `Func<MarkdownFontRole, float, ImFontPtr?>? FontResolver`, `Action<string>? OnLinkClicked`, `Func<string, MarkdownImageResult?>? ImageResolver`, `IReadOnlyList<float> HeadingScales` (default `[2.0,1.6,1.35,1.15,1.0,0.9]`), `float? WrapWidth`, `float ListIndentPixels` (default 20), `float ParagraphSpacingPixels` (default 6), `ImGuiVector4? LinkColor`. Static `DefaultHeadingScales`.

- [ ] **Step 1: Write the failing test**

Create `tests/ImGui.Markdown.Tests/MarkdownConfigTests.cs`:

```csharp
// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Markdown.Tests;

using ktsu.ImGui.Markdown;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class MarkdownConfigTests
{
	[TestMethod]
	public void Defaults_HeadingScales_AreDescendingSixEntries()
	{
		MarkdownConfig config = new();
		Assert.AreEqual(6, config.HeadingScales.Count);
		Assert.AreEqual(2.0f, config.HeadingScales[0]);
		Assert.AreEqual(0.9f, config.HeadingScales[5]);
	}

	[TestMethod]
	public void Defaults_SpacingAndIndent_ArePositive()
	{
		MarkdownConfig config = new();
		Assert.IsTrue(config.ListIndentPixels > 0f);
		Assert.IsTrue(config.ParagraphSpacingPixels > 0f);
	}

	[TestMethod]
	public void Defaults_ResolversAreNull()
	{
		MarkdownConfig config = new();
		Assert.IsNull(config.FontResolver);
		Assert.IsNull(config.OnLinkClicked);
		Assert.IsNull(config.ImageResolver);
	}
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/ImGui.Markdown.Tests/ --filter "FullyQualifiedName~MarkdownConfigTests"`
Expected: FAIL to compile (`MarkdownConfig` not defined).

- [ ] **Step 3: Write the config source**

Create `ImGui.Markdown/MarkdownConfig.cs`:

```csharp
// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Markdown;

using System;
using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

/// <summary>
/// Identifies the typographic role of a run of markdown text so a font resolver can
/// supply the matching font variant.
/// </summary>
public enum MarkdownFontRole
{
	/// <summary>Normal body text.</summary>
	Body,
	/// <summary>Strong / bold text.</summary>
	Bold,
	/// <summary>Emphasised / italic text.</summary>
	Italic,
	/// <summary>Combined bold and italic text.</summary>
	BoldItalic,
	/// <summary>Monospace code text.</summary>
	Code,
	/// <summary>Level 1 heading.</summary>
	H1,
	/// <summary>Level 2 heading.</summary>
	H2,
	/// <summary>Level 3 heading.</summary>
	H3,
	/// <summary>Level 4 heading.</summary>
	H4,
	/// <summary>Level 5 heading.</summary>
	H5,
	/// <summary>Level 6 heading.</summary>
	H6,
}

/// <summary>
/// The result of resolving a local image source: the ImGui texture id and the size at
/// which to draw it.
/// </summary>
/// <param name="TextureId">The ImGui texture identifier.</param>
/// <param name="Size">The draw size in pixels.</param>
public readonly record struct MarkdownImageResult(nint TextureId, Vector2 Size);

/// <summary>
/// Rendering options for the markdown widgets. All members are optional; defaults produce
/// a self-contained renderer that uses faux emphasis styling and image placeholders.
/// </summary>
public sealed record MarkdownConfig
{
	/// <summary>Default heading size multipliers applied to the live body font size, H1 first.</summary>
	public static IReadOnlyList<float> DefaultHeadingScales { get; } = [2.0f, 1.6f, 1.35f, 1.15f, 1.0f, 0.9f];

	/// <summary>
	/// Resolves a font for a role at a target pixel size. Return <see langword="null"/> to
	/// fall back to the current font (with faux bold/italic styling for emphasis roles).
	/// </summary>
	public Func<MarkdownFontRole, float, ImFontPtr?>? FontResolver { get; init; }

	/// <summary>
	/// Invoked when a link is clicked. When <see langword="null"/>, http/https/mailto links
	/// open with the operating system default handler.
	/// </summary>
	public Action<string>? OnLinkClicked { get; init; }

	/// <summary>
	/// Resolves a local image source to a texture. Return <see langword="null"/> (or supply
	/// no resolver) to render a placeholder box with the alt text.
	/// </summary>
	public Func<string, MarkdownImageResult?>? ImageResolver { get; init; }

	/// <summary>Heading size multipliers applied to the live body font size, H1 first.</summary>
	public IReadOnlyList<float> HeadingScales { get; init; } = DefaultHeadingScales;

	/// <summary>Explicit wrap width in pixels; when <see langword="null"/>, the available content width is used.</summary>
	public float? WrapWidth { get; init; }

	/// <summary>Indentation applied per list nesting level, in pixels.</summary>
	public float ListIndentPixels { get; init; } = 20.0f;

	/// <summary>Vertical spacing added after paragraphs and blocks, in pixels.</summary>
	public float ParagraphSpacingPixels { get; init; } = 6.0f;

	/// <summary>Explicit link color; when <see langword="null"/>, a theme color is used.</summary>
	public ImGuiVector4? LinkColor { get; init; }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/ImGui.Markdown.Tests/ --filter "FullyQualifiedName~MarkdownConfigTests"`
Expected: 3 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add ImGui.Markdown/MarkdownConfig.cs tests/ImGui.Markdown.Tests/MarkdownConfigTests.cs
git commit -m "feat(markdown): add MarkdownConfig, MarkdownFontRole, MarkdownImageResult"
```

---

### Task 3: Link policy (pure)

Decides which URL schemes auto-open, and wraps the OS-open call so failures never throw into the render loop.

**Files:**
- Create: `ImGui.Markdown/LinkPolicy.cs`
- Test: `tests/ImGui.Markdown.Tests/LinkPolicyTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: `internal static class LinkPolicy` with `bool ShouldAutoOpen(string url)` and `void Activate(string url, Action<string>? onClicked)`.

- [ ] **Step 1: Write the failing test**

Create `tests/ImGui.Markdown.Tests/LinkPolicyTests.cs`:

```csharp
// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Markdown.Tests;

using ktsu.ImGui.Markdown;

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
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/ImGui.Markdown.Tests/ --filter "FullyQualifiedName~LinkPolicyTests"`
Expected: FAIL to compile (`LinkPolicy` not defined).

- [ ] **Step 3: Write the implementation**

Create `ImGui.Markdown/LinkPolicy.cs`:

```csharp
// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Markdown;

using System;
using System.Diagnostics;

/// <summary>
/// Determines how markdown links are activated and which schemes may be opened by the OS.
/// </summary>
internal static class LinkPolicy
{
	private static readonly string[] AutoOpenSchemes = ["http://", "https://", "mailto:"];

	/// <summary>
	/// Returns whether the given URL uses a scheme this renderer is willing to hand to the
	/// operating system default handler.
	/// </summary>
	/// <param name="url">The URL to test.</param>
	/// <returns><see langword="true"/> for http, https, and mailto; otherwise <see langword="false"/>.</returns>
	public static bool ShouldAutoOpen(string url)
	{
		if (string.IsNullOrEmpty(url))
		{
			return false;
		}

		foreach (string scheme in AutoOpenSchemes)
		{
			if (url.StartsWith(scheme, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Activates a clicked link: invokes the callback when supplied, otherwise opens
	/// auto-openable schemes with the OS default handler. Never throws.
	/// </summary>
	/// <param name="url">The clicked URL.</param>
	/// <param name="onClicked">Optional user callback that takes precedence over auto-open.</param>
	public static void Activate(string url, Action<string>? onClicked)
	{
		if (string.IsNullOrEmpty(url))
		{
			return;
		}

		if (onClicked is not null)
		{
			onClicked(url);
			return;
		}

		if (!ShouldAutoOpen(url))
		{
			return;
		}

		try
		{
			using Process? process = Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
		}
		catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException or System.IO.FileNotFoundException)
		{
			// Opening a URL is best-effort; swallow launcher failures so the render loop is unaffected.
		}
	}
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/ImGui.Markdown.Tests/ --filter "FullyQualifiedName~LinkPolicyTests"`
Expected: 3 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add ImGui.Markdown/LinkPolicy.cs tests/ImGui.Markdown.Tests/LinkPolicyTests.cs
git commit -m "feat(markdown): add LinkPolicy for scheme filtering and safe OS-open"
```

---

### Task 4: InlineLayout word-wrap engine (pure)

The core wrapping logic: given styled runs, a width, and an injected text-measure function, produce wrapped lines of positioned tokens. Pure and GPU-free so it is fully unit-testable.

**Files:**
- Create: `ImGui.Markdown/InlineLayout.cs`
- Test: `tests/ImGui.Markdown.Tests/InlineLayoutTests.cs`

**Interfaces:**
- Consumes: `MarkdownFontRole` (Task 2).
- Produces:
  - `internal readonly record struct InlineRun(string Text, MarkdownFontRole Role, string? LinkUrl, bool IsImage)`
  - `internal readonly record struct LaidOutToken(string Text, MarkdownFontRole Role, string? LinkUrl, bool IsImage, float X, float Width)`
  - `internal sealed class LaidOutLine { IReadOnlyList<LaidOutToken> Tokens; float Width; }`
  - `internal static class InlineLayout` with
    `IReadOnlyList<LaidOutLine> Wrap(IReadOnlyList<InlineRun> runs, float maxWidth, Func<string, MarkdownFontRole, bool, float> measure)` where the measure delegate's third argument is `isImage`.

Wrapping rules: each run's text is split on single spaces into word tokens (empty tokens from consecutive spaces are dropped). Tokens are packed greedily; a space of width `measure(" ", MarkdownFontRole.Body)` separates tokens placed on the same line. A token that does not fit starts a new line. A token wider than `maxWidth` is placed alone on its own line (no mid-word breaking in v1). `IsImage` runs become a single token whose text is the run text (used later as the image key) and are never split.

- [ ] **Step 1: Write the failing tests**

Create `tests/ImGui.Markdown.Tests/InlineLayoutTests.cs`:

```csharp
// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Markdown.Tests;

using System;
using System.Collections.Generic;

using ktsu.ImGui.Markdown;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class InlineLayoutTests
{
	// Each character is one unit wide; role/isImage ignored. A single space is one unit.
	private static float Measure(string text, MarkdownFontRole role, bool isImage) => text.Length;

	[TestMethod]
	public void Wrap_ShortRun_ProducesSingleLine()
	{
		List<InlineRun> runs = [new("hello world", MarkdownFontRole.Body, null, false)];
		IReadOnlyList<LaidOutLine> lines = InlineLayout.Wrap(runs, 100f, Measure);
		Assert.AreEqual(1, lines.Count);
		Assert.AreEqual(2, lines[0].Tokens.Count);
	}

	[TestMethod]
	public void Wrap_BreaksAtWordBoundaryWhenExceedingWidth()
	{
		// "aaa bbb ccc" with width 7: "aaa bbb" (3+1+3=7) fits, "ccc" wraps.
		List<InlineRun> runs = [new("aaa bbb ccc", MarkdownFontRole.Body, null, false)];
		IReadOnlyList<LaidOutLine> lines = InlineLayout.Wrap(runs, 7f, Measure);
		Assert.AreEqual(2, lines.Count);
		Assert.AreEqual(2, lines[0].Tokens.Count);
		Assert.AreEqual(1, lines[1].Tokens.Count);
		Assert.AreEqual("ccc", lines[1].Tokens[0].Text);
	}

	[TestMethod]
	public void Wrap_TokenWiderThanWidth_GetsOwnLine()
	{
		List<InlineRun> runs = [new("supercalifragilistic", MarkdownFontRole.Body, null, false)];
		IReadOnlyList<LaidOutLine> lines = InlineLayout.Wrap(runs, 5f, Measure);
		Assert.AreEqual(1, lines.Count);
		Assert.AreEqual(1, lines[0].Tokens.Count);
	}

	[TestMethod]
	public void Wrap_TokenXPositions_AccountForSpaces()
	{
		List<InlineRun> runs = [new("ab cd", MarkdownFontRole.Body, null, false)];
		IReadOnlyList<LaidOutLine> lines = InlineLayout.Wrap(runs, 100f, Measure);
		Assert.AreEqual(0f, lines[0].Tokens[0].X);
		// "ab" width 2 + one space => second token starts at 3.
		Assert.AreEqual(3f, lines[0].Tokens[1].X);
	}

	[TestMethod]
	public void Wrap_ImageRun_IsSingleUnsplitToken()
	{
		List<InlineRun> runs = [new("logo.png", MarkdownFontRole.Body, null, true)];
		IReadOnlyList<LaidOutLine> lines = InlineLayout.Wrap(runs, 3f, Measure);
		Assert.AreEqual(1, lines[0].Tokens.Count);
		Assert.AreEqual("logo.png", lines[0].Tokens[0].Text);
		Assert.IsTrue(lines[0].Tokens[0].IsImage);
	}

	[TestMethod]
	public void Wrap_EmptyRuns_ProducesNoLines()
	{
		IReadOnlyList<LaidOutLine> lines = InlineLayout.Wrap([], 100f, Measure);
		Assert.AreEqual(0, lines.Count);
	}
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/ImGui.Markdown.Tests/ --filter "FullyQualifiedName~InlineLayoutTests"`
Expected: FAIL to compile (`InlineLayout`, `InlineRun`, etc. not defined).

- [ ] **Step 3: Write the implementation**

Create `ImGui.Markdown/InlineLayout.cs`:

```csharp
// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Markdown;

using System;
using System.Collections.Generic;

/// <summary>A styled run of inline content produced from the markdown AST.</summary>
/// <param name="Text">The run text (for images, the resolved src used as the image key).</param>
/// <param name="Role">The typographic role controlling font selection.</param>
/// <param name="LinkUrl">The enclosing link URL, or <see langword="null"/> when not a link.</param>
/// <param name="IsImage">Whether this run is an inline image rather than text.</param>
internal readonly record struct InlineRun(string Text, MarkdownFontRole Role, string? LinkUrl, bool IsImage);

/// <summary>A single positioned token within a laid-out line.</summary>
/// <param name="Text">The token text.</param>
/// <param name="Role">The typographic role.</param>
/// <param name="LinkUrl">The enclosing link URL, or <see langword="null"/>.</param>
/// <param name="IsImage">Whether this token is an inline image (its <paramref name="Text"/> is the image key).</param>
/// <param name="X">The token's horizontal offset from the line start, in pixels.</param>
/// <param name="Width">The measured token width, in pixels.</param>
internal readonly record struct LaidOutToken(string Text, MarkdownFontRole Role, string? LinkUrl, bool IsImage, float X, float Width);

/// <summary>A wrapped line of tokens.</summary>
internal sealed class LaidOutLine
{
	/// <summary>The positioned tokens on this line, in order.</summary>
	public required IReadOnlyList<LaidOutToken> Tokens { get; init; }

	/// <summary>The total line width in pixels (end of the last token).</summary>
	public required float Width { get; init; }
}

/// <summary>
/// Pure word-wrap engine. Splits styled runs into word tokens and greedily packs them into
/// lines that fit within a maximum width, using an injected measurement function.
/// </summary>
internal static class InlineLayout
{
	/// <summary>
	/// Wraps the given runs into lines no wider than <paramref name="maxWidth"/>.
	/// </summary>
	/// <param name="runs">The styled runs to lay out.</param>
	/// <param name="maxWidth">The maximum line width in pixels.</param>
	/// <param name="measure">Measures the pixel width of text for a given role.</param>
	/// <returns>The wrapped lines.</returns>
	public static IReadOnlyList<LaidOutLine> Wrap(
		IReadOnlyList<InlineRun> runs,
		float maxWidth,
		Func<string, MarkdownFontRole, bool, float> measure)
	{
		ArgumentNullException.ThrowIfNull(runs);
		ArgumentNullException.ThrowIfNull(measure);

		float spaceWidth = measure(" ", MarkdownFontRole.Body, false);

		List<LaidOutLine> lines = [];
		List<LaidOutToken> current = [];
		float cursorX = 0.0f;

		void FlushLine()
		{
			if (current.Count > 0)
			{
				lines.Add(new LaidOutLine { Tokens = current.ToArray(), Width = cursorX });
			}

			current = [];
			cursorX = 0.0f;
		}

		foreach (InlineRun run in runs)
		{
			IEnumerable<string> tokens = run.IsImage
				? [run.Text]
				: SplitWords(run.Text);

			foreach (string token in tokens)
			{
				float tokenWidth = measure(token, run.Role, run.IsImage);
				float advance = current.Count == 0 ? 0.0f : spaceWidth;

				if (current.Count > 0 && cursorX + advance + tokenWidth > maxWidth)
				{
					FlushLine();
					advance = 0.0f;
				}

				float x = cursorX + advance;
				current.Add(new LaidOutToken(token, run.Role, run.LinkUrl, run.IsImage, x, tokenWidth));
				cursorX = x + tokenWidth;
			}
		}

		FlushLine();
		return lines;
	}

	private static IEnumerable<string> SplitWords(string text)
	{
		foreach (string part in text.Split(' '))
		{
			if (part.Length > 0)
			{
				yield return part;
			}
		}
	}
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/ImGui.Markdown.Tests/ --filter "FullyQualifiedName~InlineLayoutTests"`
Expected: 6 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add ImGui.Markdown/InlineLayout.cs tests/ImGui.Markdown.Tests/InlineLayoutTests.cs
git commit -m "feat(markdown): add pure InlineLayout word-wrap engine"
```

---

### Task 5: Markdig parser and bounded parse cache

Builds the Markdig pipeline, exposes parsing, and provides a bounded source-string-keyed cache so the static `Render` entry only re-parses when the text changes. Also adds the public `MarkdownDocument` instance.

**Files:**
- Create: `ImGui.Markdown/MarkdownParser.cs`
- Create: `ImGui.Markdown/MarkdownDocument.cs`
- Test: `tests/ImGui.Markdown.Tests/MarkdownParserTests.cs`

**Interfaces:**
- Consumes: nothing beyond Markdig.
- Produces:
  - `internal static class MarkdownParser` with `Markdig.Syntax.MarkdownDocument Parse(string markdown)` and `Markdig.Syntax.MarkdownDocument GetOrParse(string markdown)` (cached, bounded to 32 entries, thread-safe enough for single-threaded ImGui render use).
  - `public sealed class MarkdownDocument` with `MarkdownDocument(string markdown)`, `string Source { get; }`, and `internal Markdig.Syntax.MarkdownDocument Ast { get; }`. (`Render` is added in Task 9.)

- [ ] **Step 1: Write the failing tests**

Create `tests/ImGui.Markdown.Tests/MarkdownParserTests.cs`:

```csharp
// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Markdown.Tests;

using ktsu.ImGui.Markdown;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class MarkdownParserTests
{
	[TestMethod]
	public void Parse_ArbitraryText_DoesNotThrow()
	{
		Markdig.Syntax.MarkdownDocument doc = MarkdownParser.Parse("*unbalanced [text](");
		Assert.IsNotNull(doc);
	}

	[TestMethod]
	public void Parse_PipeTable_IsRecognized()
	{
		string md = "| a | b |\n|---|---|\n| 1 | 2 |\n";
		Markdig.Syntax.MarkdownDocument doc = MarkdownParser.Parse(md);
		int tables = 0;
		foreach (Markdig.Syntax.Block block in doc)
		{
			if (block is Markdig.Extensions.Tables.Table)
			{
				tables++;
			}
		}

		Assert.AreEqual(1, tables);
	}

	[TestMethod]
	public void GetOrParse_SameSource_ReturnsCachedInstance()
	{
		string md = "# Cached";
		Markdig.Syntax.MarkdownDocument first = MarkdownParser.GetOrParse(md);
		Markdig.Syntax.MarkdownDocument second = MarkdownParser.GetOrParse(md);
		Assert.AreSame(first, second);
	}

	[TestMethod]
	public void GetOrParse_ChangedSource_ReturnsNewInstance()
	{
		Markdig.Syntax.MarkdownDocument first = MarkdownParser.GetOrParse("# One");
		Markdig.Syntax.MarkdownDocument second = MarkdownParser.GetOrParse("# Two");
		Assert.AreNotSame(first, second);
	}

	[TestMethod]
	public void Document_ExposesSource()
	{
		MarkdownDocument document = new("# Title");
		Assert.AreEqual("# Title", document.Source);
	}
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/ImGui.Markdown.Tests/ --filter "FullyQualifiedName~MarkdownParserTests"`
Expected: FAIL to compile.

- [ ] **Step 3: Write the parser**

Create `ImGui.Markdown/MarkdownParser.cs`:

```csharp
// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Markdown;

using System.Collections.Generic;

using Markdig;

using MarkdigAst = Markdig.Syntax.MarkdownDocument;

/// <summary>
/// Parses markdown into a Markdig AST and caches parses keyed by source string so the
/// immediate-mode render loop does not re-parse unchanged text every frame.
/// </summary>
internal static class MarkdownParser
{
	private const int MaxCacheEntries = 32;

	private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
		.UsePipeTables()
		.UseTaskLists()
		.UseAutoLinks()
		.Build();

	// Insertion-ordered cache; oldest entry is evicted when the cap is exceeded.
	private static readonly Dictionary<string, MarkdigAst> Cache = [];
	private static readonly Queue<string> InsertionOrder = new();
	private static readonly object Gate = new();

	/// <summary>Parses markdown into a fresh AST using the configured pipeline.</summary>
	/// <param name="markdown">The markdown source.</param>
	/// <returns>The parsed document.</returns>
	public static MarkdigAst Parse(string markdown) => Markdown.Parse(markdown ?? string.Empty, Pipeline);

	/// <summary>Returns a cached AST for the source, parsing and caching it on first use.</summary>
	/// <param name="markdown">The markdown source.</param>
	/// <returns>The cached parsed document.</returns>
	public static MarkdigAst GetOrParse(string markdown)
	{
		string key = markdown ?? string.Empty;
		lock (Gate)
		{
			if (Cache.TryGetValue(key, out MarkdigAst? cached))
			{
				return cached;
			}

			MarkdigAst parsed = Parse(key);
			Cache[key] = parsed;
			InsertionOrder.Enqueue(key);

			while (InsertionOrder.Count > MaxCacheEntries)
			{
				string evict = InsertionOrder.Dequeue();
				Cache.Remove(evict);
			}

			return parsed;
		}
	}
}
```

- [ ] **Step 4: Write the document type**

Create `ImGui.Markdown/MarkdownDocument.cs`:

```csharp
// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Markdown;

using MarkdigAst = Markdig.Syntax.MarkdownDocument;

/// <summary>
/// A parsed markdown document. Construct once from a source string and render it each frame;
/// the parse happens only in the constructor. Use this for hot render paths where the source
/// is stable across frames.
/// </summary>
public sealed class MarkdownDocument
{
	/// <summary>The original markdown source.</summary>
	public string Source { get; }

	/// <summary>The parsed Markdig AST.</summary>
	internal MarkdigAst Ast { get; }

	/// <summary>Initializes a new instance parsed from the given markdown source.</summary>
	/// <param name="markdown">The markdown source to parse.</param>
	public MarkdownDocument(string markdown)
	{
		Source = markdown ?? string.Empty;
		Ast = MarkdownParser.Parse(Source);
	}
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test tests/ImGui.Markdown.Tests/ --filter "FullyQualifiedName~MarkdownParserTests"`
Expected: 5 tests PASS.

- [ ] **Step 6: Commit**

```bash
git add ImGui.Markdown/MarkdownParser.cs ImGui.Markdown/MarkdownDocument.cs tests/ImGui.Markdown.Tests/MarkdownParserTests.cs
git commit -m "feat(markdown): add Markdig pipeline, bounded parse cache, MarkdownDocument"
```

---

### Task 6: Sizing and list-marker helpers (pure)

Two pure helpers used by the renderers: heading pixel-size computation from the live body size, and list marker text formatting.

**Files:**
- Create: `ImGui.Markdown/Rendering/MarkdownSizing.cs`
- Create: `ImGui.Markdown/Rendering/ListMarker.cs`
- Test: `tests/ImGui.Markdown.Tests/MarkdownSizingTests.cs`
- Test: `tests/ImGui.Markdown.Tests/ListMarkerTests.cs`

**Interfaces:**
- Consumes: `MarkdownFontRole` (Task 2).
- Produces:
  - `internal static class MarkdownSizing` with `float HeadingPixelSize(float bodyPixelSize, int level, IReadOnlyList<float> scales)` and `MarkdownFontRole HeadingRole(int level)` and `MarkdownFontRole EmphasisRole(bool bold, bool italic)`.
  - `internal static class ListMarker` with `string For(bool ordered, int itemIndex, int startNumber, bool? taskChecked)`.

`HeadingPixelSize` clamps `level` to 1..6 and multiplies `bodyPixelSize` by `scales[level-1]` (falling back to 1.0 when scales is too short). `HeadingRole` clamps 1..6 to `H1..H6`. `For` returns `"{n}."` for ordered lists (n = startNumber + itemIndex), `"[x]"`/`"[ ]"` for task items, and a bullet `"-"` otherwise.

- [ ] **Step 1: Write the failing tests**

Create `tests/ImGui.Markdown.Tests/MarkdownSizingTests.cs`:

```csharp
// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Markdown.Tests;

using System.Collections.Generic;

using ktsu.ImGui.Markdown;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class MarkdownSizingTests
{
	private static readonly IReadOnlyList<float> Scales = MarkdownConfig.DefaultHeadingScales;

	[TestMethod]
	public void HeadingPixelSize_H1_DoublesBody()
	{
		Assert.AreEqual(28.0f, MarkdownSizing.HeadingPixelSize(14.0f, 1, Scales));
	}

	[TestMethod]
	public void HeadingPixelSize_ClampsOutOfRangeLevel()
	{
		Assert.AreEqual(14.0f * 2.0f, MarkdownSizing.HeadingPixelSize(14.0f, 0, Scales));
		Assert.AreEqual(14.0f * 0.9f, MarkdownSizing.HeadingPixelSize(14.0f, 9, Scales));
	}

	[TestMethod]
	public void HeadingRole_MapsLevelToRole()
	{
		Assert.AreEqual(MarkdownFontRole.H1, MarkdownSizing.HeadingRole(1));
		Assert.AreEqual(MarkdownFontRole.H6, MarkdownSizing.HeadingRole(6));
		Assert.AreEqual(MarkdownFontRole.H1, MarkdownSizing.HeadingRole(-3));
	}

	[TestMethod]
	public void EmphasisRole_CombinesBoldAndItalic()
	{
		Assert.AreEqual(MarkdownFontRole.Body, MarkdownSizing.EmphasisRole(false, false));
		Assert.AreEqual(MarkdownFontRole.Bold, MarkdownSizing.EmphasisRole(true, false));
		Assert.AreEqual(MarkdownFontRole.Italic, MarkdownSizing.EmphasisRole(false, true));
		Assert.AreEqual(MarkdownFontRole.BoldItalic, MarkdownSizing.EmphasisRole(true, true));
	}
}
```

Create `tests/ImGui.Markdown.Tests/ListMarkerTests.cs`:

```csharp
// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Markdown.Tests;

using ktsu.ImGui.Markdown;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class ListMarkerTests
{
	[TestMethod]
	public void For_Unordered_ReturnsBullet()
	{
		Assert.AreEqual("-", ListMarker.For(ordered: false, itemIndex: 0, startNumber: 1, taskChecked: null));
	}

	[TestMethod]
	public void For_Ordered_CountsFromStart()
	{
		Assert.AreEqual("1.", ListMarker.For(ordered: true, itemIndex: 0, startNumber: 1, taskChecked: null));
		Assert.AreEqual("4.", ListMarker.For(ordered: true, itemIndex: 2, startNumber: 2, taskChecked: null));
	}

	[TestMethod]
	public void For_Task_ReturnsCheckbox()
	{
		Assert.AreEqual("[ ]", ListMarker.For(ordered: false, itemIndex: 0, startNumber: 1, taskChecked: false));
		Assert.AreEqual("[x]", ListMarker.For(ordered: false, itemIndex: 0, startNumber: 1, taskChecked: true));
	}
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/ImGui.Markdown.Tests/ --filter "FullyQualifiedName~MarkdownSizingTests|FullyQualifiedName~ListMarkerTests"`
Expected: FAIL to compile.

- [ ] **Step 3: Write MarkdownSizing**

Create `ImGui.Markdown/Rendering/MarkdownSizing.cs`:

```csharp
// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Markdown;

using System;
using System.Collections.Generic;

/// <summary>Pure size and role computations shared by the renderers.</summary>
internal static class MarkdownSizing
{
	/// <summary>Computes the pixel size for a heading level from the live body size and scales.</summary>
	/// <param name="bodyPixelSize">The current body font pixel size (already DPI/accessibility scaled).</param>
	/// <param name="level">The heading level (clamped to 1..6).</param>
	/// <param name="scales">The heading size multipliers, H1 first.</param>
	/// <returns>The heading pixel size.</returns>
	public static float HeadingPixelSize(float bodyPixelSize, int level, IReadOnlyList<float> scales)
	{
		ArgumentNullException.ThrowIfNull(scales);
		int clamped = Math.Clamp(level, 1, 6);
		float scale = clamped - 1 < scales.Count ? scales[clamped - 1] : 1.0f;
		return bodyPixelSize * scale;
	}

	/// <summary>Maps a heading level (clamped 1..6) to its font role.</summary>
	/// <param name="level">The heading level.</param>
	/// <returns>The matching <see cref="MarkdownFontRole"/>.</returns>
	public static MarkdownFontRole HeadingRole(int level) => Math.Clamp(level, 1, 6) switch
	{
		1 => MarkdownFontRole.H1,
		2 => MarkdownFontRole.H2,
		3 => MarkdownFontRole.H3,
		4 => MarkdownFontRole.H4,
		5 => MarkdownFontRole.H5,
		_ => MarkdownFontRole.H6,
	};

	/// <summary>Combines bold/italic flags into the corresponding emphasis role.</summary>
	/// <param name="bold">Whether the run is bold.</param>
	/// <param name="italic">Whether the run is italic.</param>
	/// <returns>The matching <see cref="MarkdownFontRole"/>.</returns>
	public static MarkdownFontRole EmphasisRole(bool bold, bool italic) => (bold, italic) switch
	{
		(true, true) => MarkdownFontRole.BoldItalic,
		(true, false) => MarkdownFontRole.Bold,
		(false, true) => MarkdownFontRole.Italic,
		_ => MarkdownFontRole.Body,
	};
}
```

- [ ] **Step 4: Write ListMarker**

Create `ImGui.Markdown/Rendering/ListMarker.cs`:

```csharp
// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Markdown;

using System.Globalization;

/// <summary>Pure formatting for list item markers.</summary>
internal static class ListMarker
{
	/// <summary>Formats the marker text for a list item.</summary>
	/// <param name="ordered">Whether the enclosing list is ordered.</param>
	/// <param name="itemIndex">The zero-based index of the item within the list.</param>
	/// <param name="startNumber">The ordered list's starting number.</param>
	/// <param name="taskChecked">For task-list items, the checked state; otherwise <see langword="null"/>.</param>
	/// <returns>The marker text, e.g. "-", "3.", "[x]".</returns>
	public static string For(bool ordered, int itemIndex, int startNumber, bool? taskChecked)
	{
		if (taskChecked.HasValue)
		{
			return taskChecked.Value ? "[x]" : "[ ]";
		}

		if (ordered)
		{
			int number = startNumber + itemIndex;
			return number.ToString(CultureInfo.InvariantCulture) + ".";
		}

		return "-";
	}
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test tests/ImGui.Markdown.Tests/ --filter "FullyQualifiedName~MarkdownSizingTests|FullyQualifiedName~ListMarkerTests"`
Expected: 7 tests PASS.

- [ ] **Step 6: Commit**

```bash
git add ImGui.Markdown/Rendering/MarkdownSizing.cs ImGui.Markdown/Rendering/ListMarker.cs tests/ImGui.Markdown.Tests/MarkdownSizingTests.cs tests/ImGui.Markdown.Tests/ListMarkerTests.cs
git commit -m "feat(markdown): add pure sizing and list-marker helpers"
```

---

### Task 7: InlineBuilder — AST inlines to styled runs (pure)

Flattens a Markdig `ContainerInline` into the `InlineRun` list the layout engine consumes, tracking emphasis nesting and link context. Pure and testable.

**Files:**
- Create: `ImGui.Markdown/Rendering/InlineBuilder.cs`
- Test: `tests/ImGui.Markdown.Tests/InlineBuilderTests.cs`

**Interfaces:**
- Consumes: `InlineRun` (Task 4), `MarkdownSizing.EmphasisRole` (Task 6), Markdig inline types.
- Produces: `internal static class InlineBuilder` with `IReadOnlyList<InlineRun> Build(Markdig.Syntax.Inlines.ContainerInline? container)`.

Behavior: walks siblings via `FirstChild`/`NextSibling`. `LiteralInline` → a Body (or current-emphasis) run with `LinkUrl` from the enclosing link. `EmphasisInline` toggles bold/italic based on `DelimiterCount` (2+ = bold, 1 = italic) and recurses. `CodeInline` → a `Code` role run. `LinkInline` sets link context for its children; when `IsImage` is true it emits one image run (`IsImage: true`, text = `Url`, role = Body). `LineBreakInline` and `AutolinkInline` handled (autolink → link run with its URL as text). `HtmlInline` → literal run of its raw tag text.

- [ ] **Step 1: Write the failing tests**

Create `tests/ImGui.Markdown.Tests/InlineBuilderTests.cs`:

```csharp
// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Markdown.Tests;

using System.Collections.Generic;
using System.Linq;

using ktsu.ImGui.Markdown;

using Markdig.Syntax;
using Markdig.Syntax.Inlines;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class InlineBuilderTests
{
	private static ContainerInline? FirstParagraphInline(string md)
	{
		MarkdownDocument document = new(md);
		ParagraphBlock paragraph = document.Ast.Descendants<ParagraphBlock>().First();
		return paragraph.Inline;
	}

	[TestMethod]
	public void Build_BoldText_ProducesBoldRun()
	{
		IReadOnlyList<InlineRun> runs = InlineBuilder.Build(FirstParagraphInline("**strong**"));
		Assert.IsTrue(runs.Any(r => r.Role == MarkdownFontRole.Bold && r.Text.Contains("strong")));
	}

	[TestMethod]
	public void Build_ItalicText_ProducesItalicRun()
	{
		IReadOnlyList<InlineRun> runs = InlineBuilder.Build(FirstParagraphInline("*soft*"));
		Assert.IsTrue(runs.Any(r => r.Role == MarkdownFontRole.Italic));
	}

	[TestMethod]
	public void Build_InlineCode_ProducesCodeRun()
	{
		IReadOnlyList<InlineRun> runs = InlineBuilder.Build(FirstParagraphInline("`code`"));
		Assert.IsTrue(runs.Any(r => r.Role == MarkdownFontRole.Code && r.Text == "code"));
	}

	[TestMethod]
	public void Build_Link_AttachesUrlToChildRuns()
	{
		IReadOnlyList<InlineRun> runs = InlineBuilder.Build(FirstParagraphInline("[text](https://x.com)"));
		Assert.IsTrue(runs.Any(r => r.LinkUrl == "https://x.com" && !r.IsImage));
	}

	[TestMethod]
	public void Build_Image_ProducesImageRun()
	{
		IReadOnlyList<InlineRun> runs = InlineBuilder.Build(FirstParagraphInline("![alt](logo.png)"));
		Assert.IsTrue(runs.Any(r => r.IsImage && r.Text == "logo.png"));
	}

	[TestMethod]
	public void Build_Null_ReturnsEmpty()
	{
		IReadOnlyList<InlineRun> runs = InlineBuilder.Build(null);
		Assert.AreEqual(0, runs.Count);
	}
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/ImGui.Markdown.Tests/ --filter "FullyQualifiedName~InlineBuilderTests"`
Expected: FAIL to compile.

- [ ] **Step 3: Write the implementation**

Create `ImGui.Markdown/Rendering/InlineBuilder.cs`:

```csharp
// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Markdown;

using System.Collections.Generic;

using Markdig.Syntax.Inlines;

/// <summary>Flattens a Markdig inline tree into a flat list of styled runs for layout.</summary>
internal static class InlineBuilder
{
	/// <summary>Builds the run list for an inline container.</summary>
	/// <param name="container">The inline container, or <see langword="null"/>.</param>
	/// <returns>The flattened styled runs.</returns>
	public static IReadOnlyList<InlineRun> Build(ContainerInline? container)
	{
		List<InlineRun> runs = [];
		if (container is not null)
		{
			Walk(container, runs, bold: false, italic: false, linkUrl: null);
		}

		return runs;
	}

	private static void Walk(ContainerInline container, List<InlineRun> runs, bool bold, bool italic, string? linkUrl)
	{
		Inline? child = container.FirstChild;
		while (child is not null)
		{
			Append(child, runs, bold, italic, linkUrl);
			child = child.NextSibling;
		}
	}

	private static void Append(Inline inline, List<InlineRun> runs, bool bold, bool italic, string? linkUrl)
	{
		switch (inline)
		{
			case LiteralInline literal:
				runs.Add(new InlineRun(literal.Content.ToString(), MarkdownSizing.EmphasisRole(bold, italic), linkUrl, IsImage: false));
				break;

			case CodeInline code:
				runs.Add(new InlineRun(code.Content, MarkdownFontRole.Code, linkUrl, IsImage: false));
				break;

			case EmphasisInline emphasis:
				bool nowBold = bold || emphasis.DelimiterCount >= 2;
				bool nowItalic = italic || emphasis.DelimiterCount == 1;
				Walk(emphasis, runs, nowBold, nowItalic, linkUrl);
				break;

			case LinkInline link when link.IsImage:
				runs.Add(new InlineRun(link.Url ?? string.Empty, MarkdownFontRole.Body, linkUrl, IsImage: true));
				break;

			case LinkInline link:
				Walk(link, runs, bold, italic, link.Url ?? linkUrl);
				break;

			case AutolinkInline autolink:
				runs.Add(new InlineRun(autolink.Url, MarkdownSizing.EmphasisRole(bold, italic), autolink.Url, IsImage: false));
				break;

			case LineBreakInline:
				runs.Add(new InlineRun(" ", MarkdownFontRole.Body, linkUrl, IsImage: false));
				break;

			case HtmlInline html:
				runs.Add(new InlineRun(html.Tag, MarkdownSizing.EmphasisRole(bold, italic), linkUrl, IsImage: false));
				break;

			case ContainerInline nested:
				Walk(nested, runs, bold, italic, linkUrl);
				break;

			default:
				// Unknown inline: emit its text form so nothing is silently dropped.
				runs.Add(new InlineRun(inline.ToString() ?? string.Empty, MarkdownSizing.EmphasisRole(bold, italic), linkUrl, IsImage: false));
				break;
		}
	}
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/ImGui.Markdown.Tests/ --filter "FullyQualifiedName~InlineBuilderTests"`
Expected: 6 tests PASS.

Note: if `EmphasisInline.DelimiterCount` naming differs in the installed Markdig version, build errors will surface here; adapt to the actual property (older versions also expose `DelimiterCount`). Verify by building before adjusting the tests.

- [ ] **Step 5: Commit**

```bash
git add ImGui.Markdown/Rendering/InlineBuilder.cs tests/ImGui.Markdown.Tests/InlineBuilderTests.cs
git commit -m "feat(markdown): flatten AST inlines into styled runs"
```

---

### Task 8: Colors and font pushing (ImGui-coupled helpers)

Resolves theme colors and pushes/pops the resolved font for a role. These touch ImGui state, so they are verified by build + the demo rather than unit tests; keep them small.

**Files:**
- Create: `ImGui.Markdown/Rendering/MarkdownColors.cs`

**Interfaces:**
- Consumes: `MarkdownConfig`, `MarkdownFontRole`, `MarkdownSizing` (Tasks 2, 6).
- Produces:
  - `internal static class MarkdownColors` with `ImGuiVector4 Link(MarkdownConfig config)`, `uint InlineCodeBackground()`, `uint BlockquoteBar()`, `uint Separator()`, `uint TextU32()`.
  - `internal sealed class ScopedMarkdownFont : IDisposable` — pushes the resolved font (or the current font at a computed size) for a role and pops on dispose; exposes `bool FauxBold` and `bool FauxItalic` when no real variant was supplied.

- [ ] **Step 1: Write MarkdownColors**

Create `ImGui.Markdown/Rendering/MarkdownColors.cs`:

```csharp
// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Markdown;

using System;

using Hexa.NET.ImGui;

/// <summary>Resolves markdown element colors from the active ImGui theme, with config overrides.</summary>
internal static class MarkdownColors
{
	/// <summary>The link color: config override, else a theme accent.</summary>
	/// <param name="config">The active config.</param>
	/// <returns>The link color as a vector.</returns>
	public static ImGuiVector4 Link(MarkdownConfig config)
	{
		if (config.LinkColor.HasValue)
		{
			return config.LinkColor.Value;
		}

		// ButtonHovered reads as an interactive accent across the bundled themes.
		Span<ImGuiVector4> colors = ImGui.GetStyle().Colors;
		return colors[(int)ImGuiCol.ButtonHovered];
	}

	/// <summary>Packed background color for inline code spans.</summary>
	/// <returns>An ImGui U32 color.</returns>
	public static uint InlineCodeBackground()
	{
		Span<ImGuiVector4> colors = ImGui.GetStyle().Colors;
		ImGuiVector4 frame = colors[(int)ImGuiCol.FrameBg];
		return ImGui.GetColorU32(frame);
	}

	/// <summary>Packed color for the blockquote accent bar.</summary>
	/// <returns>An ImGui U32 color.</returns>
	public static uint BlockquoteBar()
	{
		Span<ImGuiVector4> colors = ImGui.GetStyle().Colors;
		return ImGui.GetColorU32(colors[(int)ImGuiCol.Border]);
	}

	/// <summary>Packed color for thematic-break separators.</summary>
	/// <returns>An ImGui U32 color.</returns>
	public static uint Separator()
	{
		Span<ImGuiVector4> colors = ImGui.GetStyle().Colors;
		return ImGui.GetColorU32(colors[(int)ImGuiCol.Separator]);
	}

	/// <summary>Packed color for normal text.</summary>
	/// <returns>An ImGui U32 color.</returns>
	public static uint TextU32()
	{
		Span<ImGuiVector4> colors = ImGui.GetStyle().Colors;
		return ImGui.GetColorU32(colors[(int)ImGuiCol.Text]);
	}
}
```

- [ ] **Step 2: Write ScopedMarkdownFont**

Append to the same file (or a sibling `ScopedMarkdownFont.cs`; keep with colors is fine as both are render helpers). Create `ImGui.Markdown/Rendering/ScopedMarkdownFont.cs`:

```csharp
// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Markdown;

using System;

using Hexa.NET.ImGui;

/// <summary>
/// Pushes the font for a markdown role and pops it on dispose. When the config supplies no
/// resolver (or the resolver returns null for an emphasis role), records that faux styling
/// should be applied by the caller.
/// </summary>
internal sealed class ScopedMarkdownFont : IDisposable
{
	private bool disposed;

	/// <summary>Whether the caller should synthesize bold (no real bold glyphs available).</summary>
	public bool FauxBold { get; }

	/// <summary>Whether the caller should synthesize italic (no real italic glyphs available).</summary>
	public bool FauxItalic { get; }

	/// <summary>The pixel size the font was pushed at.</summary>
	public float PixelSize { get; }

	/// <summary>Pushes the resolved font for a role at a target pixel size.</summary>
	/// <param name="role">The typographic role.</param>
	/// <param name="pixelSize">The target pixel size (already scaled).</param>
	/// <param name="config">The active config providing the optional resolver.</param>
	public ScopedMarkdownFont(MarkdownFontRole role, float pixelSize, MarkdownConfig config)
	{
		PixelSize = pixelSize;
		ImFontPtr? resolved = config.FontResolver?.Invoke(role, pixelSize);

		if (resolved.HasValue && resolved.Value.Handle is not null)
		{
			ImGui.PushFont(resolved.Value, pixelSize);
		}
		else
		{
			// No variant: keep the current font at the target size and fall back to faux styling.
			ImGui.PushFont(ImGui.GetFont(), pixelSize);
			FauxBold = role is MarkdownFontRole.Bold or MarkdownFontRole.BoldItalic;
			FauxItalic = role is MarkdownFontRole.Italic or MarkdownFontRole.BoldItalic;
		}
	}

	/// <summary>Pops the pushed font.</summary>
	public void Dispose()
	{
		if (!disposed)
		{
			disposed = true;
			ImGui.PopFont();
		}
	}
}
```

- [ ] **Step 3: Build to verify it compiles**

Run: `dotnet build ImGui.Markdown/ImGui.Markdown.csproj`
Expected: build succeeds. If `ImFontPtr.Handle` is not the null-check member in the installed binding, use the available emptiness check (e.g. compare `resolved.Value.NativePtr` to null); verify against `ImGui.App/FontAppearance.cs` usage.

- [ ] **Step 4: Commit**

```bash
git add ImGui.Markdown/Rendering/MarkdownColors.cs ImGui.Markdown/Rendering/ScopedMarkdownFont.cs
git commit -m "feat(markdown): add theme color resolution and scoped font pushing"
```

---

### Task 9: InlineRenderer and BlockRenderer, plus public entry points

Wires everything into the drawing walkers and the public API. Draw code is verified by build now and visually in the demo (Task 10). This task deliberately bundles both renderers and the entry points because they only become independently exercisable together.

**Files:**
- Create: `ImGui.Markdown/Rendering/InlineRenderer.cs`
- Create: `ImGui.Markdown/Rendering/BlockRenderer.cs`
- Create: `ImGui.Markdown/ImGuiMarkdown.cs`
- Modify: `ImGui.Markdown/MarkdownDocument.cs` (add `Render`)
- Test: none new (logic already covered; this is draw wiring verified by build + demo).

**Interfaces:**
- Consumes: `InlineBuilder`, `InlineLayout`, `MarkdownSizing`, `ListMarker`, `MarkdownColors`, `ScopedMarkdownFont`, `LinkPolicy`, `MarkdownConfig`, `MarkdownParser`, `MarkdownDocument.Ast`.
- Produces:
  - `internal static class InlineRenderer` with `void Render(Markdig.Syntax.Inlines.ContainerInline? inline, MarkdownConfig config)`.
  - `internal static class BlockRenderer` with `void Render(Markdig.Syntax.ContainerBlock blocks, MarkdownConfig config)`.
  - `public static partial class ImGuiMarkdown` with `void Render(string markdown, MarkdownConfig? config = null)` and `void Render(MarkdownDocument document, MarkdownConfig? config = null)`.
  - `MarkdownDocument.Render(MarkdownConfig? config = null)`.

- [ ] **Step 1: Write InlineRenderer**

Create `ImGui.Markdown/Rendering/InlineRenderer.cs`:

```csharp
// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Markdown;

using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

using Markdig.Syntax.Inlines;

/// <summary>Draws a flattened inline run list using the pure layout engine, then paints each token.</summary>
internal static class InlineRenderer
{
	/// <summary>Lays out and draws the inline content of a block.</summary>
	/// <param name="inline">The inline container to render.</param>
	/// <param name="config">The active config.</param>
	public static void Render(ContainerInline? inline, MarkdownConfig config)
	{
		IReadOnlyList<InlineRun> runs = InlineBuilder.Build(inline);
		if (runs.Count == 0)
		{
			return;
		}

		float wrapWidth = config.WrapWidth ?? ImGui.GetContentRegionAvail().X;
		if (wrapWidth <= 0.0f)
		{
			wrapWidth = 1.0f;
		}

		float bodySize = ImGui.GetFontSize();
		float lineHeight = ImGui.GetTextLineHeightWithSpacing();

		IReadOnlyList<LaidOutLine> lines = InlineLayout.Wrap(runs, wrapWidth, (text, role, isImage) => Measure(text, role, bodySize, config, isImage));

		Vector2 origin = ImGui.GetCursorScreenPos();
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		ImGuiVector4 linkColor = MarkdownColors.Link(config);
		uint linkU32 = ImGui.GetColorU32(linkColor);
		uint textU32 = MarkdownColors.TextU32();

		float y = 0.0f;
		foreach (LaidOutLine line in lines)
		{
			foreach (LaidOutToken token in line.Tokens)
			{
				Vector2 pos = new(origin.X + token.X, origin.Y + y);
				DrawToken(token, pos, bodySize, config, drawList, token.LinkUrl is null ? textU32 : linkU32);
			}

			y += lineHeight;
		}

		// Reserve the space the text occupied so following blocks flow beneath it.
		ImGui.Dummy(new Vector2(wrapWidth, y));
	}

	private static float Measure(string text, MarkdownFontRole role, float bodySize, MarkdownConfig config, bool isImage)
	{
		if (isImage)
		{
			MarkdownImageResult? image = config.ImageResolver?.Invoke(text);
			return image?.Size.X ?? ImagePlaceholderWidth;
		}

		float size = role is >= MarkdownFontRole.H1 and <= MarkdownFontRole.H6
			? MarkdownSizing.HeadingPixelSize(bodySize, (int)role - (int)MarkdownFontRole.H1 + 1, config.HeadingScales)
			: bodySize;

		// CalcTextSize measures at the current font size; scale by the role's target size ratio.
		float baseWidth = ImGui.CalcTextSize(text).X;
		return baseWidth * (size / bodySize);
	}

	private const float ImagePlaceholderWidth = 120.0f;

	private static void DrawToken(LaidOutToken token, Vector2 pos, float bodySize, MarkdownConfig config, ImDrawListPtr drawList, uint color)
	{
		MarkdownFontRole role = token.Role;
		float size = role is >= MarkdownFontRole.H1 and <= MarkdownFontRole.H6
			? MarkdownSizing.HeadingPixelSize(bodySize, (int)role - (int)MarkdownFontRole.H1 + 1, config.HeadingScales)
			: bodySize;

		if (token.IsImage)
		{
			MarkdownImageResult? image = config.ImageResolver?.Invoke(token.Text);
			if (image.HasValue)
			{
				ImGui.SetCursorScreenPos(pos);
				unsafe
				{
					ImGui.Image(new ImTextureRef(texId: image.Value.TextureId), image.Value.Size);
				}
			}
			else
			{
				// Placeholder box with the src/alt text for remote or unresolved images.
				drawList.AddRect(pos, pos + new Vector2(token.Width, size), MarkdownColors.Separator());
				drawList.AddText(pos + new Vector2(2.0f, 0.0f), color, token.Text);
			}

			return;
		}

		using ScopedMarkdownFont font = new(role, size, config);

		if (role == MarkdownFontRole.Code)
		{
			// Subtle background behind inline code.
			Vector2 pad = new(2.0f, 1.0f);
			drawList.AddRectFilled(pos - pad, pos + new Vector2(token.Width, size) + pad, MarkdownColors.InlineCodeBackground(), 2.0f);
		}

		drawList.AddText(pos, color, token.Text);

		if (font.FauxBold)
		{
			// Second draw offset by one pixel thickens the glyphs.
			drawList.AddText(pos + new Vector2(1.0f, 0.0f), color, token.Text);
		}

		if (token.LinkUrl is not null)
		{
			// Underline and hit-test the link token.
			float underlineY = pos.Y + size;
			drawList.AddLine(new Vector2(pos.X, underlineY), new Vector2(pos.X + token.Width, underlineY), color, 1.0f);

			ImGui.SetCursorScreenPos(pos);
			ImGui.InvisibleButton("##mdlink_" + token.LinkUrl + "_" + pos.X.ToString(System.Globalization.CultureInfo.InvariantCulture) + "_" + pos.Y.ToString(System.Globalization.CultureInfo.InvariantCulture), new Vector2(token.Width, size));
			if (ImGui.IsItemHovered())
			{
				ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
			}

			if (ImGui.IsItemClicked())
			{
				LinkPolicy.Activate(token.LinkUrl, config.OnLinkClicked);
			}
		}
	}
}
```

Note on faux italic: true glyph shear requires per-vertex manipulation not exposed simply through `AddText`; v1 renders faux-italic tokens upright (the run is still styled via any real italic font when supplied). This is an accepted v1 limitation recorded in the spec's "faux fallback" scope.

- [ ] **Step 2: Write BlockRenderer**

Create `ImGui.Markdown/Rendering/BlockRenderer.cs`:

```csharp
// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Markdown;

using System.Numerics;

using Hexa.NET.ImGui;

using Markdig.Extensions.TaskLists;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

/// <summary>Walks block-level AST nodes and issues ImGui draw calls for each.</summary>
internal static class BlockRenderer
{
	/// <summary>Renders all blocks in a container.</summary>
	/// <param name="blocks">The container block (document or nested container).</param>
	/// <param name="config">The active config.</param>
	public static void Render(ContainerBlock blocks, MarkdownConfig config)
	{
		foreach (Block block in blocks)
		{
			RenderBlock(block, config);
		}
	}

	private static void RenderBlock(Block block, MarkdownConfig config)
	{
		switch (block)
		{
			case HeadingBlock heading:
				RenderHeading(heading, config);
				break;

			case ParagraphBlock paragraph:
				InlineRenderer.Render(paragraph.Inline, config);
				ImGui.Dummy(new Vector2(0.0f, config.ParagraphSpacingPixels));
				break;

			case ListBlock list:
				RenderList(list, config);
				break;

			case QuoteBlock quote:
				RenderQuote(quote, config);
				break;

			case Markdig.Syntax.CodeBlock code:
				RenderCodeBlock(code, config);
				break;

			case ThematicBreakBlock:
				RenderThematicBreak();
				break;

			case Markdig.Extensions.Tables.Table table:
				RenderTable(table, config);
				break;

			case HtmlBlock html:
				RenderHtmlAsText(html, config);
				break;

			case ContainerBlock container:
				Render(container, config);
				break;

			default:
				break;
		}
	}

	private static void RenderHeading(HeadingBlock heading, MarkdownConfig config)
	{
		float body = ImGui.GetFontSize();
		float size = MarkdownSizing.HeadingPixelSize(body, heading.Level, config.HeadingScales);
		using (new ScopedMarkdownFont(MarkdownSizing.HeadingRole(heading.Level), size, config))
		{
			InlineRenderer.Render(heading.Inline, config);
		}

		ImGui.Dummy(new Vector2(0.0f, config.ParagraphSpacingPixels));
	}

	private static void RenderList(ListBlock list, MarkdownConfig config)
	{
		int index = 0;
		int start = 1;
		if (list.IsOrdered && int.TryParse(list.OrderedStart, out int parsed))
		{
			start = parsed;
		}

		foreach (Block item in list)
		{
			if (item is ListItemBlock listItem)
			{
				bool? taskChecked = TryGetTaskState(listItem);
				string marker = ListMarker.For(list.IsOrdered, index, start, taskChecked);

				ImGui.TextUnformatted(marker);
				ImGui.SameLine();
				ImGui.Indent(config.ListIndentPixels);
				Render(listItem, config);
				ImGui.Unindent(config.ListIndentPixels);
				index++;
			}
		}
	}

	private static bool? TryGetTaskState(ListItemBlock listItem)
	{
		foreach (Block child in listItem)
		{
			if (child is ParagraphBlock paragraph && paragraph.Inline is not null)
			{
				Inline? inline = paragraph.Inline.FirstChild;
				while (inline is not null)
				{
					if (inline is TaskList task)
					{
						return task.Checked;
					}

					inline = inline.NextSibling;
				}
			}
		}

		return null;
	}

	private static void RenderQuote(QuoteBlock quote, MarkdownConfig config)
	{
		Vector2 start = ImGui.GetCursorScreenPos();
		ImGui.Indent(config.ListIndentPixels);
		Render(quote, config);
		ImGui.Unindent(config.ListIndentPixels);

		Vector2 end = ImGui.GetCursorScreenPos();
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		float barX = start.X + (config.ListIndentPixels * 0.35f);
		drawList.AddLine(new Vector2(barX, start.Y), new Vector2(barX, end.Y), MarkdownColors.BlockquoteBar(), 2.0f);
	}

	private static void RenderCodeBlock(Markdig.Syntax.CodeBlock code, MarkdownConfig config)
	{
		string text = ExtractCodeText(code);
		float size = ImGui.GetFontSize();
		Vector2 start = ImGui.GetCursorScreenPos();
		Vector2 avail = ImGui.GetContentRegionAvail();

		using (new ScopedMarkdownFont(MarkdownFontRole.Code, size, config))
		{
			Vector2 textSize = ImGui.CalcTextSize(text);
			float height = textSize.Y + 8.0f;
			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			drawList.AddRectFilled(start, start + new Vector2(avail.X, height), MarkdownColors.InlineCodeBackground(), 3.0f);
			ImGui.Dummy(new Vector2(0.0f, 4.0f));
			ImGui.Indent(4.0f);
			ImGui.TextUnformatted(text);
			ImGui.Unindent(4.0f);
			ImGui.Dummy(new Vector2(0.0f, 4.0f));
		}

		ImGui.Dummy(new Vector2(0.0f, config.ParagraphSpacingPixels));
	}

	private static string ExtractCodeText(Markdig.Syntax.CodeBlock code)
	{
		System.Text.StringBuilder builder = new();
		Markdig.Helpers.StringLineGroup lines = code.Lines;
		for (int i = 0; i < lines.Count; i++)
		{
			builder.AppendLine(lines.Lines[i].ToString());
		}

		return builder.ToString().TrimEnd('\n', '\r');
	}

	private static void RenderThematicBreak()
	{
		Vector2 start = ImGui.GetCursorScreenPos();
		float width = ImGui.GetContentRegionAvail().X;
		float y = start.Y + 4.0f;
		ImGui.GetWindowDrawList().AddLine(new Vector2(start.X, y), new Vector2(start.X + width, y), MarkdownColors.Separator(), 1.0f);
		ImGui.Dummy(new Vector2(width, 9.0f));
	}

	private static void RenderTable(Markdig.Extensions.Tables.Table table, MarkdownConfig config)
	{
		// v1: render each row's cells as inline content separated by a tab-like spacing.
		// A full column-aligned table can replace this later without touching callers.
		foreach (Block rowBlock in table)
		{
			if (rowBlock is Markdig.Extensions.Tables.TableRow row)
			{
				bool first = true;
				foreach (Block cellBlock in row)
				{
					if (cellBlock is Markdig.Extensions.Tables.TableCell cell)
					{
						if (!first)
						{
							ImGui.SameLine();
							ImGui.TextUnformatted("  |  ");
							ImGui.SameLine();
						}

						first = false;
						foreach (Block content in cell)
						{
							if (content is ParagraphBlock paragraph)
							{
								InlineRenderer.Render(paragraph.Inline, config);
							}
						}
					}
				}
			}
		}

		ImGui.Dummy(new Vector2(0.0f, config.ParagraphSpacingPixels));
	}

	private static void RenderHtmlAsText(HtmlBlock html, MarkdownConfig config)
	{
		string text = ExtractCodeText(html);
		using (new ScopedMarkdownFont(MarkdownFontRole.Code, ImGui.GetFontSize(), config))
		{
			ImGui.TextUnformatted(text);
		}
	}
}
```

Note: `HtmlBlock` also exposes `Lines` (a `StringLineGroup`) like code blocks, so `ExtractCodeText` accepts either via its `Markdig.Syntax.CodeBlock`/`LeafBlock` shape — if the compiler rejects reusing it for `HtmlBlock`, add a `LeafBlock`-typed overload. Verify by building.

- [ ] **Step 3: Write the public entry point**

Create `ImGui.Markdown/ImGuiMarkdown.cs`:

```csharp
// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Markdown;

using MarkdigAst = Markdig.Syntax.MarkdownDocument;

/// <summary>
/// Renders CommonMark markdown inside Dear ImGui. The static <see cref="Render(string, MarkdownConfig?)"/>
/// caches parses by source string; for hot paths, construct a <see cref="MarkdownDocument"/> once and
/// render it each frame.
/// </summary>
public static partial class ImGuiMarkdown
{
	private static readonly MarkdownConfig DefaultConfig = new();

	/// <summary>Parses (cached) and renders markdown at the current cursor position.</summary>
	/// <param name="markdown">The markdown source.</param>
	/// <param name="config">Optional rendering config; defaults are used when omitted.</param>
	public static void Render(string markdown, MarkdownConfig? config = null)
	{
		if (string.IsNullOrEmpty(markdown))
		{
			return;
		}

		MarkdigAst ast = MarkdownParser.GetOrParse(markdown);
		BlockRenderer.Render(ast, config ?? DefaultConfig);
	}

	/// <summary>Renders a pre-parsed document at the current cursor position.</summary>
	/// <param name="document">The parsed document.</param>
	/// <param name="config">Optional rendering config; defaults are used when omitted.</param>
	public static void Render(MarkdownDocument document, MarkdownConfig? config = null)
	{
		ArgumentNullException.ThrowIfNull(document);
		BlockRenderer.Render(document.Ast, config ?? DefaultConfig);
	}
}
```

- [ ] **Step 4: Add `Render` to MarkdownDocument**

In `ImGui.Markdown/MarkdownDocument.cs`, add this method inside the class (after the constructor):

```csharp
	/// <summary>Renders this document at the current cursor position.</summary>
	/// <param name="config">Optional rendering config; defaults are used when omitted.</param>
	public void Render(MarkdownConfig? config = null) => ImGuiMarkdown.Render(this, config);
```

- [ ] **Step 5: Build and run the full test suite**

Run:
```bash
dotnet build ImGui.Markdown/ImGui.Markdown.csproj
dotnet test tests/ImGui.Markdown.Tests/
```
Expected: build succeeds; all tests from Tasks 1-7 PASS. Fix any binding-name mismatches surfaced by the compiler (see the notes in Steps 1-2), preferring the actual API member over changing behavior.

- [ ] **Step 6: Commit**

```bash
git add ImGui.Markdown/Rendering/InlineRenderer.cs ImGui.Markdown/Rendering/BlockRenderer.cs ImGui.Markdown/ImGuiMarkdown.cs ImGui.Markdown/MarkdownDocument.cs
git commit -m "feat(markdown): add block/inline renderers and public Render entry points"
```

---

### Task 10: Example demo

A runnable demo that showcases every element and wires a real `FontResolver` (via `ImGui.App`'s `FontAppearance`) and an `ImageResolver` (via `ImGui.App`'s texture loading), demonstrating real bold/italic and DPI/accessibility scaling.

**Files:**
- Create: `examples/ImGuiMarkdownDemo/ImGuiMarkdownDemo.csproj`
- Create: `examples/ImGuiMarkdownDemo/ImGuiMarkdownDemo.cs`
- Modify: `ImGui.sln` (add the example)

**Interfaces:**
- Consumes: `ImGuiMarkdown.Render`, `MarkdownConfig`, `MarkdownFontRole`, `ImGui.App` (`ImGuiApp`, `ImGuiAppConfig`, `FontAppearance`).
- Produces: an executable demo.

- [ ] **Step 1: Create the example csproj**

Create `examples/ImGuiMarkdownDemo/ImGuiMarkdownDemo.csproj` (mirrors the Widgets demo):

```xml
<Project>
  <Sdk Name="Microsoft.NET.Sdk" />
  <Sdk Name="ktsu.Sdk" />
  <Sdk Name="ktsu.Sdk.App" />

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <TargetFrameworks></TargetFrameworks>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\ImGui.App\ImGui.App.csproj" />
    <ProjectReference Include="..\..\ImGui.Markdown\ImGui.Markdown.csproj" />
    <PackageReference Include="Hexa.NET.ImGui" />
    <PackageReference Include="Polyfill" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Write the demo**

Create `examples/ImGuiMarkdownDemo/ImGuiMarkdownDemo.cs`:

```csharp
// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Examples.Markdown;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;
using ktsu.ImGui.Markdown;

internal static class ImGuiMarkdownDemo
{
	private const string Sample = """
		# ImGui.Markdown

		A **CommonMark** renderer for *Dear ImGui*, with `inline code`, [links](https://github.com/ktsu-dev), and more.

		## Lists

		- First item
		- Second item with **bold**
		  - Nested item
		- [x] Completed task
		- [ ] Pending task

		1. One
		2. Two
		3. Three

		## Quote

		> The best way to predict the future is to invent it.

		## Code

		```
		var greeting = "hello";
		Console.WriteLine(greeting);
		```

		## Table

		| Feature | Status |
		|---------|--------|
		| Headings | Yes |
		| Tables | Yes |

		---

		That's the tour.
		""";

	private static void Main()
	{
		MarkdownConfig config = new()
		{
			FontResolver = ResolveFont,
			OnLinkClicked = null, // fall back to OS open for http/https/mailto
		};

		ImGuiApp.Start(new ImGuiAppConfig
		{
			Title = "ImGui.Markdown - Demo",
			OnRender = _ =>
			{
				ImGui.Begin("Markdown");
				ImGuiMarkdown.Render(Sample, config);
				ImGui.End();
			},
		});
	}

	// Map markdown roles to app fonts. Body/emphasis reuse the default font (faux bold applies);
	// headings request the default font at the target pixel size so DPI + GlobalScale are honored.
	private static ImFontPtr? ResolveFont(MarkdownFontRole role, float pixelSize)
	{
		// Returning null lets the renderer keep the current font at pixelSize with faux styling.
		// If the app registers named "bold"/"italic" fonts via ImGuiAppConfig.Fonts, resolve them here, e.g.:
		//   return ImGuiApp.FindBestFontForAppearance("bold", ImGuiApp.PtsToPx-equivalent, out _);
		return null;
	}
}
```

Note: the `ResolveFont` stub intentionally returns null (faux styling) so the demo runs without bundled font variants. If the app registers bold/italic fonts, wire `FontAppearance`/`ImGuiApp.FindBestFontForAppearance` here — this is the documented extension point, not required for the demo to run.

- [ ] **Step 3: Register the example in the solution**

Run:
```bash
dotnet sln ImGui.sln add examples/ImGuiMarkdownDemo/ImGuiMarkdownDemo.csproj
```

- [ ] **Step 4: Build the demo**

Run: `dotnet build examples/ImGuiMarkdownDemo/ImGuiMarkdownDemo.csproj`
Expected: build succeeds.

- [ ] **Step 5: Run the demo and verify visually**

Run: `dotnet run --project examples/ImGuiMarkdownDemo`
Expected: a window renders the sample with larger headings, bullet/numbered/task lists, an indented quote with an accent bar, a shaded code block, a table, a horizontal rule, and a clickable link. Close the window to exit.

- [ ] **Step 6: Commit**

```bash
git add examples/ImGuiMarkdownDemo/ ImGui.sln
git commit -m "feat(markdown): add ImGuiMarkdownDemo example"
```

---

### Task 11: Package README and suite documentation

Documents the new package and updates the suite docs so the addition is discoverable.

**Files:**
- Create: `ImGui.Markdown/README.md`
- Modify: `CLAUDE.md` (add ImGui.Markdown to the Libraries and Dependencies sections)
- Modify: `README.md` (root) — add the package to the suite listing if such a listing exists.

**Interfaces:**
- Consumes: nothing.
- Produces: documentation only.

- [ ] **Step 1: Write the package README**

Create `ImGui.Markdown/README.md` covering: purpose, install, quick start (static `ImGuiMarkdown.Render` and `MarkdownDocument`), the `MarkdownConfig` options (FontResolver, OnLinkClicked, ImageResolver, HeadingScales, WrapWidth), supported CommonMark elements, and v1 limitations (no syntax highlighting, no remote image download, HTML shown as text). Use fenced C# examples mirroring the demo.

- [ ] **Step 2: Update CLAUDE.md**

Add a bullet under "Libraries" describing `ImGui.Markdown` (`ktsu.ImGui.Markdown`) as the CommonMark renderer built on Markdig, layered without an ImGui.App dependency, and add `Markdig` to the Dependencies list. Add the `examples/ImGuiMarkdownDemo/` entry under Examples.

- [ ] **Step 3: Build the whole solution and run all tests**

Run:
```bash
dotnet build
dotnet test
```
Expected: entire solution builds; all tests pass.

- [ ] **Step 4: Commit**

```bash
git add ImGui.Markdown/README.md CLAUDE.md README.md
git commit -m "docs(markdown): add package README and update suite docs"
```

---

## Self-Review

**Spec coverage:**
- Full CommonMark + extensions (tables, task lists, autolinks): Task 5 pipeline; rendering in Tasks 7-9. ✓
- Markdig dependency: Task 1. ✓
- Standalone package: Task 1. ✓
- Named fonts + faux fallback: Task 8 (`ScopedMarkdownFont`), Task 9 (faux-bold draw). Faux-italic upright limitation noted. ✓
- Links: callback + OS-open with scheme filter: Task 3 + Task 9. ✓
- Images: local via resolver, remote/unresolved placeholder: `MarkdownImageResult` (Task 2), image runs carried through layout (`InlineRun.IsImage` → `LaidOutToken.IsImage`, Task 4), drawn in `InlineRenderer.DrawToken` (Task 9: resolve via `config.ImageResolver`, draw texture with `ImGui.Image(new ImTextureRef(texId: id), size)` in an `unsafe` block, else placeholder rect + alt text). ✓
- Static + document API: Tasks 5 and 9. ✓
- Decoupled scaling via live font size: Tasks 6, 9. ✓
- Testing of pure logic: Tasks 2-7. ✓
- Demo: Task 10. ✓

**Image-token consistency (resolved inline):** `LaidOutToken` carries a `bool IsImage` threaded from `InlineRun.IsImage` through `InlineLayout.Wrap`; the wrap measure delegate is `Func<string, MarkdownFontRole, bool, float>` so image tokens are measured at their resolved (or placeholder) width; `InlineRenderer.DrawToken` branches on `token.IsImage`. `InlineLayoutTests.Wrap_ImageRun_IsSingleUnsplitToken` asserts the flag. These are all reflected in the Task 4 and Task 9 code above (no deferred fix outstanding).

**Placeholder scan:** No "TBD"/"TODO" in task steps. The `ResolveFont` demo stub returns null by design (documented extension point), not a placeholder.

**Type consistency:** `MarkdownFontRole`, `InlineRun`, `LaidOutToken` (with `IsImage`), `LaidOutLine`, `MarkdownImageResult`, `MarkdownConfig` names and signatures are consistent across tasks. `MarkdownDocument.Ast` (internal) is consumed by `ImGuiMarkdown.Render` and `InlineBuilderTests`. The wrap measure delegate signature matches between Task 4 (definition), its tests, and Task 9 (call site).

**Binding-version risk:** Markdig and Hexa.NET.ImGui member names (`EmphasisInline.DelimiterCount`, `ListBlock.OrderedStart`, `TaskList.Checked`, `ImFontPtr.Handle`, `ImTextureRef` ctor) are the documented/observed forms but may differ slightly by installed version. Each task where this matters carries a "verify by building" note; the executor adapts to the actual API rather than the spec's assumed name.
