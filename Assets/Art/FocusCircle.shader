Shader "Hidden/FocusCircle"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Focus ("Focus", Vector) = (.25, .5, .5, 1)
        _Color ("Tint", Color) = (.25, .5, .5, 1)
    }
    SubShader
    {
        // No culling or depth
        Blend One OneMinusSrcAlpha
        Cull Off ZWrite Off ZTest Always
        Tags { "QUEUE"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "QUEUE"="Transparent" "RenderType"="Transparent" }
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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            fixed4 _Focus;
            fixed4 _Color;

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = _Color;

                // Get position in pixels

                fixed2 pos = i.uv;
                pos.x = pos.x;
                pos.y = pos.y * _ScreenParams.y/_ScreenParams.x;
                // If within the specified distance, lerp alpha
                float dist = distance(pos, _Focus.xy);
                if (dist < _Focus.z)
                {
                    // Quadratic seemed to be the best
                    col.a = lerp(col.a, 0.0, 1-pow(dist/_Focus.z, 2.0));
                }
                return col;
            }
            ENDCG
        }
    }
}
