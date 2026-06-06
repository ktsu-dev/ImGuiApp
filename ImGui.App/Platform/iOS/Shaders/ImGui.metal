// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

// Dear ImGui Metal shader, ported from the upstream imgui_impl_metal.mm reference.
// Compiled at runtime from this source via IMTLDevice.CreateLibrary(...) (see
// MetalRendererBackend). Vertices arrive at buffer(0) described by an MTLVertexDescriptor
// matching ImDrawVert (pos: float2 @0, uv: float2 @8, col: uchar4 @16, stride 20); the
// orthographic projection matrix is pushed inline at buffer(1) via SetVertexBytes.

#include <metal_stdlib>
using namespace metal;

struct Uniforms
{
	float4x4 projectionMatrix;
};

struct VertexIn
{
	float2 position [[attribute(0)]];
	float2 texCoords [[attribute(1)]];
	uchar4 color [[attribute(2)]];
};

struct VertexOut
{
	float4 position [[position]];
	float2 texCoords;
	float4 color;
};

vertex VertexOut imgui_vertex(VertexIn in [[stage_in]],
                              constant Uniforms& uniforms [[buffer(1)]])
{
	VertexOut out;
	out.position = uniforms.projectionMatrix * float4(in.position, 0.0, 1.0);
	out.texCoords = in.texCoords;
	out.color = float4(in.color) / float4(255.0);
	return out;
}

fragment half4 imgui_fragment(VertexOut in [[stage_in]],
                              texture2d<half, access::sample> tex [[texture(0)]],
                              sampler texSampler [[sampler(0)]])
{
	half4 texColor = tex.sample(texSampler, in.texCoords);
	return half4(in.color) * texColor;
}
