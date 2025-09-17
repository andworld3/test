Shader "Custom/ALReactiveParticle"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _EmissionColor ("Emission Color", Color) = (1,0.5,0,1)
        _EmissionGain ("Emission Gain", Range(0,5)) = 1.0

        [Header(AudioLink)]
        _AL_Enable ("AudioLink Enable", Float) = 1.0
        _AL_Band ("Band (0=Bass 1=LowMid 2=HighMid 3=Treble)", Range(0,3)) = 0
        _AL_Gain ("AudioLink Gain", Range(0,4)) = 1.5
        _AL_Smooth ("Smoothing", Range(0,1)) = 0.15

        [Header(Color Blending)]
        _ColorA ("Color A", Color) = (1,1,1,1)
        _ColorB ("Color B", Color) = (1,0,0,1)

        [Header(Vertex Displacement)]
        _DisplaceAmp ("Displacement Amount", Range(0,1)) = 0.1

        [Header(Render Settings)]
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "IgnoreProjector"="True"
        }
        LOD 200

        Pass
        {
            Name "FORWARD"
            Tags { "LightMode"="ForwardBase" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"

            // AudioLink Global Texture
            Texture2D&lt;float4&gt; _AudioTexture;
            SamplerState sampler_AudioTexture;

            // Properties
            sampler2D _MainTex;
            float4 _MainTex_ST;

            fixed4 _BaseColor;
            fixed4 _EmissionColor;
            float _EmissionGain;

            float _AL_Enable;
            float _AL_Band;
            float _AL_Gain;
            float _AL_Smooth;

            fixed4 _ColorA;
            fixed4 _ColorB;

            float _DisplaceAmp;
            float _Cutoff;

            // Smoothing variables (static for persistence)
            static float _smoothedLevel = 0.0;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float3 worldNormal : TEXCOORD1;
            };

            // AudioLink Band Sampling
            float SampleAudioBand(int band)
            {
                if (_AL_Enable &lt; 0.5) return 0.0;

                // AudioLink texture coordinates
                // Band data is stored in specific UV coordinates
                float2 uv = float2(0.0, 0.0);

                // Map band to UV coordinates (simplified mapping)
                switch (band)
                {
                    case 0: uv = float2(0.125, 0.0); break; // Bass
                    case 1: uv = float2(0.375, 0.0); break; // LowMid
                    case 2: uv = float2(0.625, 0.0); break; // HighMid
                    case 3: uv = float2(0.875, 0.0); break; // Treble
                }

                // Sample the audio texture
                float4 audioData = _AudioTexture.SampleLevel(sampler_AudioTexture, uv, 0);
                float level = audioData.r; // Use red channel

                // Apply gain and clamp
                level = saturate(level * _AL_Gain);

                // Apply smoothing (EMA filter)
                _smoothedLevel = lerp(_smoothedLevel, level, _AL_Smooth);

                return _smoothedLevel;
            }

            v2f vert (appdata v)
            {
                v2f o;

                // Sample audio level
                float audioLevel = SampleAudioBand((int)_AL_Band);

                // Apply vertex displacement
                float3 displacement = v.normal * (audioLevel * _DisplaceAmp);
                v.vertex.xyz += displacement;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample main texture
                fixed4 col = tex2D(_MainTex, i.uv);

                // Sample audio level for fragment processing
                float audioLevel = SampleAudioBand((int)_AL_Band);

                // Base color blending
                fixed4 baseCol = lerp(_ColorA, _ColorB, audioLevel) * _BaseColor;
                col *= baseCol * i.color;

                // Emission
                fixed3 emission = _EmissionColor.rgb * audioLevel * _EmissionGain;
                col.rgb += emission;

                // Alpha cutoff
                if (col.a &lt; _Cutoff)
                    discard;

                return col;
            }
            ENDCG
        }
    }

    SubShader
    {
        // Fallback for older hardware
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _BaseColor;

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

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _BaseColor;
                return col;
            }
            ENDCG
        }
    }

    FallBack "Sprites/Default"
}