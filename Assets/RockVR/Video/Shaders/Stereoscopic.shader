Shader "RockVR/Stereoscopic"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "" {}
    }
        CGINCLUDE

#include "UnityCG.cginc"

    struct v2f 
    {
        float4 pos : POSITION;
        float2 uv : TEXCOORD0;
    };

    uniform sampler2D _MainTex;
    uniform float _FlipX;

    v2f vert (appdata_img v) 
    {
        v2f o;
        o.pos = UnityObjectToClipPos (v.vertex);

#if STEREOPACK_TOP
        o.pos.y = (o.pos.y / 2.0) - 0.5;
#elif STEREOPACK_BOTTOM
        o.pos.y = (o.pos.y / 2.0) + 0.5;
#elif STEREOPACK_LEFT
        o.pos.x = (o.pos.x / 2.0) - 0.5;
#elif STEREOPACK_RIGHT
        o.pos.x = (o.pos.x / 2.0) + 0.5;
#endif

        float2 uv = v.texcoord.xy;
        o.uv = uv;
        return o;
    }

    half4 frag (v2f i): COLOR
    {
        return tex2D (_MainTex, i.uv);
    }
        ENDCG

    Subshader 
    {
        ZTest Always
        Cull Off
        ZWrite Off
        Fog{ Mode off }
        Pass
        {
            CGPROGRAM
#pragma multi_compile __ STEREOPACK_TOP STEREOPACK_BOTTOM STEREOPACK_LEFT STEREOPACK_RIGHT
#pragma vertex vert
#pragma fragment frag
            ENDCG
        }
    }
    Fallback Off
}