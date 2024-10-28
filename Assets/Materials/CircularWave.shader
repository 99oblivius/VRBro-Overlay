Shader "Custom/GradientRadialWaveShader"
{
    Properties
    {
        _WaveSpeed ("Wave Speed", Range(0, 10)) = 1.0
        _WaveFrequency ("Wave Frequency", Range(0, 0.01)) = 1.0
        _WaveAmplitude ("Wave Amplitude", Range(0, 1)) = 0.1
        _Color1 ("Color 1", Color) = (1,1,1,1)
        _Color2 ("Color 2", Color) = (0,0,0,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 localPos : TEXCOORD1;
            };

            float _WaveSpeed;
            float _WaveFrequency;
            float _WaveAmplitude;
            fixed4 _Color1;
            fixed4 _Color2;

            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                
                // Calculate local coordinates
                float3 localPos = mul(unity_WorldToObject, v.vertex).xyz;
                o.localPos = localPos;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {

                float dist = length(i.localPos.xy + float2(0, 80000));

                float gradient = (_WaveAmplitude * (sin(_Time.y * 5 + i.localPos.y / 15000) + 1) * ((sin(dist * _WaveFrequency + _Time.y * _WaveSpeed)) + 1)) / 2;

                fixed4 color = lerp(_Color1, _Color2, gradient);

                return color;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
