namespace ktsu.ImGuiApp.ImGuiController;

//Adapted from https://github.com/dotnet/Silk.NET/blob/main/src/OpenGL/Extensions/Silk.NET.OpenGL.Extensions.ImGui/Util.cs

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Contracts;

using Silk.NET.OpenGL;

internal static class Util
{
	[Pure]
	public static float Clamp(float value, float min, float max) => value < min ? min : value > max ? max : value;

	[Conditional("DEBUG")]
	public static void CheckGlError(this GL gl, string title)
	{
		var error = gl.GetError();
		if (error != GLEnum.NoError)
		{
			Debug.Print($"{title}: {error}");
		}
	}
}
