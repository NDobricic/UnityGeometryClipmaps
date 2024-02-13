Shader "Custom/ChunkShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Origin ("Origin", Vector) = (0,0,0)
        _Size ("Size", Vector) = (1.0, 1.0, 1.0)
        _Heightmap ("Heightmap", 2D) = "white" {}
        _TileSize ("Tile Size", Float) = 1.0
        // slider for the noise scale
        _NoiseFrequency ("Noise Frequency", Range(0.01, 200)) = 100
        _MaxHeight ("Max Height", Range(0, 100)) = 50
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #include "Packages/jp.keijiro.noiseshader/Shader/ClassicNoise2D.hlsl"

        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Declare a vertex modifier function
        #pragma vertex vert

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float3 height;
            float4 coord;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        float3 _Origin;
        float3 _Size;
        sampler2D _Heightmap;
        float _TileSize;
        float _NoiseFrequency;
        float _MaxHeight;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        // Custom vertex shader function
        void vert(inout appdata_full v, out Input o)
        {
            float4 coord = float4(_Origin.x / _Size.x + v.vertex.x, _Origin.z / _Size.z + v.vertex.z, 0, 0) / _TileSize;
            coord += float4(0.5, 0.5, 0, 0);

            float3 h = tex2Dlod(_Heightmap, coord);
            o.coord = coord;
            o.height = h;
            v.vertex.y = h.r * _MaxHeight;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            //o.Albedo = IN.height;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
