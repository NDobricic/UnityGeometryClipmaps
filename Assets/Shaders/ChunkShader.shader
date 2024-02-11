Shader "Custom/ChunkShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Offset ("Offset", Vector) = (0,0,0)
        _Size ("Size", Vector) = (1.0, 1.0, 1.0)
        _Heightmap ("Heightmap", 2D) = "white" {}
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
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        float3 _Offset;
        float3 _Size;
        sampler2D _Heightmap;
        float _NoiseFrequency;
        float _MaxHeight;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        // Custom vertex shader function
        void vert(inout appdata_full v)
        {
            float h = ClassicNoise(float2(_Offset.x + v.vertex.x * _Size.x, _Offset.z + v.vertex.z * _Size.z) / _NoiseFrequency) * _MaxHeight;
            v.vertex.y += h;

            // Approximate the partial derivatives
            float dx = 0.01; // Small offset in x
            float dz = 0.01; // Small offset in z
            float h_dx = ClassicNoise(float2(_Offset.x + (v.vertex.x + dx) * _Size.x, _Offset.z + v.vertex.z * _Size.z) / _NoiseFrequency) * _MaxHeight;
            float h_dz = ClassicNoise(float2(_Offset.x + v.vertex.x * _Size.x, _Offset.z + (v.vertex.z + dz) * _Size.z) / _NoiseFrequency) * _MaxHeight;

            // Calculate gradient vector components
            float grad_x = (h_dx - h) / dx;
            float grad_z = (h_dz - h) / dz;

            // Construct normal vector from gradient
            float3 normal = normalize(float3(-grad_x, 1.0, -grad_z));
            v.normal = normal;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
