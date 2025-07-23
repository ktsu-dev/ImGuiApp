// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp;

using System.Diagnostics;
using System.Threading;

/// <summary>
/// A PID controller-based frame limiter that provides accurate frame rate control
/// by learning from past errors and dynamically adjusting sleep times.
/// </summary>
internal class PidFrameLimiter
{
	private readonly double kp; // Proportional gain
	private readonly double ki; // Integral gain
	private readonly double kd; // Derivative gain

	private double previousError;
	private double integral;
	private DateTime lastFrameTime;
	private double baseSleepMs;
	private bool isInitialized;

	// Smoothing for frame time measurements
	private readonly Queue<double> recentFrameTimes = new();
	private double frameTimeSum;
	private const int FrameHistorySize = 10;

	// Auto-tuning state
	private bool isTuning;
	private int currentTuningStep;
	private DateTime tuningStartTime;
	private readonly List<TuningResult> tuningResults = [];
	private TuningResult? bestTuningResult;

	internal readonly struct TuningResult
	{
		public double Kp { get; init; }
		public double Ki { get; init; }
		public double Kd { get; init; }
		public double AverageError { get; init; }
		public double MaxError { get; init; }
		public double Stability { get; init; } // Lower is better
		public double Score { get; init; } // Higher is better
	}

	// Predefined parameter sets to test during auto-tuning
	private static readonly (double kp, double ki, double kd)[] CoarseTuningParameterSets =
	[
		// Phase 1: Coarse tuning - wider range, longer tests
		// Conservative settings
		(0.1, 0.02, 0.005),
		(0.2, 0.05, 0.01),
		(0.3, 0.07, 0.015),
		(0.4, 0.08, 0.02),
		(0.5, 0.09, 0.025),
		(0.6, 0.10, 0.03),

		// Balanced settings
		(0.7, 0.09, 0.04),
		(0.8, 0.10, 0.05), // Previous default
		(0.9, 0.11, 0.06),
		(1.0, 0.12, 0.07),
		(1.1, 0.13, 0.08),
		(1.2, 0.15, 0.09),

		// Aggressive settings (including new optimal default)
		(1.3, 0.16, 0.10),
		(1.4, 0.18, 0.11),
		(1.5, 0.20, 0.12),
		(1.7, 0.22, 0.14),
		(1.8, 0.048, 0.237), // New optimal default
		(2.0, 0.25, 0.15),
		(2.3, 0.28, 0.17),

		// Specialized configurations
		(0.6, 0.30, 0.02), // High integral for steady-state accuracy
		(0.4, 0.35, 0.01), // Very high integral
		(1.8, 0.05, 0.25), // High derivative for fast response
		(2.2, 0.03, 0.30), // Very high derivative
		(0.9, 0.08, 0.01), // Low derivative for stability
		(1.1, 0.06, 0.005), // Very low derivative
	];

	// Fine tuning around best coarse result
	private (double kp, double ki, double kd)[] currentFineTuningParameters = [];
	private (double kp, double ki, double kd)[] currentPrecisionTuningParameters = [];

	private enum TuningPhase
	{
		Coarse,
		Fine,
		Precision,
		Complete
	}

	private TuningPhase currentTuningPhase = TuningPhase.Coarse;
	private const double CoarseTuningDurationSeconds = 8.0; // Longer tests for coarse tuning
	private const double FineTuningDurationSeconds = 12.0;   // Even longer for fine tuning
	private const double PrecisionTuningDurationSeconds = 15.0; // Longest for precision tuning

	/// <summary>
	/// Initializes a new instance of the PidFrameLimiter class.
	/// </summary>
	/// <param name="proportionalGain">Proportional gain (Kp) - how strongly to react to current error</param>
	/// <param name="integralGain">Integral gain (Ki) - how strongly to react to accumulated error over time</param>
	/// <param name="derivativeGain">Derivative gain (Kd) - how strongly to react to rate of change of error</param>
	public PidFrameLimiter(double proportionalGain = 1.8, double integralGain = 0.048, double derivativeGain = 0.237)
	{
		kp = proportionalGain;
		ki = integralGain;
		kd = derivativeGain;
		Reset();
	}

