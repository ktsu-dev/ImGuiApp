# Headless UI Test Harness Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a `ktsu.ImGui.App.Testing` package that runs an ImGui application with no display, no GPU and no driver, injects input directly into ImGui, advances frames under test control, and captures rendered pixels.

**Architecture:** A CPU rasterizer implements the existing internal `IRendererBackend`, so no OpenGL context is needed anywhere. A headless controller owns the ImGui context and font atlas, because `ImGuiController` is welded to Silk's `GL`, `IView` and `IInputContext`. The frame body inside `ImGuiApp` is extracted so the harness drives the same application rendering code that ships, rather than a copy of it.

**Tech Stack:** C# on net10.0/net9.0/net8.0, Hexa.NET.ImGui 2.2.x, MSTest via `MSTest.Sdk`, `ktsu.Sdk`. No new third-party dependencies.

**Spec:** `docs/superpowers/specs/2026-08-19-headless-ui-test-harness-design.md`

## Global Constraints

- Target frameworks for the library: `net10.0;net9.0;net8.0`. Test projects target `net10.0` only.
- Tabs for indentation. File-scoped namespaces. Using directives inside the namespace. Braces on all control flow. Explicit accessibility modifiers. No `this.` qualifier. Nullable enabled. Warnings as errors.
- Line endings are LF in this repository, which overrides the CRLF guidance in the global instructions.
- Never edit or commit `.editorconfig`, `.gitattributes` or `.gitignore`. `ktsu.Sdk` rewrites them on every build.
- No global warning suppressions. Use targeted attributes with justifications.
- Commit messages carry a version tag of `[major]`, `[minor]`, `[patch]` or `[pre]`. No `Co-Authored-By` lines.
- Add no new package references. PNG encoding uses `System.IO.Compression.ZLibStream` from the base class library, which also avoids the SixLabors licensing problem recorded in issue #230.
- Use semantic asserts such as `Assert.AreEqual` and `Assert.IsTrue` with meaningful messages, never bare `Assert.IsTrue(a == b)`.
- Copyright header on every new file: `// Copyright (c) 2023-2026 ktsu-dev contributors`.
- Work on branch `feat/headless-ui-test-harness`, which already exists and carries the spec commit.

---

### Task 1: Project Scaffolding

**Files:**
- Create: `ImGui.App.Testing/ImGui.App.Testing.csproj`
- Create: `ImGui.App.Testing/AssemblyInfo.cs`
- Create: `tests/ImGui.App.Testing.Tests/ImGui.App.Testing.Tests.csproj`
- Create: `tests/ImGui.App.Testing.Tests/ScaffoldingTests.cs`
- Modify: `ImGui.App/ImGui.App.csproj`
- Modify: `ImGui.sln`

**Interfaces:**
- Consumes: nothing.
- Produces: assembly `ktsu.ImGui.App.Testing` with root namespace `ktsu.ImGui.App.Testing`, referencing `ImGui.App` and able to see its internals.

- [x] **Step 1: Create the library project file**

Create `ImGui.App.Testing/ImGui.App.Testing.csproj`:

```xml
<Project>
  <Sdk Name="Microsoft.NET.Sdk" />
  <Sdk Name="ktsu.Sdk" />

  <PropertyGroup>
    <TargetFrameworks>net10.0;net9.0;net8.0</TargetFrameworks>
    <RootNamespace>ktsu.ImGui.App.Testing</RootNamespace>
    <AssemblyName>$(RootNamespace)</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <InternalsVisibleTo Include="ktsu.ImGui.App.Testing.Tests" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Hexa.NET.ImGui" />
    <PackageReference Include="Polyfill" PrivateAssets="all" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\ImGui.App\ImGui.App.csproj" />
  </ItemGroup>

</Project>
```

- [x] **Step 2: Grant the harness access to ImGui.App internals**

`IRendererBackend` and the frame rendering helpers are internal. Add to the `ItemGroup` in `ImGui.App/ImGui.App.csproj` that already contains `<InternalsVisibleTo Include="ktsu.ImGui.App.Tests" />`:

```xml
    <InternalsVisibleTo Include="ktsu.ImGui.App.Testing" />
```

- [x] **Step 3: Create the assembly info file**

Per repository convention, assembly attributes live in their own file. Create `ImGui.App.Testing/AssemblyInfo.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

using System.Runtime.CompilerServices;

[assembly: CLSCompliant(false)]
[assembly: InternalsVisibleTo("ktsu.ImGui.App.Testing.Tests")]
```

- [x] **Step 4: Create the test project file**

Create `tests/ImGui.App.Testing.Tests/ImGui.App.Testing.Tests.csproj`:

```xml
<Project>
  <Sdk Name="MSTest.Sdk" />
  <Sdk Name="ktsu.Sdk" />

  <PropertyGroup>
    <IsTestProject>true</IsTestProject>
    <TargetFramework>net10.0</TargetFramework>
    <TargetFrameworks></TargetFrameworks>
    <RootNamespace>ktsu.ImGui.App.Testing.Tests</RootNamespace>
    <AssemblyName>$(RootNamespace)</AssemblyName>
    <EnableSourceLink>false</EnableSourceLink>
    <NoWarn>CA1051;CA1002;CA1062;CA1515;CA1707;CA1815;CA1819;CA1822;CA2227;CS8604;IDE0060;MSTEST0039</NoWarn>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\ImGui.App.Testing\ImGui.App.Testing.csproj" />
  </ItemGroup>
</Project>
```

- [x] **Step 5: Write a scaffolding test**

Create `tests/ImGui.App.Testing.Tests/ScaffoldingTests.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class ScaffoldingTests
{
	[TestMethod]
	public void TestingAssembly_CanSeeImGuiAppInternals()
	{
		// IRendererBackend is internal to ktsu.ImGui.App. If InternalsVisibleTo is wired
		// correctly this compiles; the assertion just anchors the test to a runtime fact.
		Type? backend = typeof(ImGuiApp).Assembly.GetType("ktsu.ImGui.App.IRendererBackend");

		Assert.IsNotNull(backend, "IRendererBackend should be resolvable from the ImGui.App assembly.");
	}
}
```

- [x] **Step 6: Add both projects to the solution**

Run:

```bash
dotnet sln ImGui.sln add ImGui.App.Testing/ImGui.App.Testing.csproj
dotnet sln ImGui.sln add tests/ImGui.App.Testing.Tests/ImGui.App.Testing.Tests.csproj
```

- [x] **Step 7: Build and run the test**

Run:

```bash
dotnet build ImGui.App.Testing/ImGui.App.Testing.csproj
dotnet build tests/ImGui.App.Testing.Tests/ImGui.App.Testing.Tests.csproj
./tests/ImGui.App.Testing.Tests/bin/Debug/net10.0/ktsu.ImGui.App.Testing.Tests.exe
```

Expected: build succeeds with zero warnings, one test passes.

- [x] **Step 8: Commit**

```bash
git add ImGui.App.Testing tests/ImGui.App.Testing.Tests ImGui.sln ImGui.App/ImGui.App.csproj
git commit -m "feat: scaffold the ktsu.ImGui.App.Testing package [minor]"
```

---

### Task 2: Pixel Buffer and PNG Encoding

**Files:**
- Create: `ImGui.App.Testing/Bitmap32.cs`
- Create: `tests/ImGui.App.Testing.Tests/Bitmap32Tests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: `Bitmap32` with `int Width`, `int Height`, `Span<byte> Pixels` (RGBA8, tightly packed), `Rgba32 GetPixel(int x, int y)`, `void SetPixel(int x, int y, Rgba32 color)`, `void Clear(Rgba32 color)`, `void SavePng(string path)`, `byte[] EncodePng()`. Also produces `readonly record struct Rgba32(byte R, byte G, byte B, byte A)`.

- [x] **Step 1: Write the failing tests**

Create `tests/ImGui.App.Testing.Tests/Bitmap32Tests.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class Bitmap32Tests
{
	[TestMethod]
	public void SetPixel_ThenGetPixel_RoundTrips()
	{
		Bitmap32 bitmap = new(4, 3);

		bitmap.SetPixel(2, 1, new Rgba32(10, 20, 30, 40));

		Assert.AreEqual(new Rgba32(10, 20, 30, 40), bitmap.GetPixel(2, 1));
	}

	[TestMethod]
	public void Clear_FillsEveryPixel()
	{
		Bitmap32 bitmap = new(3, 2);

		bitmap.Clear(new Rgba32(1, 2, 3, 255));

		for (int y = 0; y < bitmap.Height; y++)
		{
			for (int x = 0; x < bitmap.Width; x++)
			{
				Assert.AreEqual(new Rgba32(1, 2, 3, 255), bitmap.GetPixel(x, y), $"Pixel {x},{y} was not cleared.");
			}
		}
	}

	[TestMethod]
	public void EncodePng_ProducesAValidSignatureAndEndsWithIend()
	{
		Bitmap32 bitmap = new(2, 2);
		bitmap.Clear(new Rgba32(255, 0, 0, 255));

		byte[] png = bitmap.EncodePng();

		byte[] signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
		CollectionAssert.AreEqual(signature, png[..8], "PNG signature is wrong.");
		Assert.IsTrue(png.Length > 8, "PNG should carry chunks after the signature.");

		// The final chunk is always IEND: length 0, type "IEND", then its CRC.
		byte[] tail = png[^8..];
		Assert.AreEqual((byte)'I', tail[4]);
		Assert.AreEqual((byte)'E', tail[5]);
		Assert.AreEqual((byte)'N', tail[6]);
		Assert.AreEqual((byte)'D', tail[7]);
	}

	[TestMethod]
	public void GetPixel_OutsideBounds_Throws() =>
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new Bitmap32(2, 2).GetPixel(2, 0));
}
```

- [x] **Step 2: Run the tests to verify they fail**

Run: `dotnet build tests/ImGui.App.Testing.Tests/ImGui.App.Testing.Tests.csproj`
Expected: compile error, `Bitmap32` and `Rgba32` do not exist.

- [x] **Step 3: Implement the pixel buffer**

Create `ImGui.App.Testing/Bitmap32.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing;

using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;

/// <summary>A straight-alpha RGBA8 color.</summary>
/// <param name="R">Red channel.</param>
/// <param name="G">Green channel.</param>
/// <param name="B">Blue channel.</param>
/// <param name="A">Alpha channel, where 0 is fully transparent.</param>
public readonly record struct Rgba32(byte R, byte G, byte B, byte A);

/// <summary>
/// A tightly packed RGBA8 pixel buffer, used as the render target for the software rasterizer and
/// as the surface a test measures. Encodes to PNG through <see cref="ZLibStream"/> so the package
/// needs no imaging dependency.
/// </summary>
/// <param name="width">Width in pixels. Must be positive.</param>
/// <param name="height">Height in pixels. Must be positive.</param>
public sealed class Bitmap32(int width, int height)
{
	/// <summary>Gets the width in pixels.</summary>
	public int Width { get; } = width > 0 ? width : throw new ArgumentOutOfRangeException(nameof(width));

	/// <summary>Gets the height in pixels.</summary>
	public int Height { get; } = height > 0 ? height : throw new ArgumentOutOfRangeException(nameof(height));

	private readonly byte[] pixels = new byte[width * height * 4];

	/// <summary>Gets the raw RGBA8 bytes, four per pixel, row-major from the top left.</summary>
	public Span<byte> Pixels => pixels;

	/// <summary>Reads one pixel.</summary>
	/// <param name="x">Column.</param>
	/// <param name="y">Row.</param>
	/// <returns>The color at that position.</returns>
	public Rgba32 GetPixel(int x, int y)
	{
		int i = Offset(x, y);
		return new Rgba32(pixels[i], pixels[i + 1], pixels[i + 2], pixels[i + 3]);
	}

	/// <summary>Writes one pixel, replacing whatever was there.</summary>
	/// <param name="x">Column.</param>
	/// <param name="y">Row.</param>
	/// <param name="color">The color to write.</param>
	public void SetPixel(int x, int y, Rgba32 color)
	{
		int i = Offset(x, y);
		pixels[i] = color.R;
		pixels[i + 1] = color.G;
		pixels[i + 2] = color.B;
		pixels[i + 3] = color.A;
	}

	/// <summary>Fills every pixel with one color.</summary>
	/// <param name="color">The fill color.</param>
	public void Clear(Rgba32 color)
	{
		for (int i = 0; i < pixels.Length; i += 4)
		{
			pixels[i] = color.R;
			pixels[i + 1] = color.G;
			pixels[i + 2] = color.B;
			pixels[i + 3] = color.A;
		}
	}

	/// <summary>Writes the buffer to disk as a PNG.</summary>
	/// <param name="path">Destination file path.</param>
	public void SavePng(string path) => File.WriteAllBytes(path, EncodePng());

	/// <summary>Encodes the buffer as a PNG byte stream.</summary>
	/// <returns>A complete PNG file in memory.</returns>
	public byte[] EncodePng()
	{
		// Each scanline is prefixed with filter type 0 (None), which keeps the encoder trivial
		// at the cost of compression ratio. Test artifacts do not need to be small.
		byte[] raw = new byte[Height * ((Width * 4) + 1)];
		int stride = Width * 4;
		for (int y = 0; y < Height; y++)
		{
			int dst = y * (stride + 1);
			raw[dst] = 0;
			Array.Copy(pixels, y * stride, raw, dst + 1, stride);
		}

		using MemoryStream compressed = new();
		using (ZLibStream deflate = new(compressed, CompressionLevel.Fastest, leaveOpen: true))
		{
			deflate.Write(raw, 0, raw.Length);
		}

		using MemoryStream png = new();
		png.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

		byte[] ihdr = new byte[13];
		BinaryPrimitives.WriteInt32BigEndian(ihdr.AsSpan(0), Width);
		BinaryPrimitives.WriteInt32BigEndian(ihdr.AsSpan(4), Height);
		ihdr[8] = 8;    // bit depth
		ihdr[9] = 6;    // color type: truecolor with alpha
		ihdr[10] = 0;   // deflate
		ihdr[11] = 0;   // adaptive filtering
		ihdr[12] = 0;   // no interlace
		WriteChunk(png, "IHDR", ihdr);
		WriteChunk(png, "IDAT", compressed.ToArray());
		WriteChunk(png, "IEND", []);

		return png.ToArray();
	}

	private int Offset(int x, int y)
	{
		if ((uint)x >= (uint)Width)
		{
			throw new ArgumentOutOfRangeException(nameof(x));
		}

		if ((uint)y >= (uint)Height)
		{
			throw new ArgumentOutOfRangeException(nameof(y));
		}

		return ((y * Width) + x) * 4;
	}

