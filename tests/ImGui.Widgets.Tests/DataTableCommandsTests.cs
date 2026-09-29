// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System.Linq;

using ktsu.Keybinding.Core.Models;
using ktsu.Keybinding.Core.Services;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Tests the data table's keyboard commands as a keymap sees them.</summary>
[TestClass]
public class DataTableCommandsTests
{
	private static (CommandRegistry Registry, KeybindingService Keybindings) CreateKeymap()
	{
		CommandRegistry registry = new();
		KeybindingService keybindings = new(registry, new ProfileManager());
		keybindings.CreateProfile("default", "Default");
		keybindings.SetActiveProfile("default");
		return (registry, keybindings);
	}

	[TestMethod]
	public void EveryCommand_HasADefaultChord() =>
		Assert.AreSequenceEqual(
			ImGuiWidgets.DataTableCommands.All.Select(command => command.Id.ToString()).Order(),
			ImGuiWidgets.DataTableCommands.DefaultChords.Keys.Order());

	/// <summary>
	/// A default chord naming a key ImGui doesn't have would parse, register and bind without complaint,
	/// and then never fire.
	/// </summary>
	[TestMethod]
	public void EveryDefaultChord_NamesKeysImGuiHas()
	{
		foreach ((string command, string text) in ImGuiWidgets.DataTableCommands.DefaultChords)
		{
			foreach (string note in Chord.Parse(text).Notes.Select(note => note.ToString()))
			{
				if (note is "CTRL" or "SHIFT" or "ALT")
				{
					continue;
				}

				Assert.IsTrue(KeyChordMatcher.TryMapKey(note, out _), $"{command} is bound to '{text}', and ImGui has no key named {note}.");
			}
		}
	}

	[TestMethod]
	public void Register_BindsEveryDefaultOnce()
	{
		(CommandRegistry registry, KeybindingService keybindings) = CreateKeymap();

		Assert.AreEqual(ImGuiWidgets.DataTableCommands.DefaultChords.Count, ImGuiWidgets.DataTableCommands.Register(registry, keybindings));
		Assert.AreEqual(0, ImGuiWidgets.DataTableCommands.Register(registry, keybindings), "Registering again rebound something.");
	}

	[TestMethod]
	public void Register_LeavesAChordTheUserBoundAlone()
	{
		(CommandRegistry registry, KeybindingService keybindings) = CreateKeymap();

		// ktsu.Keybinding's BindChord only accepts a chord for a command the registry already
		// knows about, so the commands are registered (without binding defaults) before the
		// user's own binding is made, the way a saved profile is replayed onto a fresh registry.
		ImGuiWidgets.DataTableCommands.Register(registry, keybindings, bindDefaultChords: false);
		keybindings.BindChord(ImGuiWidgets.DataTableCommands.Copy, keybindings.ParseChord("Ctrl+Insert"));

		ImGuiWidgets.DataTableCommands.Register(registry, keybindings);

		Assert.AreEqual(keybindings.ParseChord("Ctrl+Insert"), keybindings.GetChord(ImGuiWidgets.DataTableCommands.Copy));
	}

	[TestMethod]
	public void Register_WithoutDefaults_BindsNothing()
	{
		(CommandRegistry registry, KeybindingService keybindings) = CreateKeymap();

		Assert.AreEqual(0, ImGuiWidgets.DataTableCommands.Register(registry, keybindings, bindDefaultChords: false));
		Assert.IsTrue(registry.IsCommandRegistered(ImGuiWidgets.DataTableCommands.Copy), "The commands were not registered.");
	}

	[TestMethod]
	public void DefaultChordOf_ParsesTheDefault() =>
		Assert.AreEqual(Chord.Parse("Ctrl+C"), ImGuiWidgets.DataTableCommands.DefaultChordOf(ImGuiWidgets.DataTableCommands.Copy));

	[TestMethod]
	public void DefaultChordOf_AnUnknownCommand_IsNull() =>
		Assert.IsNull(ImGuiWidgets.DataTableCommands.DefaultChordOf("datatable.nothing"));
}
