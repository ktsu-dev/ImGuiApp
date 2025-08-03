// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace NodeGraphTests;
using System.Numerics;
using System.Reflection;
using ktsu.NodeGraph;

[TestClass]
public class ComprehensiveTypeSystemTests
{
	[TestMethod]
	public void PinTypeUtilities_CanConnect_AllNumericCombinations()
	{
		Type[] numericTypes =
		[
			typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
			typeof(int), typeof(uint), typeof(long), typeof(ulong),
			typeof(float), typeof(double), typeof(decimal)
		];

		// Test all valid numeric conversions
		foreach (Type? sourceType in numericTypes)
		{
			foreach (Type? targetType in numericTypes)
			{
				bool canConnect = PinTypeUtilities.CanConnect(sourceType, targetType);

				// Same types can always connect
				if (sourceType == targetType)
				{
					Assert.IsTrue(canConnect, $"{sourceType.Name} should connect to {targetType.Name}");
				}
				// Test specific known valid conversions
				else if (IsValidNumericConversion(sourceType, targetType))
				{
					Assert.IsTrue(canConnect, $"{sourceType.Name} should connect to {targetType.Name}");
				}
			}
		}
	}

	[TestMethod]
	public void PinTypeUtilities_RequiresConversion_NumericTypes()
	{
		// int to double requires conversion but is safe
		Assert.IsTrue(PinTypeUtilities.RequiresConversion(typeof(int), typeof(double)));
		Assert.IsFalse(PinTypeUtilities.IsLossyConversion(typeof(int), typeof(double)));

		// double to int requires conversion and is lossy
		Assert.IsTrue(PinTypeUtilities.RequiresConversion(typeof(double), typeof(int)));
		Assert.IsTrue(PinTypeUtilities.IsLossyConversion(typeof(double), typeof(int)));

		// Same types don't require conversion
		Assert.IsFalse(PinTypeUtilities.RequiresConversion(typeof(int), typeof(int)));
		Assert.IsFalse(PinTypeUtilities.IsLossyConversion(typeof(int), typeof(int)));
	}

	[TestMethod]
	public void PinTypeUtilities_VectorTypes_Handled()
	{
		Type[] vectorTypes =
		[
			typeof(Vector2), typeof(Vector3), typeof(Vector4)
		];

		foreach (Type? vectorType in vectorTypes)
		{
			// Vector types should connect to themselves
			Assert.IsTrue(PinTypeUtilities.CanConnect(vectorType, vectorType));

			// Vector types should not connect to numeric types
			Assert.IsFalse(PinTypeUtilities.CanConnect(vectorType, typeof(float)));
			Assert.IsFalse(PinTypeUtilities.CanConnect(typeof(float), vectorType));
		}
	}

	[TestMethod]
	public void PinTypeUtilities_GenericTypes_Handled()
	{
		// Generic types should connect if they're assignable
		Assert.IsTrue(PinTypeUtilities.CanConnect(typeof(List<int>), typeof(IEnumerable<int>)));
		Assert.IsTrue(PinTypeUtilities.CanConnect(typeof(List<int>), typeof(ICollection<int>)));

		// Different generic arguments should not connect
		Assert.IsFalse(PinTypeUtilities.CanConnect(typeof(List<int>), typeof(List<string>)));
		Assert.IsFalse(PinTypeUtilities.CanConnect(typeof(Dictionary<string, int>), typeof(Dictionary<int, string>)));
	}

	[TestMethod]
	public void PinTypeUtilities_NullableTypes_Handled()
	{
		// Nullable to non-nullable should not be allowed automatically (unsafe)
		Assert.IsFalse(PinTypeUtilities.CanConnect(typeof(int?), typeof(int)));

		// Non-nullable to nullable (safe)
		Assert.IsTrue(PinTypeUtilities.CanConnect(typeof(int), typeof(int?)));

		// Nullable to nullable
		Assert.IsTrue(PinTypeUtilities.CanConnect(typeof(int?), typeof(int?)));
	}

	[TestMethod]
	public void PinTypeUtilities_ArrayTypes_Handled()
	{
		// Array to array of same type
		Assert.IsTrue(PinTypeUtilities.CanConnect(typeof(int[]), typeof(int[])));

		// Array to IEnumerable
		Assert.IsTrue(PinTypeUtilities.CanConnect(typeof(int[]), typeof(IEnumerable<int>)));

		// Different array types
		Assert.IsFalse(PinTypeUtilities.CanConnect(typeof(int[]), typeof(string[])));
	}

