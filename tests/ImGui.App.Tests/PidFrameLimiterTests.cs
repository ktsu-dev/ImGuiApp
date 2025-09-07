// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.App.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using ktsu.ImGui.App;

/// <summary>
/// Tests for PidFrameLimiter class covering frame rate limiting, PID controller logic, and auto-tuning functionality.
/// </summary>
[TestClass]
public sealed class PidFrameLimiterTests
{
	private PidFrameLimiter? _frameLimiter;

	[TestInitialize]
	public void Setup()
	{
		_frameLimiter = new PidFrameLimiter();
	}

	[TestCleanup]
	public void Cleanup()
	{
		_frameLimiter?.StopAutoTuning();
	}

	#region Constructor Tests

	[TestMethod]
	public void Constructor_WithDefaultParameters_InitializesCorrectly()
	{
		PidFrameLimiter limiter = new();
		(double kp, double ki, double kd) = limiter.GetCurrentParameters();

		Assert.AreEqual(1.8, kp, 0.001);
		Assert.AreEqual(0.048, ki, 0.001);
		Assert.AreEqual(0.237, kd, 0.001);
	}

	[TestMethod]
	public void Constructor_WithCustomParameters_InitializesCorrectly()
	{
		const double testKp = 2.0;
		const double testKi = 0.1;
		const double testKd = 0.3;

		PidFrameLimiter limiter = new(testKp, testKi, testKd);
		(double kp, double ki, double kd) = limiter.GetCurrentParameters();

		Assert.AreEqual(testKp, kp, 0.001);
		Assert.AreEqual(testKi, ki, 0.001);
		Assert.AreEqual(testKd, kd, 0.001);
	}

	#endregion

	#region Basic Functionality Tests

	[TestMethod]
	public void Reset_ClearsInternalState()
	{
		Assert.IsNotNull(_frameLimiter);

		// Simulate some frame limiting to build internal state
		_frameLimiter.LimitFrameRate(33.33); // 30 FPS
		Thread.Sleep(50);
		_frameLimiter.LimitFrameRate(33.33);

		// Reset should clear the state
		_frameLimiter.Reset();

		// After reset, the limiter should behave as if newly created
		string diagnosticInfo = _frameLimiter.GetDiagnosticInfo();
		Assert.IsNotNull(diagnosticInfo);
		Assert.IsTrue(diagnosticInfo.Contains("Sleep: 0"));
	}

	[TestMethod]
	public void LimitFrameRate_WithValidTargetTime_DoesNotThrow()
	{
		Assert.IsNotNull(_frameLimiter);

		try
		{
			_frameLimiter.LimitFrameRate(16.67); // 60 FPS
			_frameLimiter.LimitFrameRate(33.33); // 30 FPS
			_frameLimiter.LimitFrameRate(100.0); // 10 FPS
		}
#pragma warning disable CA1031 // Do not catch general exception types
		catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
		{
			Assert.Fail($"Expected no exception, but got: {ex.Message}");
		}
	}

	[TestMethod]
	public void LimitFrameRate_WithZeroTargetTime_HandlesGracefully()
	{
		Assert.IsNotNull(_frameLimiter);

		try
		{
			_frameLimiter.LimitFrameRate(0.0);
		}
#pragma warning disable CA1031 // Do not catch general exception types
		catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
		{
			Assert.Fail($"Expected no exception, but got: {ex.Message}");
		}
	}

	[TestMethod]
	public void LimitFrameRate_WithNegativeTargetTime_HandlesGracefully()
	{
		Assert.IsNotNull(_frameLimiter);

		try
		{
			_frameLimiter.LimitFrameRate(-10.0);
		}
#pragma warning disable CA1031 // Do not catch general exception types
		catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
		{
			Assert.Fail($"Expected no exception, but got: {ex.Message}");
		}
	}

	[TestMethod]
	public void GetDiagnosticInfo_ReturnsValidString()
	{
		Assert.IsNotNull(_frameLimiter);

		string diagnosticInfo = _frameLimiter.GetDiagnosticInfo();

		Assert.IsNotNull(diagnosticInfo);
		Assert.IsTrue(diagnosticInfo.Contains("PID State"));
		Assert.IsTrue(diagnosticInfo.Contains("Sleep"));
		Assert.IsTrue(diagnosticInfo.Contains("Error"));
		Assert.IsTrue(diagnosticInfo.Contains("Frame Time"));
	}

