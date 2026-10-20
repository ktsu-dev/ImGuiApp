// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing;

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

/// <summary>
/// Writes the frame a test was looking at when it failed, so the failure can be diagnosed from the
/// picture rather than by reproducing it.
/// </summary>
/// <remarks>
/// <para>
/// Knows nothing about any test framework. A test's cleanup decides whether the test failed and
/// calls <see cref="Save"/> before it disposes the harness; with MSTest that is a check of
/// <c>TestContext.CurrentTestOutcome</c>, and the returned path goes to
/// <c>TestContext.AddResultFile</c> so the frame is attached to the result.
/// </para>
/// <para>
/// Frames go to <c>test-artifacts/&lt;suite&gt;/&lt;test&gt;.png</c> beside the test assembly, or under
/// the folder named by the <see cref="DirectoryVariable"/> environment variable when it is set. The
/// assembly's folder rather than the working directory, because a test is free to change the
/// working directory and a path that moved with it would not be predictable.
/// </para>
/// </remarks>
public static class FailureFrames
{
	/// <summary>The environment variable that overrides where failure frames are written.</summary>
	public const string DirectoryVariable = "IMGUI_TEST_ARTIFACTS";

	/// <summary>Gets the folder failure frames are written under.</summary>
	public static string Root
	{
		get
		{
			string? configured = Environment.GetEnvironmentVariable(DirectoryVariable);
			return string.IsNullOrWhiteSpace(configured)
				? Path.Combine(AppContext.BaseDirectory, "test-artifacts")
				: Path.GetFullPath(configured);
		}
	}

	/// <summary>Gets the path a failing test's frame is written to.</summary>
	/// <param name="suite">The test class, usually its fully qualified name.</param>
	/// <param name="test">The test method's name.</param>
	/// <returns>The full path of the PNG.</returns>
	public static string PathFor(string suite, string test)
	{
		Ensure.NotNull(suite);
		Ensure.NotNull(test);

		return Path.Combine(Root, Sanitize(suite), Sanitize(test) + ".png");
	}

	/// <summary>
	/// Writes the harness's most recent frame for a test that failed.
	/// </summary>
	/// <remarks>
	/// Never throws for want of a frame: a test can fail before its harness started, before the first
	/// step, or after it disposed the harness itself, and a cleanup that threw then would bury the
	/// failure being reported under one about the picture. Those cases return null, as does a frame
	/// that could not be written.
	/// </remarks>
	/// <param name="harness">The harness the test drove, or null when it never started one.</param>
	/// <param name="failed">Whether the test failed. Nothing is written for a test that did not.</param>
	/// <param name="suite">The test class, usually its fully qualified name.</param>
	/// <param name="test">The test method's name.</param>
	/// <returns>The path written, or null when nothing was.</returns>
	public static string? Save(ImGuiAppHarness? harness, bool failed, string suite, string test)
	{
		string path = PathFor(suite, test);

		try
		{
			// A frame left by an earlier failing run of the same test would otherwise read as this
			// run's, so it goes whether or not this run failed.
			if (File.Exists(path))
			{
				File.Delete(path);
			}

			if (!failed || harness is null || harness.FrameCount == 0)
			{
				return null;
			}

			CapturedFrame frame = harness.Capture();
			Directory.CreateDirectory(Path.GetDirectoryName(path)!);
			frame.SavePng(path);
			return path;
		}
		catch (Exception e) when (e is InvalidOperationException or IOException or UnauthorizedAccessException)
		{
			// ObjectDisposedException is an InvalidOperationException, which covers a harness the
			// test already disposed.
			Trace.TraceWarning($"Could not save the failure frame for {suite}.{test}: {e.Message}");
			return null;
		}
	}

	private static string Sanitize(string name)
	{
		char[] invalid = Path.GetInvalidFileNameChars();
		string safe = new([.. name.Select(c => invalid.Contains(c) || c is '/' or '\\' or ':' ? '_' : c)]);
		return safe.Length == 0 ? "_" : safe;
	}
}
