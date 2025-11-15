// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp;

using System.Reflection;
using Hexa.NET.ImGui;

/// <summary>
/// Manages automatic initialization and lifecycle of ImGui extensions like ImGuizmo, ImNodes, and ImPlot.
/// </summary>
public static class ImGuiExtensionManager
{
	// Cached reflection info for performance
	private static MethodInfo? imGuizmoBeginFrame;
	private static MethodInfo? imGuizmoSetImGuiContext;
	private static MethodInfo? imNodesBeginFrame;
	private static MethodInfo? imNodesSetImGuiContext;
	private static MethodInfo? imNodesCreateContext;
	private static MethodInfo? imNodesSetCurrentContext;
	private static MethodInfo? imNodesStyleColorsDark;
	private static MethodInfo? imNodesGetStyle;
	private static MethodInfo? imNodesDestroyContext;
	private static MethodInfo? imPlotBeginFrame;
	private static MethodInfo? imPlotSetImGuiContext;
	private static MethodInfo? imPlotCreateContext;
	private static MethodInfo? imPlotSetCurrentContext;
	private static MethodInfo? imPlotStyleColorsDark;
	private static MethodInfo? imPlotGetStyle;
	private static MethodInfo? imPlotDestroyContext;

	// Extension contexts
	private static object? nodesContext;
	private static object? plotContext;

	private static bool initialized;

	/// <summary>
	/// Initialize extension detection. Called once during application startup.
	/// </summary>
	public static void Initialize()
	{
		if (initialized)
		{
			return;
		}

		initialized = true;

		InitializeImGuizmo();
		InitializeImNodes();
		InitializeImPlot();
	}

	private static void InitializeImGuizmo()
	{
		try
		{
			Assembly imGuizmoAssembly = Assembly.Load("Hexa.NET.ImGuizmo");
			Type? imGuizmoType = imGuizmoAssembly.GetType("Hexa.NET.ImGuizmo.ImGuizmo");
			imGuizmoBeginFrame = imGuizmoType?.GetMethod("BeginFrame", BindingFlags.Public | BindingFlags.Static, Type.EmptyTypes);

			// Find SetImGuiContext method that takes ImGuiContextPtr parameter
			Type? imGuiContextPtrType = Assembly.Load("Hexa.NET.ImGui").GetType("Hexa.NET.ImGui.ImGuiContextPtr");
			if (imGuiContextPtrType != null)
			{
				imGuizmoSetImGuiContext = imGuizmoType?.GetMethod("SetImGuiContext", BindingFlags.Public | BindingFlags.Static, [imGuiContextPtrType]);
			}

			if (imGuizmoBeginFrame != null)
			{
				DebugLogger.Log("ImGuiExtensionManager: ImGuizmo detected and will be auto-initialized");
			}
		}
		catch (Exception ex) when (ex is FileNotFoundException or FileLoadException or BadImageFormatException or AmbiguousMatchException)
		{
			// ImGuizmo not available or has ambiguous methods - this is fine
			DebugLogger.Log($"ImGuiExtensionManager: ImGuizmo not available or has issues: {ex.Message}");
		}
	}

