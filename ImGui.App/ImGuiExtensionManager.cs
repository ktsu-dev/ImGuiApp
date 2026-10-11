// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App;

using System.Diagnostics.CodeAnalysis;
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

	// Extensions whose assembly has been probed, so each is loaded and reflected over at most once
	private static ImGuiAppExtensions probed;

	/// <summary>
	/// Gets the extensions the last <see cref="Initialize(ImGuiAppExtensions)"/> asked for. Nothing
	/// outside this set is touched.
	/// </summary>
	public static ImGuiAppExtensions Enabled { get; private set; }

	/// <summary>
	/// Initialize detection of every extension. Called once during application startup.
	/// </summary>
	public static void Initialize() => Initialize(ImGuiAppExtensions.All);

	/// <summary>
	/// Initialize detection of the given extensions only. An extension left out is never loaded,
	/// so it costs nothing at startup, and the calls that follow skip it.
	/// </summary>
	/// <param name="extensions">The extensions to detect.</param>
	public static void Initialize(ImGuiAppExtensions extensions)
	{
		Enabled = extensions & ImGuiAppExtensions.All;
		Probe(Enabled);
	}

	/// <summary>
	/// Turns on the given extensions in addition to those already enabled, after launch. Unlike
	/// <see cref="Initialize(ImGuiAppExtensions)"/>, which replaces the enabled set, this only adds
	/// to it, so an extension that is already on keeps its context and its state.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Each newly enabled extension is set up exactly as it is at launch: its assembly is probed if
	/// it has not been, it is handed the ImGui context when <see cref="SetImGuiContext(ImGuiContextPtr?)"/>
	/// has already run, and ImNodes and ImPlot are given a context of their own with the dark
	/// style. ImGuizmo, which needs a <c>BeginFrame</c> every frame, gets one at once when this is
	/// called inside a frame, so it can be drawn with in the same frame; <see cref="BeginFrame"/>
	/// covers it from the next frame on.
	/// </para>
	/// <para>
	/// Call it on the render thread, between frames or inside <c>OnRender</c> before the extension
	/// is drawn with. Enabling an extension that is already on does nothing.
	/// </para>
	/// </remarks>
	/// <param name="extensions">The extensions to turn on.</param>
	/// <returns>
	/// Which of <paramref name="extensions"/> are ready to draw with: ImGuizmo when its assembly was
	/// found, ImNodes and ImPlot when their context exists. Before the ImGui context has been shared,
	/// no context can exist yet, so ImNodes and ImPlot are reported when their assembly was found.
	/// An extension asked for and missing from the result is not referenced, and drawing with it
	/// faults in native code.
	/// </returns>
	public static ImGuiAppExtensions Enable(ImGuiAppExtensions extensions)
	{
		ImGuiAppExtensions requested = extensions & ImGuiAppExtensions.All;
		ImGuiAppExtensions added = requested & ~Enabled;

		if (added != ImGuiAppExtensions.None)
		{
			Enabled |= added;
			Probe(added);

			// Before the context is shared, launch will set these up along with everything else.
			if (IsImGuiContextShared)
			{
				ImGuiContextPtr context = ImGui.GetCurrentContext();
				SetImGuiContextFor(added, context);
				CreateExtensionContexts();

				// ImGuizmo draws into a window its BeginFrame opens; inside a frame that has already
				// begun, BeginFrame has passed it by, so it is begun here instead.
				if (added.HasFlag(ImGuiAppExtensions.ImGuizmo) && context.WithinFrameScope)
				{
					BeginFrameFor(ImGuiAppExtensions.ImGuizmo);
				}
			}
		}

		return Ready(requested);
	}

	/// <summary>
	/// Gets which of the given extensions are ready to draw with. See <see cref="Enable(ImGuiAppExtensions)"/>.
	/// </summary>
	private static ImGuiAppExtensions Ready(ImGuiAppExtensions extensions)
	{
		ImGuiAppExtensions ready = ImGuiAppExtensions.None;

		if (IsImGuizmoAvailable)
		{
			ready |= ImGuiAppExtensions.ImGuizmo;
		}

		if (IsImGuiContextShared ? IsImNodesContextCreated : IsImNodesAvailable)
		{
			ready |= ImGuiAppExtensions.ImNodes;
		}

		if (IsImGuiContextShared ? IsImPlotContextCreated : IsImPlotAvailable)
		{
			ready |= ImGuiAppExtensions.ImPlot;
		}

		return ready & extensions;
	}

	/// <summary>
	/// Gets whether <see cref="SetImGuiContext(ImGuiContextPtr?)"/> has run since the last
	/// <see cref="Cleanup"/>, which is what a launched application or a started harness has done.
	/// </summary>
	internal static bool IsImGuiContextShared { get; private set; }

	/// <summary>
	/// Loads and reflects over each of the given extensions that has not been probed already.
	/// </summary>
	private static void Probe(ImGuiAppExtensions extensions)
	{
		ImGuiAppExtensions toProbe = extensions & ~probed;
		probed |= toProbe;

		if (toProbe.HasFlag(ImGuiAppExtensions.ImGuizmo))
		{
			InitializeImGuizmo();
		}

		if (toProbe.HasFlag(ImGuiAppExtensions.ImNodes))
		{
			InitializeImNodes();
		}

		if (toProbe.HasFlag(ImGuiAppExtensions.ImPlot))
		{
			InitializeImPlot();
		}
	}

	private static bool IsEnabled(ImGuiAppExtensions extension) => (Enabled & extension) != 0;

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

	[SuppressMessage("Major Code Smell", "S2589:Boolean expressions should not be gratuitous", Justification = "Null checks guard reflection results that may be null at runtime; static analysis cannot determine this.")]
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

	[SuppressMessage("Major Code Smell", "S2589:Boolean expressions should not be gratuitous", Justification = "Null checks guard reflection results that may be null at runtime; static analysis cannot determine this.")]
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
		// Guard: extension BeginFrame calls require a valid ImGui context
		if (!IsImGuiContextShared)
		{
			return;
		}

		BeginFrameFor(Enabled);
	}

	/// <summary>
	/// Calls BeginFrame for each of the given extensions that is enabled and has one.
	/// </summary>
	private static void BeginFrameFor(ImGuiAppExtensions extensions)
	{
		ImGuiAppExtensions toBegin = extensions & Enabled;

		// Call ImGuizmo.BeginFrame() if available
		try
		{
			if (toBegin.HasFlag(ImGuiAppExtensions.ImGuizmo))
			{
				imGuizmoBeginFrame?.Invoke(null, null);
			}
		}
		catch (TargetInvocationException ex)
		{
			DebugLogger.Log($"ImGuiExtensionManager: Error calling ImGuizmo.BeginFrame(): {ex.InnerException?.Message ?? ex.Message}");
		}

		// Call ImNodes.BeginFrame() if available
		try
		{
			if (toBegin.HasFlag(ImGuiAppExtensions.ImNodes))
			{
				imNodesBeginFrame?.Invoke(null, null);
			}
		}
		catch (TargetInvocationException ex)
		{
			DebugLogger.Log($"ImGuiExtensionManager: Error calling ImNodes.BeginFrame(): {ex.InnerException?.Message ?? ex.Message}");
		}

		// Call ImPlot.BeginFrame() if available
		try
		{
			if (toBegin.HasFlag(ImGuiAppExtensions.ImPlot))
			{
				imPlotBeginFrame?.Invoke(null, null);
			}
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
		SetImGuiContextFor(Enabled, context ?? ImGui.GetCurrentContext());
		IsImGuiContextShared = true;
	}

	/// <summary>
	/// Hands the ImGui context to each of the given extensions that is enabled.
	/// </summary>
	private static void SetImGuiContextFor(ImGuiAppExtensions extensions, ImGuiContextPtr contextToSet)
	{
		ImGuiAppExtensions toSet = extensions & Enabled;

		// Set context for ImGuizmo if available
		try
		{
			if (toSet.HasFlag(ImGuiAppExtensions.ImGuizmo))
			{
				imGuizmoSetImGuiContext?.Invoke(null, [contextToSet]);
			}
		}
		catch (TargetInvocationException ex)
		{
			DebugLogger.Log($"ImGuiExtensionManager: Error calling ImGuizmo.SetImGuiContext(): {ex.InnerException?.Message ?? ex.Message}");
		}

		// Set context for ImNodes if available
		try
		{
			if (toSet.HasFlag(ImGuiAppExtensions.ImNodes))
			{
				imNodesSetImGuiContext?.Invoke(null, [contextToSet]);
			}
		}
		catch (TargetInvocationException ex)
		{
			DebugLogger.Log($"ImGuiExtensionManager: Error calling ImNodes.SetImGuiContext(): {ex.InnerException?.Message ?? ex.Message}");
		}

		// Set context for ImPlot if available
		try
		{
			if (toSet.HasFlag(ImGuiAppExtensions.ImPlot))
			{
				imPlotSetImGuiContext?.Invoke(null, [contextToSet]);
			}
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
		if (IsEnabled(ImGuiAppExtensions.ImNodes) && nodesContext == null && imNodesCreateContext != null && imNodesSetCurrentContext != null)
		{
			try
			{
				nodesContext = imNodesCreateContext.Invoke(null, null);
				if (nodesContext != null)
				{
					imNodesSetCurrentContext.Invoke(null, [nodesContext]);

					ApplyExtensionDarkStyle(imNodesStyleColorsDark, imNodesGetStyle);

					DebugLogger.Log("ImGuiExtensionManager: ImNodes context created and dark style applied");
				}
			}
			catch (TargetInvocationException ex)
			{
				DebugLogger.Log($"ImGuiExtensionManager: Error creating ImNodes context: {ex.InnerException?.Message ?? ex.Message}");
			}
		}

		// Create and set ImPlot context and set style
		if (IsEnabled(ImGuiAppExtensions.ImPlot) && plotContext == null && imPlotCreateContext != null && imPlotSetCurrentContext != null)
		{
			try
			{
				plotContext = imPlotCreateContext.Invoke(null, null);
				if (plotContext != null)
				{
					imPlotSetCurrentContext.Invoke(null, [plotContext]);

					ApplyExtensionDarkStyle(imPlotStyleColorsDark, imPlotGetStyle);

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
	/// Applies the extension's dark color style when both style methods are available.
	/// </summary>
	/// <param name="styleColorsDark">The extension's StyleColorsDark method, if available.</param>
	/// <param name="getStyle">The extension's GetStyle method, if available.</param>
	private static void ApplyExtensionDarkStyle(MethodInfo? styleColorsDark, MethodInfo? getStyle)
	{
		// Apply dark style if available
		if (styleColorsDark != null && getStyle != null)
		{
			object? style = getStyle.Invoke(null, null);
			if (style != null)
			{
				styleColorsDark.Invoke(null, [style]);
			}
		}
	}

	/// <summary>
	/// Cleanup extension contexts. Should be called during application shutdown.
	/// </summary>
	public static void Cleanup()
	{
		// Clear ImGuizmo context by setting it to null to free any internal state
		if (IsEnabled(ImGuiAppExtensions.ImGuizmo) && imGuizmoSetImGuiContext != null)
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

		IsImGuiContextShared = false;
	}

	/// <summary>
	/// Gets whether ImGuizmo is available and was asked for.
	/// </summary>
	public static bool IsImGuizmoAvailable => IsEnabled(ImGuiAppExtensions.ImGuizmo) && imGuizmoBeginFrame != null;

	/// <summary>
	/// Gets whether ImNodes is available and was asked for. Hexa's binding has no BeginFrame for it,
	/// so a CreateContext is what shows it is there.
	/// </summary>
	public static bool IsImNodesAvailable => IsEnabled(ImGuiAppExtensions.ImNodes) && (imNodesBeginFrame ?? imNodesCreateContext) != null;

	/// <summary>
	/// Gets whether ImPlot is available and was asked for. Hexa's binding has no BeginFrame for it,
	/// so a CreateContext is what shows it is there.
	/// </summary>
	public static bool IsImPlotAvailable => IsEnabled(ImGuiAppExtensions.ImPlot) && (imPlotBeginFrame ?? imPlotCreateContext) != null;

	/// <summary>
	/// Gets whether ImNodes context has been created.
	/// </summary>
	public static bool IsImNodesContextCreated => nodesContext != null;

	/// <summary>
	/// Gets whether ImPlot context has been created.
	/// </summary>
	public static bool IsImPlotContextCreated => plotContext != null;
}
