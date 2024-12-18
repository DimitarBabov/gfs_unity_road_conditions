Shader "Custom/ExtractRoadHighlightMaskLatLon"
{
    Properties
    {
        _MainTex ("Map Tile", 2D) = "white" {}

        // The original road colors (for detection)
        _RoadFillRGB ("Detected Road Fill (RGB)", Color) = (0.992, 0.886, 0.576, 1)
        _RoadOutlineRGB ("Detected Road Outline (RGB)", Color) = (0.973, 0.671, 0, 1)

        // Single highlight color for road pixels
        _HighlightColor ("Highlight Color", Color) = (1, 0, 0, 1)

        // Threshold for color matching
        _Threshold ("Color Threshold", Range(0,1)) = 0.0

        // Option to preserve original pixel if not road (1) or show transparent (0)
        _PreserveNonRoad ("Preserve Non-Road (1=Yes, 0=No)", Range(0,1)) = 0

        // Large mask texture covering entire region (e.g., entire US)
        _MaskTex ("Mask Texture", 2D) = "white" {}

        // MAP bounding box (current visible map extents in lon/lat)
        _MapMinLon("Map Min Lon", Float) = -180
        _MapMaxLon("Map Max Lon", Float) = 180
        _MapMinLat("Map Min Lat", Float) = -90
        _MapMaxLat("Map Max Lat", Float) = 90

        // MASK bounding box (the lat/lon extents of the entire mask)
        _MaskMinLon("Mask Min Lon", Float) = -125
        _MaskMaxLon("Mask Max Lon", Float) = -66
        _MaskMinLat("Mask Min Lat", Float) = 24
        _MaskMaxLat("Mask Max Lat", Float) = 49
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
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
                float2 uv  : TEXCOORD0; // map UV
            };

            sampler2D _MainTex;
            float4    _MainTex_ST;

            sampler2D _MaskTex;
            float4    _MaskTex_ST;

            float4 _RoadFillRGB;
            float4 _RoadOutlineRGB;
            float4 _HighlightColor;
            float  _Threshold;
            float  _PreserveNonRoad;

            // Map bounding box
            float _MapMinLon, _MapMaxLon, _MapMinLat, _MapMaxLat;

            // Mask bounding box
            float _MaskMinLon, _MaskMaxLon, _MaskMinLat, _MaskMaxLat;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                // Default UV transform for the map
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
                // 1) Sample the map tile pixel using map UV
                float4 originalCol = tex2D(_MainTex, i.uv);

                // 2) Convert the map UV -> lat/lon by linearly interpolating the map bounding box
                float lon = lerp(_MapMinLon, _MapMaxLon, i.uv.x);
                float lat = lerp(_MapMaxLat, _MapMinLat, i.uv.y); 
                // Note: we invert lat from top to bottom

                // 3) Convert lat/lon -> mask UV
                float maskU = (lon - _MaskMinLon) / (_MaskMaxLon - _MaskMinLon);
                float maskV = (lat - _MaskMinLat) / (_MaskMaxLat - _MaskMinLat);

                // 4) Sample the mask texture at the derived UV
                float4 maskSample = tex2D(_MaskTex, float2(maskU, maskV));

                // If mask pixel is black or alpha == 0, skip road detection
                bool maskIsBlackOrTransparent =
                    (maskSample.a <= 0.01) ||
                    (maskSample.r < 0.01 && maskSample.g < 0.01 && maskSample.b < 0.01);

                if (maskIsBlackOrTransparent)
                {
                    // Show original pixel or transparent
                    if (_PreserveNonRoad >= 0.5) return float4(originalCol.rgb, 1.0);
                    else return float4(0,0,0,0);
                }

                // 5) Proceed with color-based road detection
                float3 pixelRGB   = originalCol.rgb;
                float3 fillRGB    = _RoadFillRGB.rgb;
                float3 outlineRGB = _RoadOutlineRGB.rgb;

                // Distances to each road color
                float distFill    = ColorDistance(pixelRGB, fillRGB);
                float distOutline = ColorDistance(pixelRGB, outlineRGB);

                // Convert distance -> match strength [0..1]
                float fillWeight    = 1.0 - saturate(distFill / _Threshold);
                float outlineWeight = 1.0 - saturate(distOutline / _Threshold);

                // Final road match strength
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
                    // Non-road pixel
                    if (_PreserveNonRoad >= 0.5) return float4(originalCol.rgb, 1.0);
                    else return float4(0,0,0,0);
                }
            }
            ENDCG
        }
    }
}
