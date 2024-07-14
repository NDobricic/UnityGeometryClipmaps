Shader "Custom/HeightmapShader"
{
    Properties
    {
        _Offset("Offset", Vector) = (0, 0, 0)
        _Origin("Origin", Vector) = (0, 0, 0)
        _NoiseFrequency("Noise Frequency", Float) = 10
        _Size("Size", Float) = 1
        _MaxHeight("Max Height", Float) = 1
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
            #pragma target 3.0

            float3 _Offset;
            float3 _Origin;
            float _NoiseFrequency;
            float _Size;
			float _MaxHeight;

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

                float2 noiseCoord = (coord - float2(0.5, 0.5)) / _NoiseFrequency + _Offset.xz;
                float3 noiseResult = noised(noiseCoord);
                
                float noise = noiseResult.x;
                float2 derivatives = noiseResult.yz;

                float3 normal = normalize(float3(-derivatives.x * _Size, _MaxHeight, -derivatives.y * _Size));
                
                normal = normal * 0.5 + 0.5;

                return float4(noise, normal);
            }
            ENDCG
        }
    }
}
