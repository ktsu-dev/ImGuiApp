// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System;
using System.Globalization;
using System.Linq;
using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests the data table's columns and editing sessions. Nothing here draws, so no ImGui context is
/// required.
/// </summary>
[TestClass]
public class DataTableColumnTests
{
	private readonly DataTableFixture fixture = new();

	private DataTablePerson Alice => fixture.People[0];

	private DataTablePerson Bob => fixture.People[1];

	private static void InCulture(string name, Action action)
	{
		CultureInfo original = CultureInfo.CurrentCulture;
		try
		{
			CultureInfo.CurrentCulture = CreateNonInvariantCulture(name);
			action();
		}
		finally
		{
			CultureInfo.CurrentCulture = original;
		}
	}

	/// <summary>
	/// This assembly runs with globalization-invariant mode on, which has no ICU data for a named
	/// culture such as "de-DE". A culture that only differs from the invariant one in its decimal
	/// separator proves the same point: that GetText and TryParse ignore CurrentCulture.
	/// </summary>
	private static CultureInfo CreateNonInvariantCulture(string name)
	{
		try
		{
			return new CultureInfo(name);
		}
		catch (CultureNotFoundException)
		{
			CultureInfo culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
			culture.NumberFormat.NumberDecimalSeparator = ",";
			return culture;
		}
	}

	[TestMethod]
	public void GetText_WithoutFormat_UsesTheInvariantCulture() =>
		InCulture("de-DE", () =>
		{
			ImGuiWidgets.DataTableColumn<DataTablePerson, double> score = new() { Label = "Score", Value = _ => 1234.5 };

			Assert.AreEqual("1234.5", score.GetText(Alice), "Cell text followed the current culture.");
		});

	[TestMethod]
	public void GetText_WithFormat_UsesIt()
	{
		ImGuiWidgets.DataTableColumn<DataTablePerson, int> age = new()
		{
			Label = "Age",
			Value = person => person.Age,
			Format = value => string.Create(CultureInfo.InvariantCulture, $"{value} years"),
		};

		Assert.AreEqual("30 years", age.GetText(Alice));
	}

	[TestMethod]
	public void GetText_OfANullValue_IsEmpty()
	{
		ImGuiWidgets.DataTableColumn<DataTablePerson, string?> nickname = new() { Label = "Nickname", Value = _ => null };

		Assert.AreEqual(string.Empty, nickname.GetText(Alice));
	}

	[TestMethod]
	public void GetText_OfAnEnum_IsItsName() =>
		Assert.AreEqual("Cheerful", fixture.Mood.GetText(fixture.People[2]));

	[TestMethod]
	public void Compare_UsesTheSuppliedComparer()
	{
		ImGuiWidgets.DataTableColumn<DataTablePerson, string> name = new()
		{
			Label = "Name",
			Value = person => person.Name,
			Comparer = StringComparer.OrdinalIgnoreCase,
		};

		DataTablePerson lower = new("alice", 1, true, DataTableMood.Calm);

		// Ordinal puts every lowercase letter after every uppercase one, so only a case-insensitive
		// comparer can put "alice" before "Bob".
		Assert.IsLessThan(0, name.Compare(lower, Bob), "The column ignored its comparer.");
	}

	[TestMethod]
	public void Validate_AnEditableColumnOfATypeWithNoBuiltInEditor_Throws()
	{
		ImGuiWidgets.DataTableColumn<DataTablePerson, Vector2> position = new()
		{
			Label = "Position",
			Value = _ => Vector2.Zero,
			OnEdit = _ => { },
		};

		Assert.ThrowsExactly<ArgumentException>(position.Validate);
	}

	[TestMethod]
	public void Validate_AReadOnlyColumnOfATypeWithNoBuiltInEditor_Passes()
	{
		ImGuiWidgets.DataTableColumn<DataTablePerson, Vector2> position = new() { Label = "Position", Value = _ => Vector2.Zero };

		position.Validate();
	}

	[TestMethod]
	public void Validate_ATypeWithNoBuiltInEditorButAnEditor_Passes()
	{
		ImGuiWidgets.DataTableColumn<DataTablePerson, Vector2> position = new()
		{
			Label = "Position",
			Value = _ => Vector2.Zero,
			OnEdit = _ => { },
			Editor = (_, ref _) => false,
		};

		position.Validate();
	}

	[TestMethod]
	public void Validate_EveryBuiltInType_Passes()
	{
		fixture.Name.Validate();
		fixture.Age.Validate();
		fixture.Active.Validate();
		fixture.Mood.Validate();
	}

	[TestMethod]
	public void IsToggle_OnlyForAnEditableBoolWithNoEditor()
	{
		ImGuiWidgets.DataTableColumn<DataTablePerson, bool> readOnly = new() { Label = "Active", Value = person => person.IsActive };

		Assert.IsTrue(fixture.Active.IsToggle, "An editable bool column is not a toggle.");
		Assert.IsFalse(readOnly.IsToggle, "A read-only bool column is a toggle.");
		Assert.IsFalse(fixture.Name.IsToggle, "A string column is a toggle.");
	}

	[TestMethod]
	public void CreateSession_OfATextColumn_StartsFromTheInvariantText()
	{
		ImGuiWidgets.DataTableEditSession? session = fixture.Age.CreateSession(DataTableFixture.AgeColumn, 0, Alice, null);

		ImGuiWidgets.DataTableTextSession<DataTablePerson, int> text = Assert.IsInstanceOfType<ImGuiWidgets.DataTableTextSession<DataTablePerson, int>>(session);
		Assert.AreEqual("30", text.Text);
	}