	private static void WriteChunk(Stream stream, string type, byte[] data)
	{
		Span<byte> length = stackalloc byte[4];
		BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
		stream.Write(length);

		byte[] typeAndData = new byte[4 + data.Length];
		for (int i = 0; i < 4; i++)
		{
			typeAndData[i] = (byte)type[i];
		}

		Array.Copy(data, 0, typeAndData, 4, data.Length);
		stream.Write(typeAndData, 0, typeAndData.Length);

		Span<byte> crc = stackalloc byte[4];
		BinaryPrimitives.WriteUInt32BigEndian(crc, Crc32(typeAndData));
		stream.Write(crc);
	}

	private static uint Crc32(ReadOnlySpan<byte> data)
	{
		uint crc = 0xFFFFFFFFu;
		foreach (byte b in data)
		{
			crc ^= b;
			for (int bit = 0; bit < 8; bit++)
			{
				uint mask = (uint)-(int)(crc & 1);
				crc = (crc >> 1) ^ (0xEDB88320u & mask);
			}
		}

		return crc ^ 0xFFFFFFFFu;
	}
}
```

- [x] **Step 4: Run the tests to verify they pass**

Run:

```bash
dotnet build tests/ImGui.App.Testing.Tests/ImGui.App.Testing.Tests.csproj
./tests/ImGui.App.Testing.Tests/bin/Debug/net10.0/ktsu.ImGui.App.Testing.Tests.exe
```

Expected: all `Bitmap32Tests` pass.

- [x] **Step 5: Verify the PNG is readable by something other than this code**

Self-validation is not proof of a correct encoder. Write one bitmap to disk and open it with an independent decoder:

```bash
dotnet build tests/ImGui.App.Testing.Tests/ImGui.App.Testing.Tests.csproj
```

Then in a scratch directory, confirm a saved file loads. Any of these is acceptable evidence: opening it in an image viewer, or running

```bash
powershell -NoProfile -Command "Add-Type -AssemblyName System.Drawing; $i=[System.Drawing.Image]::FromFile('<path>'); '{0}x{1}' -f $i.Width,$i.Height"
```

Expected: the reported dimensions match the bitmap. Record the result in the commit message if it needed a fix.

- [x] **Step 6: Commit**

```bash
git add ImGui.App.Testing/Bitmap32.cs tests/ImGui.App.Testing.Tests/Bitmap32Tests.cs
git commit -m "feat: add an RGBA pixel buffer with dependency-free PNG encoding [minor]"
```

---

### Task 3: Triangle Rasterization

**Files:**
- Create: `ImGui.App.Testing/SoftwareRasterizer.cs`
- Create: `tests/ImGui.App.Testing.Tests/SoftwareRasterizerTests.cs`

**Interfaces:**
- Consumes: `Bitmap32`, `Rgba32` from Task 2.
- Produces: `SoftwareRasterizer` with `void FillTriangle(Bitmap32 target, in Vertex a, in Vertex b, in Vertex c, TextureSource? texture, in Rectangle scissor)` and `readonly record struct Vertex(Vector2 Position, Vector2 Uv, Rgba32 Color)`. Task 4 adds texture sampling, Task 5 adds scissor honoring. This task fills flat-colored triangles with the scissor covering the whole target.

- [x] **Step 1: Write the failing test**

Create `tests/ImGui.App.Testing.Tests/SoftwareRasterizerTests.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing.Tests;

using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class SoftwareRasterizerTests
{
	private static Vertex V(float x, float y, Rgba32 color) => new(new Vector2(x, y), Vector2.Zero, color);

	[TestMethod]
	public void FillTriangle_CoversInteriorPixels()
	{
		Bitmap32 target = new(16, 16);
		target.Clear(new Rgba32(0, 0, 0, 255));
		Rgba32 red = new(255, 0, 0, 255);

		// A right triangle covering the top-left half of a 12x12 box.
		SoftwareRasterizer.FillTriangle(
			target,
			V(0, 0, red), V(12, 0, red), V(0, 12, red),
			texture: null,
			scissor: Rectangle.FullSize(target));

		Assert.AreEqual(red, target.GetPixel(2, 2), "A pixel well inside the triangle should be filled.");
	}

	[TestMethod]
	public void FillTriangle_LeavesExteriorPixelsUntouched()
	{
		Bitmap32 target = new(16, 16);
		Rgba32 background = new(0, 0, 0, 255);
		target.Clear(background);
		Rgba32 red = new(255, 0, 0, 255);

		SoftwareRasterizer.FillTriangle(
			target,
			V(0, 0, red), V(12, 0, red), V(0, 12, red),
			texture: null,
			scissor: Rectangle.FullSize(target));

		Assert.AreEqual(background, target.GetPixel(14, 14), "A pixel outside the triangle must not be touched.");
	}

	[TestMethod]
	public void FillTriangle_ClampsToTargetBounds()
	{
		Bitmap32 target = new(8, 8);
		target.Clear(new Rgba32(0, 0, 0, 255));
		Rgba32 green = new(0, 255, 0, 255);

		// Deliberately spills far outside the target on every side.
		SoftwareRasterizer.FillTriangle(
			target,
			V(-50, -50, green), V(500, -50, green), V(-50, 500, green),
			texture: null,
			scissor: Rectangle.FullSize(target));

		Assert.AreEqual(green, target.GetPixel(0, 0), "Rasterizing off-target must clip rather than throw.");
	}

	[TestMethod]
	public void FillTriangle_DegenerateTriangle_DrawsNothing()
	{
		Bitmap32 target = new(8, 8);
		Rgba32 background = new(0, 0, 0, 255);
		target.Clear(background);

		SoftwareRasterizer.FillTriangle(
			target,
			V(1, 1, new Rgba32(255, 0, 0, 255)),
			V(1, 1, new Rgba32(255, 0, 0, 255)),
			V(1, 1, new Rgba32(255, 0, 0, 255)),
			texture: null,
			scissor: Rectangle.FullSize(target));

		Assert.AreEqual(background, target.GetPixel(1, 1), "A zero-area triangle should draw nothing.");
	}
}
```

- [x] **Step 2: Run the test to verify it fails**

Run: `dotnet build tests/ImGui.App.Testing.Tests/ImGui.App.Testing.Tests.csproj`
Expected: compile error, `SoftwareRasterizer`, `Vertex`, `TextureSource` and `Rectangle` do not exist.

- [x] **Step 3: Implement the rasterizer**

Create `ImGui.App.Testing/SoftwareRasterizer.cs`. `TextureSource` is declared here but only sampled in Task 4, so this task passes `null` and fills with vertex color:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing;

using System;
using System.Numerics;

/// <summary>A rasterizer input vertex, matching ImGui's vertex layout.</summary>
/// <param name="Position">Position in target pixels.</param>
/// <param name="Uv">Texture coordinate, normalized.</param>
/// <param name="Color">Vertex color, straight alpha.</param>
public readonly record struct Vertex(Vector2 Position, Vector2 Uv, Rgba32 Color);

/// <summary>An integer rectangle used for scissor clipping.</summary>
/// <param name="MinX">Inclusive left edge.</param>
/// <param name="MinY">Inclusive top edge.</param>
/// <param name="MaxX">Exclusive right edge.</param>
/// <param name="MaxY">Exclusive bottom edge.</param>
public readonly record struct Rectangle(int MinX, int MinY, int MaxX, int MaxY)
{
	/// <summary>Builds a rectangle covering an entire bitmap.</summary>
	/// <param name="target">The bitmap to cover.</param>
	/// <returns>A rectangle spanning the whole target.</returns>
	public static Rectangle FullSize(Bitmap32 target)
	{
		ArgumentNullException.ThrowIfNull(target);
		return new Rectangle(0, 0, target.Width, target.Height);
	}
}

/// <summary>
/// A CPU rasterizer covering the subset of drawing ImGui emits: indexed triangle lists, one
/// texture per draw command, vertex color modulation, straight-alpha blending, and scissor
/// rectangles. Deliberately not general purpose.
/// </summary>
public static class SoftwareRasterizer
{
	/// <summary>
	/// Fills one triangle into the target, blending over what is already there.
	/// </summary>
	/// <param name="target">The bitmap to draw into.</param>
	/// <param name="a">First vertex.</param>
	/// <param name="b">Second vertex.</param>
	/// <param name="c">Third vertex.</param>
	/// <param name="texture">Texture to sample, or null to use vertex color alone.</param>
	/// <param name="scissor">Clip rectangle in target pixels.</param>
	public static void FillTriangle(Bitmap32 target, in Vertex a, in Vertex b, in Vertex c, TextureSource? texture, in Rectangle scissor)
	{
		ArgumentNullException.ThrowIfNull(target);

		float area = Edge(a.Position, b.Position, c.Position);
		if (Math.Abs(area) < 1e-6f)
		{
			// Zero-area triangle. ImGui emits these routinely for collapsed geometry.
			return;
		}

		// Work in a consistent winding so the edge functions share a sign.
		Vertex v0 = a;
		Vertex v1 = area < 0 ? c : b;
		Vertex v2 = area < 0 ? b : c;
		area = Math.Abs(area);

		int minX = Math.Max(scissor.MinX, (int)MathF.Floor(Min3(v0.Position.X, v1.Position.X, v2.Position.X)));
		int minY = Math.Max(scissor.MinY, (int)MathF.Floor(Min3(v0.Position.Y, v1.Position.Y, v2.Position.Y)));
		int maxX = Math.Min(scissor.MaxX, (int)MathF.Ceiling(Max3(v0.Position.X, v1.Position.X, v2.Position.X)));
		int maxY = Math.Min(scissor.MaxY, (int)MathF.Ceiling(Max3(v0.Position.Y, v1.Position.Y, v2.Position.Y)));

		minX = Math.Max(minX, 0);
		minY = Math.Max(minY, 0);
		maxX = Math.Min(maxX, target.Width);
		maxY = Math.Min(maxY, target.Height);

		for (int y = minY; y < maxY; y++)
		{
			for (int x = minX; x < maxX; x++)
			{
				Vector2 p = new(x + 0.5f, y + 0.5f);

				float w0 = Edge(v1.Position, v2.Position, p);
				float w1 = Edge(v2.Position, v0.Position, p);
				float w2 = Edge(v0.Position, v1.Position, p);

				if (w0 < 0 || w1 < 0 || w2 < 0)
				{
					continue;
				}

				float l0 = w0 / area;
				float l1 = w1 / area;
				float l2 = w2 / area;

				Rgba32 source = Interpolate(v0.Color, v1.Color, v2.Color, l0, l1, l2);

				if (texture is not null)
				{
					Vector2 uv = (v0.Uv * l0) + (v1.Uv * l1) + (v2.Uv * l2);
					source = Modulate(source, texture.Sample(uv.X, uv.Y));
				}

				target.SetPixel(x, y, BlendOver(source, target.GetPixel(x, y)));
			}
		}
	}

	private static float Edge(Vector2 a, Vector2 b, Vector2 p) =>
		((b.X - a.X) * (p.Y - a.Y)) - ((b.Y - a.Y) * (p.X - a.X));

	private static float Min3(float a, float b, float c) => MathF.Min(a, MathF.Min(b, c));

	private static float Max3(float a, float b, float c) => MathF.Max(a, MathF.Max(b, c));

	private static Rgba32 Interpolate(Rgba32 a, Rgba32 b, Rgba32 c, float l0, float l1, float l2) => new(
		ToByte((a.R * l0) + (b.R * l1) + (c.R * l2)),
		ToByte((a.G * l0) + (b.G * l1) + (c.G * l2)),
		ToByte((a.B * l0) + (b.B * l1) + (c.B * l2)),
		ToByte((a.A * l0) + (b.A * l1) + (c.A * l2)));

	internal static Rgba32 Modulate(Rgba32 a, Rgba32 b) => new(
		ToByte(a.R * b.R / 255f),
		ToByte(a.G * b.G / 255f),
		ToByte(a.B * b.B / 255f),
		ToByte(a.A * b.A / 255f));

	internal static Rgba32 BlendOver(Rgba32 source, Rgba32 destination)
	{
		// Straight-alpha source-over. Alpha is coverage and never passes through a transfer
		// function, matching how the rest of this ecosystem treats it.
		float sa = source.A / 255f;
		float da = destination.A / 255f;
		float outA = sa + (da * (1f - sa));

		if (outA <= 0f)
		{
			return new Rgba32(0, 0, 0, 0);
		}

		float Channel(byte s, byte d) => ((s / 255f * sa) + (d / 255f * da * (1f - sa))) / outA;

		return new Rgba32(
			ToByte(Channel(source.R, destination.R) * 255f),
			ToByte(Channel(source.G, destination.G) * 255f),
			ToByte(Channel(source.B, destination.B) * 255f),
			ToByte(outA * 255f));
	}

	private static byte ToByte(float value) => (byte)Math.Clamp(MathF.Round(value), 0f, 255f);
}
```

- [x] **Step 4: Add the texture source placeholder**

`FillTriangle` references `TextureSource`. Create the minimal type now so this task compiles, and Task 4 tests its sampling. Add to `ImGui.App.Testing/TextureSource.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing;

/// <summary>
/// An RGBA8 texture the rasterizer can sample. Wraps a <see cref="Bitmap32"/> so the font atlas
/// and any application texture share one representation.
/// </summary>
/// <param name="pixels">The texture pixels.</param>
public sealed class TextureSource(Bitmap32 pixels)
{
	/// <summary>Gets the underlying pixels.</summary>
	public Bitmap32 Pixels { get; } = pixels;

	/// <summary>
	/// Samples the texture with nearest-neighbor filtering and clamped addressing.
	/// </summary>
	/// <param name="u">Horizontal coordinate, normalized.</param>
	/// <param name="v">Vertical coordinate, normalized.</param>
	/// <returns>The sampled color.</returns>
	public Rgba32 Sample(float u, float v) => throw new NotImplementedException("Task 4 implements sampling.");
}
```

- [x] **Step 5: Run the tests to verify they pass**

Run:

```bash
dotnet build tests/ImGui.App.Testing.Tests/ImGui.App.Testing.Tests.csproj
./tests/ImGui.App.Testing.Tests/bin/Debug/net10.0/ktsu.ImGui.App.Testing.Tests.exe
```

Expected: all four `SoftwareRasterizerTests` pass. `TextureSource.Sample` is never reached, because every test passes `texture: null`.

- [x] **Step 6: Commit**

```bash
git add ImGui.App.Testing/SoftwareRasterizer.cs ImGui.App.Testing/TextureSource.cs tests/ImGui.App.Testing.Tests/SoftwareRasterizerTests.cs
git commit -m "feat: rasterize flat-shaded triangles in software [minor]"
```

---

### Task 4: Texture Sampling and Alpha Blending