	/// <summary>
	/// Starts the comprehensive PID auto-tuning procedure with multiple phases.
	/// Phase 1: Coarse tuning (8s per test, 24 parameters)
	/// Phase 2: Fine tuning (12s per test, 25 parameters around best result)
	/// Phase 3: Precision tuning (15s per test, 9 parameters for final optimization)
	/// Total time: ~12-15 minutes for maximum accuracy
	/// Defaults: Kp=1.8, Ki=0.048, Kd=0.237 (from comprehensive tuning results)
	/// </summary>
	public void StartAutoTuning()
	{
		isTuning = true;
		currentTuningStep = 0;
		currentTuningPhase = TuningPhase.Coarse;
		tuningStartTime = DateTime.UtcNow;
		tuningResults.Clear();
		bestTuningResult = null;
		tuningErrors.Clear();

		// Reset phase-specific arrays
		currentFineTuningParameters = [];
		currentPrecisionTuningParameters = [];

		// Start with first coarse parameter set
		if (CoarseTuningParameterSets.Length > 0)
		{
			(double newKp, double newKi, double newKd) = CoarseTuningParameterSets[0];
			SetTuningParameters(newKp, newKi, newKd);
		}
	}

	/// <summary>
	/// Stops the auto-tuning procedure and applies the best found parameters.
	/// </summary>
	public void StopAutoTuning()
	{
		if (isTuning && bestTuningResult.HasValue)
		{
			TuningResult best = bestTuningResult.Value;
			SetTuningParameters(best.Kp, best.Ki, best.Kd);
		}

		isTuning = false;
		currentTuningStep = 0;
	}

	/// <summary>
	/// Gets the current auto-tuning progress and status with phase information.
	/// </summary>
	/// <returns>Tuning progress information</returns>
	public (bool isActive, int currentStep, int totalSteps, double progressPercent, TuningResult? bestResult, string phase) GetTuningStatusDetailed()
	{
		if (!isTuning)
		{
			return (false, 0, 0, 0.0, bestTuningResult, "Complete");
		}

		(int currentSteps, int totalSteps, string phaseName) = currentTuningPhase switch
		{
			TuningPhase.Coarse => (currentTuningStep, CoarseTuningParameterSets.Length, "Coarse Tuning"),
			TuningPhase.Fine => (currentTuningStep, currentFineTuningParameters.Length, "Fine Tuning"),
			TuningPhase.Precision => (currentTuningStep, currentPrecisionTuningParameters.Length, "Precision Tuning"),
			_ => (0, 1, "Complete")
		};

		double progress = totalSteps > 0 ? currentSteps / (double)totalSteps * 100.0 : 100.0;
		return (isTuning, currentSteps + 1, totalSteps, progress, bestTuningResult, phaseName);
	}

	// Keep the old method for compatibility
	public (bool isActive, int currentStep, int totalSteps, double progressPercent, TuningResult? bestResult) GetTuningStatus()
	{
		(bool isActive, int currentStep, int totalSteps, double progressPercent, TuningResult? bestResult, string _) = GetTuningStatusDetailed();
		return (isActive, currentStep, totalSteps, progressPercent, bestResult);
	}

	private double GetCurrentTuningDuration() => currentTuningPhase switch
	{
		TuningPhase.Coarse => CoarseTuningDurationSeconds,
		TuningPhase.Fine => FineTuningDurationSeconds,
		TuningPhase.Precision => PrecisionTuningDurationSeconds,
		_ => CoarseTuningDurationSeconds
	};

