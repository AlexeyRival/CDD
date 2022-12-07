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
	float3 color: COLOR;
	float2 uv : TEXCOORD0;
};

struct Interpolators {
	float4 position : SV_POSITION;

	half3 worldNormal : TEXCOORD0; 
	float2 uv : TEXCOORD3;
	float3 normal : TEXCOORD1;
	float3 worldPos : TEXCOORD2;
	float color : COLOR;
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
	i.worldNormal = UnityObjectToWorldNormal(v.normal);
	i.normal = UnityObjectToWorldNormal(v.normal);
	i.uv = TRANSFORM_TEX(v.uv, _MainTex);
	i.color = v.color;
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

float4 MyOreFragment (Interpolators i) : SV_TARGET {
	i.normal = normalize(i.normal);

	float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);


	float2 step;
	step.x = sin(i.worldPos.x / 20) + (1 - cos(i.worldPos.y / 20)) / 2;
	step.y = cos(i.worldPos.z / 20) + (1 - cos(i.worldPos.y / 20)) / 2;

	float noise = SimplexNoise(i.worldPos / 10);//20
	float fnoise =  SimplexNoise(-i.worldPos / 4);//4
	//float output = clamp(floor((noise*0.6+fnoise*0.3)* 3 + 1.5), 0.2, 0.9);

	//float output = (((noise*0.6+fnoise*0.3)* 3 + 1.5));
	//fixed4 pixelColor = tex2D(_MainTexture, step);
	float multiplier = 10;
	 //прекраснейшие инфузории!
	float x = (sin(i.worldPos.x * SimplexNoise(i.worldPos* multiplier)* SimplexNoise(i.worldPos * multiplier)));
	float y = (cos(i.worldPos.y * SimplexNoise(i.worldPos * multiplier)* SimplexNoise(i.worldPos * multiplier)));
	float z = 1 - (cos(i.worldPos.z * SimplexNoise(i.worldPos * multiplier)* SimplexNoise(i.worldPos * multiplier)));

	float3 thirdvector = float3(x, y, z);
	float3 normal = i.normal+(1>0?(thirdvector/15):0);
	


	//float output = clamp(((noise * 0.6 + fnoise * 0.3) * 3 + 1.5 + SimplexNoise(i.worldPos+thirdvector)), 0, 0.9);
	float output = SimplexNoise(i.worldPos/10);
	float3 albedo = tex2D(_MainTex, step).rgb * output * _Tint.rgb;

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

float4 MyFragmentProgram (Interpolators i) : SV_TARGET {
	i.normal = normalize(i.normal);

	float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);


	float2 step;
	step.x = sin(i.worldPos.x / 20) + (1 - cos(i.worldPos.y / 20)) / 2;
	step.y = cos(i.worldPos.z / 20) + (1 - cos(i.worldPos.y / 20)) / 2;

	float noise = SimplexNoise(i.worldPos / 10);//20
	float fnoise =  SimplexNoise(-i.worldPos / 4);//4
	//float output = clamp(floor((noise*0.6+fnoise*0.3)* 3 + 1.5), 0.2, 0.9);

	//float output = (((noise*0.6+fnoise*0.3)* 3 + 1.5));
	//fixed4 pixelColor = tex2D(_MainTexture, step);
	float multiplier = 10;
	/* //прекраснейшие инфузории!
	float x = (sin(i.worldPos.x * SimplexNoise(i.worldPos* multiplier)* SimplexNoise(i.worldPos * multiplier)));
	float y = (cos(i.worldPos.y * SimplexNoise(i.worldPos * multiplier)* SimplexNoise(i.worldPos * multiplier)));
	float z = 1 - (cos(i.worldPos.z * SimplexNoise(i.worldPos * multiplier)* SimplexNoise(i.worldPos * multiplier)));

	float3 thirdvector = float3(x, y, z);
	float3 normal = i.normal+(1>0?(thirdvector/15):0);*/
	