**Files:**
- Modify: `ImGui.App.Testing/TextureSource.cs`
- Create: `tests/ImGui.App.Testing.Tests/TextureSourceTests.cs`
- Create: `tests/ImGui.App.Testing.Tests/BlendingTests.cs`

**Interfaces:**
- Consumes: `Bitmap32`, `Rgba32`, `SoftwareRasterizer.Modulate`, `SoftwareRasterizer.BlendOver` from Tasks 2 and 3.
- Produces: a working `TextureSource.Sample(float u, float v)`.

- [x] **Step 1: Write the failing sampling tests**

Create `tests/ImGui.App.Testing.Tests/TextureSourceTests.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class TextureSourceTests
{
	private static TextureSource TwoByTwo()
	{
		Bitmap32 pixels = new(2, 2);
		pixels.SetPixel(0, 0, new Rgba32(255, 0, 0, 255));
		pixels.SetPixel(1, 0, new Rgba32(0, 255, 0, 255));
		pixels.SetPixel(0, 1, new Rgba32(0, 0, 255, 255));
		pixels.SetPixel(1, 1, new Rgba32(255, 255, 0, 255));
		return new TextureSource(pixels);
	}

	[TestMethod]
	public void Sample_TopLeft_ReturnsFirstTexel() =>
		Assert.AreEqual(new Rgba32(255, 0, 0, 255), TwoByTwo().Sample(0.1f, 0.1f));

	[TestMethod]
	public void Sample_BottomRight_ReturnsLastTexel() =>
		Assert.AreEqual(new Rgba32(255, 255, 0, 255), TwoByTwo().Sample(0.9f, 0.9f));

	[TestMethod]
	public void Sample_BeyondRange_ClampsRatherThanWrapping() =>
		Assert.AreEqual(new Rgba32(255, 255, 0, 255), TwoByTwo().Sample(5f, 5f));

	[TestMethod]
	public void Sample_NegativeCoordinates_ClampToFirstTexel() =>
		Assert.AreEqual(new Rgba32(255, 0, 0, 255), TwoByTwo().Sample(-3f, -3f));
}
```

- [x] **Step 2: Write the failing blending tests**

Create `tests/ImGui.App.Testing.Tests/BlendingTests.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class BlendingTests
{
	[TestMethod]
	public void BlendOver_OpaqueSource_ReplacesDestination()
	{
		Rgba32 result = SoftwareRasterizer.BlendOver(new Rgba32(255, 0, 0, 255), new Rgba32(0, 0, 255, 255));

		Assert.AreEqual(new Rgba32(255, 0, 0, 255), result);
	}

	[TestMethod]
	public void BlendOver_TransparentSource_LeavesDestination()
	{
		Rgba32 result = SoftwareRasterizer.BlendOver(new Rgba32(255, 0, 0, 0), new Rgba32(0, 0, 255, 255));

		Assert.AreEqual(new Rgba32(0, 0, 255, 255), result);
	}

	[TestMethod]
	public void BlendOver_HalfAlpha_MixesTowardTheSource()
	{
		Rgba32 result = SoftwareRasterizer.BlendOver(new Rgba32(255, 255, 255, 128), new Rgba32(0, 0, 0, 255));

		// 128/255 of the way from black to white, rounded.
		Assert.IsTrue(result.R is >= 127 and <= 129, $"Expected roughly half-way, got {result.R}.");
		Assert.AreEqual(255, result.A, "Blending onto an opaque destination stays opaque.");
	}

	[TestMethod]
	public void Modulate_WhiteTexel_LeavesVertexColorUnchanged()
	{
		Rgba32 result = SoftwareRasterizer.Modulate(new Rgba32(10, 20, 30, 40), new Rgba32(255, 255, 255, 255));

		Assert.AreEqual(new Rgba32(10, 20, 30, 40), result);
	}
}
```

- [x] **Step 3: Run the tests to verify they fail**

Run:

```bash
dotnet build tests/ImGui.App.Testing.Tests/ImGui.App.Testing.Tests.csproj
./tests/ImGui.App.Testing.Tests/bin/Debug/net10.0/ktsu.ImGui.App.Testing.Tests.exe
```

Expected: the four `TextureSourceTests` fail with `NotImplementedException`. The `BlendingTests` should already pass, because Task 3 implemented `BlendOver` and `Modulate`. If any blending test fails, fix the implementation now, because the rasterizer depends on it.

- [x] **Step 4: Implement sampling**

Replace the body of `Sample` in `ImGui.App.Testing/TextureSource.cs`:

```csharp
	public Rgba32 Sample(float u, float v)
	{
		// Nearest neighbor with clamped addressing. ImGui's atlas sampling is effectively
		// point-sampled at integer scale, and nearest keeps output identical across machines
		// with no filtering rules to disagree about.
		int x = (int)MathF.Floor(u * Pixels.Width);
		int y = (int)MathF.Floor(v * Pixels.Height);

		x = Math.Clamp(x, 0, Pixels.Width - 1);
		y = Math.Clamp(y, 0, Pixels.Height - 1);

		return Pixels.GetPixel(x, y);
	}
```

Add `using System;` to the file's using directives if it is not already present.

- [x] **Step 5: Run the tests to verify they pass**

Run:

```bash
dotnet build tests/ImGui.App.Testing.Tests/ImGui.App.Testing.Tests.csproj
./tests/ImGui.App.Testing.Tests/bin/Debug/net10.0/ktsu.ImGui.App.Testing.Tests.exe
```

Expected: every test passes.

- [x] **Step 6: Add a textured triangle test**

Append to `tests/ImGui.App.Testing.Tests/SoftwareRasterizerTests.cs`:

```csharp
	[TestMethod]
	public void FillTriangle_WithTexture_ModulatesVertexColorByTexel()
	{
		Bitmap32 target = new(16, 16);
		target.Clear(new Rgba32(0, 0, 0, 255));

		Bitmap32 texel = new(1, 1);
		texel.SetPixel(0, 0, new Rgba32(0, 255, 0, 255));

		SoftwareRasterizer.FillTriangle(
			target,
			V(0, 0, new Rgba32(255, 255, 255, 255)),
			V(12, 0, new Rgba32(255, 255, 255, 255)),
			V(0, 12, new Rgba32(255, 255, 255, 255)),
			new TextureSource(texel),
			Rectangle.FullSize(target));

		Assert.AreEqual(new Rgba32(0, 255, 0, 255), target.GetPixel(2, 2), "White vertex color times a green texel is green.");
	}
```

- [x] **Step 7: Run the tests again**

Run: `./tests/ImGui.App.Testing.Tests/bin/Debug/net10.0/ktsu.ImGui.App.Testing.Tests.exe`
Expected: all tests pass.

- [x] **Step 8: Commit**

```bash
git add ImGui.App.Testing/TextureSource.cs tests/ImGui.App.Testing.Tests
git commit -m "feat: sample textures and blend straight alpha in the rasterizer [minor]"
```

---

### Task 5: Scissor Clipping

**Files:**
- Create: `tests/ImGui.App.Testing.Tests/ScissorTests.cs`

**Interfaces:**
- Consumes: `SoftwareRasterizer.FillTriangle` from Task 3, which already accepts and applies a scissor rectangle.
- Produces: no new API. This task proves clipping works and fixes it if it does not.

- [x] **Step 1: Write the failing test**

Create `tests/ImGui.App.Testing.Tests/ScissorTests.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing.Tests;

using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class ScissorTests
{
	private static Vertex V(float x, float y) =>
		new(new Vector2(x, y), Vector2.Zero, new Rgba32(255, 0, 0, 255));

	[TestMethod]
	public void FillTriangle_OutsideScissor_IsNotDrawn()
	{
		Bitmap32 target = new(32, 32);
		Rgba32 background = new(0, 0, 0, 255);
		target.Clear(background);

		// The triangle covers the whole bitmap, but the scissor admits only a small box.
		SoftwareRasterizer.FillTriangle(
			target,
			V(0, 0), V(32, 0), V(0, 32),
			texture: null,
			scissor: new Rectangle(4, 4, 8, 8));

		Assert.AreEqual(new Rgba32(255, 0, 0, 255), target.GetPixel(5, 5), "Inside the scissor should be drawn.");
		Assert.AreEqual(background, target.GetPixel(1, 1), "Outside the scissor must be untouched.");
		Assert.AreEqual(background, target.GetPixel(10, 2), "Outside the scissor must be untouched.");
	}

	[TestMethod]
	public void FillTriangle_ScissorLargerThanTarget_DoesNotThrow()
	{
		Bitmap32 target = new(8, 8);
		target.Clear(new Rgba32(0, 0, 0, 255));

		SoftwareRasterizer.FillTriangle(
			target,
			V(0, 0), V(8, 0), V(0, 8),
			texture: null,
			scissor: new Rectangle(-100, -100, 1000, 1000));

		Assert.AreEqual(new Rgba32(255, 0, 0, 255), target.GetPixel(1, 1), "An oversized scissor should clamp to the target.");
	}

	[TestMethod]
	public void FillTriangle_EmptyScissor_DrawsNothing()
	{
		Bitmap32 target = new(8, 8);
		Rgba32 background = new(0, 0, 0, 255);
		target.Clear(background);

		SoftwareRasterizer.FillTriangle(
			target,
			V(0, 0), V(8, 0), V(0, 8),
			texture: null,
			scissor: new Rectangle(4, 4, 4, 4));

		Assert.AreEqual(background, target.GetPixel(4, 4), "A zero-area scissor should admit nothing.");
	}
}
```

- [x] **Step 2: Run the tests**

Run:

```bash
dotnet build tests/ImGui.App.Testing.Tests/ImGui.App.Testing.Tests.csproj
./tests/ImGui.App.Testing.Tests/bin/Debug/net10.0/ktsu.ImGui.App.Testing.Tests.exe
```

Expected: these tests should pass against the Task 3 implementation, which already intersects the scissor with the triangle bounds and clamps to the target. If any fail, fix `FillTriangle` and rerun. Do not weaken the tests to match the implementation.

- [x] **Step 3: Commit**

```bash
git add tests/ImGui.App.Testing.Tests/ScissorTests.cs
git commit -m "test: cover scissor clipping in the rasterizer [patch]"
```

---

### Task 6: Software Renderer Backend

**Files:**
- Create: `ImGui.App.Testing/SoftwareRendererBackend.cs`
- Create: `tests/ImGui.App.Testing.Tests/SoftwareRendererBackendTests.cs`

**Interfaces:**
- Consumes: `IRendererBackend` (internal to `ktsu.ImGui.App`), `Bitmap32`, `TextureSource`, `SoftwareRasterizer`, `Rectangle` from earlier tasks.
- Produces: `internal sealed class SoftwareRendererBackend : IRendererBackend` with `Bitmap32 Target { get; }`, `void Clear(Rgba32 color)`, plus the interface members `CreateTexture`, `UpdateTexture`, `DeleteTexture`, `RenderDrawData`.

- [x] **Step 1: Write the failing test**

Create `tests/ImGui.App.Testing.Tests/SoftwareRendererBackendTests.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class SoftwareRendererBackendTests
{
	[TestMethod]
	public void CreateTexture_ReturnsDistinctHandles()
	{
		using SoftwareRendererBackend backend = new(8, 8);
		byte[] rgba = new byte[4];

		nint first = backend.CreateTexture(rgba, 1, 1);
		nint second = backend.CreateTexture(rgba, 1, 1);

		Assert.AreNotEqual(first, second, "Each texture needs its own handle.");
		Assert.AreNotEqual(0, first, "Zero is reserved for 'no texture'.");
	}

	[TestMethod]
	public void UpdateTexture_ReplacesPixelsInPlace()
	{
		using SoftwareRendererBackend backend = new(8, 8);
		byte[] red = [255, 0, 0, 255];
		byte[] green = [0, 255, 0, 255];

		nint id = backend.CreateTexture(red, 1, 1);
		bool updated = backend.UpdateTexture(id, green, 1, 1);

		Assert.IsTrue(updated, "The software backend can always update in place.");
		Assert.AreEqual(new Rgba32(0, 255, 0, 255), backend.GetTexture(id).Sample(0.5f, 0.5f));
	}

	[TestMethod]
	public void DeleteTexture_RemovesTheHandle()
	{
		using SoftwareRendererBackend backend = new(8, 8);
		nint id = backend.CreateTexture(new byte[4], 1, 1);

		backend.DeleteTexture(id);

		Assert.ThrowsExactly<KeyNotFoundException>(() => backend.GetTexture(id));
	}

	[TestMethod]
	public void Clear_FillsTheRenderTarget()
	{
		using SoftwareRendererBackend backend = new(4, 4);

		backend.Clear(new Rgba32(9, 9, 9, 255));

		Assert.AreEqual(new Rgba32(9, 9, 9, 255), backend.Target.GetPixel(0, 0));
	}
}
```

- [x] **Step 2: Run the test to verify it fails**

Run: `dotnet build tests/ImGui.App.Testing.Tests/ImGui.App.Testing.Tests.csproj`
Expected: compile error, `SoftwareRendererBackend` does not exist.

- [x] **Step 3: Implement the backend**

