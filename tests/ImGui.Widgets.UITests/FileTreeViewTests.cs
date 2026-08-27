// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System;
using System.IO;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives the Hexa-backed <see cref="ImGuiWidgets.FileTreeView"/> on its own, over a directory the
/// test creates so it does not depend on whatever happens to be on the machine.
/// </summary>
/// <remarks>
/// Like the date picker, this control draws Material Icons glyphs and falls back to placeholder
/// boxes without that font. These tests register none, so they cover navigation and layout.
/// </remarks>
[TestClass]
public sealed class FileTreeViewTests : WidgetTest
{
	private const string Id = "files";
	private static readonly Vector2 Size = new(280f, 320f);

	private string root = string.Empty;
	private AbsoluteDirectoryPath current = new();
	private AbsoluteDirectoryPath home = new();
	private bool changed;

	[TestInitialize]
	public void CreateTree()
	{
		root = Path.Combine(Path.GetTempPath(), "widget-uitests-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(Path.Combine(root, "alpha"));
		Directory.CreateDirectory(Path.Combine(root, "beta"));
		File.WriteAllText(Path.Combine(root, "readme.txt"), "hello");

		current = root.As<AbsoluteDirectoryPath>();
		home = root.As<AbsoluteDirectoryPath>();
	}

	[TestCleanup]
	public void DeleteTree()
	{
		if (root.Length > 0 && Directory.Exists(root))
		{
			Directory.Delete(root, recursive: true);
		}
	}

	private void Draw()
	{
		Vector2 origin = ImGui.GetCursorScreenPos();
		changed |= ImGuiWidgets.FileTreeView(Id, Size, ref current, home);
		MarkSpan(Id, origin);
	}

	[TestMethod]
	public void FileTreeView_DrawsTheRegionItIsGiven()
	{
		Start(Draw);

		Rectangle rect = RectOf(Id);

		Assert.IsTrue(Math.Abs(rect.Width - Size.X) <= 6, $"The tree claimed {rect.Width}px of width rather than {Size.X}.");
		AssertSomethingWasDrawn("the file tree");
	}

	[TestMethod]
	public void FileTreeView_LeftAlone_KeepsTheCurrentFolder()
	{
		Start(Draw);
		Step(5);

		Assert.AreEqual(root, current.ToString(), "The tree moved off the folder it was given.");
		Assert.IsFalse(changed, "The tree reported a change nobody made.");
	}

	// The tree is rooted at the machine's drives and its home shortcut, not at the folder passed
	// in, so what it lists does not follow the current folder around and a test cannot assert on
	// the contents of a directory it made. What it can assert is that the control keeps the folder
	// it was handed and survives being driven.
	[TestMethod]
	public void FileTreeView_KeepsTheFolderItIsGivenAcrossFrames()
	{
		Start(Draw);
		Step(60);

		Assert.AreEqual(root, current.ToString(), "The tree replaced the folder it was given.");
	}

	[TestMethod]
	public void FileTreeView_ScrollsWithoutError()
	{
		Start(Draw);

		Vector2 center = CenterOf(Id);
		Harness.Mouse.Wheel(center.X, center.Y, -3);
		Step(2);

		Assert.IsTrue(IsVisible(Id), "The tree stopped drawing after being scrolled.");
	}

	[TestMethod]
	public void FileTreeView_ClickingInsideItDoesNotCrash()
	{
		Start(Draw);

		ClickFraction(Id, 0.5f, 0.5f);
		Step(2);

		Assert.IsTrue(IsVisible(Id), "The tree stopped drawing after being clicked.");
	}
}
