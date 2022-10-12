#include "stereokit.hlsli"

//--name = touchpanel
//--color:color           = 1,1,1,1
//--emission_factor:color = 0,0,0,0
//--metallic              = 0
//--roughness             = 1
//--tex_scale             = 1
//--uvXoffset = 0
//--uvYoffset = 0
//--glowAmount = 0.1
//--minValue = 0
//--maxValue = 0.5
//--xAmount = 0
//--yAmount = 0
//--uScale = 1;
//--vScale = 1;
//--buttonAmount = 1;
float4 color;
float4 emission_factor;
float metallic;
float roughness;
float tex_scale;
float uvXoffset;
float uvYoffset;
float glowAmount;
float minValue;
float maxValue;
float xAmount;
float yAmount;
float uScale;
float vScale;
int buttonAmount;

//--diffuse   = white
//--emission  = white
//--metal     = white
//--normal    = flat
//--occlusion = white
Texture2D diffuse : register(t0);
SamplerState diffuse_s : register(s0);
Texture2D emission : register(t1);
SamplerState emission_s : register(s1);
Texture2D metal : register(t2);
SamplerState metal_s : register(s2);
Texture2D normal : register(t3);
SamplerState normal_s : register(s3);
Texture2D occlusion : register(t4);
SamplerState occlusion_s : register(s4);

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
	//o.uv = ((input.uv + float2(-uvXoffset, -uvYoffset)) * float2(uScale, vScale) * tex_scale) * float2(1, -1);
	o.uv = (input.uv + float2(-uvXoffset, -uvYoffset)) * float2(uScale, vScale) * tex_scale;
	o.color = input.color * sk_inst[id].color * color;
	o.irradiance = Lighting(o.normal);
	o.view_dir = sk_camera_pos[o.view_id].xyz - o.world;
	return o;
}

float MipLevel(float ndotv)
{
	float2 dx = ddx(ndotv * sk_cubemap_i.x);
	float2 dy = ddy(ndotv * sk_cubemap_i.y);
	float delta = max(dot(dx, dx), dot(dy, dy));
	return 0.5 * log2(delta);
}

float3 FresnelSchlickRoughness(float cosTheta, float3 F0, float roughness)
{
	return F0 + (max(1 - roughness, F0) - F0) * pow(1.0 - cosTheta, 5.0);
}

// See: https://www.unrealengine.com/en-US/blog/physically-based-shading-on-mobile
float2 brdf_appx(half Roughness, half NoV)
{
	const half4 c0 = { -1, -0.0275, -0.572, 0.022 };
	const half4 c1 = { 1, 0.0425, 1.04, -0.04 };
	half4 r = Roughness * c0 + c1;
	half a004 = min(r.x * r.x, exp2(-9.28 * NoV)) * r.x + r.y;
	half2 AB = half2(-1.04, 1.04) * a004 + r.zw;
	return AB;
}

struct FingerDistStruct
{
	float from_finger;
	float on_plane;
};

FingerDistStruct FingerDistInfo(float3 world_pos, float3 world_norm)
{
	FingerDistStruct result;
	result.from_finger = 10000;
	result.on_plane = 10000;
	
	for (int i = 0; i < 2; i++)
	{
		float3 to_finger = sk_fingertip[i].xyz - world_pos;
		float d = dot(world_norm, to_finger);
		float3 on_plane = sk_fingertip[i].xyz + d * world_norm;

		// Also make distances behind the plane negative
		//float finger_dist = length(to_finger);
		//if (abs(result.from_finger) > finger_dist)
		//	//result.from_finger = 10000;
		//	result.from_finger = finger_dist * sign(d);
		
		if (d <= 0)
		{
			result.on_plane = 1;
		}
			result.on_plane = min(result.on_plane, length(world_pos - on_plane));
		}

	return result;
}

float drawButton(FingerDistStruct fingerInfo, float2 uv, float4 pos, float2 size, float radius, float thickness)
{

	float d = length(max(abs(uv - float2(pos.x, pos.y)), size) - size) - radius;
	float e = length(max(abs(uv - float2(pos.x, pos.y)), min(fingerInfo.on_plane.x, size) - 0.005) - (min(fingerInfo.on_plane.x, size) - 0.005)) - (radius - 0.005);
	//float e = length(max(abs(uv - float2(pos.x, pos.y)), size - 0.005) - (size - 0.005)) - (radius - 0.005);
	
	return smoothstep(0.55, 0.45, abs(d / thickness) * 5.0) + smoothstep(0.66, 0.33, e / thickness * 5.0);
}

float drawButtonTEST(FingerDistStruct fingerInfo, float2 uv, float4 pos, float2 size, float radius, float thickness)
{

	float d = length(max(abs(uv - float2(pos.x, pos.y)), size) - size) - radius;
	
	return smoothstep(0.55, 0.45, abs(d / thickness) * 5.0);
}

//float rectangle2(float2 uv, float2 pos, float2 size)
//{
//	return (step(pos.x, uv.x) - step(pos.x + size.x, uv.x))
//       * (step(pos.y - size.y, uv.y) - step(pos.y, uv.y));
//}