Create `ImGui.App.Testing/SoftwareRendererBackend.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing;

using System;
using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;

/// <summary>
/// Renders ImGui draw data into a <see cref="Bitmap32"/> on the CPU. Implements the same
/// <see cref="IRendererBackend"/> seam the OpenGL and Metal backends do, so an application renders
/// through its normal path with no GPU present.
/// </summary>
/// <param name="width">Render target width in pixels.</param>
/// <param name="height">Render target height in pixels.</param>
internal sealed class SoftwareRendererBackend(int width, int height) : IRendererBackend
{
	private readonly Dictionary<nint, TextureSource> textures = [];
	private nint nextId = 1;

	/// <summary>Gets the render target holding the most recently rendered frame.</summary>
	public Bitmap32 Target { get; } = new(width, height);

	/// <summary>Fills the render target with one color, replacing the previous frame.</summary>
	/// <param name="color">The clear color.</param>
	public void Clear(Rgba32 color) => Target.Clear(color);

	/// <summary>Looks up a texture by handle.</summary>
	/// <param name="id">A handle returned by <see cref="CreateTexture"/>.</param>
	/// <returns>The texture behind that handle.</returns>
	public TextureSource GetTexture(nint id) => textures[id];

	/// <inheritdoc/>
	public nint CreateTexture(ReadOnlySpan<byte> rgba, int width, int height)
	{
		nint id = nextId++;
		textures[id] = new TextureSource(ToBitmap(rgba, width, height));
		return id;
	}

	/// <inheritdoc/>
	public bool UpdateTexture(nint id, ReadOnlySpan<byte> rgba, int width, int height)
	{
		textures[id] = new TextureSource(ToBitmap(rgba, width, height));
		return true;
	}

	/// <inheritdoc/>
	public void DeleteTexture(nint id) => textures.Remove(id);

	/// <inheritdoc/>
	public void RenderDrawData(ImDrawDataPtr drawData)
	{
		if (drawData.Handle is null || drawData.CmdListsCount == 0)
		{
			return;
		}

		Vector2 origin = drawData.DisplayPos;

		for (int list = 0; list < drawData.CmdListsCount; list++)
		{
			ImDrawListPtr cmdList = drawData.CmdLists[list];

			for (int cmdIndex = 0; cmdIndex < cmdList.CmdBuffer.Size; cmdIndex++)
			{
				ImDrawCmdPtr cmd = cmdList.CmdBuffer[cmdIndex];

				// A user callback replaces drawing for that command. The harness has no
				// callbacks of its own and cannot execute an application's, so skip it.
				if (cmd.UserCallback != null)
				{
					continue;
				}

				Rectangle scissor = new(
					(int)MathF.Floor(cmd.ClipRect.X - origin.X),
					(int)MathF.Floor(cmd.ClipRect.Y - origin.Y),
					(int)MathF.Ceiling(cmd.ClipRect.Z - origin.X),
					(int)MathF.Ceiling(cmd.ClipRect.W - origin.Y));

				textures.TryGetValue(cmd.GetTexID(), out TextureSource? texture);

				for (int i = 0; i < cmd.ElemCount; i += 3)
				{
					uint i0 = cmdList.IdxBuffer[(int)(cmd.IdxOffset + i)];
					uint i1 = cmdList.IdxBuffer[(int)(cmd.IdxOffset + i + 1)];
					uint i2 = cmdList.IdxBuffer[(int)(cmd.IdxOffset + i + 2)];

					Vertex a = ToVertex(cmdList.VtxBuffer[(int)(cmd.VtxOffset + i0)], origin);
					Vertex b = ToVertex(cmdList.VtxBuffer[(int)(cmd.VtxOffset + i1)], origin);
					Vertex c = ToVertex(cmdList.VtxBuffer[(int)(cmd.VtxOffset + i2)], origin);

					SoftwareRasterizer.FillTriangle(Target, a, b, c, texture, scissor);
				}
			}
		}
	}

	/// <inheritdoc/>
	public void Dispose() => textures.Clear();

	private static Vertex ToVertex(ImDrawVert vertex, Vector2 origin) => new(
		new Vector2(vertex.Pos.X - origin.X, vertex.Pos.Y - origin.Y),
		new Vector2(vertex.Uv.X, vertex.Uv.Y),
		FromAbgr(vertex.Col));

	private static Rgba32 FromAbgr(uint packed) => new(
		(byte)(packed & 0xFF),
		(byte)((packed >> 8) & 0xFF),
		(byte)((packed >> 16) & 0xFF),
		(byte)((packed >> 24) & 0xFF));

	private static Bitmap32 ToBitmap(ReadOnlySpan<byte> rgba, int width, int height)
	{
		Bitmap32 bitmap = new(width, height);
		rgba[..(width * height * 4)].CopyTo(bitmap.Pixels);
		return bitmap;
	}
}
```

- [x] **Step 4: Run the tests to verify they pass**

Run:

```bash
dotnet build tests/ImGui.App.Testing.Tests/ImGui.App.Testing.Tests.csproj
./tests/ImGui.App.Testing.Tests/bin/Debug/net10.0/ktsu.ImGui.App.Testing.Tests.exe
```

Expected: all `SoftwareRendererBackendTests` pass.

Note on the ImGui binding: `cmd.GetTexID()`, `drawData.CmdLists`, `cmdList.IdxBuffer` and `ImDrawVert.Col` are the Hexa.NET.ImGui 2.2.x shapes. If a member name does not resolve, check the actual surface with

```bash
grep -n "GetTexID\|CmdLists\|IdxBuffer" ImGui.App/bin/Debug/net10.0/Hexa.NET.ImGui.xml | head
```

and adjust, rather than guessing.

- [x] **Step 5: Commit**

```bash
git add ImGui.App.Testing/SoftwareRendererBackend.cs tests/ImGui.App.Testing.Tests/SoftwareRendererBackendTests.cs
git commit -m "feat: render ImGui draw data through a software backend [minor]"
```

---

### Task 7: Frame Contents Extraction

**Files:**
- Modify: `ImGui.App/ImGuiApp.cs:718-742`
- Create: `tests/ImGui.App.Tests/FrameContentsTests.cs`

**Interfaces:**
- Consumes: existing internal `RenderWithScaling`, `RenderAppMenu`, `RenderWindowContents`, `RenderPerformanceMonitor`.
- Produces: `internal static void RenderFrameContents(ImGuiAppConfig config, float delta)` on `ImGuiApp`, callable by the harness.

- [x] **Step 1: Read the current handler**

Run: `sed -n '718,745p' ImGui.App/ImGuiApp.cs`

The body inside `window!.Render += delta => { ... }` currently performs the font check, the GL clear, a `RenderWithScaling` block wrapping the frame wrapper and three render calls, `controller?.Render()`, and `ApplyFrameRateLimit()`.

- [x] **Step 2: Extract the application content into a shared method**

Add this method to `ImGuiApp`, directly after `SetupWindowRenderHandler`:

```csharp
	/// <summary>
	/// Runs the application-facing part of one frame: the frame wrapper, the application menu,
	/// the application's render callback, and the performance monitor. Shared by the windowed
	/// render handler and by the headless test harness, so both drive identical application code
	/// rather than one driving a copy of the other.
	/// </summary>
	/// <param name="config">The configuration supplying the render callbacks.</param>
	/// <param name="delta">Seconds elapsed since the previous frame.</param>
	internal static void RenderFrameContents(ImGuiAppConfig config, float delta)
	{
		ArgumentNullException.ThrowIfNull(config);

		RenderWithScaling(() =>
		{
			using ScopedAction? frameWrapper = config.FrameWrapperFactory.Invoke();
			RenderAppMenu(config.OnAppMenu);
			RenderWindowContents(config.OnRender, delta);
			RenderPerformanceMonitor();
		});
	}
```

- [x] **Step 3: Call the extracted method from the window handler**

Replace the `RenderWithScaling(() => { ... });` block inside `window!.Render += delta => { ... }` with a single call, leaving the font check, GL clear, `controller?.Render()` and `ApplyFrameRateLimit()` exactly as they are:

```csharp
			RenderFrameContents(config, (float)delta);
```

- [x] **Step 4: Write a test that the extraction preserves call order**

Create `tests/ImGui.App.Tests/FrameContentsTests.cs`. This does not start a window. It calls the extracted method directly and records the order the callbacks fire, which is the behavior the extraction must preserve:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Tests;

using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class FrameContentsTests
{
	[TestMethod]
	public void RenderFrameContents_NullConfig_Throws() =>
		Assert.ThrowsExactly<ArgumentNullException>(() => ImGuiApp.RenderFrameContents(null!, 0.016f));

	[TestMethod]
	public void RenderFrameContents_InvokesTheFrameWrapperAroundTheCallbacks()
	{
		List<string> order = [];

		ImGuiAppConfig config = new()
		{
			FrameWrapperFactory = () => new ktsu.ScopedAction.ScopedAction(
				onOpen: () => order.Add("wrapper-open"),
				onClose: () => order.Add("wrapper-close")),
			OnAppMenu = () => order.Add("menu"),
			OnRender = _ => order.Add("render"),
		};

		// Without an ImGui context the render helpers cannot draw, so this asserts only the
		// ordering that RenderFrameContents itself controls. A context-backed end-to-end check
		// lives in the harness tests.
		try
		{
			ImGuiApp.RenderFrameContents(config, 0.016f);
		}
		catch (Exception)
		{
			// Drawing without a context is expected to fail here; ordering is still observable.
		}

		Assert.IsTrue(order.Count > 0, "The frame wrapper or a callback should have run.");
		Assert.AreEqual("wrapper-open", order[0], "The frame wrapper must open before any callback runs.");
	}
}
```

- [x] **Step 5: Run the ImGui.App tests**

Run:

```bash
dotnet build tests/ImGui.App.Tests/ImGui.App.Tests.csproj
./tests/ImGui.App.Tests/bin/Debug/net10.0/ktsu.ImGui.App.Tests.exe
```

Expected: the existing suite still passes and the two new tests pass. If the ordering test proves impossible to run without an ImGui context, delete it and rely on the harness end-to-end test in Task 9 instead. Say so in the commit message rather than leaving a test that asserts nothing.

- [x] **Step 6: Confirm the windowed path still works**

The extraction touches the real render loop, so verify a windowed application still runs:

```bash
dotnet build examples/ImGuiAppDemo/ImGuiAppDemo.csproj
```

Expected: build succeeds. Run the demo briefly if a display is available and confirm the window renders its menu.

- [x] **Step 7: Commit**

```bash
git add ImGui.App/ImGuiApp.cs tests/ImGui.App.Tests/FrameContentsTests.cs
git commit -m "refactor: extract per-frame application rendering into RenderFrameContents [patch]"
```

---

### Task 8: Headless ImGui Context

**Files:**
- Create: `ImGui.App.Testing/HeadlessImGuiContext.cs`
- Create: `tests/ImGui.App.Testing.Tests/HeadlessImGuiContextTests.cs`

**Interfaces:**
- Consumes: `SoftwareRendererBackend` from Task 6.
- Produces: `internal sealed class HeadlessImGuiContext : IDisposable` with a constructor taking `(int width, int height, float dpiScale, SoftwareRendererBackend backend)`, and members `void BeginFrame(float delta)`, `void EndFrame()`, `ImGuiIOPtr IO { get; }`.

- [x] **Step 1: Write the failing test**

Create `tests/ImGui.App.Testing.Tests/HeadlessImGuiContextTests.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing.Tests;

using Hexa.NET.ImGui;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class HeadlessImGuiContextTests
{
	[TestMethod]
	public void Constructor_SetsDisplaySize()
	{
		using SoftwareRendererBackend backend = new(320, 240);
		using HeadlessImGuiContext context = new(320, 240, 1.0f, backend);

		Assert.AreEqual(320f, context.IO.DisplaySize.X, "Display width should match the requested size.");
		Assert.AreEqual(240f, context.IO.DisplaySize.Y, "Display height should match the requested size.");
	}

	[TestMethod]
	public void Constructor_BuildsAFontAtlasTexture()
	{
		using SoftwareRendererBackend backend = new(64, 64);
		using HeadlessImGuiContext context = new(64, 64, 1.0f, backend);

		Assert.IsTrue(context.IO.Fonts.TexIsBuilt, "The font atlas must be built before the first frame.");
	}

	[TestMethod]
	public void BeginFrame_ThenEndFrame_ProducesDrawData()
	{
		using SoftwareRendererBackend backend = new(64, 64);
		using HeadlessImGuiContext context = new(64, 64, 1.0f, backend);

		context.BeginFrame(1f / 60f);
		ImGui.Begin("probe");
		ImGui.TextUnformatted("hello");
		ImGui.End();
		context.EndFrame();

		ImDrawDataPtr drawData = ImGui.GetDrawData();

		Assert.IsTrue(drawData.CmdListsCount > 0, "A window with text should produce at least one command list.");
	}
}
```

- [x] **Step 2: Run the test to verify it fails**

Run: `dotnet build tests/ImGui.App.Testing.Tests/ImGui.App.Testing.Tests.csproj`
Expected: compile error, `HeadlessImGuiContext` does not exist.

- [x] **Step 3: Implement the headless context**

Create `ImGui.App.Testing/HeadlessImGuiContext.cs`. The atlas API here matches what `ImGuiController.RecreateFontDeviceTexture` uses in this repository, which is the authoritative reference for the bound ImGui version:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing;

using System;
using System.Numerics;

using Hexa.NET.ImGui;

/// <summary>
/// Owns an ImGui context with no window, no input backend and no GPU. Exists because
/// <c>ImGuiController</c> is simultaneously the context owner and the OpenGL renderer, and every
/// constructor overload requires Silk's <c>GL</c>, <c>IView</c> and <c>IInputContext</c>. Tracked
/// for removal by ImGuiApp issue #313, which splits those responsibilities.
/// </summary>
internal sealed class HeadlessImGuiContext : IDisposable
{
	private readonly SoftwareRendererBackend backend;
	private ImGuiContextPtr context;
	private nint fontTextureId;
	private bool disposed;

	/// <summary>Gets the ImGui IO block for this context.</summary>
	public ImGuiIOPtr IO => ImGui.GetIO();

	/// <summary>Initializes the context, display metrics and font atlas.</summary>
	/// <param name="width">Display width in pixels.</param>
	/// <param name="height">Display height in pixels.</param>
	/// <param name="dpiScale">Framebuffer scale applied to the display.</param>
	/// <param name="backend">The renderer that will receive draw data and own the atlas texture.</param>
	public HeadlessImGuiContext(int width, int height, float dpiScale, SoftwareRendererBackend backend)
	{
		ArgumentNullException.ThrowIfNull(backend);
		this.backend = backend;

		context = ImGui.CreateContext();
		ImGui.SetCurrentContext(context);

		ImGuiIOPtr io = ImGui.GetIO();
		io.DisplaySize = new Vector2(width, height);
		io.DisplayFramebufferScale = new Vector2(dpiScale, dpiScale);
		io.DeltaTime = 1f / 60f;

		// Layout state must never leak between test runs, so nothing is read from or written
		// to disk. IniFilename is a pointer in this binding, so clearing it means null.
		unsafe
		{
			io.IniFilename = null;
			io.LogFilename = null;
		}

		BuildFontAtlas(io);
	}

	/// <summary>Begins a frame.</summary>
	/// <param name="delta">Seconds elapsed since the previous frame.</param>
	public void BeginFrame(float delta)
	{
		ImGui.SetCurrentContext(context);
		ImGui.GetIO().DeltaTime = delta;
		ImGui.NewFrame();
	}

	/// <summary>Ends the frame and submits its draw data to the renderer.</summary>
	public void EndFrame()
	{
		ImGui.Render();
		backend.RenderDrawData(ImGui.GetDrawData());
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		if (disposed)
		{
			return;
		}

		if (fontTextureId != 0)
		{
			backend.DeleteTexture(fontTextureId);
			fontTextureId = 0;
		}

		if (context.Handle is not null)
		{
			ImGui.DestroyContext(context);
			context = default;
		}

		disposed = true;
	}

	private unsafe void BuildFontAtlas(ImGuiIOPtr io)
	{
		if (!io.Fonts.TexIsBuilt)
		{
			ImGuiP.ImFontAtlasBuildMain(io.Fonts);
		}

		ImTextureDataPtr texData = io.Fonts.TexData;
		if (texData.Pixels is null || texData.Width <= 0 || texData.Height <= 0)
		{
			throw new InvalidOperationException("ImGui produced no font atlas pixels, so nothing could be rendered.");
		}

		ReadOnlySpan<byte> pixels = new(texData.Pixels, texData.Width * texData.Height * 4);
		fontTextureId = backend.CreateTexture(pixels, texData.Width, texData.Height);
		texData.SetTexID(fontTextureId);
	}
}
```