	private static void InitializeImNodes()
	{
		try
		{
			Assembly imNodesAssembly = Assembly.Load("Hexa.NET.ImNodes");
			Type? imNodesType = imNodesAssembly.GetType("Hexa.NET.ImNodes.ImNodes");
			imNodesBeginFrame = imNodesType?.GetMethod("BeginFrame", BindingFlags.Public | BindingFlags.Static, Type.EmptyTypes);
			imNodesCreateContext = imNodesType?.GetMethod("CreateContext", BindingFlags.Public | BindingFlags.Static, Type.EmptyTypes);
			imNodesGetStyle = imNodesType?.GetMethod("GetStyle", BindingFlags.Public | BindingFlags.Static, Type.EmptyTypes);

			// Find SetImGuiContext method that takes ImGuiContextPtr parameter
			Type? imGuiContextPtrType = Assembly.Load("Hexa.NET.ImGui").GetType("Hexa.NET.ImGui.ImGuiContextPtr");
			if (imGuiContextPtrType != null)
			{
				imNodesSetImGuiContext = imNodesType?.GetMethod("SetImGuiContext", BindingFlags.Public | BindingFlags.Static, [imGuiContextPtrType]);
			}

			// Find context-related methods
			if (imNodesCreateContext != null)
			{
				Type? contextType = imNodesCreateContext.ReturnType;
				if (contextType != null)
				{
					imNodesSetCurrentContext = imNodesType?.GetMethod("SetCurrentContext", BindingFlags.Public | BindingFlags.Static, [contextType]);

					// Find DestroyContext method that takes the context type parameter
					imNodesDestroyContext = imNodesType?.GetMethod("DestroyContext", BindingFlags.Public | BindingFlags.Static, [contextType]);

					// Find StyleColorsDark method that takes ImNodesStylePtr parameter
					Type? styleType = imNodesAssembly.GetType("Hexa.NET.ImNodes.ImNodesStylePtr");
					if (styleType != null)
					{
						imNodesStyleColorsDark = imNodesType?.GetMethod("StyleColorsDark", BindingFlags.Public | BindingFlags.Static, [styleType]);
					}
				}
			}

			if (imNodesBeginFrame != null)
			{
				DebugLogger.Log("ImGuiExtensionManager: ImNodes detected and will be auto-initialized");
			}
		}
		catch (Exception ex) when (ex is FileNotFoundException or FileLoadException or BadImageFormatException or AmbiguousMatchException)
		{
			// ImNodes not available or has ambiguous methods - this is fine
			DebugLogger.Log($"ImGuiExtensionManager: ImNodes not available or has issues: {ex.Message}");
		}
	}

	private static void InitializeImPlot()
	{
		try
		{
			Assembly imPlotAssembly = Assembly.Load("Hexa.NET.ImPlot");
			Type? imPlotType = imPlotAssembly.GetType("Hexa.NET.ImPlot.ImPlot");
			imPlotBeginFrame = imPlotType?.GetMethod("BeginFrame", BindingFlags.Public | BindingFlags.Static, Type.EmptyTypes);
			imPlotCreateContext = imPlotType?.GetMethod("CreateContext", BindingFlags.Public | BindingFlags.Static, Type.EmptyTypes);
			imPlotGetStyle = imPlotType?.GetMethod("GetStyle", BindingFlags.Public | BindingFlags.Static, Type.EmptyTypes);

			// Find SetImGuiContext method that takes ImGuiContextPtr parameter
			Type? imGuiContextPtrType = Assembly.Load("Hexa.NET.ImGui").GetType("Hexa.NET.ImGui.ImGuiContextPtr");
			if (imGuiContextPtrType != null)
			{
				imPlotSetImGuiContext = imPlotType?.GetMethod("SetImGuiContext", BindingFlags.Public | BindingFlags.Static, [imGuiContextPtrType]);
			}

			// Find context-related methods
			if (imPlotCreateContext != null)
			{
				Type? contextType = imPlotCreateContext.ReturnType;
				if (contextType != null)
				{
					imPlotSetCurrentContext = imPlotType?.GetMethod("SetCurrentContext", BindingFlags.Public | BindingFlags.Static, [contextType]);

					// Find DestroyContext method that takes the context type parameter
					imPlotDestroyContext = imPlotType?.GetMethod("DestroyContext", BindingFlags.Public | BindingFlags.Static, [contextType]);

					// Find StyleColorsDark method that takes ImPlotStylePtr parameter
					Type? styleType = imPlotAssembly.GetType("Hexa.NET.ImPlot.ImPlotStylePtr");
					if (styleType != null)
					{
						imPlotStyleColorsDark = imPlotType?.GetMethod("StyleColorsDark", BindingFlags.Public | BindingFlags.Static, [styleType]);
					}
				}
			}

			if (imPlotBeginFrame != null)
			{
				DebugLogger.Log("ImGuiExtensionManager: ImPlot detected and will be auto-initialized");
			}
		}
		catch (Exception ex) when (ex is FileNotFoundException or FileLoadException or BadImageFormatException or AmbiguousMatchException)
		{
			// ImPlot not available or has ambiguous methods - this is fine
			DebugLogger.Log($"ImGuiExtensionManager: ImPlot not available or has issues: {ex.Message}");
		}
	}