	float x = ((i.worldPos.x * SimplexNoise(i.worldPos* multiplier)));
	float y = ((i.worldPos.y * SimplexNoise(i.worldPos * multiplier)));
	float z = 1 - ((i.worldPos.z * SimplexNoise(i.worldPos * multiplier)));
	x *= x;
	y *= y;
	z *= z;
	float3 thirdvector = float3(clamp(x,-1,1),clamp(y,-1,1),clamp(z,-1,1));
	
	float metallic = _Metallic;
	float smoosy = _Smoothness;
	//normal = i.normal*(thirdvector>0?(thirdvector/10):1);
	//float3 normal = i.normal +(1 > 0 ? (thirdvector / 20) : 0);
	//float fff = SimplexNoise(i.worldPos * 0.0001);
	float3 normal = i.normal;// +float3(sin(fff), cos(fff), -cos(fff)) * 0.1;
	
	bool isPosValid = i.worldPos.x > 0 && i.worldPos.y > 0 && i.worldPos.z > 0;
	float3 biome = isPosValid?tex3D(_biomeTex,float3(i.worldPos.x/300,i.worldPos.y/120,i.worldPos.z/300)):float3(0,0,0);
	float3 albedo;
	
	//float output = clamp(floor((noise * 0.6 + fnoise * 0.3) * 3 + 1.5 + SimplexNoise(i.worldPos + thirdvector)), 0, 1);
	float nn = SimplexNoise(i.worldPos/8);
	float output = clamp(floor((nn)+1+ abs(SimplexNoise(i.worldPos/4 + thirdvector))), 0, 1); 

	//normal *= output*0.6;
	//if (output < 0.7) 
	if (output < 0.7) 
	{
		metallic = .25;
		smoosy = .45;
		multiplier = .1;

		float scale = 1;//10
		x = i.worldPos.x%scale;// / 1200;
		y = i.worldPos.y%scale;// / 2;
		z = i.worldPos.z%scale;// / 1200;
		//output = float3(x, y, z);
		thirdvector = float3(x, y, z);
		
		float nk = .3;
		float th = .1;
		nn = SimplexNoise(i.worldPos *10);
		float p = .3; 
		if (
			(x%.15<2 - abs(nn) * nk *abs((y/p)-floor((y/p)+.5))+th&&
			x% .15 - abs(nn) * nk >2*abs((y/p)-floor((y/p)+.5))-th)||
			(y% .15 <2 - abs(nn) * nk *abs((z/p)-floor((z/p)+.5))+th&&
			y% .15 - abs(nn) * nk >2*abs((z/p)-floor((z/p)+.5))-th)||
			(z% .15 <2 - abs(nn) * nk *abs((y/p)-floor((y/p)+.5))+th&&
			z% .15 - abs(nn) * nk >2*abs((y/p)-floor((y/p)+.5))-th)
			//y%1==2*abs(z/p-floor(z/p+.5))||
			//z%1==2*abs(x/p-floor(x/p+.5))
			) {
			float power = 0.01;//0.00012
			float multiplier = 20;
			float h1 = (x + y + z) / (3 * scale);
			float h2 = ((x)+(y - power) + (z)) / (3 * scale);
			//output = 1;
			normal = normalize(float3(normal.x + (h1 - h2) * multiplier, normal.y + (h1 - h2) * multiplier, normal.z + (h1 - h2) * multiplier));
		}
		//normal = normalize(float3(normal.x + (h1 - h2) * multiplier, normal.y + (h1 - h2) * multiplier, normal.z + (h1 - h2) * multiplier));
		//normal = i.normal + (1 > 0 ? (thirdvector / 15) : 0);
	}
	else 
	{

		float fff = abs(SimplexNoise(i.worldPos /2));
		float fafa = abs(SimplexNoise(i.worldPos * .125));
		float bbb = clamp(SimplexNoise(i.worldPos * .0625) + .5, 0, 1);
		float s = fff + fafa + bbb;
		if (s > 0 && s < 1.5)
		{
			output = clamp(1/pow(1.5, s),0,1);

			float scale = 3;//10
			float power = 0.0006;//0.00012
			float multiplier = 10.5;
			float h1 = 1-abs(SimplexNoise(i.worldPos * scale))*0.8 + abs(SimplexNoiseGrad(i.worldPos * 20))*.2;
			float h2 = 1-abs(SimplexNoise((i.worldPos - float3(power, power, power)) * scale))*.8+ abs(SimplexNoiseGrad((i.worldPos - float3(0.00005, 0.00005, 0.00005)) * 20))*.2;
			normal = normalize(float3(normal.x + (h1 - h2) * multiplier, normal.y + (h1 - h2) * multiplier, normal.z + (h1 - h2) * multiplier));
			//normal = (normal * s);
		//	albedo = float3(1, 0, 1);
		}
		else
		{
			float scale = 20;//10
			float power = 0.00005;//0.00012
			float multiplier = 4.5;
			float h1 = abs(SimplexNoiseGrad(i.worldPos * scale));
			float h2 = abs(SimplexNoiseGrad((i.worldPos - float3(power, power, power)) * scale));
			//float h2 = abs(SimplexNoise((i.worldPos - (normal*0.0001)) * 8));
			//albedo += float3(1, 1, 1) * (h1 - h2) *multiplier;
			//normal = normalize(normal / (abs(h1 - h2)));
			//normal = normalize(float3(normal.x - (h1 - h2) * 10,normal.y - (h1 - h2) * 10, normal.z - (h1 - h2) * 10));
			normal = normalize(float3(normal.x + (h1 - h2) * multiplier, normal.y + (h1 - h2) * multiplier, normal.z + (h1 - h2) * multiplier));
		}
	//	normal = normalize(float3((normal.x * (0.5 + output)), (normal.y * (0.5 - output)), (normal.z * (output))));
	}
	float coef = biome.x + biome.y + biome.z;
	if (coef!=0) {
		if(metallic < .25 * coef)metallic = .25*coef;
		if(smoosy < .45*coef)smoosy = .45*coef;
		albedo = tex2D(_MainTex, step).rgb *  _Tint.rgb* (output * (1-coef)+biome* (coef));
	}
	else {
		albedo = tex2D(_MainTex, step).rgb * output * _Tint.rgb;
	}

