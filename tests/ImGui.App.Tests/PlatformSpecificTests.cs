// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.App.Test;

using Microsoft.VisualStudio.TestTools.UnitTesting;

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

		Assert.IsTrue(scaleFactor > 0);
		Assert.IsTrue(scaleFactor <= 10.25); // MaxScaleFactor
	}

	[TestMethod]
	public void ForceDpiAware_GetActualScaleFactor_ReturnsValidValue()
	{
		double actualScale = ForceDpiAware.GetActualScaleFactor();

		Assert.IsTrue(actualScale > 0);
	}

	#endregion

	#region GdiPlusHelper Tests

	[TestMethod]
	public void GdiPlusHelper_IsStaticClass()
	{
		Type gdiPlusHelperType = typeof(GdiPlusHelper);
		Assert.IsTrue(gdiPlusHelperType.IsAbstract && gdiPlusHelperType.IsSealed);
	}

	#endregion

	#region NativeMethods Tests

	[TestMethod]
	public void NativeMethods_IsStaticClass()
	{
		Type nativeMethodsType = typeof(NativeMethods);
		Assert.IsTrue(nativeMethodsType.IsAbstract && nativeMethodsType.IsSealed);
	}

	[TestMethod]
	public void NativeMethods_IsInternalClass()
	{
		Type nativeMethodsType = typeof(NativeMethods);
		Assert.IsTrue(nativeMethodsType.IsClass);
		Assert.IsFalse(nativeMethodsType.IsPublic);
	}

	#endregion

	#region Type System Tests

	[TestMethod]
	public void UniformFieldInfo_IsStruct()
	{
		Type type = typeof(ImGuiController.UniformFieldInfo);
		Assert.IsTrue(type.IsValueType);
		Assert.IsFalse(type.IsClass);
	}

	[TestMethod]
	public void UniformFieldInfo_HasExpectedFields()
	{
		// Test UniformFieldInfo through direct access using internal visibility
		ImGuiController.UniformFieldInfo uniformInfo = new()
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
		Type shaderType = typeof(ImGuiController.Shader);
		Assert.IsTrue(shaderType.IsClass);
		Assert.IsFalse(shaderType.IsPublic);
	}

	[TestMethod]
	public void Texture_IsInternalClass()
	{
		Type textureType = typeof(ImGuiController.Texture);
		Assert.IsTrue(textureType.IsClass);
		Assert.IsFalse(textureType.IsPublic);
	}

	#endregion

	#region Interface Tests

	[TestMethod]
	public void IGL_IsInterface()
	{
		Type iglType = typeof(ImGuiController.IGL);
		Assert.IsTrue(iglType.IsInterface);
		Assert.IsTrue(iglType.IsPublic);
	}

	[TestMethod]
	public void IGL_InheritsFromIDisposable()
	{
		Type iglType = typeof(ImGuiController.IGL);
		Assert.IsTrue(typeof(IDisposable).IsAssignableFrom(iglType));
	}

	[TestMethod]
	public void IOpenGLFactory_IsInterface()
	{
		Type factoryType = typeof(IOpenGLFactory);
		Assert.IsTrue(factoryType.IsInterface);
		Assert.IsTrue(factoryType.IsPublic);
	}

	[TestMethod]
	public void IOpenGLProvider_IsInterface()
	{
		Type providerType = typeof(IOpenGLProvider);
		Assert.IsTrue(providerType.IsInterface);
		Assert.IsTrue(providerType.IsPublic);
	}

	[TestMethod]
	public void IOpenGLProvider_InheritsFromIDisposable()
	{
		Type providerType = typeof(IOpenGLProvider);
		Assert.IsTrue(typeof(IDisposable).IsAssignableFrom(providerType));
	}

	[TestMethod]
	public void GLWrapper_ImplementsIGL()
	{
		Type wrapperType = typeof(ImGuiController.GLWrapper);
		Assert.IsTrue(typeof(ImGuiController.IGL).IsAssignableFrom(wrapperType));
	}

	#endregion
}
