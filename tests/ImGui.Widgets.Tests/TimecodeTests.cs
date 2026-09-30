// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests the timecode arithmetic behind TimecodeField: formatting, parsing, the drop-frame label
/// scheme, and conversion to and from seconds. All pure — no ImGui context required.
/// </summary>
[TestClass]
public class TimecodeTests
{
	[TestMethod]
	public void Format_NonDrop_PadsEveryField() =>
		Assert.AreEqual("01:00:02:11", Timecode.Format(90061, TimecodeRate.Fps25));

	[TestMethod]
	public void Format_DropFrame_SkipsTwoLabelsEachMinute()
	{
		Assert.AreEqual("00:00:59;29", Timecode.Format(1799, TimecodeRate.Fps29_97Drop));
		Assert.AreEqual("00:01:00;02", Timecode.Format(1800, TimecodeRate.Fps29_97Drop));
	}

	[TestMethod]
	public void Format_DropFrame_KeepsEveryTenthMinute() =>
		Assert.AreEqual("00:10:00;00", Timecode.Format(17982, TimecodeRate.Fps29_97Drop));

	[TestMethod]
	public void Format_DropFrame_OneHourIsExact() =>
		Assert.AreEqual("01:00:00;00", Timecode.Format(107892, TimecodeRate.Fps29_97Drop));

	[TestMethod]
	public void Format_DropFrame_At59_94_SkipsFour()
	{
		Assert.AreEqual("00:00:59;59", Timecode.Format(3599, TimecodeRate.Fps59_94Drop));
		Assert.AreEqual("00:01:00;04", Timecode.Format(3600, TimecodeRate.Fps59_94Drop));
	}

	[TestMethod]
	public void Format_NonDrop29_97_DoesNotSkip() =>
		Assert.AreEqual("00:01:00:00", Timecode.Format(1800, TimecodeRate.Fps29_97NonDrop));

	[TestMethod]
	public void Format_Negative_HasASign() =>
		Assert.AreEqual("-00:00:01:01", Timecode.Format(-26, TimecodeRate.Fps25));

	[TestMethod]
	public void Format_HoursAreNotWrapped() =>
		Assert.AreEqual("25:00:00:00", Timecode.Format(25 * 3600 * 25, TimecodeRate.Fps25));

	[TestMethod]
	public void Format_IntMinValue_DoesNotOverflow() =>
		StringAssert.StartsWith(Timecode.Format(int.MinValue, TimecodeRate.Fps25), "-", StringComparison.Ordinal);

	[TestMethod]
	public void ToFrame_InvertsFromFrame_ForEveryFrameOfAnHour()
	{
		TimecodeRate rate = TimecodeRate.Fps29_97Drop;
		for (int frame = 0; frame <= 107892; frame++)
		{
			(int hours, int minutes, int seconds, int frames) = Timecode.FromFrame(frame, rate);
			int back = Timecode.ToFrame(hours, minutes, seconds, frames, rate);
			if (back != frame)
			{
				Assert.Fail($"Frame {frame} labelled {hours}:{minutes}:{seconds};{frames} came back as {back}.");
			}
		}
	}

	[TestMethod]
	public void ToFrame_RejectsADroppedLabel()
	{
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Timecode.ToFrame(0, 1, 0, 0, TimecodeRate.Fps29_97Drop));
		Assert.AreEqual(17982, Timecode.ToFrame(0, 10, 0, 0, TimecodeRate.Fps29_97Drop));
	}

	[TestMethod]
	public void FromFrame_RejectsANegativeFrame() =>
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Timecode.FromFrame(-1, TimecodeRate.Fps25));

	[TestMethod]
	public void TryParse_AcceptsRightAlignedFields()
	{
		Assert.IsTrue(Timecode.TryParse("1:00", TimecodeRate.Fps25, out int oneSecond));
		Assert.AreEqual(25, oneSecond);
		Assert.IsTrue(Timecode.TryParse("2:00:00", TimecodeRate.Fps25, out int twoMinutes));
		Assert.AreEqual(3000, twoMinutes);
		Assert.IsTrue(Timecode.TryParse("00:00:02:10", TimecodeRate.Fps25, out int full));
		Assert.AreEqual(60, full);
	}

	[TestMethod]
	public void TryParse_AcceptsBareDigitsAsPairs()
	{
		Assert.IsTrue(Timecode.TryParse("10000", TimecodeRate.Fps25, out int oneMinute));
		Assert.AreEqual(1500, oneMinute);
		Assert.IsFalse(Timecode.TryParse("123456789", TimecodeRate.Fps25, out int tooLong));
		Assert.AreEqual(0, tooLong);
	}

	[TestMethod]
	public void TryParse_AcceptsAnySeparatorAndASign()
	{
		Assert.IsTrue(Timecode.TryParse("-00.00.01;01", TimecodeRate.Fps25, out int frame));
		Assert.AreEqual(-26, frame);
	}

	[TestMethod]
	[DataRow("00:60:00:00")]
	[DataRow("00:00:60:00")]
	[DataRow("00:00:00:25")]
	[DataRow("")]
	[DataRow("ab")]
	[DataRow("1::2")]
	public void TryParse_RejectsOutOfRangeFields(string text)
	{
		Assert.IsFalse(Timecode.TryParse(text, TimecodeRate.Fps25, out int frame));
		Assert.AreEqual(0, frame);
	}

	[TestMethod]
	public void TryParse_RejectsADroppedDropFrameLabel()
	{
		Assert.IsFalse(Timecode.TryParse("00:01:00;00", TimecodeRate.Fps29_97Drop, out _));
		Assert.IsTrue(Timecode.TryParse("00:01:00;02", TimecodeRate.Fps29_97Drop, out int frame));
		Assert.AreEqual(1800, frame);
	}

	[TestMethod]
	public void Seconds_UseTheExactRationalRate()
	{
		Assert.AreEqual(60.06, Timecode.ToSeconds(1800, TimecodeRate.Fps29_97NonDrop), 1e-9);
		Assert.AreEqual(1800, Timecode.FromSeconds(60.06, TimecodeRate.Fps29_97NonDrop));
		Assert.AreEqual(1, Timecode.FromSeconds(0.02, TimecodeRate.Fps25));
	}

	[TestMethod]
	public void FromSeconds_Saturates()
	{
		Assert.AreEqual(int.MaxValue, Timecode.FromSeconds(1e12, TimecodeRate.Fps60));
		Assert.ThrowsExactly<ArgumentException>(() => Timecode.FromSeconds(double.NaN, TimecodeRate.Fps60));
	}

	[TestMethod]
	public void Rate_RejectsDropFrameAtOtherRates()
	{
		Assert.ThrowsExactly<ArgumentException>(() => new TimecodeRate(25, 1, true));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TimecodeRate(0, 1));
	}

	[TestMethod]
	public void Rate_NamesItself()
	{
		Assert.AreEqual("23.976", TimecodeRate.Fps23_976.ToString());
		Assert.AreEqual("29.97 DF", TimecodeRate.Fps29_97Drop.ToString());
		Assert.AreEqual("24", TimecodeRate.Fps24.ToString());
		Assert.AreEqual(60, TimecodeRate.Fps59_94NonDrop.NominalFramesPerSecond);
	}

	[TestMethod]
	public void DefaultRate_IsRefusedByName()
	{
		ArgumentException error = Assert.ThrowsExactly<ArgumentException>(() => Timecode.Format(0, default));
		Assert.AreEqual("rate", error.ParamName);
	}
}
