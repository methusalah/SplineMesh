// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// shader to convert cubemap to equirectangular projection
// render full screen quad, maps uv to spherical coordinates, does cubemap lookup
// sgreen 8/4/2016

Shader "RockVR/CubemapToClindrical"
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
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
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
                //o.uv = (v.uv *_SphereScale + _SphereOffset) * float2(UNITY_PI*2.0, UNITY_PI);   // convert to angles
                o.uv = (v.uv *_SphereScale + _SphereOffset) * float2(UNITY_PI*2.0, UNITY_PI*0.5);   // convert to angles
                return o;
            }

            // convert spherical coordinates (azimuth, elevation angles) to Cartesian coordinates (x, y, z) on sphere
            float3 sphericalToCartesian(float2 a)
            {
                return float3(cos(a.x),
                    //cos(a.y),
                      -(a.y - UNITY_PI*0.25),
                      sin(a.x));
    }

    fixed4 frag(v2f i) : SV_Target
    {
        float3 dir = sphericalToCartesian(i.uv);
        dir = mul(_CubeTransform, float4(dir, 1)).xyz;
        fixed4 col = texCUBE(_CubeTex, dir);
        return col;
    }
    ENDCG
}
    }
}
