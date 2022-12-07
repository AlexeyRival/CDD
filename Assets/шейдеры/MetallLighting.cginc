// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

#if !defined(MY_LIGHTING_INCLUDED)
#define MY_LIGHTING_INCLUDED

#include "AutoLight.cginc"
#include "UnityPBSLighting.cginc"
#include "Packages/jp.keijiro.noiseshader/Shader/SimplexNoise3D.hlsl"

float4 _Tint;
sampler2D _MainTex;
float4 _MainTex_ST;

sampler3D _biomeTex;

float _Metallic;
float _Smoothness;
float _Scale;

struct VertexData {
	float4 position : POSITION;
	float3 worldPos: TEXCOORD2;
	float3 normal : NORMAL;
	float2 uv : TEXCOORD0;
};

struct Interpolators {
	float4 position : SV_POSITION;
	float2 uv : TEXCOORD0;
	float3 normal : TEXCOORD1;
	float3 worldPos : TEXCOORD2;

	#if defined(VERTEXLIGHT_ON)
		float3 vertexLightColor : TEXCOORD3;
	#endif
};

void ComputeVertexLightColor (inout Interpolators i) {
	#if defined(VERTEXLIGHT_ON)
		i.vertexLightColor = Shade4PointLights(
			unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
			unity_LightColor[0].rgb, unity_LightColor[1].rgb,
			unity_LightColor[2].rgb, unity_LightColor[3].rgb,
			unity_4LightAtten0, i.worldPos, i.normal
		);
	#endif
}

Interpolators MyVertexProgram (VertexData v) {
	Interpolators i;

	v.worldPos = mul(unity_ObjectToWorld, v.position);
	float multiplier = 1/3.1415;
	float x = sin(v.worldPos.x*SimplexNoise(v.worldPos * multiplier));
	float y = cos(v.worldPos.y * SimplexNoise(v.worldPos * multiplier));
	float z = - cos(v.worldPos.z * SimplexNoise(v.worldPos * multiplier));
	v.position += float4(x,y,z,0)*0.01*_Scale;

	i.position = UnityObjectToClipPos(v.position);
	i.worldPos = mul(unity_ObjectToWorld, v.position);
	i.normal = UnityObjectToWorldNormal(v.normal);
	i.uv = TRANSFORM_TEX(v.uv, _MainTex);
	ComputeVertexLightColor(i);
	return i;
}

UnityLight CreateLight (Interpolators i) {
	UnityLight light;

	#if defined(POINT) || defined(POINT_COOKIE) || defined(SPOT)
		light.dir = normalize(_WorldSpaceLightPos0.xyz - i.worldPos);
	#else
		light.dir = _WorldSpaceLightPos0.xyz;
	#endif
	
	UNITY_LIGHT_ATTENUATION(attenuation, 0, i.worldPos);
	light.color = _LightColor0.rgb * attenuation;
	light.ndotl = DotClamped(i.normal, light.dir);
	return light;
}

UnityIndirect CreateIndirectLight (Interpolators i) {
	UnityIndirect indirectLight;
	indirectLight.diffuse = 0;
	indirectLight.specular = 0;

	#if defined(VERTEXLIGHT_ON)
		indirectLight.diffuse = i.vertexLightColor;
	#endif

	#if defined(FORWARD_BASE_PASS)
		indirectLight.diffuse += max(0, ShadeSH9(float4(i.normal, 1)));
	#endif

	return indirectLight;
}


float4 MyFragmentProgram (Interpolators i) : SV_TARGET {
	i.normal = normalize(i.normal);

	float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);


	float2 step;
	step.x = sin(i.worldPos.x / 20) + (1 - cos(i.worldPos.y / 20)) / 2;
	step.y = cos(i.worldPos.z / 20) + (1 - cos(i.worldPos.y / 20)) / 2;

	float noise = SimplexNoise(i.worldPos / 10);//20
	float fnoise =  SimplexNoise(-i.worldPos / 4);//4
	float3 normal = i.normal;
	
	float albedo = tex2D(_MainTex, i.uv).rgb * _Tint.rgb;
	

	
	//
	

	float3 specularTint;
	float oneMinusReflectivity;
	albedo = DiffuseAndSpecularFromMetallic(
		albedo, _Metallic, specularTint, oneMinusReflectivity
	);

	return UNITY_BRDF_PBS(
		albedo, specularTint,
		oneMinusReflectivity, _Smoothness,
		normal, viewDir,
		CreateLight(i), CreateIndirectLight(i)
	);
}

#endif