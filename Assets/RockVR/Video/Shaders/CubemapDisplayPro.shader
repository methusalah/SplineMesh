// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "RockVR/CubemapDisplayPro"
{
    Properties
    {
    }
    SubShader
    {
        // No culling or depth
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 uv : TEXCOORD0;
            };

            struct v2f
            {
                float3 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            samplerCUBE _CubeTex;
            float2 _SphereScale;
            float2 _SphereOffset;
            float4x4 _CubeTransform;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv * float3(1.0, _SphereScale.y, 1.0);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 dir = mul(_CubeTransform, float4(i.uv, 1)).xyz;
                fixed4 col = texCUBE(_CubeTex, dir);
                return col;
            }
            ENDCG
        }
    }
}
