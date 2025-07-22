// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp.Test;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using ktsu.ImGuiApp.ImGuiController;
using System.Reflection;

[TestClass]
public class UtilityClassTests
{
	[TestMethod]
	public void UniformFieldInfo_IsStruct()
	{
		Type type = typeof(UniformFieldInfo);
		Assert.IsTrue(type.IsValueType);
		Assert.IsFalse(type.IsClass);
	}

	[TestMethod]
	public void UniformFieldInfo_HasExpectedFields()
	{
		Type type = typeof(UniformFieldInfo);

		FieldInfo? locationField = type.GetField("Location");
		FieldInfo? nameField = type.GetField("Name");
		FieldInfo? sizeField = type.GetField("Size");
		FieldInfo? typeField = type.GetField("Type");

		Assert.IsNotNull(locationField);
		Assert.IsNotNull(nameField);
		Assert.IsNotNull(sizeField);
		Assert.IsNotNull(typeField);

		Assert.AreEqual(typeof(int), locationField.FieldType);
		Assert.AreEqual(typeof(string), nameField.FieldType);
		Assert.AreEqual(typeof(int), sizeField.FieldType);
		Assert.AreEqual(typeof(Silk.NET.OpenGL.UniformType), typeField.FieldType);
	}

	[TestMethod]
	public void Shader_IsInternalClass()
	{
		Type shaderType = typeof(Shader);
		Assert.IsTrue(shaderType.IsClass);
		Assert.IsFalse(shaderType.IsPublic);
	}

	[TestMethod]
	public void Texture_IsInternalClass()
	{
		Type textureType = typeof(Texture);
		Assert.IsTrue(textureType.IsClass);
		Assert.IsFalse(textureType.IsPublic);
	}

	[TestMethod]
	public void IGL_IsInterface()
	{
		Type iglType = typeof(IGL);
		Assert.IsTrue(iglType.IsInterface);
		Assert.IsTrue(iglType.IsPublic);
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
	public void GLWrapper_ImplementsIGL()
	{
		Type wrapperType = typeof(GLWrapper);
		Assert.IsTrue(typeof(IGL).IsAssignableFrom(wrapperType));
	}

	[TestMethod]
	public void OpenGLProvider_ImplementsIDisposable()
	{
		Type providerType = typeof(OpenGLProvider);
		Assert.IsTrue(typeof(IDisposable).IsAssignableFrom(providerType));
	}

	[TestMethod]
	public void WindowOpenGLFactory_ImplementsIOpenGLFactory()
	{
		Type factoryType = typeof(WindowOpenGLFactory);
		Assert.IsTrue(typeof(IOpenGLFactory).IsAssignableFrom(factoryType));
	}

	[TestMethod]
	public void ImGuiFontConfig_IsStruct()
	{
		Type configType = typeof(ImGuiFontConfig);
		Assert.IsTrue(configType.IsValueType);
		Assert.IsFalse(configType.IsClass);
	}

	[TestMethod]
	public void ImGuiFontConfig_ImplementsIEquatable()
	{
		Type configType = typeof(ImGuiFontConfig);
		Type equatableType = typeof(IEquatable<ImGuiFontConfig>);
		Assert.IsTrue(equatableType.IsAssignableFrom(configType));
	}

	[TestMethod]
	public void TextureCoordinate_IsEnum()
	{
		Type coordType = typeof(TextureCoordinate);
		Assert.IsTrue(coordType.IsEnum);
		Assert.AreEqual(typeof(int), Enum.GetUnderlyingType(coordType));
	}

	[TestMethod]
	public void Util_IsStaticClass()
	{
		Type utilType = typeof(Util);
		Assert.IsTrue(utilType.IsAbstract && utilType.IsSealed);
	}

	[TestMethod]
	public void ImGuiApp_IsStaticClass()
	{
		Type appType = typeof(ImGuiApp);
		Assert.IsTrue(appType.IsAbstract && appType.IsSealed);
	}

	[TestMethod]
	public void ImGuiAppTextureInfo_IsClass()
	{
		Type textureInfoType = typeof(ImGuiAppTextureInfo);
		Assert.IsTrue(textureInfoType.IsClass);
		Assert.IsTrue(textureInfoType.IsPublic);
	}

	[TestMethod]
	public void ImGuiAppWindowState_IsClass()
	{
		Type windowStateType = typeof(ImGuiAppWindowState);
		Assert.IsTrue(windowStateType.IsClass);
		Assert.IsTrue(windowStateType.IsPublic);
	}

	[TestMethod]
	public void ImGuiAppConfig_IsClass()
	{
		Type configType = typeof(ImGuiAppConfig);
		Assert.IsTrue(configType.IsClass);
		Assert.IsTrue(configType.IsPublic);
	}

	[TestMethod]
	public void ImGuiAppPerformanceSettings_IsClass()
	{
		Type settingsType = typeof(ImGuiAppPerformanceSettings);
		Assert.IsTrue(settingsType.IsClass);
		Assert.IsTrue(settingsType.IsPublic);
	}
}