	#endregion

	#region Manual PID Parameter Tests

	[TestMethod]
	public void SetManualPidParameters_DoesNotThrow()
	{
		Assert.IsNotNull(_frameLimiter);

		const double newKp = 1.5;
		const double newKi = 0.08;
		const double newKd = 0.2;

		try
		{
			_frameLimiter.SetManualPidParameters(newKp, newKi, newKd);
		}
#pragma warning disable CA1031 // Do not catch general exception types
		catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
		{
			Assert.Fail($"Expected no exception, but got: {ex.Message}");
		}
	}

	[TestMethod]
	public void SetManualPidParameters_StopsAutoTuning()
	{
		Assert.IsNotNull(_frameLimiter);

		_frameLimiter.StartAutoTuning();
		(bool isActive, _, _, _, _) = _frameLimiter.GetTuningStatus();
		Assert.IsTrue(isActive);

		_frameLimiter.SetManualPidParameters(1.0, 0.1, 0.2);

		(isActive, _, _, _, _) = _frameLimiter.GetTuningStatus();
		Assert.IsFalse(isActive);
	}

	#endregion

	#region Auto-Tuning Tests

	[TestMethod]
	public void StartAutoTuning_ActivatesTuning()
	{
		Assert.IsNotNull(_frameLimiter);

		_frameLimiter.StartAutoTuning();

		(bool isActive, int currentStep, int totalSteps, double progress, _) = _frameLimiter.GetTuningStatus();

		Assert.IsTrue(isActive);
		Assert.IsTrue(currentStep > 0);
		Assert.IsTrue(totalSteps > 0);
		Assert.IsTrue(progress is >= 0.0 and <= 100.0);
	}

	[TestMethod]
	public void StopAutoTuning_DeactivatesTuning()
	{
		Assert.IsNotNull(_frameLimiter);

		_frameLimiter.StartAutoTuning();
		_frameLimiter.StopAutoTuning();

		(bool isActive, _, _, _, _) = _frameLimiter.GetTuningStatus();

		Assert.IsFalse(isActive);
	}

	[TestMethod]
	public void GetTuningStatusDetailed_ReturnsValidPhaseInfo()
	{
		Assert.IsNotNull(_frameLimiter);

		_frameLimiter.StartAutoTuning();

		(bool isActive, int currentStep, int totalSteps, double progress, _, string phase) = _frameLimiter.GetTuningStatusDetailed();

		Assert.IsTrue(isActive);
		Assert.IsTrue(currentStep > 0);
		Assert.IsTrue(totalSteps > 0);
		Assert.IsTrue(progress is >= 0.0 and <= 100.0);
		Assert.IsNotNull(phase);
		Assert.IsTrue(phase.Contains("Tuning"));
	}

	[TestMethod]
	public void GetTuningStatus_WhenNotTuning_ReturnsInactiveStatus()
	{
		Assert.IsNotNull(_frameLimiter);

		(bool isActive, int currentStep, int totalSteps, double progress, _) = _frameLimiter.GetTuningStatus();

		Assert.IsFalse(isActive);
		Assert.AreEqual(0, currentStep);
		Assert.AreEqual(0, totalSteps);
		Assert.AreEqual(0.0, progress);
	}

	#endregion

	#region Frame Time History Tests

	[TestMethod]
	public void UpdateFrameTimeHistory_MaintainsCorrectHistorySize()
	{
		Assert.IsNotNull(_frameLimiter);

		// Add more frame times than the history size
		for (int i = 0; i < 20; i++)
		{
			_frameLimiter.UpdateFrameTimeHistory(16.67);
		}

		// The internal queue should not exceed FrameHistorySize (10)
		double smoothedTime = _frameLimiter.GetSmoothedFrameTime();
		Assert.AreEqual(16.67, smoothedTime, 0.1);
	}

	[TestMethod]
	public void GetSmoothedFrameTime_WithNoHistory_ReturnsZero()
	{
		Assert.IsNotNull(_frameLimiter);

		double smoothedTime = _frameLimiter.GetSmoothedFrameTime();

		Assert.AreEqual(0.0, smoothedTime);
	}

	[TestMethod]
	public void GetSmoothedFrameTime_WithHistory_ReturnsAverage()
	{
		Assert.IsNotNull(_frameLimiter);

		_frameLimiter.UpdateFrameTimeHistory(10.0);
		_frameLimiter.UpdateFrameTimeHistory(20.0);
		_frameLimiter.UpdateFrameTimeHistory(30.0);

		double smoothedTime = _frameLimiter.GetSmoothedFrameTime();

		Assert.AreEqual(20.0, smoothedTime, 0.1);
	}