//float roundedRectangle(vec2 pos, vec2 size, float radius, float thickness)
//{
//	float d = length(max(abs(uv - pos), size) - size) - radius;
//	return smoothstep(0.66, 0.33, d / thickness * 5.0);
//}
//float FingerGlowExing(float3 world_pos, float3 world_norm, float2 texCoord)
//{
//	float dist = 0;
//	float dist2 = 0;
	
//	float tau = 3.1415926535 * 2.0;
	
//	float2 xy = (2.0 * texCoord.xy) - 1.0;

//	//if (texCoord.x > 0.9 & texCoord.y > 0.9)
//	//{
//	//	dist = 1;
//	//}
	
//	// {x ^ {2}}{a ^ {2}} + {y ^ {2}}{b ^ {2}}
	
//	//dist = ((texCoord.x * texCoord.x) / (0.01 * 0.01)) + ((texCoord.y * texCoord.y) / (0.01 * 0.01));
	
//	dist = ((pow((texCoord.x - 0.5), 2) / pow(0.05, 2)) + (pow((texCoord.y - 0.16), 2) / pow(0.05, 2))) <= 1;
	
//	dist2 = (0.7 + 0.5 * cos(xy.x * 10.0 * tau * 0.15 * clamp(floor(5.0 + 10.0 * cos(2)), 0.0, 10.0))) * abs(1.0 / (30.0 * xy.y));
	

	
//	//float ring = 0;
//	//for (int i = 0; i < 2; i++)
//	//{
//	//	float3 to_finger = sk_fingertip[i].xyz - world_pos;
//	//	float d = 1;
//	//	float3 on_plane = sk_fingertip[i].xyz - d * world_norm;
		
//	//	float xcoord = saturate(max(minValue, (maxValue - abs(texCoord.x - floor((on_plane.x - xAmount) * ((10))) - 0.5 - uScale / 2)) * 1000)) *
//	//				   saturate(max(minValue, (maxValue - abs(texCoord.y - floor((on_plane.z - yAmount) * ((10))) - 0.5 - vScale / 2)) * 1000));

//	//	//float dist_from_finger = length(to_finger);
//	//	//float dist_on_plane = length(world_pos - on_plane);
//	//	dist += xcoord;
//	//}

//	return dist;
//}

float2 FingerGlowExing(float3 world_pos, float3 world_norm)
{
	float dist = 1;
	float ring = 0;
	for (int i = 0; i < 2; i++)
	{
		float3 to_finger = sk_fingertip[i].xyz - world_pos;
		float d = dot(world_norm, to_finger);
		float3 on_plane = sk_fingertip[i].xyz - d * world_norm;

		float dist_from_finger = length(to_finger);
		float dist_on_plane = length(world_pos - on_plane);
		ring = max(ring, saturate(1 - abs(d * 0.5 - dist_on_plane) * 600));
		dist = min(dist, dist_from_finger * 2);
	}

	return float2(dist, ring);
}

float FingerGlowing(float3 world_pos, float3 world_norm)
{
	float2 glow = FingerGlowExing(world_pos, world_norm);
	glow.x = pow(saturate(1 - glow.x / 0.12), 2);
	return (glow.x * 0.2) + (glow.y * glow.x);
}

float4 ps(psIn input) : SV_TARGET
{
	FingerDistStruct fingerDistance = FingerDistInfo(input.world.xyz, input.normal);
	
	float buttons = 0;
	
	for (uint i = 0; i < buttonAmount; i++)
	{
		buttons += drawButton(fingerDistance, input.uv, button[i], 0.03, 0.01, 0.025);
	}
	
	float4 albedo = diffuse.Sample(diffuse_s, input.uv) * input.color;
	float3 emissive = emission.Sample(emission_s, input.uv).rgb * emission_factor.rgb;
	float2 metal_rough = metal.Sample(metal_s, input.uv).gb; // b is metallic, rough is g
	float ao = occlusion.Sample(occlusion_s, input.uv).r; // occlusion is sometimes part of the metal tex, uses r channel

	float3 view = normalize(input.view_dir);
	float3 reflection = reflect(-view, input.normal);
	float ndotv = max(0, dot(input.normal, view));

	float metallic_final = metal_rough.y * metallic;
	float rough_final = metal_rough.x * roughness;

	float3 F0 = 0.04;
	F0 = lerp(F0, albedo.rgb, metallic_final);
	float3 F = FresnelSchlickRoughness(ndotv, F0, rough_final);
	float3 kS = F;

	float mip = (1 - pow(1 - rough_final, 2)) * sk_cubemap_i.z;
	mip = max(mip, MipLevel(ndotv));
	float3 prefilteredColor = sk_cubemap.SampleLevel(sk_cubemap_s, reflection, mip).rgb;
	float2 envBRDF = brdf_appx(rough_final, ndotv);
	float3 specular = prefilteredColor * (F * envBRDF.x + envBRDF.y);

	float3 kD = 1 - kS;
	kD *= 1.0 - metallic_final;

	float3 diffuse = albedo.rgb * input.irradiance * ao;
	float3 color = (kD * diffuse + specular * ao);
	
	//glow1 += FingerGlowing(input.world.xyz, input.normal);
	
	//glow1 += dot(fingerDistance.on_plane, float2(button[0].x, button[0].y));

	float4 col = float4(lerp(color.rgb, float3(255, 255, 255), buttons.rrr), input.color.a);
	
	return float4(color + emissive, albedo.a) * col;
}