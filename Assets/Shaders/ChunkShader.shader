Shader "Custom/ChunkShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.0
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Origin ("Origin", Vector) = (0,0,0)
        _Size ("Size", Vector) = (1.0, 1.0, 1.0)
        _PlayerPos ("Player Position", Vector) = (0, 0, 0)
        _Heightmap ("Heightmap", 2D) = "white" {}
        _LowResHeightmap ("Low Res Heightmap", 2D) = "white" {}
        _BlendFactor ("Blend Factor", Range(0, 0.5)) = 0.125
        _TileSize ("Tile Size", Float) = 1.0
        _NoiseFrequency ("Noise Frequency", Range(0.01, 200)) = 100
        _MaxHeight ("Max Height", Range(0, 100)) = 50
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM

        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.5

        sampler2D _MainTex;
        sampler2D _Heightmap;
        sampler2D _LowResHeightmap;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
            float2 heightmapCoord;
            float2 lowResHeightmapCoord;
            float alpha;
            float height;
            INTERNAL_DATA
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        float3 _Origin;
        float3 _Size;
        float3 _PlayerPos;
        float _TileSize;
        float _NoiseFrequency;
        float _MaxHeight;

        UNITY_INSTANCING_BUFFER_START(Props)
        UNITY_INSTANCING_BUFFER_END(Props)

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            
            float2 coord = float2(_Origin.x / _Size.x + v.vertex.x, _Origin.z / _Size.z + v.vertex.z) / _TileSize + 0.5;
            float2 lowResCoord = float2(_Origin.x / _Size.x + v.vertex.x, _Origin.z / _Size.z + v.vertex.z) / (_TileSize * 2) + 0.5;
            o.heightmapCoord = coord;
            o.lowResHeightmapCoord = lowResCoord;

            float2 distPlayer = (((_Origin.xz + v.vertex.xz * _Size.xz) - _PlayerPos.xz) / _Size.xz / _TileSize + 0.5);
            float alpha = 1.0 - min(min(distPlayer.x, 1.0 - distPlayer.x), min(distPlayer.y, 1.0 - distPlayer.y)) * 8;
            alpha = clamp(alpha, 0, 1);
            o.alpha = alpha;
            
            float4 heightData = tex2Dlod(_Heightmap, float4(coord, 0, 0));
            float4 lowResHeightData = tex2Dlod(_LowResHeightmap, float4(lowResCoord, 0, 0));
            
            float height = lerp(heightData.r, lowResHeightData.r, alpha);
            
            o.height = height;
            v.vertex.y = height * _MaxHeight;
            
            float3 normal = heightData.gba * 2 - 1;
            v.normal = normal;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            //o.Albedo = float3(IN.alpha, IN.alpha, IN.alpha);
            
            // Use the height to influence the color if desired
            // o.Albedo *= IN.height;

            //float3 normal = tex2D(_Heightmap, IN.heightmapUV).gba * 2 - 1;
            //o.Normal = normal;
            
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
        }
        ENDCG
    }
    FallBack "Diffuse"
}