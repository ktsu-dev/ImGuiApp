// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.examples.ImGuiPhosphorDemo.UITests;

using System.Collections.Generic;

using ktsu.ImGui.Phosphor;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>The terminal's typing rules, which need no frame.</summary>
[TestClass]
public sealed class PhosphorTerminalTests
{
	private static readonly string[] AbThenC = ["AB", "C"];
	private static readonly string[] OneTwo = ["ONE", "TWO"];
	private static readonly string[] AEmptyB = ["A", "", "B"];
	private static readonly string[] TwoThree = ["TWO", "THREE"];

	[TestMethod]
	public void Advance_RevealsCharactersAtTheTypingRate()
	{
		PhosphorTerminal terminal = new(10f);
		terminal.WriteLine("HELLO");
		terminal.WriteLine("WORLD");

		terminal.Advance(0.3f);
		IReadOnlyList<string> visible = terminal.VisibleLines();

		Assert.HasCount(1, visible);
		Assert.AreEqual("HEL", visible[0]);
		Assert.IsTrue(terminal.IsTyping);
	}

	[TestMethod]
	public void Advance_CrossesIntoTheNextLine()
	{
		PhosphorTerminal terminal = new(10f);
		terminal.WriteLine("AB");
		terminal.WriteLine("CD");

		terminal.Advance(0.3f);

		Assert.AreSequenceEqual(AbThenC, terminal.VisibleLines());
	}

	[TestMethod]
	public void Advance_CarriesFractionsOfACharacterAcrossFrames()
	{
		PhosphorTerminal terminal = new(10f);
		terminal.WriteLine("ABCDEF");

		for (int i = 0; i < 6; i++)
		{
			terminal.Advance(0.05f);
		}

		Assert.AreEqual("ABC", terminal.VisibleLines()[0]);
	}

	[TestMethod]
	public void ZeroRate_ShowsEverythingAtOnce()
	{
		PhosphorTerminal terminal = new(0f);
		terminal.WriteLine("ALL AT ONCE");

		terminal.Advance(0f);

		Assert.IsFalse(terminal.IsTyping);
		Assert.AreEqual("ALL AT ONCE", terminal.VisibleLines()[0]);
	}

	[TestMethod]
	public void SkipToEnd_FinishesTyping()
	{
		PhosphorTerminal terminal = new(1f);
		terminal.WriteLine("ONE\nTWO");

		terminal.SkipToEnd();

		Assert.IsFalse(terminal.IsTyping);
		Assert.AreSequenceEqual(OneTwo, terminal.VisibleLines());
	}

	[TestMethod]
	public void EmptyLines_AreKeptAndShown()
	{
		PhosphorTerminal terminal = new(0f);
		terminal.WriteLine("A");
		terminal.WriteLine(string.Empty);
		terminal.WriteLine("B");

		terminal.Advance(0f);

		Assert.AreSequenceEqual(AEmptyB, terminal.VisibleLines());
	}

	[TestMethod]
	public void MaxLines_DropsTheOldestWithoutLosingProgress()
	{
		PhosphorTerminal terminal = new(0f) { MaxLines = 2 };
		terminal.WriteLine("ONE");
		terminal.Advance(0f);
		terminal.WriteLine("TWO");
		terminal.WriteLine("THREE");

		Assert.AreSequenceEqual(TwoThree, terminal.Lines);
		Assert.IsTrue(terminal.IsTyping);
	}

	[TestMethod]
	public void Clear_EmptiesTheTerminal()
	{
		PhosphorTerminal terminal = new(0f);
		terminal.WriteLine("GONE");
		terminal.Advance(0f);

		terminal.Clear();

		Assert.HasCount(0, terminal.Lines);
		Assert.HasCount(0, terminal.VisibleLines());
	}
}