- [x] **Step 4: Allow unsafe blocks in the project**

`BuildFontAtlas` and the ini filename assignment need unsafe code. Add to the `PropertyGroup` in `ImGui.App.Testing/ImGui.App.Testing.csproj`:

```xml
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
```

- [x] **Step 5: Run the tests to verify they pass**

Run:

```bash
dotnet build tests/ImGui.App.Testing.Tests/ImGui.App.Testing.Tests.csproj
./tests/ImGui.App.Testing.Tests/bin/Debug/net10.0/ktsu.ImGui.App.Testing.Tests.exe
```

Expected: all three `HeadlessImGuiContextTests` pass.

- [x] **Step 6: Commit**

```bash
git add ImGui.App.Testing tests/ImGui.App.Testing.Tests/HeadlessImGuiContextTests.cs
git commit -m "feat: add a headless ImGui context with a software font atlas [minor]"
```

---

### Task 9: The Harness Entry Point

**Files:**
- Create: `ImGui.App.Testing/ImGuiAppHarness.cs`
- Create: `ImGui.App.Testing/HarnessOptions.cs`
- Create: `tests/ImGui.App.Testing.Tests/ImGuiAppHarnessTests.cs`

**Interfaces:**
- Consumes: `HeadlessImGuiContext`, `SoftwareRendererBackend`, `ImGuiApp.RenderFrameContents` from Tasks 6, 7 and 8.
- Produces: `public sealed class ImGuiAppHarness : IDisposable` with `static ImGuiAppHarness Start(ImGuiAppConfig config, HarnessOptions options)`, `void Step()`, `void Step(int frames)`, `int FrameCount { get; }`, `Bitmap32 Target { get; }`. Also `public sealed record HarnessOptions` with `int Width`, `int Height`, `float DpiScale`, `float FrameDelta`, `Rgba32 ClearColor`.

- [x] **Step 1: Write the failing test**

Create `tests/ImGui.App.Testing.Tests/ImGuiAppHarnessTests.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing.Tests;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class ImGuiAppHarnessTests
{
	private static HarnessOptions SmallWindow() => new() { Width = 200, Height = 120 };

	[TestMethod]
	public void Step_InvokesTheApplicationRenderCallback()
	{
		int calls = 0;
		ImGuiAppConfig config = new() { OnRender = _ => calls++ };

		using ImGuiAppHarness harness = ImGuiAppHarness.Start(config, SmallWindow());
		harness.Step();

		Assert.AreEqual(1, calls, "One step should invoke OnRender exactly once.");
	}

	[TestMethod]
	public void Step_AdvancesTheFrameCount()
	{
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(new ImGuiAppConfig(), SmallWindow());

		harness.Step(frames: 3);

		Assert.AreEqual(3, harness.FrameCount);
	}

	[TestMethod]
	public void Step_RendersIntoTheTarget()
	{
		ImGuiAppConfig config = new()
		{
			OnRender = _ =>
			{
				ImGui.SetNextWindowPos(new System.Numerics.Vector2(0, 0));
				ImGui.SetNextWindowSize(new System.Numerics.Vector2(120, 80));
				ImGui.Begin("probe", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize);
				ImGui.TextUnformatted("hello");
				ImGui.End();
			},
		};

		using ImGuiAppHarness harness = ImGuiAppHarness.Start(config, SmallWindow());
		harness.Step(frames: 2);

		bool anythingDrawn = false;
		for (int y = 0; y < harness.Target.Height && !anythingDrawn; y++)
		{
			for (int x = 0; x < harness.Target.Width; x++)
			{
				if (harness.Target.GetPixel(x, y) != harness.Options.ClearColor)
				{
					anythingDrawn = true;
					break;
				}
			}
		}

		Assert.IsTrue(anythingDrawn, "Rendering a window should change pixels in the target.");
	}

	[TestMethod]
	public void Step_ExceptionInRenderCallback_PropagatesWithFrameNumber()
	{
		ImGuiAppConfig config = new() { OnRender = _ => throw new InvalidOperationException("boom") };
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(config, SmallWindow());

		HarnessFrameException error = Assert.ThrowsExactly<HarnessFrameException>(harness.Step);

		Assert.AreEqual(0, error.FrameNumber, "The first frame is frame zero.");
		Assert.IsInstanceOfType<InvalidOperationException>(error.InnerException);
	}

	[TestMethod]
	public void Start_WhileAnotherHarnessIsLive_Throws()
	{
		using ImGuiAppHarness first = ImGuiAppHarness.Start(new ImGuiAppConfig(), SmallWindow());

		Assert.ThrowsExactly<InvalidOperationException>(
			() => ImGuiAppHarness.Start(new ImGuiAppConfig(), SmallWindow()));
	}
}
```

- [x] **Step 2: Run the test to verify it fails**

Run: `dotnet build tests/ImGui.App.Testing.Tests/ImGui.App.Testing.Tests.csproj`
Expected: compile error, `ImGuiAppHarness`, `HarnessOptions` and `HarnessFrameException` do not exist.

- [x] **Step 3: Implement the options record**

Create `ImGui.App.Testing/HarnessOptions.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing;

/// <summary>
/// Everything that would otherwise vary between runs, pinned so a test produces the same result on
/// a developer machine and on a continuous integration runner.
/// </summary>
public sealed record HarnessOptions
{
	/// <summary>Gets the display width in pixels.</summary>
	public int Width { get; init; } = 1280;

	/// <summary>Gets the display height in pixels.</summary>
	public int Height { get; init; } = 720;

	/// <summary>Gets the framebuffer scale. Fixed so layout does not depend on the host display.</summary>
	public float DpiScale { get; init; } = 1.0f;

	/// <summary>Gets the seconds reported to the application for every frame, regardless of real time.</summary>
	public float FrameDelta { get; init; } = 1f / 60f;

	/// <summary>Gets the color the target is filled with before each frame.</summary>
	public Rgba32 ClearColor { get; init; } = new(0, 0, 0, 255);
}
```

- [x] **Step 4: Implement the frame exception**

Create `ImGui.App.Testing/HarnessFrameException.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing;

using System;

/// <summary>
/// Thrown when an application callback fails during a harness frame. Carries the frame number,
/// because a failure on frame 200 of a drag is a different problem from one on frame 0.
/// </summary>
public sealed class HarnessFrameException : Exception
{
	/// <summary>Gets the zero-based frame that failed.</summary>
	public int FrameNumber { get; }

	/// <summary>Initializes a new instance of the <see cref="HarnessFrameException"/> class.</summary>
	public HarnessFrameException() => FrameNumber = -1;

	/// <summary>Initializes a new instance of the <see cref="HarnessFrameException"/> class.</summary>
	/// <param name="message">The error message.</param>
	public HarnessFrameException(string message) : base(message) => FrameNumber = -1;

	/// <summary>Initializes a new instance of the <see cref="HarnessFrameException"/> class.</summary>
	/// <param name="message">The error message.</param>
	/// <param name="innerException">The failure that occurred inside the frame.</param>
	public HarnessFrameException(string message, Exception innerException)
		: base(message, innerException) => FrameNumber = -1;

	/// <summary>Initializes a new instance of the <see cref="HarnessFrameException"/> class.</summary>
	/// <param name="frameNumber">The zero-based frame that failed.</param>
	/// <param name="innerException">The failure that occurred inside the frame.</param>
	public HarnessFrameException(int frameNumber, Exception innerException)
		: base($"The application threw during harness frame {frameNumber}.", innerException) =>
		FrameNumber = frameNumber;
}
```

- [x] **Step 5: Implement the harness**

Create `ImGui.App.Testing/ImGuiAppHarness.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing;

using System;

using ktsu.ImGui.App;

/// <summary>
/// Runs an ImGui application with no window, no display and no GPU, advancing frames under the
/// caller's control. Input is injected straight into ImGui, so nothing reaches the operating
/// system and no other application on the machine is disturbed.
/// </summary>
public sealed class ImGuiAppHarness : IDisposable
{
	// ImGui contexts are process-global. Two live harnesses would corrupt each other's state in
	// ways that surface as unrelated test failures, so the second one is refused outright.
	private static ImGuiAppHarness? live;

	private readonly ImGuiAppConfig config;
	private readonly SoftwareRendererBackend backend;
	private readonly HeadlessImGuiContext context;
	private bool disposed;

	/// <summary>Gets the options this harness was started with.</summary>
	public HarnessOptions Options { get; }

	/// <summary>Gets the number of frames advanced so far.</summary>
	public int FrameCount { get; private set; }

	/// <summary>Gets the render target holding the most recently rendered frame.</summary>
	public Bitmap32 Target => backend.Target;

	private ImGuiAppHarness(ImGuiAppConfig config, HarnessOptions options)
	{
		this.config = config;
		Options = options;
		backend = new SoftwareRendererBackend(options.Width, options.Height);
		context = new HeadlessImGuiContext(options.Width, options.Height, options.DpiScale, backend);
	}

	/// <summary>
	/// Starts a harness around an application configuration. Pass the same configuration the
	/// application gives <see cref="ImGuiApp.Start"/>, so the test exercises the real setup.
	/// </summary>
	/// <param name="config">The application configuration.</param>
	/// <param name="options">Determinism settings.</param>
	/// <returns>A live harness. Dispose it to release the ImGui context.</returns>
	public static ImGuiAppHarness Start(ImGuiAppConfig config, HarnessOptions options)
	{
		ArgumentNullException.ThrowIfNull(config);
		ArgumentNullException.ThrowIfNull(options);

		if (live is not null)
		{
			throw new InvalidOperationException(
				"An ImGuiAppHarness is already running in this process. ImGui contexts are global, so harnesses cannot overlap. Dispose the previous one first.");
		}

		ImGuiAppHarness harness = new(config, options);
		live = harness;

		config.OnStart?.Invoke();

		return harness;
	}

	/// <summary>Advances exactly one frame.</summary>
	public void Step() => Step(1);

	/// <summary>Advances a number of frames.</summary>
	/// <param name="frames">How many frames to advance. Must be positive.</param>
	public void Step(int frames)
	{
		ObjectDisposedException.ThrowIf(disposed, this);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(frames);

		for (int i = 0; i < frames; i++)
		{
			int frameNumber = FrameCount;

			try
			{
				context.BeginFrame(Options.FrameDelta);

				// The application's update callback and anything marshaled onto the UI thread
				// must run, or work queued from a worker never lands. ImageGui uploads its
				// preview texture through the invoker, so skipping this would leave every
				// asynchronous result invisible to the test.
				config.OnUpdate?.Invoke(Options.FrameDelta);
				ImGuiApp.Invoker.DoInvokes();

				backend.Clear(Options.ClearColor);
				ImGuiApp.RenderFrameContents(config, Options.FrameDelta);

				context.EndFrame();
			}
			catch (Exception error) when (error is not HarnessFrameException)
			{
				throw new HarnessFrameException(frameNumber, error);
			}

			FrameCount++;
		}
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		if (disposed)
		{
			return;
		}

		context.Dispose();
		backend.Dispose();

		if (ReferenceEquals(live, this))
		{
			live = null;
		}

		disposed = true;
	}
}
```

- [x] **Step 6: Run the tests to verify they pass**

Run:

```bash
dotnet build tests/ImGui.App.Testing.Tests/ImGui.App.Testing.Tests.csproj
./tests/ImGui.App.Testing.Tests/bin/Debug/net10.0/ktsu.ImGui.App.Testing.Tests.exe
```

Expected: all `ImGuiAppHarnessTests` pass.

`RenderFrameContents` calls `RenderWithScaling`, which calls `FindBestFontForAppearance`. That
reads `ImGuiApp.FontIndices`, a static dictionary the harness never populates, so it is worth
knowing in advance what happens. It was checked while writing this plan: with an empty
`FontIndices` every lookup misses and the method falls through to its final fallback,
`fontIndex = 0`, returning `fonts[0]`. A freshly created ImGui context always carries a default
font at index zero, so the call should succeed and the harness renders with the default font.

If it does fail anyway, populate what the harness needs in its constructor rather than bypassing
the shared method. Bypassing it would defeat the point of Task 7, which exists so the harness and
the window drive identical application code.

- [x] **Step 7: Commit**

```bash
git add ImGui.App.Testing tests/ImGui.App.Testing.Tests/ImGuiAppHarnessTests.cs
git commit -m "feat: add the headless harness entry point and frame stepping [minor]"
```

---

### Task 10: Conditional Stepping

**Files:**
- Modify: `ImGui.App.Testing/ImGuiAppHarness.cs`
- Create: `tests/ImGui.App.Testing.Tests/StepUntilTests.cs`

**Interfaces:**
- Consumes: `ImGuiAppHarness.Step` from Task 9.
- Produces: `bool StepUntil(Func<bool> predicate, int maxFrames)` on `ImGuiAppHarness`.

- [x] **Step 1: Write the failing test**

Create `tests/ImGui.App.Testing.Tests/StepUntilTests.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing.Tests;

using ktsu.ImGui.App;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class StepUntilTests
{
	private static HarnessOptions SmallWindow() => new() { Width = 64, Height = 64 };

	[TestMethod]
	public void StepUntil_PredicateBecomesTrue_ReturnsTrueAndStops()
	{
		int frames = 0;
		ImGuiAppConfig config = new() { OnRender = _ => frames++ };
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(config, SmallWindow());

		bool reached = harness.StepUntil(() => frames >= 3, maxFrames: 100);

		Assert.IsTrue(reached, "The predicate became true, so StepUntil should report success.");
		Assert.AreEqual(3, frames, "It should stop as soon as the predicate holds, not run the whole budget.");
	}

	[TestMethod]
	public void StepUntil_PredicateNeverTrue_ReturnsFalseAtTheBudget()
	{
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(new ImGuiAppConfig(), SmallWindow());

		bool reached = harness.StepUntil(() => false, maxFrames: 5);

		Assert.IsFalse(reached, "An exhausted budget reports false rather than throwing.");
		Assert.AreEqual(5, harness.FrameCount, "It should spend exactly the budget.");
	}

	[TestMethod]
	public void StepUntil_PredicateAlreadyTrue_DoesNotStep()
	{
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(new ImGuiAppConfig(), SmallWindow());

		bool reached = harness.StepUntil(() => true, maxFrames: 10);

		Assert.IsTrue(reached);
		Assert.AreEqual(0, harness.FrameCount, "An already-satisfied predicate should advance nothing.");
	}
}
```

