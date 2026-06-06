// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

#if IOS

namespace ktsu.ImGui.App;

using CoreAnimation;

using CoreGraphics;

using Foundation;

using Metal;

using ObjCRuntime;

using UIKit;

/// <summary>
/// A <see cref="UIView"/> whose backing layer is a <see cref="CAMetalLayer"/>, so the Metal backend
/// can render directly into the view's drawable. Overriding <c>+layerClass</c> is the supported way
/// to make UIKit create a Metal layer for the view; the layer is configured for the device and the
/// renderer's pixel format, and its drawable size is kept in sync with the view's pixel dimensions.
/// </summary>
internal sealed class MetalView : UIView
{
	/// <summary>
	/// Tells UIKit to back this view with a <see cref="CAMetalLayer"/>. Exported under the Objective-C
	/// selector <c>layerClass</c>, which UIKit queries once when creating the view's layer.
	/// </summary>
	/// <returns>The <see cref="CAMetalLayer"/> class.</returns>
	[Export("layerClass")]
	public static Class LayerClass() => new(typeof(CAMetalLayer));

	/// <summary>Gets the view's backing layer typed as the Metal layer it is created as.</summary>
	public CAMetalLayer MetalLayer => (CAMetalLayer)Layer;

	/// <summary>Gets the layer's contents scale (pixel density), used to size drawables and ImGui's framebuffer.</summary>
	public float Scale => (float)MetalLayer.ContentsScale;

	/// <summary>Creates the view at the given frame and configures its Metal layer.</summary>
	/// <param name="frame">The initial frame, in points.</param>
	public MetalView(CGRect frame)
		: base(frame)
	{
		CAMetalLayer metalLayer = MetalLayer;
		metalLayer.Device = MTLDevice.SystemDefault;
		metalLayer.PixelFormat = MTLPixelFormat.BGRA8Unorm;
		metalLayer.FramebufferOnly = true;
		metalLayer.ContentsScale = UIScreen.MainScreen.Scale;
	}

	/// <summary>Keeps the drawable size matched to the view's pixel dimensions across layout/rotation.</summary>
	public override void LayoutSubviews()
	{
		base.LayoutSubviews();
		nfloat scale = MetalLayer.ContentsScale;
		MetalLayer.DrawableSize = new CGSize(Bounds.Width * scale, Bounds.Height * scale);
	}
}

#endif