	#endregion

	#region High-Precision Sleep Tests

	[TestMethod]
	public void ApplyHighPrecisionSleep_WithZeroTime_ReturnsImmediately()
	{
		DateTime start = DateTime.UtcNow;
		PidFrameLimiter.ApplyHighPrecisionSleep(0.0);
		DateTime end = DateTime.UtcNow;

		double elapsedMs = (end - start).TotalMilliseconds;
		Assert.IsTrue(elapsedMs < 5.0, $"Expected immediate return, but took {elapsedMs}ms");
	}

	[TestMethod]
	public void ApplyHighPrecisionSleep_WithNegativeTime_ReturnsImmediately()
	{
		DateTime start = DateTime.UtcNow;
		PidFrameLimiter.ApplyHighPrecisionSleep(-10.0);
		DateTime end = DateTime.UtcNow;

		double elapsedMs = (end - start).TotalMilliseconds;
		Assert.IsTrue(elapsedMs < 5.0, $"Expected immediate return, but took {elapsedMs}ms");
	}

	[TestMethod]
	public void ApplyHighPrecisionSleep_WithSmallTime_SleepsApproximately()
	{
		const double targetSleepMs = 2.0;
		DateTime start = DateTime.UtcNow;
		PidFrameLimiter.ApplyHighPrecisionSleep(targetSleepMs);
		DateTime end = DateTime.UtcNow;

		double elapsedMs = (end - start).TotalMilliseconds;

		// Allow for some tolerance in timing due to OS scheduling
		Assert.IsTrue(elapsedMs >= targetSleepMs * 0.8, $"Sleep was too short: {elapsedMs}ms vs target {targetSleepMs}ms");
		Assert.IsTrue(elapsedMs <= targetSleepMs * 2.0, $"Sleep was too long: {elapsedMs}ms vs target {targetSleepMs}ms");
	}

	#endregion

	#region Tuning Algorithm Tests

	[TestMethod]
	public void CalculateStability_WithEmptyList_ReturnsZero()
	{
		List<double> emptyErrors = [];
		double stability = PidFrameLimiter.CalculateStability(emptyErrors);
		Assert.AreEqual(0.0, stability);
	}

	[TestMethod]
	public void CalculateStability_WithSingleValue_ReturnsZero()
	{
		List<double> singleError = [5.0];
		double stability = PidFrameLimiter.CalculateStability(singleError);
		Assert.AreEqual(0.0, stability);
	}

	[TestMethod]
	public void CalculateStability_WithVariedValues_ReturnsPositiveStability()
	{
		List<double> variedErrors = [1.0, 5.0, 3.0, 7.0, 2.0];
		double stability = PidFrameLimiter.CalculateStability(variedErrors);
		Assert.IsTrue(stability > 0.0);
	}

	[TestMethod]
	public void CalculateStability_WithIdenticalValues_ReturnsZero()
	{
		List<double> identicalErrors = [3.0, 3.0, 3.0, 3.0];
		double stability = PidFrameLimiter.CalculateStability(identicalErrors);
		Assert.AreEqual(0.0, stability, 0.001);
	}

	[TestMethod]
	public void CalculateScore_WithLowErrors_ReturnsHighScore()
	{
		double score = PidFrameLimiter.CalculateScore(0.1, 0.2, 0.05);
		Assert.IsTrue(score > 0.5, $"Expected high score for low errors, got {score}");
	}

	[TestMethod]
	public void CalculateScore_WithHighErrors_ReturnsLowScore()
	{
		double score = PidFrameLimiter.CalculateScore(10.0, 20.0, 5.0);
		Assert.IsTrue(score < 0.1, $"Expected low score for high errors, got {score}");
	}

	[TestMethod]
	public void CalculateScore_IsAlwaysPositive()
	{
		double score1 = PidFrameLimiter.CalculateScore(0.0, 0.0, 0.0);
		double score2 = PidFrameLimiter.CalculateScore(100.0, 200.0, 50.0);

		Assert.IsTrue(score1 > 0.0);
		Assert.IsTrue(score2 > 0.0);
	}

	#endregion

	#region Integration Tests

