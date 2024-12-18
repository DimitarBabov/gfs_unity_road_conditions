Shader "Custom/ExtractRoadHighlightMaskLatLonNoSaturate_6Params_NoTextureCheck"
{
    Properties
    {
        _MainTex ("Map Tile", 2D) = "white" {}

        // Road colors
        _RoadFillRGB ("Detected Road Fill (RGB)", Color) = (0.992, 0.886, 0.576, 1)
        _RoadOutlineRGB ("Detected Road Outline (RGB)", Color) = (0.973, 0.671, 0, 1)

        // Up to 6 parameters, each with a mask and highlight color
        _Param1MaskTex ("Param1 Mask Texture", 2D) = "white" {}
        _Param1HighlightColor ("Param1 Highlight Color", Color) = (1, 0, 0, 1)

        _Param2MaskTex ("Param2 Mask Texture", 2D) = "white" {}
        _Param2HighlightColor ("Param2 Highlight Color", Color) = (0, 1, 0, 1)

        _Param3MaskTex ("Param3 Mask Texture", 2D) = "white" {}
        _Param3HighlightColor ("Param3 Highlight Color", Color) = (0, 0, 1, 1)

        _Param4MaskTex ("Param4 Mask Texture", 2D) = "white" {}
        _Param4HighlightColor ("Param4 Highlight Color", Color) = (1, 1, 0, 1)

        _Param5MaskTex ("Param5 Mask Texture", 2D) = "white" {}
        _Param5HighlightColor ("Param5 Highlight Color", Color) = (1, 0, 1, 1)

        _Param6MaskTex ("Param6 Mask Texture", 2D) = "white" {}
        _Param6HighlightColor ("Param6 Highlight Color", Color) = (0, 1, 1, 1)

        _Threshold ("Color Threshold", Range(0,1)) = 0.0
        _PreserveNonRoad ("Preserve Non-Road (1=Yes, 0=No)", Range(0,1)) = 0

        // Map bounding box
        _MapMinLon("Map Min Lon", Float) = -180
        _MapMaxLon("Map Max Lon", Float) = 180
        _MapMinLat("Map Min Lat", Float) = -90
        _MapMaxLat("Map Max Lat", Float) = 90

        // Mask bounding box
        _MaskMinLon("Mask Min Lon", Float) = -180
        _MaskMaxLon("Mask Max Lon", Float) = 180
        _MaskMinLat("Mask Min Lat", Float) = -90
        _MaskMaxLat("Mask Max Lat", Float) = 90
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
                float2 uv  : TEXCOORD0; // Map UV
            };

            sampler2D _MainTex;

            sampler2D _Param1MaskTex;
            sampler2D _Param2MaskTex;
            sampler2D _Param3MaskTex;
            sampler2D _Param4MaskTex;
            sampler2D _Param5MaskTex;
            sampler2D _Param6MaskTex;

            float4 _RoadFillRGB;
            float4 _RoadOutlineRGB;
            float4 _Param1HighlightColor;
            float4 _Param2HighlightColor;
            float4 _Param3HighlightColor;
            float4 _Param4HighlightColor;
            float4 _Param5HighlightColor;
            float4 _Param6HighlightColor;
            float  _Threshold;
            float  _PreserveNonRoad;

            float _MapMinLon, _MapMaxLon, _MapMinLat, _MapMaxLat;
            float _MaskMinLon, _MaskMaxLon, _MaskMinLat, _MaskMaxLat;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            float ColorDistance(float3 c1, float3 c2)
            {
                float3 diff = c1 - c2;
                return sqrt(dot(diff, diff));
            }

            bool Approximately(float a, float b, float epsilon)
            {
                return abs(a - b) < epsilon;
            }

            bool IsNoTextureAssigned(float4 sample)
            {
                // Check if sample is close to mid-gray (0.5,0.5,0.5)
                return (Approximately(sample.r, 0.5, 0.01) &&
                        Approximately(sample.g, 0.5, 0.01) &&
                        Approximately(sample.b, 0.5, 0.01));
            }

            float4 SampleParamMaskAndCheckWhite(sampler2D maskTex, float lon, float lat)
            {
                float maskU = (lon - _MaskMinLon) / (_MaskMaxLon - _MaskMinLon);
                float maskV = (lat - _MaskMinLat) / (_MaskMaxLat - _MaskMinLat);

                float4 mSample = tex2D(maskTex, float2(maskU, maskV));

                // If no texture assigned: alpha=0
                if (IsNoTextureAssigned(mSample))
                {
                    mSample.a = 0.0;
                    return mSample;
                }

                // Consider pixel white if r,g,b > 0.9
                bool isWhite = (mSample.r > 0.9 && mSample.g > 0.9 && mSample.b > 0.9);

                if (!isWhite)
                {
                    mSample.a = 0.0;
                }
                return mSample;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float4 originalCol = tex2D(_MainTex, i.uv);

                // Convert map UV -> lat/lon (linear)
                float lon = lerp(_MapMinLon, _MapMaxLon, i.uv.x);
                float lat = lerp(_MapMinLat, _MapMaxLat, i.uv.y);

                // Sample each param mask
                float4 p1 = SampleParamMaskAndCheckWhite(_Param1MaskTex, lon, lat);
                float4 p2 = SampleParamMaskAndCheckWhite(_Param2MaskTex, lon, lat);
                float4 p3 = SampleParamMaskAndCheckWhite(_Param3MaskTex, lon, lat);
                float4 p4 = SampleParamMaskAndCheckWhite(_Param4MaskTex, lon, lat);
                float4 p5 = SampleParamMaskAndCheckWhite(_Param5MaskTex, lon, lat);
                float4 p6 = SampleParamMaskAndCheckWhite(_Param6MaskTex, lon, lat);

                bool p1Visible = (p1.a > 0.01);
                bool p2Visible = (p2.a > 0.01);
                bool p3Visible = (p3.a > 0.01);
                bool p4Visible = (p4.a > 0.01);
                bool p5Visible = (p5.a > 0.01);
                bool p6Visible = (p6.a > 0.01);

                bool anyParamVisible = p1Visible || p2Visible || p3Visible || p4Visible || p5Visible || p6Visible;

                if (!anyParamVisible)
                {
                    // No param visible
                    if (_PreserveNonRoad >= 0.5) return float4(originalCol.rgb, 1.0);
                    else return float4(0,0,0,0);
                }

                // Road detection
                float3 pixelRGB = originalCol.rgb;
                float3 fillRGB = _RoadFillRGB.rgb;
                float3 outlineRGB = _RoadOutlineRGB.rgb;

                float distFill = ColorDistance(pixelRGB, fillRGB);
                float distOutline = ColorDistance(pixelRGB, outlineRGB);

                float fillWeight = 1.0 - saturate(distFill / _Threshold);
                float outlineWeight = 1.0 - saturate(distOutline / _Threshold);
                float roadWeight = max(fillWeight, outlineWeight);

                if (roadWeight > 0.0)
                {
                    // Choose highlight color based on first visible param
                    float4 highlight;
                    if (p1Visible) highlight = _Param1HighlightColor;
                    else if (p2Visible) highlight = _Param2HighlightColor;
                    else if (p3Visible) highlight = _Param3HighlightColor;
                    else if (p4Visible) highlight = _Param4HighlightColor;
                    else if (p5Visible) highlight = _Param5HighlightColor;
                    else highlight = _Param6HighlightColor; // p6Visible must be true if reached here

                    highlight.a *= roadWeight;
                    return highlight;
                }
                else
                {
                    // No roads detected
                    if (_PreserveNonRoad >= 0.5) return float4(originalCol.rgb, 1.0);
                    else return float4(0,0,0,0);
                }
            }
            ENDCG
        }
    }
}
