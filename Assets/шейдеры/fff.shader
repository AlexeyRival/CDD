Shader "Custom/fff"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #include "Packages/jp.keijiro.noiseshader/Shader/SimplexNoise3D.hlsl"
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
            float3 normal;
            float3 worldNormal; INTERNAL_DATA
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {

            float3 viewDir = normalize(_WorldSpaceCameraPos - IN.worldPos);


            float2 step;
            step.x = sin(IN.worldPos.x / 20) + (1 - cos(IN.worldPos.y / 20)) / 2;
            step.y = cos(IN.worldPos.z / 20) + (1 - cos(IN.worldPos.y / 20)) / 2;

            float noise = SimplexNoise(IN.worldPos / 10);//20
            float fnoise = SimplexNoise(-IN.worldPos / 4);//4
			float multiplier = 10;

			float x = ((IN.worldPos.x * SimplexNoise(IN.worldPos * multiplier)));
			float y = ((IN.worldPos.y * SimplexNoise(IN.worldPos * multiplier)));
			float z = 1 - ((IN.worldPos.z * SimplexNoise(IN.worldPos * multiplier)));
			x *= x;
			y *= y;
			z *= z;
			float3 thirdvector = float3(clamp(x, -1, 1), clamp(y, -1, 1), clamp(z, -1, 1));

			float metallic = _Metallic;
			float3 normal = o.Normal;

			bool isPosValid = IN.worldPos.x > 0 && IN.worldPos.y > 0 && IN.worldPos.z > 0;
			float3 biome = float3(0.5,0.5,0.5);// _Color;// isPosValid ? tex3D(_biomeTex, float3(IN.worldPos.x / 300, IN.worldPos.y / 120, IN.worldPos.z / 300)) : float3(0, 0, 0);
			float3 albedo;

			float nn = SimplexNoise(IN.worldPos / 8);
			float output = clamp(floor((nn)+1 + abs(SimplexNoise(IN.worldPos / 4 + thirdvector))), 0, 1);

			if (output < 0.7)
			{
				metallic = .25;
				multiplier = .1;

				float scale = 1;//10
				x = IN.worldPos.x % scale;// / 1200;
				y = IN.worldPos.y % scale;// / 2;
				z = IN.worldPos.z % scale;// / 1200;
				thirdvector = float3(x, y, z);

				float nk = .3;
				float th = .1;
				nn = SimplexNoise(IN.worldPos * 10);
				float p = .3;
				if (
					(x % .15 < 2 - abs(nn) * nk * abs((y / p) - floor((y / p) + .5)) + th &&
						x % .15 - abs(nn) * nk > 2 * abs((y / p) - floor((y / p) + .5)) - th) ||
					(y % .15 < 2 - abs(nn) * nk * abs((z / p) - floor((z / p) + .5)) + th &&
						y % .15 - abs(nn) * nk > 2 * abs((z / p) - floor((z / p) + .5)) - th) ||
					(z % .15 < 2 - abs(nn) * nk * abs((y / p) - floor((y / p) + .5)) + th &&
						z % .15 - abs(nn) * nk > 2 * abs((y / p) - floor((y / p) + .5)) - th)
					) {
					float power = 0.01;//0.00012
					float multiplier = 20;
					float h1 = (x + y + z) / (3 * scale);
					float h2 = ((x)+(y - power) + (z)) / (3 * scale);
					normal = normalize(float3(normal.x + (h1 - h2) * multiplier, normal.y + (h1 - h2) * multiplier, normal.z + (h1 - h2) * multiplier));
				}
			}
			else
			{

				float fff = abs(SimplexNoise(IN.worldPos / 2));
				float fafa = abs(SimplexNoise(IN.worldPos * .125));
				float bbb = clamp(SimplexNoise(IN.worldPos * .0625) + .5, 0, 1);
				float s = fff + fafa + bbb;
				if (s > 0 && s < 1.5)
				{
					output = clamp(1 / pow(1.5, s), 0, 1);

					float scale = 3;//10
					float power = 0.0006;//0.00012
					float multiplier = 10.5;
					float h1 = 1 - abs(SimplexNoise(IN.worldPos * scale)) * 0.8 + abs(SimplexNoiseGrad(IN.worldPos * 20)) * .2;
					float h2 = 1 - abs(SimplexNoise((IN.worldPos - float3(power, power, power)) * scale)) * .8 + abs(SimplexNoiseGrad((IN.worldPos - float3(0.00005, 0.00005, 0.00005)) * 20)) * .2;
					normal = normalize(float3(normal.x + (h1 - h2) * multiplier, normal.y + (h1 - h2) * multiplier, normal.z + (h1 - h2) * multiplier));
				}
				else
				{
					float scale = 20;//10
					float power = 0.00005;//0.00012
					float multiplier = 4.5;
					float h1 = abs(SimplexNoiseGrad(IN.worldPos * scale));
					float h2 = abs(SimplexNoiseGrad((IN.worldPos - float3(power, power, power)) * scale));
					normal = normalize(float3(normal.x + (h1 - h2) * multiplier, normal.y + (h1 - h2) * multiplier, normal.z + (h1 - h2) * multiplier));
				}
			}
			float coef = biome.x + biome.y + biome.z;
			if (coef != 0) {
				if (metallic < .25 * coef)metallic = .25 * coef;
				albedo = tex2D(_MainTex, step).rgb * (output * (1 - coef) + biome * (coef));
			}
			else {
				albedo = tex2D(_MainTex, step).rgb * output;
			}

			albedo *= _Color;
			if (coef > 0.3) {
				float fff = abs(SimplexNoise(IN.worldPos * 4));
				float fafa = abs(SimplexNoise(IN.worldPos * .5));
				float bbb = clamp(SimplexNoise(IN.worldPos * .125) + .5, 0, 1);
				float s = fff + fafa + bbb;
				if (s > 1 && s < 1.01)
				{
					albedo = biome * coef;
				}
			}
			//albedo = WorldNormalVector(IN, o.Normal);
			//albedo = IN.position*0.0625;
			//
            // Metallic and smoothness come from slider variables
			o.Albedo = albedo;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = 1;
            o.Normal = normal;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
