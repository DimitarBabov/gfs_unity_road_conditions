Shader "Custom/ExtractRoadHighlightMaskIgnoreBlack"
{
    Properties
    {
        _MainTex ("Map Tile", 2D) = "white" {}

        // The original road colors (for detection)
        _RoadFillRGB ("Detected Road Fill (RGB)", Color) = (0.992, 0.886, 0.576, 1)   // ~253/226/147
        _RoadOutlineRGB ("Detected Road Outline (RGB)", Color) = (0.973, 0.671, 0, 1) // ~248/171/0

        // Single highlight color
        _HighlightColor ("Highlight Color", Color) = (1, 0, 0, 1) // default red

        // Threshold for color matching
        _Threshold ("Color Threshold", Range(0,1)) = 0.0

        // Option to preserve the original pixel if not a road (1) or show transparent (0)
        _PreserveNonRoad ("Preserve Non-Road (1=Yes, 0=No)", Range(0,1)) = 0

        // Additional mask texture.
        // If the mask pixel is black or alpha=0 => ignore road detection for that pixel.
        _MaskTex ("Mask Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 200

        Pass
        {
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            sampler2D _MaskTex; // The additional mask texture
            float4 _MaskTex_ST;

            float4 _RoadFillRGB;
            float4 _RoadOutlineRGB;
            float4 _HighlightColor;
            float  _Threshold;
            float  _PreserveNonRoad;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float ColorDistance(float3 c1, float3 c2)
            {
                float3 diff = c1 - c2;
                return sqrt(dot(diff, diff)); // Euclidean distance in RGB
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Sample the map tile pixel
                float4 originalCol = tex2D(_MainTex, i.uv);

                // Sample the mask pixel
                float4 maskSample  = tex2D(_MaskTex, i.uv);

                // If mask pixel is black or alpha == 0, skip road detection
                // Let's define "black" if RGB < 0.01 (tiny threshold) AND alpha <= 0.01 
                bool maskIsBlackOrTransparent = 
                    (maskSample.a <= 0.01) ||
                    (maskSample.r < 0.01 && maskSample.g < 0.01 && maskSample.b < 0.01);

                if (maskIsBlackOrTransparent)
                {
                    // Show original pixel or make transparent
                    if (_PreserveNonRoad >= 0.5)
                    {
                        // Preserve the original
                        return float4(originalCol.rgb, 1.0);
                    }
                    else
                    {
                        // Transparent
                        return float4(0,0,0,0);
                    }
                }

                // Otherwise, proceed with road detection
                float3 pixelRGB   = originalCol.rgb;
                float3 fillRGB    = _RoadFillRGB.rgb;
                float3 outlineRGB = _RoadOutlineRGB.rgb;

                // Distance from pixel to each road color
                float distFill     = ColorDistance(pixelRGB, fillRGB);
                float distOutline  = ColorDistance(pixelRGB, outlineRGB);

                // Convert distance -> match strength [0..1]
                float fillWeight    = 1.0 - saturate(distFill / _Threshold);
                float outlineWeight = 1.0 - saturate(distOutline / _Threshold);

                // Max of the two weights => final road match strength
                float roadWeight = max(fillWeight, outlineWeight);

                if (roadWeight > 0.0)
                {
                    // Pixel matches either fill or outline color
                    float4 highlight = _HighlightColor;
                    highlight.a *= roadWeight;
                    return highlight;
                }
                else
                {
                    // Non-road pixel => preserve or transparent
                    if (_PreserveNonRoad >= 0.5)
                    {
                        return float4(originalCol.rgb, 1.0);
                    }
                    else
                    {
                        return float4(0,0,0,0);
                    }
                }
            }
            ENDCG
        }
    }
}
