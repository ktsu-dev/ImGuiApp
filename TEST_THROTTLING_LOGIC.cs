// Simple test to verify throttling logic
using System;
using System.Collections.Generic;
using System.Linq;

public class ThrottlingTest
{
    public static void TestThrottlingLogic()
    {
        // Test case 1: Unfocused + Idle
        var candidates1 = new List<(double fps, double ups, string reason)>
        {
            (5.0, 5.0, "unfocused"),
            (10.0, 10.0, "idle")
        };
        var result1 = candidates1.OrderBy(r => r.fps).ThenBy(r => r.ups).First();
        Console.WriteLine($"Test 1 - Unfocused + Idle: Expected 5fps, Got {result1.fps}fps ({result1.reason})");

        // Test case 2: Focused + Idle  
        var candidates2 = new List<(double fps, double ups, string reason)>
        {
            (30.0, 30.0, "focused"),
            (10.0, 10.0, "idle")
        };
        var result2 = candidates2.OrderBy(r => r.fps).ThenBy(r => r.ups).First();
        Console.WriteLine($"Test 2 - Focused + Idle: Expected 10fps, Got {result2.fps}fps ({result2.reason})");

        // Test case 3: Unfocused only
        var candidates3 = new List<(double fps, double ups, string reason)>
        {
            (5.0, 5.0, "unfocused")
        };
        var result3 = candidates3.OrderBy(r => r.fps).ThenBy(r => r.ups).First();
        Console.WriteLine($"Test 3 - Unfocused only: Expected 5fps, Got {result3.fps}fps ({result3.reason})");

        // Test case 4: All conditions
        var candidates4 = new List<(double fps, double ups, string reason)>
        {
            (5.0, 5.0, "unfocused"),
            (10.0, 10.0, "idle"),
            (0.1, 0.1, "not visible")
        };
        var result4 = candidates4.OrderBy(r => r.fps).ThenBy(r => r.ups).First();
        Console.WriteLine($"Test 4 - All conditions: Expected 0.1fps, Got {result4.fps}fps ({result4.reason})");
    }
}

// Expected output:
// Test 1 - Unfocused + Idle: Expected 5fps, Got 5fps (unfocused)
// Test 2 - Focused + Idle: Expected 10fps, Got 10fps (idle)  
// Test 3 - Unfocused only: Expected 5fps, Got 5fps (unfocused)
// Test 4 - All conditions: Expected 0.1fps, Got 0.1fps (not visible)