	private void GenerateFineTuningParameters(TuningResult bestResult)
	{
		// Generate 25 parameter combinations around the best coarse result
		List<(double kp, double ki, double kd)> fineParams = [];

		double baseKp = bestResult.Kp;
		double baseKi = bestResult.Ki;
		double baseKd = bestResult.Kd;

		// Create a 5x5 grid around the best parameters (25 combinations)
		double[] kpMultipliers = [0.8, 0.9, 1.0, 1.1, 1.2];
		double[] kiMultipliers = [0.7, 0.85, 1.0, 1.15, 1.3];

		foreach (double kpMult in kpMultipliers)
		{
			foreach (double kiMult in kiMultipliers)
			{
				double newKp = Math.Max(0.05, baseKp * kpMult);
				double newKi = Math.Max(0.01, baseKi * kiMult);
				double newKd = baseKd; // Keep Kd constant for fine tuning
				fineParams.Add((newKp, newKi, newKd));
			}
		}

		currentFineTuningParameters = [.. fineParams];
	}

	private void GeneratePrecisionTuningParameters(TuningResult bestResult)
	{
		// Generate 9 parameter combinations for final precision tuning
		List<(double kp, double ki, double kd)> precisionParams = [];

		double baseKp = bestResult.Kp;
		double baseKi = bestResult.Ki;
		double baseKd = bestResult.Kd;

		// Create a tight 3x3 grid for precision (9 combinations)
		double[] multipliers = [0.95, 1.0, 1.05];

		foreach (double kpMult in multipliers)
		{
			foreach (double kiMult in multipliers)
			{
				double newKp = Math.Max(0.05, baseKp * kpMult);
				double newKi = Math.Max(0.01, baseKi * kiMult);
				double newKd = Math.Max(0.001, baseKd * kiMult); // Fine-tune derivative too
				precisionParams.Add((newKp, newKi, newKd));
			}
		}

		currentPrecisionTuningParameters = [.. precisionParams];
	}

	private void HandleAutoTuningProgression(double error)
	{
		tuningErrors.Add(Math.Abs(error));

		// Check if we've tested current parameter set long enough
		double testDuration = (DateTime.UtcNow - tuningStartTime).TotalSeconds;
		if (testDuration >= GetCurrentTuningDuration() && tuningErrors.Count > 10) // Ensure we have enough data
		{
			// Verify we have valid arrays and indices before proceeding
			if (!ValidateCurrentTuningState())
			{
				StopAutoTuning();
				return;
			}

			// Process current test results
			ProcessCurrentTuningResults();

			// Advance to next parameter or phase
			AdvanceToNextTuningStep();
		}
	}

	private bool ValidateCurrentTuningState()
	{
		return currentTuningPhase switch
		{
			TuningPhase.Coarse => currentTuningStep >= 0 && currentTuningStep < CoarseTuningParameterSets.Length,
			TuningPhase.Fine => currentFineTuningParameters.Length > 0 && currentTuningStep >= 0 && currentTuningStep < currentFineTuningParameters.Length,
			TuningPhase.Precision => currentPrecisionTuningParameters.Length > 0 && currentTuningStep >= 0 && currentTuningStep < currentPrecisionTuningParameters.Length,
			_ => false
		};
	}

	private void ProcessCurrentTuningResults()
	{
		// Calculate performance metrics for current parameter set
		double avgError = tuningErrors.Average();
		double maxError = tuningErrors.Max();
		double stability = CalculateStability(tuningErrors);
		double score = CalculateScore(avgError, maxError, stability);

		// Store result - get parameters based on current phase
		(double currentKp, double currentKi, double currentKd) = GetCurrentTuningParameters();

		TuningResult result = new()
		{
			Kp = currentKp,
			Ki = currentKi,
			Kd = currentKd,
			AverageError = avgError,
			MaxError = maxError,
			Stability = stability,
			Score = score
		};

		tuningResults.Add(result);

		// Update best result if this one is better
		if (!bestTuningResult.HasValue || score > bestTuningResult.Value.Score)
		{
			bestTuningResult = result;
		}
	}