	[TestMethod]
	public void FrameLimiter_ConsecutiveCalls_MaintainsStableState()
	{
		Assert.IsNotNull(_frameLimiter);

		const double targetFrameTime = 33.33; // 30 FPS

		try
		{
			for (int i = 0; i < 5; i++)
			{
				_frameLimiter.LimitFrameRate(targetFrameTime);
				Thread.Sleep(10); // Simulate some frame processing time
			}

			string diagnosticInfo = _frameLimiter.GetDiagnosticInfo();
			Assert.IsNotNull(diagnosticInfo);
		}
#pragma warning disable CA1031 // Do not catch general exception types
		catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
		{
			Assert.Fail($"Expected stable operation, but got: {ex.Message}");
		}
	}

	[TestMethod]
	public void FrameLimiter_ChangingTargetFrameRate_AdaptsCorrectly()
	{
		Assert.IsNotNull(_frameLimiter);

		try
		{
			// Start with 60 FPS
			_frameLimiter.LimitFrameRate(16.67);
			Thread.Sleep(20);

			// Change to 30 FPS
			_frameLimiter.LimitFrameRate(33.33);
			Thread.Sleep(40);

			// Change to 10 FPS
			_frameLimiter.LimitFrameRate(100.0);
			Thread.Sleep(110);

			string diagnosticInfo = _frameLimiter.GetDiagnosticInfo();
			Assert.IsNotNull(diagnosticInfo);
		}
#pragma warning disable CA1031 // Do not catch general exception types
		catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
		{
			Assert.Fail($"Expected adaptive behavior, but got: {ex.Message}");
		}
	}

	#endregion

	#region Edge Case Tests

	[TestMethod]
	public void FrameLimiter_VeryHighFrameRate_HandlesCorrectly()
	{
		Assert.IsNotNull(_frameLimiter);

		try
		{
			_frameLimiter.LimitFrameRate(1.0); // 1000 FPS
		}
#pragma warning disable CA1031 // Do not catch general exception types
		catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
		{
			Assert.Fail($"Expected to handle very high frame rate, but got: {ex.Message}");
		}
	}

	[TestMethod]
	public void FrameLimiter_VeryLowFrameRate_HandlesCorrectly()
	{
		Assert.IsNotNull(_frameLimiter);

		try
		{
			_frameLimiter.LimitFrameRate(1000.0); // 1 FPS
		}
#pragma warning disable CA1031 // Do not catch general exception types
		catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
		{
			Assert.Fail($"Expected to handle very low frame rate, but got: {ex.Message}");
		}
	}

	[TestMethod]
	public void AutoTuning_MultipleStartStop_HandlesCorrectly()
	{
		Assert.IsNotNull(_frameLimiter);

		try
		{
			_frameLimiter.StartAutoTuning();
			_frameLimiter.StopAutoTuning();
			_frameLimiter.StartAutoTuning();
			_frameLimiter.StopAutoTuning();

			(bool isActive, _, _, _, _) = _frameLimiter.GetTuningStatus();
			Assert.IsFalse(isActive);
		}
#pragma warning disable CA1031 // Do not catch general exception types
		catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
		{
			Assert.Fail($"Expected to handle multiple start/stop cycles, but got: {ex.Message}");
		}
	}

	#endregion

	#region Internal State Validation Tests

	[TestMethod]
	public void ValidateCurrentTuningState_WhenNotTuning_DoesNotThrow()
	{
		Assert.IsNotNull(_frameLimiter);

		try
		{
			_frameLimiter.ValidateCurrentTuningState();
		}
#pragma warning disable CA1031 // Do not catch general exception types
		catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
		{
			Assert.Fail($"Expected no exception, but got: {ex.Message}");
		}
	}

	[TestMethod]
	public void GetCurrentTuningDuration_ReturnsValidDuration()
	{
		Assert.IsNotNull(_frameLimiter);

		double duration = _frameLimiter.GetCurrentTuningDuration();
		Assert.IsTrue(duration > 0.0);
	}

	[TestMethod]
	public void GetCurrentTuningParameters_WhenNotTuning_DoesNotThrow()
	{
		Assert.IsNotNull(_frameLimiter);

		try
		{
			_frameLimiter.GetCurrentTuningParameters();
		}
#pragma warning disable CA1031 // Do not catch general exception types
		catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
		{
			Assert.Fail($"Expected no exception, but got: {ex.Message}");
		}
	}

	#endregion
}
