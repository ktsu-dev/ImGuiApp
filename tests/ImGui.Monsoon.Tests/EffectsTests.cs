// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Monsoon.Tests;

using System.Numerics;

using ktsu.ImGui.Monsoon.Effects;

[TestClass]
public sealed class EffectsTests
{
	[TestMethod]
	public void Hash_IsDeterministicAndInUnitRange()
	{
		for (long i = 0; i < 2000; i++)
		{
			float value = Noise.Hash(7u, i);
			Assert.IsGreaterThanOrEqualTo(0f, value);
			Assert.IsLessThan(1f, value);
			Assert.AreEqual(value, Noise.Hash(7u, i));
		}
	}

	[TestMethod]
	public void Hash_DiffersBetweenSeeds()
	{
		int same = 0;
		for (long i = 0; i < 200; i++)
		{
			if (Noise.Hash(1u, i) == Noise.Hash(2u, i))
			{
				same++;
			}
		}

		Assert.IsLessThan(5, same);
	}

	[TestMethod]
	public void SeedOf_IsStableAndDistinguishesText()
	{
		Assert.AreEqual(Noise.SeedOf("feed"), Noise.SeedOf("feed"));
		Assert.AreNotEqual(Noise.SeedOf("feed"), Noise.SeedOf("grid"));
	}

	[TestMethod]
	public void Flicker_WithNoAmount_IsFullBrightness()
	{
		for (double t = 0; t < 10; t += 0.013)
		{
			Assert.AreEqual(1f, Flicker.Brightness(t, 3u, 0f));
		}
	}

	[TestMethod]
	public void Flicker_StaysWithinItsDepthAndSometimesDips()
	{
		const float Amount = 0.4f;
		bool dipped = false;
		for (double t = 0; t < 30; t += 1d / Flicker.StepsPerSecond)
		{
			float brightness = Flicker.Brightness(t, 11u, Amount);
			Assert.IsGreaterThanOrEqualTo(1f - Amount - 1e-5f, brightness);
			Assert.IsLessThanOrEqualTo(1f, brightness);
			dipped |= brightness < 1f;
		}

		Assert.IsTrue(dipped, "Thirty seconds of flicker never dipped.");
	}

	[TestMethod]
	public void Glitch_AtZeroIntensity_IsInactive()
	{
		for (double t = 0; t < 10; t += 0.05)
		{
			GlitchFrame frame = Glitch.Compute(t, 5u, 0f, 6);
			Assert.IsFalse(frame.Active);
			Assert.AreEqual(0f, frame.Split);
			Assert.IsTrue(frame.Offsets.All(o => o == 0f));
		}
	}

	[TestMethod]
	public void Glitch_ClampsSliceCountAndBurstsSometimes()
	{
		Assert.HasCount(1, Glitch.Compute(0, 1u, 1f, 0).Offsets);

		bool active = false;
		for (double t = 0; t < 60; t += 0.02)
		{
			GlitchFrame frame = Glitch.Compute(t, 9u, 1f, 5);
			Assert.HasCount(5, frame.Offsets);
			active |= frame.Active;
		}

		Assert.IsTrue(active, "A minute at full intensity never glitched.");
	}

	[TestMethod]
	public void Rain_StaysInsideThePane()
	{
		Vector2 size = new(320f, 180f);
		for (double t = 0; t < 5; t += 0.1)
		{
			IReadOnlyList<RainStreak> streaks = RainField.Streaks(size, t, 42u, 40, 300f);
			Assert.HasCount(40, streaks);
			foreach (RainStreak s in streaks)
			{
				foreach (Vector2 p in new[] { s.Top, s.Bottom })
				{
					Assert.IsTrue(p.X >= 0f && p.X <= size.X && p.Y >= 0f && p.Y <= size.Y, $"Streak point {p} is outside {size}.");
				}

				Assert.IsTrue(s.Alpha is >= 0.08f and <= 0.30f, $"Alpha {s.Alpha} out of range.");
			}
		}
	}

	[TestMethod]
	public void Rain_IsEmptyForAnEmptyPaneOrNoStreaks()
	{
		Assert.IsEmpty(RainField.Streaks(Vector2.Zero, 1, 1u, 10, 100f));
		Assert.IsEmpty(RainField.Streaks(new Vector2(100f, 100f), 1, 1u, 0, 100f));
	}

	[TestMethod]
	[DataRow(0f, 10, 0)]
	[DataRow(1f, 10, 10)]
	[DataRow(0.5f, 10, 5)]
	[DataRow(0.05f, 10, 1)]
	[DataRow(-3f, 10, 0)]
	[DataRow(7f, 10, 10)]
	[DataRow(float.NaN, 10, 0)]
	public void Segments_LitCount(float value, int segments, int expected) =>
		Assert.AreEqual(expected, Segments.LitCount(value, segments));

	[TestMethod]
	public void Segments_OnlyTheTopIsAlarm()
	{
		Assert.IsFalse(Segments.IsAlarm(0, 20));
		Assert.IsFalse(Segments.IsAlarm(16, 20));
		Assert.IsTrue(Segments.IsAlarm(17, 20));
		Assert.IsTrue(Segments.IsAlarm(19, 20));
	}
}