	private (double kp, double ki, double kd) GetCurrentTuningParameters()
	{
		return currentTuningPhase switch
		{
			TuningPhase.Coarse => CoarseTuningParameterSets[currentTuningStep],
			TuningPhase.Fine => currentFineTuningParameters[currentTuningStep],
			TuningPhase.Precision => currentPrecisionTuningParameters[currentTuningStep],
			_ => (CurrentKp, CurrentKi, CurrentKd) // Fallback to current values
		};
	}

	private void AdvanceToNextTuningStep()
	{
		currentTuningStep++;
		bool advanceToNextPhase = CheckAndHandlePhaseTransition();

		// Start next parameter test
		(double nextKp, double nextKi, double nextKd) = GetNextTuningParameters(advanceToNextPhase);
		SetTuningParameters(nextKp, nextKi, nextKd);

		tuningStartTime = DateTime.UtcNow;
		tuningErrors.Clear();
	}

	private bool CheckAndHandlePhaseTransition()
	{
		switch (currentTuningPhase)
		{
			case TuningPhase.Coarse:
				if (currentTuningStep >= CoarseTuningParameterSets.Length)
				{
					if (bestTuningResult.HasValue)
					{
						GenerateFineTuningParameters(bestTuningResult.Value);
						currentTuningPhase = TuningPhase.Fine;
						currentTuningStep = 0;
						return true;
					}
					else
					{
						StopAutoTuning();
						return false;
					}
				}
				break;

			case TuningPhase.Fine:
				if (currentTuningStep >= currentFineTuningParameters.Length)
				{
					if (bestTuningResult.HasValue)
					{
						GeneratePrecisionTuningParameters(bestTuningResult.Value);
						currentTuningPhase = TuningPhase.Precision;
						currentTuningStep = 0;
						return true;
					}
					else
					{
						StopAutoTuning();
						return false;
					}
				}
				break;

			case TuningPhase.Precision:
				if (currentTuningStep >= currentPrecisionTuningParameters.Length)
				{
					StopAutoTuning();
					return false;
				}
				break;
		}
		return false;
	}

	private (double kp, double ki, double kd) GetNextTuningParameters(bool isNewPhase)
	{
		if (isNewPhase)
		{
			// Starting new phase - get first parameter of new phase
			return currentTuningPhase switch
			{
				TuningPhase.Fine when currentFineTuningParameters.Length > 0
					=> currentFineTuningParameters[0],
				TuningPhase.Precision when currentPrecisionTuningParameters.Length > 0
					=> currentPrecisionTuningParameters[0],
				_ => (CurrentKp, CurrentKi, CurrentKd) // Safe fallback
			};
		}
		else
		{
			// Continue with current phase
			return currentTuningPhase switch
			{
				TuningPhase.Coarse when currentTuningStep < CoarseTuningParameterSets.Length
					=> CoarseTuningParameterSets[currentTuningStep],
				TuningPhase.Fine when currentFineTuningParameters.Length > 0 && currentTuningStep < currentFineTuningParameters.Length
					=> currentFineTuningParameters[currentTuningStep],
				TuningPhase.Precision when currentPrecisionTuningParameters.Length > 0 && currentTuningStep < currentPrecisionTuningParameters.Length
					=> currentPrecisionTuningParameters[currentTuningStep],
				_ => (CurrentKp, CurrentKi, CurrentKd) // Safe fallback
			};
		}
	}

	private void SetTuningParameters(double newKp, double newKi, double newKd)
	{
		// Use reflection or create new fields to temporarily override the readonly kp, ki, kd
		// For now, we'll store them in temporary fields and use them during tuning
		tuningKp = newKp;
		tuningKi = newKi;
		tuningKd = newKd;
		Reset(); // Reset PID state when changing parameters
	}

	// Temporary fields for tuning (will override readonly values during tuning)
	private double tuningKp;
	private double tuningKi;
	private double tuningKd;
	private readonly List<double> tuningErrors = [];

