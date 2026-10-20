// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing.Support;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// The MSTest half of <see cref="FailureFrames"/>: decides from the test's outcome whether it failed,
/// and attaches the frame to the result. Linked into every suite that drives an
/// <see cref="ImGuiAppHarness"/>, so each cleanup is one call rather than a copy of this.
/// </summary>
/// <remarks>
/// Call it from a <c>[TestCleanup]</c> before the harness is disposed. MSTest has set
/// <see cref="TestContext.CurrentTestOutcome"/> to the test method's outcome by then.
/// </remarks>
internal static class FailureFrameCleanup
{
	/// <summary>Reports whether an outcome is a failure worth a picture.</summary>
	/// <remarks>
	/// An inconclusive test did not fail, and one still in progress has not finished, so neither
	/// leaves a frame behind.
	/// </remarks>
	/// <param name="outcome">The test's outcome.</param>
	/// <returns>True for a failed, errored, timed out or aborted test.</returns>
	public static bool IsFailure(UnitTestOutcome outcome) =>
		outcome is UnitTestOutcome.Failed or UnitTestOutcome.Error or UnitTestOutcome.Timeout or UnitTestOutcome.Aborted;

	/// <summary>
	/// Writes the harness's most recent frame when the current test failed, and attaches it to the
	/// test's result.
	/// </summary>
	/// <param name="context">The current test's context.</param>
	/// <param name="harness">The harness the test drove, or null when it never started one.</param>
	/// <returns>The path written, or null when nothing was.</returns>
	public static string? KeepFailureFrame(this TestContext context, ImGuiAppHarness? harness)
	{
		string? path = FailureFrames.Save(
			harness,
			IsFailure(context.CurrentTestOutcome),
			context.FullyQualifiedTestClassName ?? "unknown",
			context.TestName ?? "unknown");

		if (path is not null)
		{
			context.AddResultFile(path);
		}

		return path;
	}
}