	[TestMethod]
	public void CreateSession_WithATypedCharacter_StartsFromIt()
	{
		ImGuiWidgets.DataTableEditSession? session = fixture.Name.CreateSession(DataTableFixture.NameColumn, 1, Bob, 'Q');

		ImGuiWidgets.DataTableTextSession<DataTablePerson, string> text = Assert.IsInstanceOfType<ImGuiWidgets.DataTableTextSession<DataTablePerson, string>>(session);
		Assert.AreEqual("Q", text.Text);
	}

	[TestMethod]
	public void CreateSession_OfAReadOnlyColumn_IsNull() =>
		Assert.IsNull(fixture.Length.CreateSession(DataTableFixture.LengthColumn, 0, Alice, null));

	[TestMethod]
	public void CreateSession_OfAToggle_IsNull() =>
		Assert.IsNull(fixture.Active.CreateSession(DataTableFixture.ActiveColumn, 0, Alice, null));

	[TestMethod]
	public void CreateSession_OfAnEnumColumn_EditsTheValue()
	{
		ImGuiWidgets.DataTableEditSession? session = fixture.Mood.CreateSession(DataTableFixture.MoodColumn, 0, Alice, null);

		ImGuiWidgets.DataTableValueSession<DataTablePerson, DataTableMood> value = Assert.IsInstanceOfType<ImGuiWidgets.DataTableValueSession<DataTablePerson, DataTableMood>>(session);
		Assert.AreEqual(DataTableMood.Calm, value.Value);
	}

	[TestMethod]
	public void TextSession_CommittingAChangedValue_RaisesOneEdit()
	{
		ImGuiWidgets.DataTableTextSession<DataTablePerson, string> session =
			(ImGuiWidgets.DataTableTextSession<DataTablePerson, string>)fixture.Name.CreateSession(DataTableFixture.NameColumn, 1, Bob, null)!;

		session.Text = "Robert";

		Assert.IsTrue(session.Commit());
		Assert.AreEqual(new ImGuiWidgets.DataTableEdit<DataTablePerson, string>(1, Bob, "Bob", "Robert"), fixture.NameEdits.Single());
	}

	[TestMethod]
	public void TextSession_CommittingAnUnchangedValue_RaisesNothing()
	{
		ImGuiWidgets.DataTableEditSession session = fixture.Name.CreateSession(DataTableFixture.NameColumn, 1, Bob, null)!;

		Assert.IsFalse(session.Commit());
		Assert.IsEmpty(fixture.NameEdits, "Opening and closing an editor recorded a change.");
	}

	[TestMethod]
	public void TextSession_CommittingTextThatDoesNotParse_RaisesNothing()
	{
		ImGuiWidgets.DataTableTextSession<DataTablePerson, int> session =
			(ImGuiWidgets.DataTableTextSession<DataTablePerson, int>)fixture.Age.CreateSession(DataTableFixture.AgeColumn, 0, Alice, null)!;

		session.Text = "thirty";

		Assert.IsFalse(session.Commit());
		Assert.IsEmpty(fixture.AgeEdits);
	}

	[TestMethod]
	public void TextSession_ParsesWithTheInvariantCulture() =>
		InCulture("de-DE", () =>
		{
			double? committed = null;
			ImGuiWidgets.DataTableColumn<DataTablePerson, double> score = new()
			{
				Label = "Score",
				Value = _ => 1.0,
				OnEdit = edit => committed = edit.NewValue,
			};

			ImGuiWidgets.DataTableTextSession<DataTablePerson, double> session =
				(ImGuiWidgets.DataTableTextSession<DataTablePerson, double>)score.CreateSession(0, 0, Alice, null)!;
			session.Text = "2.5";
			session.Commit();

			Assert.AreEqual(2.5, committed);
		});

	[TestMethod]
	public void ValueSession_CommittingAChangedValue_RaisesOneEdit()
	{
		ImGuiWidgets.DataTableValueSession<DataTablePerson, DataTableMood> session =
			(ImGuiWidgets.DataTableValueSession<DataTablePerson, DataTableMood>)fixture.Mood.CreateSession(DataTableFixture.MoodColumn, 0, Alice, null)!;

		session.Value = DataTableMood.Grumpy;

		Assert.IsTrue(session.Commit());
		Assert.AreEqual(
			new ImGuiWidgets.DataTableEdit<DataTablePerson, DataTableMood>(0, Alice, DataTableMood.Calm, DataTableMood.Grumpy),
			fixture.MoodEdits.Single());
	}

	[TestMethod]
	public void Toggle_RaisesTheOppositeValue()
	{
		fixture.Active.Toggle(1, Bob);

		Assert.AreEqual(new ImGuiWidgets.DataTableEdit<DataTablePerson, bool>(1, Bob, false, true), fixture.ActiveEdits.Single());
	}

	[TestMethod]
	public void Toggle_OfAReadOnlyBool_RaisesNothing()
	{
		ImGuiWidgets.DataTableColumn<DataTablePerson, bool> readOnly = new() { Label = "Active", Value = person => person.IsActive };

		readOnly.Toggle(1, Bob);

		Assert.IsEmpty(fixture.ActiveEdits);
	}

	[TestMethod]
	[DataRow("plain", "plain")]
	[DataRow("tab\there", "\"tab\there\"")]
	[DataRow("two\nlines", "\"two\nlines\"")]
	[DataRow("say \"hi\"", "\"say \"\"hi\"\"\"")]
	public void QuoteForCopy_QuotesOnlyWhatASpreadsheetWouldSplit(string text, string expected) =>
		Assert.AreEqual(expected, ImGuiWidgets.DataTableText.QuoteForCopy(text));
}
