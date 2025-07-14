// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp.ImGuiController;

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
		GLEnum error = gl.GetError();
		if (error != GLEnum.NoError)
		{
			Debug.Print($"{title}: {error}");
		}
	}
}
