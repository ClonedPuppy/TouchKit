#include <stereokit.hlsli>

//--name = app/floor

//--color:color = 0,0,0,1
float4 color;
//--radius      = 5,10,0,0
float4 radius;
//--iTime		= 0.1
float iTime;

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

psIn vs(vsIn input, uint id : SV_InstanceID) {
	psIn o;
	o.view_id = id % sk_view_count;
	id        = id / sk_view_count;

	o.world = mul(input.pos, sk_inst[id].world);
	o.pos   = mul(o.world,   sk_viewproj[o.view_id]);
	
	o.uv = input.uv;

	return o;
}

float4 ps(psIn input) : SV_TARGET{

	float2 p = (input.uv);
	float tau = 3.1415926535 * 2.0;
	float a = atan2(p.x, p.y);
	float r = length(p) * 0.75;
	float2 xy = input.uv;
	
	////get the color
	//float xCol = (xy.x - (iTime / 3.0)) * 3.0;
	//xCol = fmod(xCol, 3.0);
	float3 horColour = float3(0.25, 0, 0);
	
	//if (xCol < 1.0)
	//{
		
	//	horColour.r += 1.0 - xCol;
	//	horColour.g += xCol;
	//}
	//else if (xCol < 2.0)
	//{
		
	//	xCol -= 1.0;
	//	horColour.g += 1.0 - xCol;
	//	horColour.b += xCol;
	//}
	//else
	//{
		
	//	xCol -= 2.0;
	//	horColour.b += 1.0 - xCol;
	//	horColour.r += xCol;
	//}

	// draw color beam
	//xy = (2.0 * input.uv) - 1.0;
	//float beamWidth = (0.7 + 0.5 * cos(xy.x * 10.0 * tau * 0.15 * clamp(floor(5.0 + 10.0 * cos(iTime)), 0.0, 10.0))) * abs(1.0 / (30.0 * xy.y));
	//float3 horBeam = float3(beamWidth, beamWidth, beamWidth);
	//float4 fragColor = float4(((horBeam) * horColour), 1.0);
	
		// draw color beam
	//xy = (2.0 * input.uv) - 1.0;
	float beamWidth = (0.7 + 0.5 * cos(input.uv.x * 10.0 * tau * 0.15 * clamp(floor(5.0 + 10.0 * cos(2)), 0.0, 10.0))) * abs(1.0 / (30.0 * input.uv.y));
	float3 horBeam = float3(beamWidth, beamWidth, beamWidth);
	
	return float4(((horBeam) * horColour), 1.0);
}

