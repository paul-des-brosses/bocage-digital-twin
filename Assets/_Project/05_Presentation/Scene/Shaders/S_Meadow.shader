// Bocage/S_Meadow — meadow / prairie sprite shader.
//
// Drives a [0,1] "moisture" channel that lerps the sprite's per-pixel
// colour between a dry tint (yellow-straw) and a moist tint (fresh
// green). Sourced from RC_SoilMoisture, itself derived from the model's
// water-table depth (cf. SoilMoistureIndicator). Hand-written HLSL
// rather than Shader Graph — see DECISIONS.md entry "9α shaders en
// HLSL" — for hand-authorability and Git diff legibility.
//
// Blend pattern mirrors Sprites/Default (premultiplied alpha, Blend One
// OneMinusSrcAlpha) so meadow sprites composite identically to the rest
// of the static scene assembled by SceneAssembler.
//
// Per CLAUDE.md §9 (sensor primacy), this shader does NOT introduce any
// time-of-year ambient cue: the colour shift is a strict function of a
// model variable that itself maps to a deployed sensor (the piezometer).
Shader "Bocage/S_Meadow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Sprite Tint", Color) = (1,1,1,1)
        _DryColor ("Dry Modulation (low moisture)", Color) = (0.85, 0.78, 0.45, 1)
        _MoistColor ("Moist Modulation (high moisture)", Color) = (0.42, 0.58, 0.32, 1)
        _Moisture ("Moisture (0=dry, 1=moist)", Range(0,1)) = 0.5
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
            fixed4 _DryColor;
            fixed4 _MoistColor;
            float _Moisture;

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
                fixed3 modulation = lerp(_DryColor.rgb, _MoistColor.rgb, saturate(_Moisture));
                c.rgb *= modulation;
                c.rgb *= c.a; // premultiplied alpha output
                return c;
            }
            ENDCG
        }
    }

    Fallback "Sprites/Default"
}
