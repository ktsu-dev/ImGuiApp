// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing.Tests;

using System;
using System.IO;

using ktsu.ImGui.App;
using ktsu.ImGui.App.Images;
using ktsu.ImGui.App.Testing.Support;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class FailureFramesTests
{
	private readonly string suite = $"FailureFramesTests-{Guid.NewGuid():N}";

	public TestContext TestContext { get; set; } = null!;

	[TestCleanup]
	public void DeleteFrames()
	{
		string folder = Path.Combine(FailureFrames.Root, suite);
		if (Directory.Exists(folder))
		{
			Directory.Delete(folder, recursive: true);
		}
	}

	private static ImGuiAppHarness StartStepped()
	{
		ImGuiAppHarness harness = ImGuiAppHarness.Start(new ImGuiAppConfig(), new HarnessOptions { Width = 64, Height = 48 });
		harness.Step();
		return harness;
	}

	[TestMethod]
	public void Save_FailedTest_WritesAPngOfTheLastFrameAtThePredictablePath()
	{
		using ImGuiAppHarness harness = StartStepped();

		string? path = FailureFrames.Save(harness, failed: true, suite, "Fails");

		Assert.AreEqual(FailureFrames.PathFor(suite, "Fails"), path);
		Assert.AreEqual(Path.Combine(FailureFrames.Root, suite, "Fails.png"), path);
		Assert.IsTrue(File.Exists(path), "The failure frame was not written.");

		byte[] file = File.ReadAllBytes(path);
		Assert.IsNotEmpty(file, "The failure frame is empty.");
		ImagePixels decoded = ImageDecoder.Decode(file);
		CapturedFrame frame = harness.Capture();
		Assert.AreEqual(frame.Width, decoded.Width, "The file should be the harness's most recent frame.");
		Assert.AreEqual(frame.Height, decoded.Height, "The file should be the harness's most recent frame.");
	}

	[TestMethod]
	public void Save_PassedTest_WritesNothing()
	{
		using ImGuiAppHarness harness = StartStepped();

		Assert.IsNull(FailureFrames.Save(harness, failed: false, suite, "Passes"));
		Assert.IsFalse(File.Exists(FailureFrames.PathFor(suite, "Passes")));
	}

	[TestMethod]
	public void Save_PassedTest_RemovesAFrameLeftByAnEarlierFailingRun()
	{
		using ImGuiAppHarness harness = StartStepped();
		string? stale = FailureFrames.Save(harness, failed: true, suite, "Flaky");
		Assert.IsNotNull(stale);

		FailureFrames.Save(harness, failed: false, suite, "Flaky");

		Assert.IsFalse(File.Exists(stale), "A passing run must not leave the previous run's picture behind.");
	}

	[TestMethod]
	public void Save_WithoutAFrameToSave_ReturnsNullRatherThanThrowing()
	{
		Assert.IsNull(FailureFrames.Save(null, failed: true, suite, "NoHarness"));

		using (ImGuiAppHarness unstepped = ImGuiAppHarness.Start(new ImGuiAppConfig(), new HarnessOptions { Width = 64, Height = 48 }))
		{
			Assert.IsNull(FailureFrames.Save(unstepped, failed: true, suite, "NoFrame"));
		}

		ImGuiAppHarness disposed = StartStepped();
		disposed.Dispose();
		Assert.IsNull(FailureFrames.Save(disposed, failed: true, suite, "Disposed"));
	}

	[TestMethod]
	public void PathFor_ReplacesCharactersAFileNameCannotHold()
	{
		string path = FailureFrames.PathFor("Some.Suite", "Case(\"a/b\", 1)");

		Assert.AreEqual("Some.Suite", Path.GetFileName(Path.GetDirectoryName(path)));
		Assert.IsFalse(Path.GetFileName(path).Contains('/', StringComparison.Ordinal));
		Assert.AreEqual(FailureFrames.Root, Path.GetDirectoryName(Path.GetDirectoryName(path)));
	}

	[TestMethod]
	public void Root_FollowsTheEnvironmentVariable()
	{
		string? previous = Environment.GetEnvironmentVariable(FailureFrames.DirectoryVariable);
		string folder = Path.Combine(Path.GetTempPath(), $"failure-frames-{Guid.NewGuid():N}");

		try
		{
			Environment.SetEnvironmentVariable(FailureFrames.DirectoryVariable, folder);
			Assert.AreEqual(Path.GetFullPath(folder), FailureFrames.Root);

			Environment.SetEnvironmentVariable(FailureFrames.DirectoryVariable, null);
			Assert.AreEqual(Path.Combine(AppContext.BaseDirectory, "test-artifacts"), FailureFrames.Root);
		}
		finally
		{
			Environment.SetEnvironmentVariable(FailureFrames.DirectoryVariable, previous);
		}
	}

	[TestMethod]
	[DataRow(UnitTestOutcome.Failed, true)]
	[DataRow(UnitTestOutcome.Error, true)]
	[DataRow(UnitTestOutcome.Timeout, true)]
	[DataRow(UnitTestOutcome.Aborted, true)]
	[DataRow(UnitTestOutcome.Passed, false)]
	[DataRow(UnitTestOutcome.Inconclusive, false)]
	[DataRow(UnitTestOutcome.InProgress, false)]
	public void IsFailure_CountsOnlyTestsThatFailed(UnitTestOutcome outcome, bool failed) =>
		Assert.AreEqual(failed, FailureFrameCleanup.IsFailure(outcome));

	[TestMethod]
	public void KeepFailureFrame_WhileTheTestIsStillRunning_WritesNothing()
	{
		// Inside a test body the outcome is still InProgress, which is not a failure.
		using ImGuiAppHarness harness = StartStepped();

		Assert.IsNull(TestContext.KeepFailureFrame(harness));
		Assert.IsFalse(File.Exists(FailureFrames.PathFor(TestContext.FullyQualifiedTestClassName!, TestContext.TestName!)));
	}
}