	/// <summary>
	/// Call BeginFrame for all detected extensions. Should be called once per frame after ImGui.NewFrame().
	/// </summary>
	public static void BeginFrame()
	{
		// Call ImGuizmo.BeginFrame() if available
		try
		{
			imGuizmoBeginFrame?.Invoke(null, null);
		}
		catch (TargetInvocationException ex)
		{
			DebugLogger.Log($"ImGuiExtensionManager: Error calling ImGuizmo.BeginFrame(): {ex.InnerException?.Message ?? ex.Message}");
		}

		// Call ImNodes.BeginFrame() if available
		try
		{
			imNodesBeginFrame?.Invoke(null, null);
		}
		catch (TargetInvocationException ex)
		{
			DebugLogger.Log($"ImGuiExtensionManager: Error calling ImNodes.BeginFrame(): {ex.InnerException?.Message ?? ex.Message}");
		}

		// Call ImPlot.BeginFrame() if available
		try
		{
			imPlotBeginFrame?.Invoke(null, null);
		}
		catch (TargetInvocationException ex)
		{
			DebugLogger.Log($"ImGuiExtensionManager: Error calling ImPlot.BeginFrame(): {ex.InnerException?.Message ?? ex.Message}");
		}
	}

	/// <summary>
	/// Sets the ImGui context for all detected extensions. Should be called once after ImGui.CreateContext().
	/// </summary>
	/// <param name="context">The ImGui context to set for the extensions. If null, uses the current context.</param>
	public static void SetImGuiContext(ImGuiContextPtr? context = null)
	{
		// Use current context if none provided
		ImGuiContextPtr contextToSet = context ?? ImGui.GetCurrentContext();

		// Set context for ImGuizmo if available
		try
		{
			imGuizmoSetImGuiContext?.Invoke(null, [contextToSet]);
		}
		catch (TargetInvocationException ex)
		{
			DebugLogger.Log($"ImGuiExtensionManager: Error calling ImGuizmo.SetImGuiContext(): {ex.InnerException?.Message ?? ex.Message}");
		}

		// Set context for ImNodes if available
		try
		{
			imNodesSetImGuiContext?.Invoke(null, [contextToSet]);
		}
		catch (TargetInvocationException ex)
		{
			DebugLogger.Log($"ImGuiExtensionManager: Error calling ImNodes.SetImGuiContext(): {ex.InnerException?.Message ?? ex.Message}");
		}

		// Set context for ImPlot if available
		try
		{
			imPlotSetImGuiContext?.Invoke(null, [contextToSet]);
		}
		catch (TargetInvocationException ex)
		{
			DebugLogger.Log($"ImGuiExtensionManager: Error calling ImPlot.SetImGuiContext(): {ex.InnerException?.Message ?? ex.Message}");
		}
	}