	albedo *= i.color;
	if(coef > 0.3){
		float fff = abs(SimplexNoise(i.worldPos * 4));
		float fafa = abs(SimplexNoise(i.worldPos * .5));
		float bbb = clamp(SimplexNoise(i.worldPos * .125) + .5, 0, 1);
		float s = fff + fafa + bbb;
		if (s > 1 && s < 1.01)
		{
			albedo = biome*coef;
		}
	}
	/*if (output > 0)
	{//ЛАВА
		float fff = abs(SimplexNoise(i.worldPos / 2));
		float fafa = abs(SimplexNoise(i.worldPos * .125));
		float bbb = clamp(SimplexNoise(i.worldPos * .0625) + .5, 0, 1);
		float s = fff + fafa + bbb;
		if (s>0&&s<1.5) {
			albedo = float3(clamp(fff / fafa,-100,5), 0, 0);
		}
	}*/
	
	/*if (output > 0) 
	{ //Штуки КРИСТАЛЛИЧЕСКИЕ
		float fff = abs(SimplexNoise(i.worldPos * 4));
		float fafa = abs(SimplexNoise(i.worldPos * .5));
		float bbb = clamp(SimplexNoise(i.worldPos * .125) + .5,0,1);
		albedo = float3(fff, fafa, bbb);
	}*/
	if (output > 0) 
	{
	}
	//albedo = normal;
	//albedo = i.position*0.0625;
	//
	

	float3 specularTint;
	float oneMinusReflectivity;
	albedo = DiffuseAndSpecularFromMetallic(
		albedo, metallic, specularTint, oneMinusReflectivity
	);

	return UNITY_BRDF_PBS(
		albedo, specularTint,
		oneMinusReflectivity, smoosy,
		normal, viewDir,
		CreateLight(i), CreateIndirectLight(i)
	);
}

#endif