- [x] **Step 2: Run the test to verify it fails**

Run: `dotnet build tests/ImGui.App.Testing.Tests/ImGui.App.Testing.Tests.csproj`
Expected: compile error, `StepUntil` does not exist.

- [x] **Step 3: Implement conditional stepping**

Add to `ImGuiAppHarness`, after `Step(int frames)`:

```csharp
	/// <summary>
	/// Advances frames until a condition holds or a frame budget runs out. The budget counts
	/// frames rather than milliseconds, so a loaded machine takes longer in real time without
	/// changing the outcome.
	/// </summary>
	/// <param name="predicate">Checked before the first frame and after every frame.</param>
	/// <param name="maxFrames">The most frames to advance. Must be positive.</param>
	/// <returns><see langword="true"/> if the condition held, <see langword="false"/> if the budget ran out.</returns>
	public bool StepUntil(Func<bool> predicate, int maxFrames)
	{
		ObjectDisposedException.ThrowIf(disposed, this);
		ArgumentNullException.ThrowIfNull(predicate);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxFrames);

		if (predicate())
		{
			return true;
		}

		for (int i = 0; i < maxFrames; i++)
		{
			Step();

			if (predicate())
			{
				return true;
			}
		}

		return false;
	}
```

- [x] **Step 4: Run the tests to verify they pass**

Run:

```bash
dotnet build tests/ImGui.App.Testing.Tests/ImGui.App.Testing.Tests.csproj
./tests/ImGui.App.Testing.Tests/bin/Debug/net10.0/ktsu.ImGui.App.Testing.Tests.exe
```

Expected: all three `StepUntilTests` pass.

- [x] **Step 5: Commit**

```bash
git add ImGui.App.Testing/ImGuiAppHarness.cs tests/ImGui.App.Testing.Tests/StepUntilTests.cs
git commit -m "feat: advance frames until a condition holds [minor]"
```

---

### Task 11: Mouse Input

**Files:**
- Create: `ImGui.App.Testing/HarnessMouse.cs`
- Modify: `ImGui.App.Testing/ImGuiAppHarness.cs`
- Create: `tests/ImGui.App.Testing.Tests/MouseInputTests.cs`

**Interfaces:**
- Consumes: `ImGuiAppHarness.Step` from Task 9.
- Produces: `HarnessMouse Mouse { get; }` on `ImGuiAppHarness`, and `public sealed class HarnessMouse` with `void MoveTo(float x, float y)`, `void Down(int button)`, `void Up(int button)`, `void Click(float x, float y, int button = 0)`, `void Drag(float fromX, float fromY, float toX, float toY, int steps = 16)`, `void Wheel(float x, float y, int clicks)`.

- [x] **Step 1: Write the failing test**

Create `tests/ImGui.App.Testing.Tests/MouseInputTests.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing.Tests;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class MouseInputTests
{
	private static HarnessOptions Window() => new() { Width = 300, Height = 200 };

	[TestMethod]
	public void MoveTo_IsVisibleToTheApplication()
	{
		Vector2 seen = Vector2.Zero;
		ImGuiAppConfig config = new() { OnRender = _ => seen = ImGui.GetIO().MousePos };

		using ImGuiAppHarness harness = ImGuiAppHarness.Start(config, Window());
		harness.Mouse.MoveTo(42, 24);
		harness.Step();

		Assert.AreEqual(42f, seen.X, "ImGui should report the injected mouse position.");
		Assert.AreEqual(24f, seen.Y);
	}

	[TestMethod]
	public void Click_ActivatesAButton()
	{
		bool pressed = false;
		ImGuiAppConfig config = new()
		{
			OnRender = _ =>
			{
				ImGui.SetNextWindowPos(Vector2.Zero);
				ImGui.SetNextWindowSize(new Vector2(200, 100));
				ImGui.Begin("probe", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoSavedSettings);
				if (ImGui.Button("press me", new Vector2(120, 30)))
				{
					pressed = true;
				}

				ImGui.End();
			},
		};

		using ImGuiAppHarness harness = ImGuiAppHarness.Start(config, Window());

		// One frame first so the button exists and ImGui knows its rectangle.
		harness.Step();
		harness.Mouse.Click(60, 25);

		Assert.IsTrue(pressed, "A click inside the button rectangle should activate it.");
	}

	[TestMethod]
	public void Wheel_IsVisibleToTheApplication()
	{
		float wheel = 0;
		ImGuiAppConfig config = new() { OnRender = _ => wheel = ImGui.GetIO().MouseWheel };

		using ImGuiAppHarness harness = ImGuiAppHarness.Start(config, Window());
		harness.Mouse.Wheel(10, 10, clicks: 3);

		Assert.AreEqual(3f, wheel, "Three wheel clicks should reach ImGui as a wheel delta of three.");
	}

	[TestMethod]
	public void Drag_MovesThroughIntermediatePositions()
	{
		List<Vector2> positions = [];
		ImGuiAppConfig config = new() { OnRender = _ => positions.Add(ImGui.GetIO().MousePos) };

		using ImGuiAppHarness harness = ImGuiAppHarness.Start(config, Window());
		harness.Mouse.Drag(10, 10, 90, 10, steps: 8);

		Assert.IsTrue(positions.Count >= 8, "A drag should render several intermediate frames.");
		Assert.AreEqual(90f, positions[^1].X, "A drag should finish at its destination.");
	}
}
```

- [x] **Step 2: Run the test to verify it fails**

Run: `dotnet build tests/ImGui.App.Testing.Tests/ImGui.App.Testing.Tests.csproj`
Expected: compile error, `Mouse` does not exist on the harness.

- [x] **Step 3: Implement the mouse**

Create `ImGui.App.Testing/HarnessMouse.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing;

using System;

using Hexa.NET.ImGui;

/// <summary>
/// Injects mouse events straight into ImGui's event queue, the same way the iOS platform port
/// does. Nothing is sent to the operating system, so no window needs focus.
/// </summary>
/// <param name="harness">The harness whose frames these events feed.</param>
public sealed class HarnessMouse(ImGuiAppHarness harness)
{
	private float x;
	private float y;

	/// <summary>Moves the pointer without advancing a frame.</summary>
	/// <param name="positionX">Target column in display pixels.</param>
	/// <param name="positionY">Target row in display pixels.</param>
	public void MoveTo(float positionX, float positionY)
	{
		x = positionX;
		y = positionY;
		ImGui.GetIO().AddMousePosEvent(positionX, positionY);
	}

	/// <summary>Presses a button without advancing a frame.</summary>
	/// <param name="button">Zero for left, one for right, two for middle.</param>
	public void Down(int button) => ImGui.GetIO().AddMouseButtonEvent(button, true);

	/// <summary>Releases a button without advancing a frame.</summary>
	/// <param name="button">Zero for left, one for right, two for middle.</param>
	public void Up(int button) => ImGui.GetIO().AddMouseButtonEvent(button, false);

	/// <summary>
	/// Clicks at a position, advancing the frames the interaction needs. ImGui activates a button
	/// on release, so a press and release inside one frame would do nothing.
	/// </summary>
	/// <param name="positionX">Column in display pixels.</param>
	/// <param name="positionY">Row in display pixels.</param>
	/// <param name="button">Zero for left, one for right, two for middle.</param>
	public void Click(float positionX, float positionY, int button = 0)
	{
		ArgumentNullException.ThrowIfNull(harness);

		MoveTo(positionX, positionY);
		harness.Step();

		Down(button);
		harness.Step();

		Up(button);
		harness.Step();
	}

	/// <summary>Presses at one position, moves in steps, and releases at another.</summary>
	/// <param name="fromX">Start column.</param>
	/// <param name="fromY">Start row.</param>
	/// <param name="toX">End column.</param>
	/// <param name="toY">End row.</param>
	/// <param name="steps">How many intermediate positions to visit. Must be positive.</param>
	/// <param name="button">Zero for left, one for right, two for middle.</param>
	public void Drag(float fromX, float fromY, float toX, float toY, int steps = 16, int button = 0)
	{
		ArgumentNullException.ThrowIfNull(harness);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(steps);

		MoveTo(fromX, fromY);
		harness.Step();

		Down(button);
		harness.Step();

		for (int i = 1; i <= steps; i++)
		{
			float t = (float)i / steps;
			MoveTo(fromX + ((toX - fromX) * t), fromY + ((toY - fromY) * t));
			harness.Step();
		}

		Up(button);
		harness.Step();
	}

	/// <summary>Scrolls the wheel at a position and advances one frame.</summary>
	/// <param name="positionX">Column in display pixels.</param>
	/// <param name="positionY">Row in display pixels.</param>
	/// <param name="clicks">Wheel detents. Positive scrolls up.</param>
	public void Wheel(float positionX, float positionY, int clicks)
	{
		ArgumentNullException.ThrowIfNull(harness);

		MoveTo(positionX, positionY);
		ImGui.GetIO().AddMouseWheelEvent(0f, clicks);
		harness.Step();
	}

	/// <summary>Gets the last position the pointer was moved to.</summary>
	public (float X, float Y) Position => (x, y);
}
```

- [x] **Step 4: Expose the mouse on the harness**

Add to `ImGuiAppHarness`, as a property initialized in the constructor:

```csharp
	/// <summary>Gets the mouse input injector for this harness.</summary>
	public HarnessMouse Mouse { get; }
```

and in the constructor body, after the context is created:

```csharp
		Mouse = new HarnessMouse(this);
```

- [x] **Step 5: Run the tests to verify they pass**

Run:

```bash
dotnet build tests/ImGui.App.Testing.Tests/ImGui.App.Testing.Tests.csproj
./tests/ImGui.App.Testing.Tests/bin/Debug/net10.0/ktsu.ImGui.App.Testing.Tests.exe
```

Expected: all four `MouseInputTests` pass. If `Click` does not activate the button, add one more frame between press and release before changing anything else, because ImGui needs the press to be visible in a completed frame.

- [x] **Step 6: Commit**

```bash
git add ImGui.App.Testing tests/ImGui.App.Testing.Tests/MouseInputTests.cs
git commit -m "feat: inject mouse input into the harness [minor]"
```

---

### Task 12: Keyboard Input

**Files:**
- Create: `ImGui.App.Testing/HarnessKeyboard.cs`
- Modify: `ImGui.App.Testing/ImGuiAppHarness.cs`
- Create: `tests/ImGui.App.Testing.Tests/KeyboardInputTests.cs`

**Interfaces:**
- Consumes: `ImGuiAppHarness.Step` from Task 9.
- Produces: `HarnessKeyboard Keyboard { get; }` on `ImGuiAppHarness`, and `public sealed class HarnessKeyboard` with `void Press(ImGuiKey key, bool ctrl = false, bool shift = false, bool alt = false)`, `void Type(string text)`, `void KeyDown(ImGuiKey key)`, `void KeyUp(ImGuiKey key)`.

- [x] **Step 1: Write the failing test**

Create `tests/ImGui.App.Testing.Tests/KeyboardInputTests.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing.Tests;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class KeyboardInputTests
{
	private static HarnessOptions Window() => new() { Width = 200, Height = 120 };

	[TestMethod]
	public void Press_IsVisibleToTheApplication()
	{
		bool sawKey = false;
		ImGuiAppConfig config = new()
		{
			OnRender = _ =>
			{
				if (ImGui.IsKeyPressed(ImGuiKey.Z))
				{
					sawKey = true;
				}
			},
		};

		using ImGuiAppHarness harness = ImGuiAppHarness.Start(config, Window());
		harness.Keyboard.Press(ImGuiKey.Z);

		Assert.IsTrue(sawKey, "The application should observe the injected key press.");
	}

	[TestMethod]
	public void Press_WithCtrl_ReportsTheModifier()
	{
		bool sawCtrlZ = false;
		ImGuiAppConfig config = new()
		{
			OnRender = _ =>
			{
				if (ImGui.GetIO().KeyCtrl && ImGui.IsKeyPressed(ImGuiKey.Z))
				{
					sawCtrlZ = true;
				}
			},
		};

		using ImGuiAppHarness harness = ImGuiAppHarness.Start(config, Window());
		harness.Keyboard.Press(ImGuiKey.Z, ctrl: true);

		Assert.IsTrue(sawCtrlZ, "Ctrl+Z should arrive with the modifier set.");
	}

	[TestMethod]
	public void Type_DeliversEveryCharacter()
	{
		string typed = string.Empty;
		ImGuiAppConfig config = new()
		{
			OnRender = _ =>
			{
				ImGuiIOPtr io = ImGui.GetIO();
				for (int i = 0; i < io.InputQueueCharacters.Size; i++)
				{
					typed += (char)io.InputQueueCharacters[i];
				}
			},
		};

		using ImGuiAppHarness harness = ImGuiAppHarness.Start(config, Window());
		harness.Keyboard.Type("ab.png");

		Assert.AreEqual("ab.png", typed, "Every character including the period should arrive in order.");
	}
}
```

- [x] **Step 2: Run the test to verify it fails**

Run: `dotnet build tests/ImGui.App.Testing.Tests/ImGui.App.Testing.Tests.csproj`
Expected: compile error, `Keyboard` does not exist on the harness.

- [x] **Step 3: Implement the keyboard**

