Shader "Custom/HeightmapShader"
{
    Properties
    {
        _Offset("Offset", Vector) = (0, 0, 0)
        _Origin("Origin", Vector) = (0, 0, 0)
        _NoiseFrequency("Noise Frequency", Float) = 10
        _Size("Size", Float) = 1
        _MaxHeight("Max Height", Float) = 1
        _NoiseScale("Noise Scale", Float) = 1
        _Octaves("Octaves", Int) = 1
        _Lacunarity("Lacunarity", Float) = 2.0
        _Persistence("Persistence", Float) = 0.5
    }

    SubShader
    {
        Lighting Off
        Blend One Zero

        Pass
        {
            CGPROGRAM
            #include "UnityCustomRenderTexture.cginc"
            #include "Packages/jp.keijiro.noiseshader/Shader/ClassicNoise2D.hlsl"
            #include "GradientNoise2D.hlsl"

            #pragma vertex CustomRenderTextureVertexShader
            #pragma fragment frag
            #pragma target 3.5

            float3 _Offset;
            float3 _Origin;
            float _NoiseFrequency;
            float _Size;
            float _MaxHeight;
            float _NoiseScale;
            int _Octaves;
            float _Lacunarity;
            float _Persistence;

            // IQ's erosion-based terrain noise (from Shadertoy "Elevated")
            // Accumulates derivatives across octaves; suppresses detail on steep
            // slopes via 1/(1+dot(d,d)), mimicking natural erosion.
            float terrainNoise(float2 p)
            {
                float sum = 0.0;
                float amp = 1.0;
                float2 d = float2(0.0, 0.0);

                for (int i = 0; i < _Octaves; i++)
                {
                    float3 n = noised(p);
                    d += n.yz;
                    sum += amp * n.x / (1.0 + dot(d, d));
                    amp *= _Persistence;
                    // Rotate to break axis-aligned artifacts, then scale
                    p = float2(0.8 * p.x - 0.6 * p.y, 0.6 * p.x + 0.8 * p.y) * _Lacunarity;
                }
                return sum;
            }

            float4 frag(v2f_customrendertexture IN) : COLOR
            {
                float3 coordOffset = floor(_Origin);
                float3 fraction = _Origin - floor(_Origin);

                float2 coord = IN.globalTexcoord.xy + coordOffset;
                if (IN.globalTexcoord.x < fraction.x) {
                    coord += float2(1, 0);
                }

                if (IN.globalTexcoord.y < fraction.y) {
                    coord += float2(0, 1);
                }

                float2 baseCoord = ((coord - float2(0.5, 0.5)) / _NoiseFrequency + _Offset.xz) * _NoiseScale;

                // Finite-difference step: 1 texel in noise-coordinate space
                float tileSize = _Size * _NoiseFrequency;
                float eps = _NoiseScale / (tileSize * _NoiseFrequency);

                float h  = terrainNoise(baseCoord);
                float hx = terrainNoise(baseCoord + float2(eps, 0));
                float hz = terrainNoise(baseCoord + float2(0, eps));

                float2 derivatives = float2(hx - h, hz - h) / eps;

                float3 normal = normalize(float3(-derivatives.x * _Size, _MaxHeight, -derivatives.y * _Size));
                normal = normal * 0.5 + 0.5;

                return float4(h, normal);
            }
            ENDCG
        }
    }
}