	// Get current PID gains (accounting for tuning mode)
	private double CurrentKp => isTuning ? tuningKp : kp;
	private double CurrentKi => isTuning ? tuningKi : ki;
	private double CurrentKd => isTuning ? tuningKd : kd;

	/// <summary>
	/// Manually sets PID parameters for custom tuning.
	/// </summary>
	/// <param name="proportionalGain">Proportional gain (Kp)</param>
	/// <param name="integralGain">Integral gain (Ki)</param>
	/// <param name="derivativeGain">Derivative gain (Kd)</param>
	public void SetManualPidParameters(double proportionalGain, double integralGain, double derivativeGain)
	{
		tuningKp = proportionalGain;
		tuningKi = integralGain;
		tuningKd = derivativeGain;
		isTuning = false; // Use manual parameters
		Reset(); // Reset PID state when changing parameters
	}

	/// <summary>
	/// Gets the current PID parameters being used.
	/// </summary>
	/// <returns>Current Kp, Ki, Kd values</returns>
	public (double kp, double ki, double kd) GetCurrentParameters() => (CurrentKp, CurrentKi, CurrentKd);

	/// <summary>
	/// Resets the PID controller state. Call this when changing target frame rates.
	/// </summary>
	public void Reset()
	{
		previousError = 0;
		integral = 0;
		lastFrameTime = DateTime.UtcNow;
		baseSleepMs = 0;
		isInitialized = false;
		recentFrameTimes.Clear();
		frameTimeSum = 0;
	}

	/// <summary>
	/// Applies frame rate limiting using PID control to achieve the target frame time.
	/// </summary>
	/// <param name="targetFrameTimeMs">Target time per frame in milliseconds</param>
	public void LimitFrameRate(double targetFrameTimeMs)
	{
		DateTime currentTime = DateTime.UtcNow;

		if (!isInitialized)
		{
			lastFrameTime = currentTime;
			baseSleepMs = Math.Max(0, targetFrameTimeMs - 1); // Less conservative estimate for faster start
			isInitialized = true;
			return;
		}

		// Measure actual frame time (includes previous sleep + rendering time)
		double actualFrameTimeMs = (currentTime - lastFrameTime).TotalMilliseconds;

		// Update lastFrameTime for next frame measurement
		lastFrameTime = currentTime;

		// Smooth the frame time measurement to reduce noise
		UpdateFrameTimeHistory(actualFrameTimeMs);
		double smoothedFrameTime = GetSmoothedFrameTime();

		// Calculate error (positive = running too fast, negative = running too slow)
		double error = targetFrameTimeMs - smoothedFrameTime;

		// PID calculations
		integral += error;

		// Prevent integral windup - clamp integral term to reasonable bounds
		double maxIntegral = targetFrameTimeMs * 2;
		integral = Math.Clamp(integral, -maxIntegral, maxIntegral);

		double derivative = error - previousError;

		// Calculate PID output (adjustment to sleep time)
		double pidOutput = (CurrentKp * error) + (CurrentKi * integral) + (CurrentKd * derivative);

		// Update base sleep time with PID adjustment - use moderate scaling
		baseSleepMs += pidOutput * 0.2; // Reduced from 0.5 for more stable control

		// Clamp sleep time to reasonable bounds
		baseSleepMs = Math.Clamp(baseSleepMs, 0, targetFrameTimeMs * 1.2); // Reduced upper bound to prevent excessive sleep

		// Apply sleep if needed - lowered threshold for better precision
		if (baseSleepMs > 0.1) // Reduced from 0.5ms to allow finer control
		{
			ApplyHighPrecisionSleep(baseSleepMs);
		}

		// Store values for next iteration
		previousError = error;

		// Handle auto-tuning progression
		if (isTuning)
		{
			HandleAutoTuningProgression(error);
		}

		// Optional: Log PID state for debugging (remove in production)
#if DEBUG
		if (DateTime.UtcNow.Millisecond % 100 < 16) // Log roughly every 100ms
		{
			Debug.WriteLine(
				$"PID Frame Limiter - Target: {targetFrameTimeMs:F1}ms, " +
				$"Actual: {smoothedFrameTime:F1}ms, Error: {error:F1}ms, " +
				$"Sleep: {baseSleepMs:F1}ms, P: {CurrentKp * error:F2}, I: {CurrentKi * integral:F2}, D: {CurrentKd * derivative:F2}");
		}
#endif
	}