	/// <summary>
	/// Creates extension contexts and applies dark styles. Should be called once after SetImGuiContext().
	/// </summary>
	public static void CreateExtensionContexts()
	{
		// Create and set ImNodes context and set style
		if (imNodesCreateContext != null && imNodesSetCurrentContext != null)
		{
			try
			{
				nodesContext = imNodesCreateContext.Invoke(null, null);
				if (nodesContext != null)
				{
					imNodesSetCurrentContext.Invoke(null, [nodesContext]);

					// Apply dark style if available
					if (imNodesStyleColorsDark != null && imNodesGetStyle != null)
					{
						object? style = imNodesGetStyle.Invoke(null, null);
						if (style != null)
						{
							imNodesStyleColorsDark.Invoke(null, [style]);
						}
					}

					DebugLogger.Log("ImGuiExtensionManager: ImNodes context created and dark style applied");
				}
			}
			catch (TargetInvocationException ex)
			{
				DebugLogger.Log($"ImGuiExtensionManager: Error creating ImNodes context: {ex.InnerException?.Message ?? ex.Message}");
			}
		}

		// Create and set ImPlot context and set style
		if (imPlotCreateContext != null && imPlotSetCurrentContext != null)
		{
			try
			{
				plotContext = imPlotCreateContext.Invoke(null, null);
				if (plotContext != null)
				{
					imPlotSetCurrentContext.Invoke(null, [plotContext]);

					// Apply dark style if available
					if (imPlotStyleColorsDark != null && imPlotGetStyle != null)
					{
						object? style = imPlotGetStyle.Invoke(null, null);
						if (style != null)
						{
							imPlotStyleColorsDark.Invoke(null, [style]);
						}
					}

					DebugLogger.Log("ImGuiExtensionManager: ImPlot context created and dark style applied");
				}
			}
			catch (TargetInvocationException ex)
			{
				DebugLogger.Log($"ImGuiExtensionManager: Error creating ImPlot context: {ex.InnerException?.Message ?? ex.Message}");
			}
		}
	}

	/// <summary>
	/// Cleanup extension contexts. Should be called during application shutdown.
	/// </summary>
	public static void Cleanup()
	{
		// Clear ImGuizmo context by setting it to null to free any internal state
		if (imGuizmoSetImGuiContext != null)
		{
			try
			{
				imGuizmoSetImGuiContext.Invoke(null, [default(ImGuiContextPtr)]);
				DebugLogger.Log("ImGuiExtensionManager: ImGuizmo context cleared");
			}
			catch (TargetInvocationException ex)
			{
				DebugLogger.Log($"ImGuiExtensionManager: Error clearing ImGuizmo context: {ex.InnerException?.Message ?? ex.Message}");
			}
		}

		// Destroy ImNodes context if created
		if (nodesContext != null && imNodesDestroyContext != null)
		{
			try
			{
				imNodesDestroyContext.Invoke(null, [nodesContext]);
				nodesContext = null;
				DebugLogger.Log("ImGuiExtensionManager: ImNodes context destroyed");
			}
			catch (TargetInvocationException ex)
			{
				DebugLogger.Log($"ImGuiExtensionManager: Error destroying ImNodes context: {ex.InnerException?.Message ?? ex.Message}");
			}
		}

		// Destroy ImPlot context if created
		if (plotContext != null && imPlotDestroyContext != null)
		{
			try
			{
				imPlotDestroyContext.Invoke(null, [plotContext]);
				plotContext = null;
				DebugLogger.Log("ImGuiExtensionManager: ImPlot context destroyed");
			}
			catch (TargetInvocationException ex)
			{
				DebugLogger.Log($"ImGuiExtensionManager: Error destroying ImPlot context: {ex.InnerException?.Message ?? ex.Message}");
			}
		}
	}

	/// <summary>
	/// Gets whether ImGuizmo is available and initialized.
	/// </summary>
	public static bool IsImGuizmoAvailable => imGuizmoBeginFrame != null;

	/// <summary>
	/// Gets whether ImNodes is available and initialized.
	/// </summary>
	public static bool IsImNodesAvailable => imNodesBeginFrame != null;

	/// <summary>
	/// Gets whether ImPlot is available and initialized.
	/// </summary>
	public static bool IsImPlotAvailable => imPlotBeginFrame != null;

	/// <summary>
	/// Gets whether ImNodes context has been created.
	/// </summary>
	public static bool IsImNodesContextCreated => nodesContext != null;

	/// <summary>
	/// Gets whether ImPlot context has been created.
	/// </summary>
	public static bool IsImPlotContextCreated => plotContext != null;
}
