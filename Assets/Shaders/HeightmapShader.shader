Shader "Custom/HeightmapShader"
{
    Properties
    {
        _Offset("Offset", Vector) = (0, 0, 0)
        _Origin("Origin", Vector) = (0, 0, 0)
        _NoiseFrequency("Noise Frequency", Float) = 10
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

            #pragma vertex CustomRenderTextureVertexShader
            #pragma fragment frag
            #pragma target 3.0

            float3 _Offset;
            float3 _Origin;
            float _NoiseFrequency;

            float4 frag(v2f_customrendertexture IN) : COLOR
            {
                float3 coordOffset = floor(_Origin);
                float3 fraction = _Origin - floor(_Origin);

                //return float4(noise, noise, noise, 1);
                //return float4(IN.globalTexcoord.x, IN.globalTexcoord.y, 0, 1);

                float2 coord = IN.globalTexcoord.xy + coordOffset;
                if (IN.globalTexcoord.x < fraction.x) {
                    coord += float2(1, 0);
                }

                if (IN.globalTexcoord.y < fraction.y) {
                    coord += float2(0, 1);
                }

                
                //return float4(coord.x / 2, coord.y / 2, 0, 1);
                float noise = ClassicNoise((coord - float2(0.5, 0.5)) / _NoiseFrequency + _Offset.xz);

                return float4(noise, noise, noise, 1);
            }
            ENDCG
        }
    }
}
