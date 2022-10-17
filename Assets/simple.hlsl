#include <stereokit.hlsli>
#include <stereokit_pbr.hlsli>

//--name					= simple

//--color:color				= 1, 1, 1, 1
//--emission_factor:color	= 0,0,0,0
//--metallic				= 1
//--roughness				= 1
float4 color;
float4 emission_factor;
float metallic;
float roughness;

//--tex_scale				= 1
//--uvXoffset				= 0
//--uvYoffset				= 0
//--uScale					= 1
//--vScale					= 1
//--buttonAmount			= 1
float tex_scale;
float uvXoffset;
float uvYoffset;
float uScale;
float vScale;
int buttonAmount;

//--diffuse					= white
//--emission				= white
//--metal					= white
//--normal					= flat
Texture2D diffuse : register(t0);
SamplerState diffuse_s : register(s0);
Texture2D emission : register(t1);
SamplerState emission_s : register(s1);
Texture2D metal : register(t2);
SamplerState metal_s : register(s2);
Texture2D normal : register(t3);
SamplerState normal_s : register(s3);

float4 button[20];
float4 slider[20];

struct vsIn
{
	float4 pos : SV_Position;
	float3 norm : NORMAL0;
	float2 uv : TEXCOORD0;
	float4 color : COLOR0;
};
struct psIn
{
	float4 pos : SV_POSITION;
	float3 normal : NORMAL0;
	float2 uv : TEXCOORD0;
	float4 color : COLOR0;
	float3 irradiance : COLOR1;
	float3 world : TEXCOORD1;
	float3 view_dir : TEXCOORD2;
	uint view_id : SV_RenderTargetArrayIndex;
};

psIn vs(vsIn input, uint id : SV_InstanceID)
{
	psIn o;
	o.view_id = id % sk_view_count;
	id = id / sk_view_count;

	o.world = mul(float4(input.pos.xyz, 1), sk_inst[id].world).xyz;
	o.pos = mul(float4(o.world, 1), sk_viewproj[o.view_id]);

	o.normal = normalize(mul(float4(input.norm, 0), sk_inst[id].world).xyz);
	o.uv = (input.uv + float2(-uvXoffset, -uvYoffset)) * float2(uScale, vScale) * tex_scale;
	o.color = input.color * sk_inst[id].color * color;
	o.irradiance = Lighting(o.normal);
	o.view_dir = sk_camera_pos[o.view_id].xyz - o.world;
	return o;
}

struct FingerDist2
{
	float from_finger;
	float on_plane;
};

FingerDist2 FingerDistanceInfo2(float3 world_pos, float3 world_norm)
{
	FingerDist2 result;
	result.from_finger = 10000;
	result.on_plane = 10000;
	
	for (int i = 0; i < 2; i++)
	{
		float3 to_finger = sk_fingertip[i].xyz - world_pos;
		float d = dot(world_norm, to_finger);
		float3 on_plane = sk_fingertip[i].xyz + d * world_norm;
		
		if (d <= 0)
		{
			result.on_plane = 1;
		}
		result.on_plane = min(result.on_plane, length(world_pos - on_plane));
	}

	return result;
}

float3 drawButton(FingerDist2 fingerInfo, float2 uv, float4 pos, float2 size, float radius, float thickness)
{

	float d = length(max(abs(uv - float2(pos.x, pos.y)), size) - size) - radius;
	float e = length(max(abs(uv - float2(pos.x, pos.y)), min(fingerInfo.on_plane.x, size) - 0.005) - (min(fingerInfo.on_plane.x, size) - 0.005)) - (radius - 0.005);
	
	float result = smoothstep(0.55, 0.45, abs(d / thickness) * 5.0) + smoothstep(0.66, 0.33, e / thickness * 5.0);
	
	return float3(result, result * pos.w, result * pos.z);
}

float4 ps(psIn input) : SV_TARGET
{
	float4 albedo = diffuse.Sample(diffuse_s, input.uv) * input.color;
	float3 emissive = emission.Sample(emission_s, input.uv).rgb * emission_factor.rgb;
	float2 metal_rough = metal.Sample(metal_s, input.uv).gb; // rough is g, b is metallic
	float ao = metal.Sample(metal_s, input.uv).r; // occlusion is sometimes part of the metal tex, uses r channel
	

	
	FingerDist2 fingerDistance = FingerDistanceInfo2(input.world.xyz, input.normal);
	
	float3 buttons = float3(0, 0, 0);
	
	for (uint i = 0; i < buttonAmount; i++)
	{
		buttons += drawButton(fingerDistance, input.uv, button[i], 0.03, 0.005, 0.025);
	}
	
	float metallic_final = lerp(metal_rough.y * metallic, buttons.b, buttons.r);
	float rough_final = lerp(metal_rough.x * roughness, buttons.g, buttons.r);
	
	//albedo = float4(lerp(albedo.rgb, float3(1, 1, 1), buttons.rrr), albedo.a);
	
	albedo = float4(albedo.rgb + buttons.rrr, albedo.a);
	
	float4 color = skpbr_shade(albedo, input.irradiance, ao, metallic_final, rough_final, input.view_dir, input.normal);
	
	color.rgb += emissive;
	
	return color;
}