Create `ImGui.App.Testing/HarnessKeyboard.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing;

using System;

using Hexa.NET.ImGui;

/// <summary>
/// Injects keyboard events straight into ImGui's event queue. Text entry goes through the
/// character queue, which is what ImGui text widgets read, while shortcuts go through key events.
/// </summary>
/// <param name="harness">The harness whose frames these events feed.</param>
public sealed class HarnessKeyboard(ImGuiAppHarness harness)
{
	/// <summary>Presses a key down without advancing a frame.</summary>
	/// <param name="key">The key to press.</param>
	public void KeyDown(ImGuiKey key) => ImGui.GetIO().AddKeyEvent(key, true);

	/// <summary>Releases a key without advancing a frame.</summary>
	/// <param name="key">The key to release.</param>
	public void KeyUp(ImGuiKey key) => ImGui.GetIO().AddKeyEvent(key, false);

	/// <summary>
	/// Presses and releases a key with optional modifiers, advancing the frames needed for the
	/// application to observe the press.
	/// </summary>
	/// <param name="key">The key to press.</param>
	/// <param name="ctrl">Whether control is held.</param>
	/// <param name="shift">Whether shift is held.</param>
	/// <param name="alt">Whether alt is held.</param>
	public void Press(ImGuiKey key, bool ctrl = false, bool shift = false, bool alt = false)
	{
		ArgumentNullException.ThrowIfNull(harness);

		ImGuiIOPtr io = ImGui.GetIO();

		if (ctrl)
		{
			io.AddKeyEvent(ImGuiKey.ModCtrl, true);
		}

		if (shift)
		{
			io.AddKeyEvent(ImGuiKey.ModShift, true);
		}

		if (alt)
		{
			io.AddKeyEvent(ImGuiKey.ModAlt, true);
		}

		io.AddKeyEvent(key, true);
		harness.Step();

		io.AddKeyEvent(key, false);

		if (ctrl)
		{
			io.AddKeyEvent(ImGuiKey.ModCtrl, false);
		}

		if (shift)
		{
			io.AddKeyEvent(ImGuiKey.ModShift, false);
		}

		if (alt)
		{
			io.AddKeyEvent(ImGuiKey.ModAlt, false);
		}

		harness.Step();
	}

	/// <summary>
	/// Types text one character per frame, through the same character queue a real keyboard feeds.
	/// </summary>
	/// <param name="text">The text to type.</param>
	public void Type(string text)
	{
		ArgumentNullException.ThrowIfNull(harness);
		ArgumentNullException.ThrowIfNull(text);

		foreach (char character in text)
		{
			ImGui.GetIO().AddInputCharacter(character);
			harness.Step();
		}
	}
}
```

- [x] **Step 4: Expose the keyboard on the harness**

Add to `ImGuiAppHarness`:

```csharp
	/// <summary>Gets the keyboard input injector for this harness.</summary>
	public HarnessKeyboard Keyboard { get; }
```

and in the constructor, next to the mouse:

```csharp
		Keyboard = new HarnessKeyboard(this);
```

- [x] **Step 5: Run the tests to verify they pass**

Run:

```bash
dotnet build tests/ImGui.App.Testing.Tests/ImGui.App.Testing.Tests.csproj
./tests/ImGui.App.Testing.Tests/bin/Debug/net10.0/ktsu.ImGui.App.Testing.Tests.exe
```

Expected: all three `KeyboardInputTests` pass.

- [x] **Step 6: Commit**

```bash
git add ImGui.App.Testing tests/ImGui.App.Testing.Tests/KeyboardInputTests.cs
git commit -m "feat: inject keyboard input and typed text into the harness [minor]"
```

---

### Task 13: Frame Capture and Measurement

**Files:**
- Create: `ImGui.App.Testing/CapturedFrame.cs`
- Modify: `ImGui.App.Testing/ImGuiAppHarness.cs`
- Create: `tests/ImGui.App.Testing.Tests/CapturedFrameTests.cs`

**Interfaces:**
- Consumes: `Bitmap32`, `Rgba32` from Task 2, `ImGuiAppHarness.Target` from Task 9.
- Produces: `CapturedFrame Capture()` on `ImGuiAppHarness`, and `public sealed class CapturedFrame` with `int Width`, `int Height`, `Rgba32 GetPixel(int x, int y)`, `void SavePng(string path)`, `Rectangle? FindBounds(Func<Rgba32, bool> predicate)`, `int CountPixels(Func<Rgba32, bool> predicate)`.

- [x] **Step 1: Write the failing test**

Create `tests/ImGui.App.Testing.Tests/CapturedFrameTests.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing.Tests;

using System.IO;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class CapturedFrameTests
{
	private static CapturedFrame FrameWithRedBox()
	{
		Bitmap32 bitmap = new(16, 16);
		bitmap.Clear(new Rgba32(0, 0, 0, 255));
		for (int y = 4; y < 9; y++)
		{
			for (int x = 3; x < 7; x++)
			{
				bitmap.SetPixel(x, y, new Rgba32(255, 0, 0, 255));
			}
		}

		return new CapturedFrame(bitmap);
	}

	[TestMethod]
	public void FindBounds_ReturnsTheTightBoxAroundMatchingPixels()
	{
		Rectangle? bounds = FrameWithRedBox().FindBounds(p => p.R > 200 && p.G < 50);

		Assert.IsNotNull(bounds);
		Assert.AreEqual(3, bounds.Value.MinX);
		Assert.AreEqual(4, bounds.Value.MinY);
		Assert.AreEqual(7, bounds.Value.MaxX, "MaxX is exclusive.");
		Assert.AreEqual(9, bounds.Value.MaxY, "MaxY is exclusive.");
	}

	[TestMethod]
	public void FindBounds_NoMatch_ReturnsNull() =>
		Assert.IsNull(FrameWithRedBox().FindBounds(p => p.G > 200));

	[TestMethod]
	public void CountPixels_CountsEveryMatch() =>
		Assert.AreEqual(20, FrameWithRedBox().CountPixels(p => p.R > 200 && p.G < 50));

	[TestMethod]
	public void SavePng_WritesAFileThatIsNotEmpty()
	{
		string path = Path.Combine(Path.GetTempPath(), $"harness-capture-{Guid.NewGuid():N}.png");

		try
		{
			FrameWithRedBox().SavePng(path);

			Assert.IsTrue(File.Exists(path), "The capture should be written to disk.");
			Assert.IsTrue(new FileInfo(path).Length > 0, "The written file should not be empty.");
		}
		finally
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
	}

	[TestMethod]
	public void Capture_TakesASnapshotThatLaterFramesDoNotChange()
	{
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(
			new ktsu.ImGui.App.ImGuiAppConfig(),
			new HarnessOptions { Width = 32, Height = 32, ClearColor = new Rgba32(1, 1, 1, 255) });

		harness.Step();
		CapturedFrame first = harness.Capture();

		// Change the clear color by starting a fresh harness would be heavier; instead paint
		// directly into the live target and confirm the earlier capture is unaffected.
		harness.Target.Clear(new Rgba32(9, 9, 9, 255));

		Assert.AreEqual(new Rgba32(1, 1, 1, 255), first.GetPixel(0, 0), "A capture must be a copy, not a view.");
	}
}
```

- [x] **Step 2: Run the test to verify it fails**

Run: `dotnet build tests/ImGui.App.Testing.Tests/ImGui.App.Testing.Tests.csproj`
Expected: compile error, `CapturedFrame` does not exist.

- [x] **Step 3: Implement the captured frame**

Create `ImGui.App.Testing/CapturedFrame.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing;

using System;

/// <summary>
/// An immutable snapshot of one rendered frame. Measurements taken here go into assertions, and
/// the whole frame can be written to disk as a diagnostic artifact when a test fails.
/// </summary>
public sealed class CapturedFrame
{
	private readonly Bitmap32 pixels;

	/// <summary>Initializes a snapshot by copying the supplied bitmap.</summary>
	/// <param name="source">The bitmap to copy.</param>
	public CapturedFrame(Bitmap32 source)
	{
		ArgumentNullException.ThrowIfNull(source);

		pixels = new Bitmap32(source.Width, source.Height);
		source.Pixels.CopyTo(pixels.Pixels);
	}

	/// <summary>Gets the frame width in pixels.</summary>
	public int Width => pixels.Width;

	/// <summary>Gets the frame height in pixels.</summary>
	public int Height => pixels.Height;

	/// <summary>Reads one pixel.</summary>
	/// <param name="x">Column.</param>
	/// <param name="y">Row.</param>
	/// <returns>The color at that position.</returns>
	public Rgba32 GetPixel(int x, int y) => pixels.GetPixel(x, y);

	/// <summary>Writes the frame to disk as a PNG.</summary>
	/// <param name="path">Destination file path.</param>
	public void SavePng(string path) => pixels.SavePng(path);

	/// <summary>
	/// Finds the tight rectangle containing every pixel matching a predicate. Useful for measuring
	/// where something was drawn, which is how a test asserts on layout without a golden image.
	/// </summary>
	/// <param name="predicate">Chooses which pixels count.</param>
	/// <returns>The bounding rectangle, or null when nothing matched.</returns>
	public Rectangle? FindBounds(Func<Rgba32, bool> predicate)
	{
		ArgumentNullException.ThrowIfNull(predicate);

		int minX = int.MaxValue;
		int minY = int.MaxValue;
		int maxX = int.MinValue;
		int maxY = int.MinValue;

		for (int y = 0; y < Height; y++)
		{
			for (int x = 0; x < Width; x++)
			{
				if (!predicate(pixels.GetPixel(x, y)))
				{
					continue;
				}

				minX = Math.Min(minX, x);
				minY = Math.Min(minY, y);
				maxX = Math.Max(maxX, x);
				maxY = Math.Max(maxY, y);
			}
		}

		return minX == int.MaxValue ? null : new Rectangle(minX, minY, maxX + 1, maxY + 1);
	}

	/// <summary>Counts pixels matching a predicate.</summary>
	/// <param name="predicate">Chooses which pixels count.</param>
	/// <returns>How many pixels matched.</returns>
	public int CountPixels(Func<Rgba32, bool> predicate)
	{
		ArgumentNullException.ThrowIfNull(predicate);

		int count = 0;
		for (int y = 0; y < Height; y++)
		{
			for (int x = 0; x < Width; x++)
			{
				if (predicate(pixels.GetPixel(x, y)))
				{
					count++;
				}
			}
		}

		return count;
	}
}
```

- [x] **Step 4: Expose capture on the harness**

Add to `ImGuiAppHarness`:

```csharp
	/// <summary>
	/// Takes an immutable snapshot of the most recently rendered frame. The snapshot copies the
	/// pixels, so later frames do not alter it.
	/// </summary>
	/// <returns>A snapshot suitable for measuring and for writing to disk.</returns>
	public CapturedFrame Capture()
	{
		ObjectDisposedException.ThrowIf(disposed, this);

		if (FrameCount == 0)
		{
			throw new InvalidOperationException("No frame has been rendered yet, so there is nothing to capture. Call Step first.");
		}

		return new CapturedFrame(backend.Target);
	}
```

- [x] **Step 5: Run the tests to verify they pass**

Run:

```bash
dotnet build tests/ImGui.App.Testing.Tests/ImGui.App.Testing.Tests.csproj
./tests/ImGui.App.Testing.Tests/bin/Debug/net10.0/ktsu.ImGui.App.Testing.Tests.exe
```

Expected: all `CapturedFrameTests` pass.

- [x] **Step 6: Commit**

```bash
git add ImGui.App.Testing tests/ImGui.App.Testing.Tests/CapturedFrameTests.cs
git commit -m "feat: capture and measure rendered frames [minor]"
```

---

### Task 14: Determinism Verification

**Files:**
- Create: `tests/ImGui.App.Testing.Tests/DeterminismTests.cs`

**Interfaces:**
- Consumes: everything built so far.
- Produces: no new API. This task proves the central claim of the design, that two runs produce identical output.

- [x] **Step 1: Write the test**

Create `tests/ImGui.App.Testing.Tests/DeterminismTests.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing.Tests;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class DeterminismTests
{
	private static ImGuiAppConfig Scenario() => new()
	{
		OnRender = _ =>
		{
			ImGui.SetNextWindowPos(new Vector2(10, 10));
			ImGui.SetNextWindowSize(new Vector2(160, 90));
			ImGui.Begin("determinism", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoSavedSettings);
			ImGui.TextUnformatted("stable output");
			ImGui.Button("a button");
			ImGui.End();
		},
	};

	private static byte[] RunOnce()
	{
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(
			Scenario(),
			new HarnessOptions { Width = 200, Height = 120 });

		harness.Step(frames: 3);

		return harness.Target.Pixels.ToArray();
	}

	[TestMethod]
	public void TwoRunsOfTheSameScenario_ProduceIdenticalPixels()
	{
		byte[] first = RunOnce();
		byte[] second = RunOnce();

		CollectionAssert.AreEqual(first, second, "The software renderer must be deterministic, or every pixel assertion is unreliable.");
	}

	[TestMethod]
	public void RenderedFrame_IsNotBlank()
	{
		byte[] frame = RunOnce();

		// A determinism test comparing two blank frames would pass while proving nothing.
		bool anythingDrawn = false;
		for (int i = 0; i < frame.Length; i += 4)
		{
			if (frame[i] != 0 || frame[i + 1] != 0 || frame[i + 2] != 0)
			{
				anythingDrawn = true;
				break;
			}
		}

		Assert.IsTrue(anythingDrawn, "The scenario should actually draw something.");
	}

	[TestMethod]
	public void HarnessDoesNotWriteAnIniFile()
	{
		string ini = Path.Combine(Directory.GetCurrentDirectory(), "imgui.ini");
		bool existedBefore = File.Exists(ini);

		RunOnce();

		if (!existedBefore)
		{
			Assert.IsFalse(File.Exists(ini), "The harness must not persist layout, or tests inherit state from each other.");
		}
	}
}
```

- [x] **Step 2: Run the tests**

Run:

```bash
dotnet build tests/ImGui.App.Testing.Tests/ImGui.App.Testing.Tests.csproj
./tests/ImGui.App.Testing.Tests/bin/Debug/net10.0/ktsu.ImGui.App.Testing.Tests.exe
```

Expected: all three pass. If the determinism test fails, the likely causes in order are: a hash-ordered iteration in the backend's texture dictionary affecting draw order, font atlas rebuilding differently on the second context, or uninitialized memory in the render target. Fix the cause rather than loosening the comparison.

- [x] **Step 3: Run the whole suite**

Run:

```bash
dotnet build ImGui.sln
./tests/ImGui.App.Testing.Tests/bin/Debug/net10.0/ktsu.ImGui.App.Testing.Tests.exe
./tests/ImGui.App.Tests/bin/Debug/net10.0/ktsu.ImGui.App.Tests.exe
./tests/ImGui.Popups.Tests/bin/Debug/net10.0/ktsu.ImGui.Popups.Tests.exe
./tests/ImGui.Widgets.Tests/bin/Debug/net10.0/ktsu.ImGui.Widgets.Tests.exe
```

Expected: the whole solution builds with zero warnings and every suite passes. The Task 7 extraction touched the shared render path, so a regression in the other suites matters here.

- [x] **Step 4: Commit**

```bash
git add tests/ImGui.App.Testing.Tests/DeterminismTests.cs
git commit -m "test: prove the harness renders deterministically [patch]"
```

---

### Task 15: Item Probes

