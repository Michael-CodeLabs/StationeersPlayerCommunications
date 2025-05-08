Shader "Custom/Hologram"
{
    Properties
    {
        [Header(Color)]
        _Color ("Color", Color) = (1,0,0,1)
        _MainTex ("MainTexture", 2D) = "white" {}
        
        [Header(General)]
        _Brightness ("Brightness", Range(0.1, 6)) = 4
        _Alpha ("Alpha", Range(0, 1)) = 0.097
        _Direction ("Direction", Vector) = (0,1,0,0)
        
        [Header(Scanlines)]
        _ScanEnabled ("Scanlines Enabled", Range(0, 1)) = 1
        _ScanTiling ("Scan Tiling", Range(0.01, 1000)) = 160
        _ScanSpeed ("Scan Speed", Range(-2, 2)) = 0
        
        [Space(10)][Header(Fresnel)]
        _FresnelColor ("Fresnel Color", Color) = (1,1,1,1)
        _FresnelPower ("Fresnel Power", Range(0.1, 10)) = 1.45
        _Offset ("Offset", Float) = -300
    }
    
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        
        CGPROGRAM
        #pragma surface surf Unlit alpha:fade
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;
        float _Brightness;
        float _Alpha;
        float3 _Direction;
        float _ScanEnabled;
        float _ScanTiling;
        float _ScanSpeed;
        fixed4 _FresnelColor;
        float _FresnelPower;
        float _Offset;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
            float3 viewDir;
            float3 worldNormal;
        };

        half4 LightingUnlit(SurfaceOutput s, half3 lightDir, half atten)
        {
            return half4(s.Albedo * _Brightness, s.Alpha);
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            // Base texture and color
            fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
            fixed4 c = tex * _Color;
            
            // Scan lines effect
            float scan = 1.0;
            if (_ScanEnabled > 0.5)
            {
                float scanPos = IN.worldPos.y + _Offset + _Time.y * _ScanSpeed;
                scan = sin(scanPos * _ScanTiling) * 0.5 + 0.5;
                scan = lerp(1.0, scan, _ScanEnabled);
            }
            
            // Fresnel effect
            float fresnel = pow(1.0 - saturate(dot(IN.viewDir, IN.worldNormal)), _FresnelPower);
            fixed4 fresnelColor = fresnel * _FresnelColor;
            
            // Combine all effects
            o.Albedo = c.rgb * scan + fresnelColor.rgb;
            o.Alpha = (c.a * _Alpha) * scan + fresnel * _FresnelColor.a;
        }
        ENDCG
    }
    FallBack "Transparent"
}