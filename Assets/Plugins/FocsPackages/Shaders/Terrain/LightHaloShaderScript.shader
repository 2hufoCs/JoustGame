Shader "Unlit/LightHalo"
{
    Properties
    {
        _LineFrequency("Line Frequency", int) = 5
        _WiggleFrequency("Wiggle Frequency", float) = 10
        _LineSpeed("Line Speed", float) = 1

        _ColorA("Color A", Color) = (1, 1, 1, 1)
        _ColorB("Color B", Color) = (0, 0, 0, 1)
        _ColorStart("Color Start", float) = 0
        _ColorEnd("Color End", float) = 1

        _OpacityMult("OpacityMutiplier", float) = 1
    }
    SubShader
    {
        // Subshader tags
        Tags 
        { 
            "RenderType" = "Transparent" // Let render pipeline know which type it is
            "Queue" = "Transparent" // Change sorting order (skybox -> opaque -> transparent)
        }


        Pass
        {
            // Pass tags
            Cull Off // Disable culling (renders transparent objects on both sides)
            ZWrite Off // Don't add mesh to depth buffer (so objects behind are visible)
            Blend One One // Additive

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            #define TAU 6.283185307179586

            int _LineFrequency;
            float _WiggleFrequency;
            float _LineSpeed;

            float3 _ColorA;
            float3 _ColorB;
            float _ColorStart;
            float _ColorEnd;

            float _OpacityMult;

            struct MeshData
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Interpolators
            {
                float4 vertex : SV_POSITION;
                float3 normal : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            float GetHalo(float2 uv, float3 normal)
            {
                float xOffset = cos(uv.x * TAU * _WiggleFrequency) * 0.02;
                float greyScale = cos((uv.y - xOffset - _Time.y * 0.2 * _LineSpeed) * TAU * _LineFrequency) * 0.5 + 0.5;
                float alphaScale = (1-uv.y) * (1-uv.y) * (1-uv.y);
                greyScale *= alphaScale;
                return greyScale * (abs(normal.y) < 0.4);
            }

            float InverseLerp(float a, float b, float v)
            {
                return saturate((v-a) / (b-a));
            }

            float3 ColorLerp(float3 colorA, float3 colorB, float t)
            {
                float x = saturate(lerp(colorA.x, colorB.x, t));
                float y = saturate(lerp(colorA.y, colorB.y, t));
                float z = saturate(lerp(colorA.z, colorB.z, t));
                return float3(x, y, z);
            }


            Interpolators vert (MeshData v)
            {
                Interpolators o;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = mul(unity_ObjectToWorld, v.normal);
                o.uv = v.uv;

                return o;
            }

            float4 frag (Interpolators i) : SV_Target
            {
                float alpha = GetHalo(i.uv, i.normal);
                float t = InverseLerp(_ColorStart, _ColorEnd, i.uv.y);
                float3 color = ColorLerp(_ColorA, _ColorB, t);
                return float4(color * alpha * _OpacityMult, 1);
            }
            ENDCG
        }
    }
}
