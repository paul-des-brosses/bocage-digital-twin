// Bocage/S_Pond — pond sprite shader.
//
// Drives a [0,1] "water level" channel that lerps the sprite's
// per-pixel colour between a low-water tint (turbid brown / silty mud)
// and a high-water tint (saturated blue-green). Sourced from
// RC_WaterTableDepth (Normalized01), so the pond visually follows the
// piezometer reading — high table = full vibrant pond, low table =
// shrunken, muddier pond.
//
// Hand-written HLSL rather than Shader Graph (cf. DECISIONS.md "9α
// shaders en HLSL"). The blend pattern mirrors Sprites/Default so the
// pond composites identically to the rest of the static scene.
//
// Per CLAUDE.md §9 (sensor primacy), the colour is a strict function
// of the simulated water-table depth — no calendar cue, no ambient
// scenic logic.
Shader "Bocage/S_Pond"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Sprite Tint", Color) = (1,1,1,1)
        _LowWaterColor ("Low-Water Modulation", Color) = (0.55, 0.50, 0.40, 1)
        _HighWaterColor ("High-Water Modulation", Color) = (0.30, 0.55, 0.62, 1)
        _WaterLevel ("Water Level (0=low, 1=high)", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ PIXELSNAP_ON

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _LowWaterColor;
            fixed4 _HighWaterColor;
            float _WaterLevel;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap(OUT.vertex);
                #endif
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;
                fixed3 modulation = lerp(_LowWaterColor.rgb, _HighWaterColor.rgb, saturate(_WaterLevel));
                c.rgb *= modulation;
                c.rgb *= c.a; // premultiplied alpha output
                return c;
            }
            ENDCG
        }
    }

    Fallback "Sprites/Default"
}