> **2026-08-19 result: built, with three deviations from the steps below.** The steps are left as
> written so the reasoning is still legible, but the code differs.
>
> 1. **The registry is not in `ktsu.ImGui.App`.** Nothing depends on that package, so `ImGui.Widgets`
>    and `ImGui.Popups` could not reach it without taking a dependency that drags windowing and
>    OpenGL into every consumer of a button. It lives in a new dependency-free `ktsu.ImGui.Probes`
>    package instead, as `ImGuiProbes`, with applications calling `ImGuiProbes.MarkItem` directly. `ImGuiProbes` also
>    carries an `Enabled` master switch.
> 2. **The libraries mark their own items.** Popups marks filesystem browser rows by filename, its
>    drives, its filename field and its buttons, plus the prompt, input and searchable list controls.
>    Twelve widgets mark the region they claim for interaction. An application therefore gets
>    addressable widgets without marking anything.
> 3. **Names are qualified and resolved by suffix.** A bare label collided across windows, which the
>    steps below did not account for. Names now record as window, then pushed scopes, then the item
>    name, with `ScopedId` pushing a probe scope alongside its identifier. Lookups match trailing
>    segments so tests still write short names. Ambiguity covers both a query matching several names
>    and one name marked twice in a frame.

Removes coordinates from tests. An application marks the items a test should be able to address, and
the harness resolves a name to the rectangle ImGui reported for that item. See Item Probes in the
spec for why Dear ImGui's test engine was rejected in favor of this.

**Files:**
- Modify: `ImGui.App/ImGuiApp.cs`
- Create: `ImGui.App.Testing/ItemProbe.cs`
- Modify: `ImGui.App.Testing/ImGuiAppHarness.cs`
- Create: `tests/ImGui.App.Testing.Tests/ItemProbeTests.cs`

**Interfaces:**
- Consumes: `ImGuiAppHarness.Step`, `HarnessMouse.Click` from Tasks 9 and 11.
- Produces: `ImGuiProbes.MarkItem(string name)` and `ImGuiProbes.SetProbe(Action<string, Vector2, Vector2>?)` in `ktsu.ImGui.Probes`. In the harness: `ItemProbe Probe { get; }` with `Rectangle? Rect(string name)`, `bool WasSeenInFrame(string name, int frame)`, `IReadOnlyCollection<string> KnownNames`, plus `ImGuiAppHarness.Click(string name)`.

- [x] **Step 1: Write the failing test**

Create `tests/ImGui.App.Testing.Tests/ItemProbeTests.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing.Tests;

using System;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class ItemProbeTests
{
	private static HarnessOptions Window() => new() { Width = 300, Height = 200 };

	private static ImGuiAppConfig ConfigWithButton(Action onPressed) => new()
	{
		OnRender = _ =>
		{
			ImGui.SetNextWindowPos(Vector2.Zero);
			ImGui.SetNextWindowSize(new Vector2(240, 150));
			ImGui.Begin("probe", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoSavedSettings);

			if (ImGui.Button("press me", new Vector2(120, 30)))
			{
				onPressed();
			}

			ImGuiProbes.MarkItem("the.button");

			ImGui.End();
		},
	};

	[TestMethod]
	public void Rect_AfterMarking_ReportsTheItemRectangle()
	{
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(ConfigWithButton(() => { }), Window());
		harness.Step();

		Rectangle? rect = harness.Probe.Rect("the.button");

		Assert.IsNotNull(rect, "A marked item should be resolvable by name.");
		Assert.IsTrue(rect.Value.Width is >= 118 and <= 122, $"The button was 120 wide, but the probe reported {rect.Value.Width}.");
		Assert.IsTrue(rect.Value.Height is >= 28 and <= 32, $"The button was 30 tall, but the probe reported {rect.Value.Height}.");
	}

	[TestMethod]
	public void Click_ByName_ActivatesTheItem()
	{
		bool pressed = false;
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(ConfigWithButton(() => pressed = true), Window());
		harness.Step();

		harness.Click("the.button");

		Assert.IsTrue(pressed, "Clicking by name should activate the item without the test naming a coordinate.");
	}

	[TestMethod]
	public void Click_UnknownName_ThrowsListingKnownNames()
	{
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(ConfigWithButton(() => { }), Window());
		harness.Step();

		ArgumentException error = Assert.ThrowsExactly<ArgumentException>(() => harness.Click("no.such.item"));

		Assert.IsTrue(
			error.Message.Contains("the.button", StringComparison.Ordinal),
			"The failure should list what was seen, since a typo is the usual cause.");
	}

	[TestMethod]
	public void Click_ItemNotDrawnThisFrame_ThrowsRatherThanClickingStalePosition()
	{
		bool visible = true;
		bool pressed = false;
		ImGuiAppConfig config = new()
		{
			OnRender = _ =>
			{
				ImGui.SetNextWindowPos(Vector2.Zero);
				ImGui.SetNextWindowSize(new Vector2(240, 150));
				ImGui.Begin("probe", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoSavedSettings);

				if (visible)
				{
					if (ImGui.Button("press me", new Vector2(120, 30)))
					{
						pressed = true;
					}

					ImGuiProbes.MarkItem("the.button");
				}

				ImGui.End();
			},
		};

		using ImGuiAppHarness harness = ImGuiAppHarness.Start(config, Window());
		harness.Step();

		visible = false;
		harness.Step();

		// Clicking a stale rectangle would hit whatever has since moved there, and could pass while
		// testing nothing at all, which is worse than failing.
		Assert.ThrowsExactly<InvalidOperationException>(() => harness.Click("the.button"));
		Assert.IsFalse(pressed);
	}

	[TestMethod]
	public void MarkItem_WithNoProbeInstalled_DoesNothing()
	{
		// Production applications call MarkItem with no harness present. It must be inert, not throw.
		ImGuiApp.SetItemProbe(null);

		ImGuiProbes.MarkItem("ignored");
	}
}
```

- [x] **Step 2: Run the test to verify it fails**

Run: `dotnet build tests/ImGui.App.Testing.Tests/ImGui.App.Testing.Tests.csproj`
Expected: compile error, `MarkItem`, `SetItemProbe`, `Probe` and `Click(string)` do not exist.

- [x] **Step 3: Add the marking API to ktsu.ImGui.App**

Add to `ImGuiApp`, next to the external frame session members:

```csharp
	private static Action<string, Vector2, Vector2>? itemProbe;

	/// <summary>
	/// Installs a callback receiving the name and rectangle of each item passed to
	/// <see cref="MarkItem"/>, or clears it when null.
	/// </summary>
	/// <remarks>
	/// Intended for test hosts. Applications call <see cref="MarkItem"/> unconditionally and pay one
	/// null check when no probe is installed, so being testable costs an application nothing in
	/// production and needs no dependency on test infrastructure.
	/// </remarks>
	/// <param name="probe">The callback, or null to stop recording.</param>
	public static void SetItemProbe(Action<string, Vector2, Vector2>? probe) => itemProbe = probe;

	/// <summary>
	/// Records the most recently submitted ImGui item under a stable name, so a test can address it
	/// without naming a coordinate. Call immediately after submitting the widget.
	/// </summary>
	/// <param name="name">A stable name for the item.</param>
	public static void MarkItem(string name)
	{
		if (itemProbe is null)
		{
			return;
		}

		itemProbe(name, ImGui.GetItemRectMin(), ImGui.GetItemRectMax());
	}
```

- [x] **Step 4: Implement the probe recorder**

Create `ImGui.App.Testing/ItemProbe.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing;

using System;
using System.Collections.Generic;
using System.Numerics;

/// <summary>
/// Records where named items were drawn, so tests address widgets by name rather than by position.
/// </summary>
public sealed class ItemProbe
{
	private readonly Dictionary<string, (Rectangle Rect, int Frame)> seen = [];

	/// <summary>Gets the names recorded so far, for diagnostics when a lookup fails.</summary>
	public IReadOnlyCollection<string> KnownNames => seen.Keys;

	/// <summary>Gets the most recent rectangle recorded for a name, or null when never seen.</summary>
	/// <param name="name">The item name.</param>
	/// <returns>The rectangle, or null.</returns>
	public Rectangle? Rect(string name) =>
		seen.TryGetValue(name, out (Rectangle Rect, int Frame) entry) ? entry.Rect : null;

	/// <summary>Gets a value indicating whether a name was marked during a given frame.</summary>
	/// <param name="name">The item name.</param>
	/// <param name="frame">The frame number to check.</param>
	/// <returns>True when the item was drawn in that frame.</returns>
	public bool WasSeenInFrame(string name, int frame) =>
		seen.TryGetValue(name, out (Rectangle Rect, int Frame) entry) && entry.Frame == frame;

	internal void Record(string name, Vector2 min, Vector2 max, int frame) =>
		seen[name] = (
			new Rectangle(
				(int)MathF.Round(min.X),
				(int)MathF.Round(min.Y),
				(int)MathF.Round(max.X),
				(int)MathF.Round(max.Y)),
			frame);
}
```

- [x] **Step 5: Wire the probe into the harness**

In `ImGuiAppHarness` add the property, install the recorder in `Start`, clear it in `Dispose`, and add
name-based clicking:

```csharp
	/// <summary>Gets the record of where named items were drawn.</summary>
	public ItemProbe Probe { get; } = new();

	/// <summary>
	/// Clicks a named item at the center of the rectangle ImGui reported for it, so the test states
	/// no coordinate of its own.
	/// </summary>
	/// <param name="name">A name the application passed to <see cref="ktsu.ImGui.Probes.ImGuiProbes.MarkItem(string)"/>.</param>
	/// <exception cref="ArgumentException">The name was never recorded.</exception>
	/// <exception cref="InvalidOperationException">The item was not drawn in the most recent frame.</exception>
	public void Click(string name)
	{
		ObjectDisposedException.ThrowIf(disposed, this);
		Ensure.NotNull(name);

		Rectangle rect = Probe.Rect(name)
			?? throw new ArgumentException(
				$"No item named '{name}' has been marked. Marked so far: {string.Join(", ", Probe.KnownNames)}.",
				nameof(name));

		if (!Probe.WasSeenInFrame(name, FrameCount - 1))
		{
			throw new InvalidOperationException(
				$"Item '{name}' was not drawn in the most recent frame, so its recorded position is stale. Clicking it would hit whatever has since moved there.");
		}

		Mouse.Click(rect.MinX + (rect.Width / 2f), rect.MinY + (rect.Height / 2f));
	}
```

In `Start`, after the harness is constructed and before `OnStart` runs:

```csharp
		ImGuiApp.SetItemProbe((name, min, max) => harness.Probe.Record(name, min, max, harness.FrameCount));
```

In `Dispose`, alongside `EndExternalFrameSession`:

```csharp
			ImGuiApp.SetItemProbe(null);
```

`ItemProbe.Record` is internal, so the harness records entries while callers cannot forge them.

- [x] **Step 6: Run the tests to verify they pass**

Run:

```bash
dotnet build tests/ImGui.App.Testing.Tests/ImGui.App.Testing.Tests.csproj
./tests/ImGui.App.Testing.Tests/bin/Debug/net10.0/ktsu.ImGui.App.Testing.Tests.exe
```

Expected: all five `ItemProbeTests` pass.

If the staleness test fails by one frame, check what `FrameCount` holds when the recorder runs. The
recorder fires during a step, before `FrameCount` is incremented, so an item drawn in the frame just
completed is recorded under `FrameCount - 1` once the step returns. Correct the arithmetic rather
than loosening the test: that test is the entire reason a stale click fails instead of silently
passing.

- [x] **Step 7: Confirm marking costs nothing in production**

An application that marks items with no probe installed must be unaffected:

```bash
dotnet build tests/ImGui.App.Tests/ImGui.App.Tests.csproj
./tests/ImGui.App.Tests/bin/Debug/net10.0/ktsu.ImGui.App.Tests.exe
```

Expected: all tests pass.

- [x] **Step 8: Commit**

```bash
git add ImGui.App/ImGuiApp.cs ImGui.App.Testing tests/ImGui.App.Testing.Tests/ItemProbeTests.cs
git commit -m "feat: address widgets by name through item probes [minor]"
```

---

### Task 16: Package Documentation

> **2026-08-19 result: done, covering two packages rather than one.** `ktsu.ImGui.Probes` needed its
> own readme and description once the registry moved there. Every code snippet in both readme files
> was compiled and run rather than eyeballed, since documentation that does not compile is worse than
> none because it is trusted.

**Files:**
- Create: `ImGui.App.Testing/README.md`
- Create: `ImGui.App.Testing/DESCRIPTION.md`

**Interfaces:**
- Consumes: the finished public API.
- Produces: package documentation matching the repository's existing convention.

- [x] **Step 1: Check the convention**

Run: `head -30 ImGui.Popups/DESCRIPTION.md ImGui.App/README.md`

Match whatever those files do for structure and tone.

- [x] **Step 2: Write the description**

Create `ImGui.App.Testing/DESCRIPTION.md` with a single paragraph, no heading, describing the package for the NuGet listing:

```markdown
Headless test harness for ktsu.ImGui.App applications. Renders through a CPU rasterizer with no display, GPU or graphics driver, injects input directly into ImGui rather than through the operating system, and advances frames under the test's control so results do not depend on timing. Captures rendered frames for measurement in assertions and as diagnostic artifacts.
```

- [x] **Step 3: Write the readme**

Create `ImGui.App.Testing/README.md` covering, in this order: what the package is for, a complete worked example starting a harness and asserting on a captured frame, the determinism guarantees, and the two known weaknesses from the spec (coordinate-based interaction, and not testing the GL backend). Use the worked example below verbatim as the centerpiece:

```csharp
using ImGuiAppHarness harness = ImGuiAppHarness.Start(app.BuildConfig(), new HarnessOptions
{
	Width = 1280,
	Height = 720,
});

harness.Mouse.Click(40, 17);
bool ready = harness.StepUntil(() => app.IsSettled, maxFrames: 300);
Assert.IsTrue(ready, "The application never settled.");

CapturedFrame frame = harness.Capture();
Rectangle? image = frame.FindBounds(p => p.A > 0);
Assert.IsNotNull(image, "Something should have been drawn.");
frame.SavePng("artifact.png");
```

- [x] **Step 4: Verify the documented example compiles**

Copy the example into a scratch test method, build it, and correct the readme if any name is wrong. Documentation that does not compile is worse than none, because it is trusted.

- [x] **Step 5: Commit**

```bash
git add ImGui.App.Testing/README.md ImGui.App.Testing/DESCRIPTION.md
git commit -m "docs: document the headless test harness package [patch]"
```

---

## Follow-Up Plan

ImageGui integration is deliberately a separate plan, because it cannot start until
`ktsu.ImGui.App.Testing` is released and ImageGui's package pins are raised. That plan covers
extracting `BuildConfig` from `ImageGuiApplication.Run`, adding `DocumentSession.IsSettled`,
creating `tests/ImageGui.App.UiTests`, the coordinate helper file, and the eight scenarios listed
under Initial Test Scenarios in the spec.

Note that ImageGui's CI currently has package publishing held, and that hold does not apply to
ImGuiApp, so releasing this package is not blocked.