	private static double CalculateStability(List<double> errors)
	{
		if (errors.Count < 2)
		{
			return 0;
		}

		double mean = errors.Average();
		double variance = errors.Sum(x => Math.Pow(x - mean, 2)) / errors.Count;
		return Math.Sqrt(variance); // Standard deviation - lower is more stable
	}

	private static double CalculateScore(double avgError, double maxError, double stability)
	{
		// Higher score is better
		// Prioritize low average error, penalize high max error and instability
		double accuracyScore = 1.0 / (1.0 + avgError); // Higher when avgError is low
		double maxErrorPenalty = 1.0 / (1.0 + (maxError * 0.5)); // Penalize high max errors
		double stabilityScore = 1.0 / (1.0 + stability); // Higher when stability is low (more stable)

		return accuracyScore * maxErrorPenalty * stabilityScore;
	}

	/// <summary>
	/// Applies high-precision sleep using a hybrid approach:
	/// Thread.Sleep for coarse timing + Stopwatch spin-waiting for fine precision.
	/// </summary>
	/// <param name="sleepTimeMs">Target sleep time in milliseconds</param>
	private static void ApplyHighPrecisionSleep(double sleepTimeMs)
	{
		if (sleepTimeMs <= 0)
		{
			return;
		}

		const double CoarseSleepThreshold = 5.0; // Reduced from 10ms for better precision
		const double SpinWaitThreshold = 0.5;   // Reduced from 1ms for finer control

		Stopwatch sw = Stopwatch.StartNew();
		double targetMs = sleepTimeMs;

		// Phase 1: Coarse sleep using Thread.Sleep for bulk of time
		if (sleepTimeMs > CoarseSleepThreshold)
		{
			double coarseSleepMs = sleepTimeMs - SpinWaitThreshold;
			int coarseSleepInt = (int)Math.Floor(coarseSleepMs);

			if (coarseSleepInt > 0)
			{
				Thread.Sleep(coarseSleepInt);
			}
		}

		// Phase 2: Fine-grained spin-waiting for remaining time
		while (sw.Elapsed.TotalMilliseconds < targetMs)
		{
			// Yield to other threads occasionally to prevent 100% CPU usage
			if (sw.Elapsed.TotalMilliseconds < targetMs - 0.05) // Reduced threshold for tighter control
			{
				Thread.Yield(); // More cooperative than Thread.Sleep(0)
			}
			// Final microsecond precision with tight spin
		}

		sw.Stop();
	}

	private void UpdateFrameTimeHistory(double frameTimeMs)
	{
		recentFrameTimes.Enqueue(frameTimeMs);
		frameTimeSum += frameTimeMs;

		while (recentFrameTimes.Count > FrameHistorySize)
		{
			frameTimeSum -= recentFrameTimes.Dequeue();
		}
	}

	private double GetSmoothedFrameTime() =>
		recentFrameTimes.Count > 0 ? frameTimeSum / recentFrameTimes.Count : 0;

	/// <summary>
	/// Gets diagnostic information about the PID controller state.
	/// </summary>
	/// <returns>A formatted string with PID state information</returns>
	public string GetDiagnosticInfo()
	{
		double smoothedFrameTime = GetSmoothedFrameTime();
		double actualFps = smoothedFrameTime > 0 ? 1000.0 / smoothedFrameTime : 0;
		return $"PID State - Sleep: {baseSleepMs:F2}ms (High-Precision), Error: {previousError:F2}ms, Integral: {integral:F2}, Frame Time: {smoothedFrameTime:F2}ms, Actual FPS: {actualFps:F1}";
	}
}
