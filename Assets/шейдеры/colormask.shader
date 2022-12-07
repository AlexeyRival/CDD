// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/colormask"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
     //   _biomeTex("3D", 3D) = "white" {}
        _biome("Biome", float) = 0
        _Color ("Main Color", Color) = (1,1,1,1)
        _DepthFactor("Depth Factor", float) = 1.0
        _DepthPow("Depth Pow", float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Geometry-3" }
                Stencil
                {
                Ref 32
                Comp Always
                Pass Replace
                }
       // UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"

        Pass
        {
            Cull Front
            ZWrite Off
            //ZTest Greater
            //Offset -1,-1
            //Blend One One
            Blend Zero One
            //Blend SrcAlpha OneMinusSrcAlpha
             //Blend DstColor OneMinusDstColor
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

           // uniform sampler2D_float _CameraDepthTexture;
            sampler2D _CameraDepthNormalsTexture;
            uniform fixed _DepthLevel;
            uniform half4 _MainTex_TexelSize;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos: TEXCOORD2;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float4 screenPos : TEXCOORD1;
                float3 worldPos: TEXCOORD2;
                float2 depth : TEXCOORD0;
            };

            //RWTexture3D<float4> _biomeTex;
            float4 _Color;
            float _biome;
            sampler2D _MainTex;
            float4 _MainTex_ST;
            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
            float _DepthFactor;
            fixed _DepthPow;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.screenPos = ComputeScreenPos(o.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                COMPUTE_EYEDEPTH(o.screenPos.w);
                UNITY_TRANSFER_FOG(o,o.vertex);
                UNITY_TRANSFER_DEPTH(o.depth);
                return o;
            }
            bool IsInsideBox(float3 v) 
            {
                const float min = float3(-0.5, -0.5, -0.5);
                const float max = float3(0.5, 0.5, 0.5);
                float3 minStep = step(min, v);
                float3 maxStep = step(max, v);
                float3 s = minStep - maxStep;
                return s.x * s.y * s.z > 0;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                //float depth = UNITY_SAMPLE_DEPTH(tex2D(_CameraDepthTexture, i.uv));
                //depth = pow(Linear01Depth(depth), _DepthLevel);
                //float4 clipPos = UnityObjectToClipPos(i.vertex);
                //float zDepth = clipPos.z / clipPos.w;
                //zDepth += 0.5;
                //fixed4 col = fixed4(0,0,0,0);
                //if (zDepth < .6) {

                float3 localpos = mul(unity_WorldToObject,i.worldPos);

                float2 screenUv = i.vertex.xy / _ScreenParams.xy;

                fixed4 col = tex2D(_MainTex, i.uv) * _Color;

                float sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.screenPos)));
                //float depth = sceneZ - i.screenPos.z;
                //float depth = sceneZ;
                //float depth = tex2D(_CameraDepthTexture,screenUv);
                float depth = DECODE_EYEDEPTH(tex2D(_CameraDepthTexture, screenUv));
                depth = Linear01Depth(depth)*400;
                //_biomeTex[ceil(i.worldPos)] = 1;
                //col = (depth*20)*(depth*20);// fixed4(localpos.x * 0.04, localpos.y * 0.04, localpos.z * 0.04, 0.5);
                col = fixed4(depth,0,1- depth,1);
                //col = (depth);
                //float4 NormalDepth;

                //DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, i.uv), NormalDepth.w, NormalDepth.xyz);
                //col = NormalDepth.w;
                
                // fade with depth
                //fixed depthFading = saturate((abs(pow(depth, _DepthPow))) / _DepthFactor);
                //col *= depthFading;
                //}
                //UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
