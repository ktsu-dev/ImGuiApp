// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.App.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using ktsu.ImGui.App.ImGuiController;

/// <summary>
/// Tests for platform-specific functionality including DPI awareness, native methods, and GDI+ helpers.
/// </summary>
[TestClass]
public class PlatformSpecificTests
{
	#region ForceDpiAware Tests

	[TestMethod]
	public void ForceDpiAware_GetWindowScaleFactor_ReturnsValidValue()
	{
		double scaleFactor = ForceDpiAware.GetWindowScaleFactor();

		Assert.IsGreaterThan(0, scaleFactor, "Scale factor should be greater than zero");
		Assert.IsLessThanOrEqualTo(10.25, scaleFactor, "Scale factor should not exceed MaxScaleFactor (10.25)");
	}

	[TestMethod]
	public void ForceDpiAware_GetActualScaleFactor_ReturnsValidValue()
	{
		double actualScale = ForceDpiAware.GetActualScaleFactor();

		Assert.IsGreaterThan(0, actualScale, "Actual scale factor should be greater than zero");
	}

	#endregion

	#region GdiPlusHelper Tests

	[TestMethod]
	public void GdiPlusHelper_IsStaticClass()
	{
		Type gdiPlusHelperType = typeof(GdiPlusHelper);
		Assert.IsTrue(gdiPlusHelperType.IsAbstract && gdiPlusHelperType.IsSealed, "GdiPlusHelper should be a static class (abstract and sealed)");
	}

	#endregion

	#region NativeMethods Tests

	[TestMethod]
	public void NativeMethods_IsStaticClass()
	{
		Type nativeMethodsType = typeof(NativeMethods);
		Assert.IsTrue(nativeMethodsType.IsAbstract && nativeMethodsType.IsSealed, "NativeMethods should be a static class (abstract and sealed)");
	}

	[TestMethod]
	public void NativeMethods_IsInternalClass()
	{
		Type nativeMethodsType = typeof(NativeMethods);
		Assert.IsTrue(nativeMethodsType.IsClass, "NativeMethods should be a class");
		Assert.IsFalse(nativeMethodsType.IsPublic, "NativeMethods should not be public (internal)");
	}

	#endregion

	#region Type System Tests

	[TestMethod]
	public void UniformFieldInfo_IsStruct()
	{
		Type type = typeof(UniformFieldInfo);
		Assert.IsTrue(type.IsValueType, "UniformFieldInfo should be a value type (struct)");
		Assert.IsFalse(type.IsClass, "UniformFieldInfo should not be a class");
	}

	[TestMethod]
	public void UniformFieldInfo_HasExpectedFields()
	{
		// Test UniformFieldInfo through direct access using internal visibility
		UniformFieldInfo uniformInfo = new()
		{
			Location = 1,
			Name = "test",
			Size = 10,
			Type = Silk.NET.OpenGL.UniformType.Float
		};

		// Verify field types by accessing them directly
		Assert.AreEqual(1, uniformInfo.Location);
		Assert.AreEqual("test", uniformInfo.Name);
		Assert.AreEqual(10, uniformInfo.Size);
		Assert.AreEqual(Silk.NET.OpenGL.UniformType.Float, uniformInfo.Type);

		// Verify field types match expected types
		Assert.IsInstanceOfType<int>(uniformInfo.Location);
		Assert.IsInstanceOfType<string>(uniformInfo.Name);
		Assert.IsInstanceOfType<int>(uniformInfo.Size);
		Assert.IsInstanceOfType<Silk.NET.OpenGL.UniformType>(uniformInfo.Type);
	}

	[TestMethod]
	public void Shader_IsInternalClass()
	{
		Type shaderType = typeof(Shader);
		Assert.IsTrue(shaderType.IsClass, "Shader should be a class");
		Assert.IsFalse(shaderType.IsPublic, "Shader should not be public (internal)");
	}

	[TestMethod]
	public void Texture_IsInternalClass()
	{
		Type textureType = typeof(Texture);
		Assert.IsTrue(textureType.IsClass, "Texture should be a class");
		Assert.IsFalse(textureType.IsPublic, "Texture should not be public (internal)");
	}

	#endregion

	#region Interface Tests

	[TestMethod]
	public void IGL_IsInterface()
	{
		Type iglType = typeof(IGL);
		Assert.IsTrue(iglType.IsInterface, "IGL should be an interface");
		Assert.IsTrue(iglType.IsPublic, "IGL should be public");
	}

	[TestMethod]
	public void IGL_InheritsFromIDisposable()
	{
		Type iglType = typeof(IGL);
		Assert.IsTrue(typeof(IDisposable).IsAssignableFrom(iglType), "IGL should inherit from IDisposable");
	}

	[TestMethod]
	public void IOpenGLFactory_IsInterface()
	{
		Type factoryType = typeof(IOpenGLFactory);
		Assert.IsTrue(factoryType.IsInterface, "IOpenGLFactory should be an interface");
		Assert.IsTrue(factoryType.IsPublic, "IOpenGLFactory should be public");
	}

	[TestMethod]
	public void IOpenGLProvider_IsInterface()
	{
		Type providerType = typeof(IOpenGLProvider);
		Assert.IsTrue(providerType.IsInterface, "IOpenGLProvider should be an interface");
		Assert.IsTrue(providerType.IsPublic, "IOpenGLProvider should be public");
	}

	[TestMethod]
	public void IOpenGLProvider_InheritsFromIDisposable()
	{
		Type providerType = typeof(IOpenGLProvider);
		Assert.IsTrue(typeof(IDisposable).IsAssignableFrom(providerType), "IOpenGLProvider should inherit from IDisposable");
	}

	[TestMethod]
	public void GLWrapper_ImplementsIGL()
	{
		Type wrapperType = typeof(GLWrapper);
		Assert.IsTrue(typeof(IGL).IsAssignableFrom(wrapperType), "GLWrapper should implement IGL");
	}

	#endregion
}
