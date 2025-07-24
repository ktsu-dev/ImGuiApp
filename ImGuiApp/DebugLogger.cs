// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp;

using ktsu.StrongPaths;
using ktsu.Extensions;

/// <summary>
/// Simple file logger for debugging crashes
/// </summary>
internal static class DebugLogger
{
	private static AbsoluteDirectoryPath AppDataPath => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create).As<AbsoluteDirectoryPath>();
	private static RelativeDirectoryPath AppDomain => System.AppDomain.CurrentDomain.FriendlyName.As<RelativeDirectoryPath>();
	private static AbsoluteDirectoryPath DomainAppDataPath => AppDataPath / AppDomain;
	private static AbsoluteFilePath LogFilePath => DomainAppDataPath / "ImGuiApp_Debug.log".As<FileName>();

	static DebugLogger()
	{
		Directory.CreateDirectory(LogFilePath.DirectoryPath);
		File.Delete(LogFilePath);
	}

	public static void Log(string message)
	{
		string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
		File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);
		Console.WriteLine(logEntry);
	}
}
