// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

#if IOS

namespace ktsu.ImGui.App;

using System.Diagnostics.CodeAnalysis;

using Foundation;

using UIKit;

/// <summary>
/// The <c>UIApplicationDelegate</c> for an ImGuiApp iOS process. Owns the single <see cref="UIWindow"/>
/// and its root <see cref="ImGuiAppViewController"/>, and forwards the application activation lifecycle
/// to <see cref="ImGuiApp.IsFocused"/>. Instantiated by UIKit via <c>UIApplication.Main</c>; the
/// <c>[Register]</c> name is the stable Objective-C class name UIKit looks up.
/// </summary>
[Register("ImGuiAppDelegate")]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "This type is a UIKit UIApplicationDelegate; the 'Delegate' suffix is the platform-idiomatic name and matches the Objective-C class registered via [Register].")]
public class ImGuiAppDelegate : UIApplicationDelegate
{
	/// <summary>Gets or sets the application's single window.</summary>
	public override UIWindow? Window { get; set; }

	/// <summary>
	/// Creates the window and root view controller and brings them on screen. UIKit calls this once,
	/// on the main thread, after the process launches.
	/// </summary>
	/// <param name="application">The shared application instance.</param>
	/// <param name="launchOptions">Launch options dictionary (unused).</param>
	/// <returns><see langword="true"/> to indicate the launch was handled.</returns>
	[SuppressMessage("Interoperability", "CA1422:Validate platform compatibility", Justification = "The single-scene 'new UIWindow(CGRect)' path is supported on iOS 15-25; it is only obsoleted at iOS 26+, where the UIWindowScene API applies. Multi-scene support is a documented post-v1 follow-up in the iOS port plan.")]
	public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
	{
		ImGuiApp.ScaleFactor = (float)UIScreen.MainScreen.Scale;

		ImGuiAppViewController viewController = new();
		ImGuiApp.ViewController = viewController;

		Window = new UIWindow(UIScreen.MainScreen.Bounds)
		{
			RootViewController = viewController,
		};
		Window.MakeKeyAndVisible();

		return true;
	}

	/// <summary>Called when the app becomes the foreground, active application.</summary>
	/// <param name="application">The shared application instance.</param>
	public override void OnActivated(UIApplication application)
	{
		ImGuiApp.IsFocused = true;
		ImGuiApp.ViewController?.ResumeRendering();
	}

	/// <summary>Called when the app is about to move from active to inactive (e.g. an incoming call).</summary>
	/// <param name="application">The shared application instance.</param>
	public override void OnResignActivation(UIApplication application) => ImGuiApp.IsFocused = false;

	/// <summary>Called when the app enters the background; pause rendering to save power.</summary>
	/// <param name="application">The shared application instance.</param>
	public override void DidEnterBackground(UIApplication application) => ImGuiApp.ViewController?.PauseRendering();

	/// <summary>Called when the app is about to re-enter the foreground.</summary>
	/// <param name="application">The shared application instance.</param>
	public override void WillEnterForeground(UIApplication application) => ImGuiApp.ViewController?.ResumeRendering();
}

#endif
