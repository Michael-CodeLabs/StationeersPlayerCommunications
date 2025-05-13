Shader "Custom/WalkieTalkieScreen"
{
    Properties
    {
        [Header(Display)]
        _BackgroundColor ("Background Color", Color) = (0.1, 0.1, 0.1, 1)
        _SegmentColor ("Segment Color", Color) = (0.2, 0.8, 0.2, 1) // Classic green
        _Brightness ("Brightness", Range(0.1, 5)) = 1.5
        
        [Header(Segments)]
        _SegmentTex ("Segment Texture", 2D) = "white" {}
        _SegmentCount ("Segment Count", Vector) = (7, 1, 0, 0) // 7-segment display
        _SegmentSpacing ("Segment Spacing", Range(0, 0.5)) = 0.1
        
        [Header(Effects)]
        _Scanlines ("Scanline Intensity", Range(0, 1)) = 0.3
        _ScanlineCount ("Scanline Count", Float) = 100
        _FlickerSpeed ("Flicker Speed", Range(0, 5)) = 0.5
        _FlickerIntensity ("Flicker Intensity", Range(0, 0.5)) = 0.05
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

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            fixed4 _BackgroundColor;
            fixed4 _SegmentColor;
            float _Brightness;
            sampler2D _SegmentTex;
            float2 _SegmentCount;
            float _SegmentSpacing;
            float _Scanlines;
            float _ScanlineCount;
            float _FlickerSpeed;
            float _FlickerIntensity;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Calculate segment UVs
                float2 segmentUV = i.uv * _SegmentCount;
                float2 segmentID = floor(segmentUV);
                float2 segmentLocalUV = frac(segmentUV);
                
                // Apply spacing between segments
                segmentLocalUV = (segmentLocalUV - _SegmentSpacing * 0.5) / (1.0 - _SegmentSpacing);
                
                // Sample segment texture (black/white mask)
                fixed segmentMask = tex2D(_SegmentTex, segmentLocalUV).r;
                
                // Create scanlines effect
                float scanline = 1.0 - _Scanlines * 0.5 * (1.0 + sin(i.uv.y * _ScanlineCount * 3.14159 * 2.0));
                
                // Create subtle flicker
                float flicker = 1.0 - _FlickerIntensity * (0.5 + 0.5 * sin(_Time.y * _FlickerSpeed * 10.0));
                
                // Combine everything
                fixed3 col = lerp(_BackgroundColor.rgb, _SegmentColor.rgb * _Brightness * flicker, segmentMask);
                col *= scanline;
                
                return fixed4(col, 1.0);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}