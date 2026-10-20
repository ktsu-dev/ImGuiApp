// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;

using ktsu.ImGui.Widgets.Animation;

/// <summary>How a carousel behaves.</summary>
public sealed record CarouselOptions
{
	/// <summary>Gets a value indicating whether a row of page dots is drawn under the pages. On by default; the dots are clickable.</summary>
	public bool ShowIndicator { get; init; } = true;
}

/// <summary>
/// A carousel's scroll position and paging rules, with no ImGui calls, so the snapping can be tested
/// on its own.
/// </summary>
/// <remarks>
/// The position is measured in pages rather than pixels, so it survives the carousel being resized:
/// 0 shows the first page and 1.5 shows the second and third half each. While a drag is under way the
/// position follows the pointer; on release it carries the drag's momentum into a critically damped
/// spring that settles on a whole page.
/// </remarks>
internal sealed class CarouselState
{
	/// <summary>
	/// The rate a fling's speed decays at, per second, matching <see cref="Scroll.InertialScroll"/>'s
	/// default. A fling would coast its speed divided by this before stopping, which is how far ahead
	/// <see cref="ResolvePage"/> looks.
	/// </summary>
	internal const float Friction = 6f;

	/// <summary>How far past the first or last page a drag may pull, in pages, before it stops.</summary>
	internal const float Overscroll = 0.25f;

	private const float Stiffness = 300f;

	private readonly Spring spring = new(0f, Stiffness, 2f * MathF.Sqrt(Stiffness)) { RestThreshold = 0.001f };
	private float pressPosition;
	private int pressPage;

	/// <summary>Initializes a new instance of the <see cref="CarouselState"/> class, showing a page.</summary>
	/// <param name="page">The page to start on.</param>
	public CarouselState(int page)
	{
		TargetPage = page;
		spring.SnapTo(page);
	}

	/// <summary>Gets the scroll position, in pages.</summary>
	public float Position => spring.Value;

	/// <summary>Gets the page the carousel is showing or settling on.</summary>
	public int TargetPage { get; private set; }

	/// <summary>Gets a value indicating whether a drag is under way.</summary>
	public bool IsDragging { get; private set; }

	/// <summary>Starts a drag from wherever the carousel is now.</summary>
	public void BeginDrag()
	{
		pressPosition = Position;
		pressPage = TargetPage;
		IsDragging = true;
	}

	/// <summary>Moves the pages with the pointer.</summary>
	/// <param name="deltaX">How far the pointer has moved since the drag began, in pixels.</param>
	/// <param name="pageWidth">The width of one page, in pixels.</param>
	/// <param name="pageCount">How many pages there are.</param>
	public void Drag(float deltaX, float pageWidth, int pageCount)
	{
		float position = pressPosition - (deltaX / MathF.Max(pageWidth, 1f));
		spring.SnapTo(Math.Clamp(position, -Overscroll, MathF.Max(pageCount - 1, 0) + Overscroll));
	}

	/// <summary>Ends a drag and sets the carousel settling on a page.</summary>
	/// <param name="velocityX">The pointer's horizontal speed at release, in pixels per second.</param>
	/// <param name="pageWidth">The width of one page, in pixels.</param>
	/// <param name="pageCount">How many pages there are.</param>
	/// <returns>The page it settles on.</returns>
	public int Release(float velocityX, float pageWidth, int pageCount)
	{
		IsDragging = false;
		float pagesPerSecond = -velocityX / MathF.Max(pageWidth, 1f);
		TargetPage = ResolvePage(Position, pagesPerSecond, pageCount, pressPage);
		spring.Target = TargetPage;
		spring.SetVelocity(pagesPerSecond);
		return TargetPage;
	}

	/// <summary>Sets the carousel settling on a page, as when a dot is clicked.</summary>
	/// <param name="page">The page.</param>
	/// <param name="pageCount">How many pages there are.</param>
	public void GoTo(int page, int pageCount)
	{
		IsDragging = false;
		TargetPage = ClampPage(page, pageCount);
		spring.Target = TargetPage;
	}

	/// <summary>Advances the settling animation. Does nothing while a drag is under way.</summary>
	/// <param name="deltaTime">Seconds since the last frame.</param>
	public void Update(float deltaTime)
	{
		if (!IsDragging)
		{
			_ = spring.Update(deltaTime);
		}
	}

	/// <summary>Decides which page a released carousel settles on.</summary>
	/// <remarks>
	/// The carousel looks ahead to where the fling would coast to and takes the nearest page there, so a
	/// quick flick turns the page even when the drag itself covered less than half of one. It never
	/// moves more than one page from where the drag started, which is how a paged view on a phone
	/// behaves and keeps a hard fling from skipping pages the user never saw.
	/// </remarks>
	/// <param name="position">The position at release, in pages.</param>
	/// <param name="pagesPerSecond">The speed at release, in pages per second, positive towards later pages.</param>
	/// <param name="pageCount">How many pages there are.</param>
	/// <param name="pressPage">The page the drag started on.</param>
	/// <returns>The page to settle on.</returns>
	internal static int ResolvePage(float position, float pagesPerSecond, int pageCount, int pressPage)
	{
		float projected = position + (pagesPerSecond / Friction);
		int page = (int)MathF.Round(projected, MidpointRounding.AwayFromZero);
		page = Math.Clamp(page, pressPage - 1, pressPage + 1);
		return ClampPage(page, pageCount);
	}

	private static int ClampPage(int page, int pageCount) => pageCount <= 0 ? 0 : Math.Clamp(page, 0, pageCount - 1);
}