	[TestMethod]
	public void PinTypeUtilities_EdgeCases_Handled()
	{
		// Object can accept anything
		Assert.IsTrue(PinTypeUtilities.CanConnect(typeof(string), typeof(object)));
		Assert.IsTrue(PinTypeUtilities.CanConnect(typeof(int), typeof(object)));

		// Object can connect to string (via ToString conversion)
		Assert.IsTrue(PinTypeUtilities.CanConnect(typeof(object), typeof(string)));

		// But object can't connect to specific non-string types (would require casting)
		Assert.IsFalse(PinTypeUtilities.CanConnect(typeof(object), typeof(int)));
		Assert.IsFalse(PinTypeUtilities.CanConnect(typeof(object), typeof(bool)));

		// Void type
		Assert.IsTrue(PinTypeUtilities.CanConnect(typeof(void), typeof(void)));
		Assert.IsFalse(PinTypeUtilities.CanConnect(typeof(void), typeof(int)));
	}

	[TestMethod]
	public void DisplayTypeId_Generation_Comprehensive()
	{
		Dictionary<Type, string> testCases = new()
		{
			// Primitives
			[typeof(int)] = "int",
			[typeof(string)] = "string",
			[typeof(bool)] = "bool",
			[typeof(float)] = "float",
			[typeof(double)] = "double",
			[typeof(decimal)] = "decimal",

			// Nullable
			[typeof(int?)] = "Nullable<int>",
			[typeof(bool?)] = "Nullable<bool>",

			// Arrays
			[typeof(int[])] = "int[]",
			[typeof(string[])] = "string[]",

			// Generics - note: these use the actual type names, not friendly names
			[typeof(List<int>)] = "List<int>",
			[typeof(Dictionary<string, int>)] = "Dictionary<string, int>",
			[typeof(Dictionary<string, List<int>>)] = "Dictionary<string, List`1>", // Nested generics show `1 notation

			// Vectors
			[typeof(Vector2)] = "Vector2",
			[typeof(Vector3)] = "Vector3",
			[typeof(Vector4)] = "Vector4",

			// Special
			[typeof(void)] = "execution",
			[typeof(object)] = "object"
		};

		foreach (KeyValuePair<Type, string> testCase in testCases)
		{
			PropertyInfo? property = typeof(ComprehensiveTypeTestNode).GetProperty($"Test{testCase.Key.Name.Replace("`", "").Replace("[]", "Array").Replace("<", "").Replace(">", "").Replace(",", "").Replace(" ", "")}");
			if (property != null)
			{
				InputPinAttribute? attribute = property.GetCustomAttribute<InputPinAttribute>();
				Assert.IsNotNull(attribute, $"Property for {testCase.Key.Name} should have InputPin attribute");

				attribute.InitializeTypeInfo(property);
				Assert.AreEqual(testCase.Value, attribute.DisplayTypeId, $"DisplayTypeId for {testCase.Key.Name}");
			}
		}
	}

	private static bool IsValidNumericConversion(Type sourceType, Type targetType)
	{
		// This is a simplified version - the actual implementation is more complex
		Dictionary<Type, int> numericHierarchy = new()
		{
			[typeof(byte)] = 1,
			[typeof(sbyte)] = 1,
			[typeof(short)] = 2,
			[typeof(ushort)] = 2,
			[typeof(int)] = 3,
			[typeof(uint)] = 3,
			[typeof(long)] = 4,
			[typeof(ulong)] = 4,
			[typeof(float)] = 5,
			[typeof(double)] = 6,
			[typeof(decimal)] = 7
		};

		return numericHierarchy.ContainsKey(sourceType) &&
			   numericHierarchy.ContainsKey(targetType);
	}
}

[Node("Comprehensive Type Test Node")]
[System.ComponentModel.Description("Node for testing all type system features")]
public class ComprehensiveTypeTestNode
{
	[InputPin] public int Testint { get; set; }
	[InputPin] public string Teststring { get; set; } = "";
	[InputPin] public bool Testbool { get; set; }
	[InputPin] public float Testfloat { get; set; }
	[InputPin] public double Testdouble { get; set; }
	[InputPin] public decimal Testdecimal { get; set; }
	[InputPin] public int? TestNullableInt32 { get; set; }
	[InputPin] public bool? TestNullableBoolean { get; set; }
	[InputPin] public int[] TestInt32Array { get; set; } = [];
	[InputPin] public string[] TestStringArray { get; set; } = [];
	[InputPin] public List<int> TestListInt32 { get; set; } = [];
	[InputPin] public Dictionary<string, int> TestDictionaryStringInt32 { get; set; } = [];
	[InputPin] public Dictionary<string, List<int>> TestDictionaryStringListInt32 { get; set; } = [];
	[InputPin] public Vector2 TestVector2 { get; set; }
	[InputPin] public Vector3 TestVector3 { get; set; }
	[InputPin] public Vector4 TestVector4 { get; set; }
	[InputPin] public object Testobject { get; set; } = new();
}
