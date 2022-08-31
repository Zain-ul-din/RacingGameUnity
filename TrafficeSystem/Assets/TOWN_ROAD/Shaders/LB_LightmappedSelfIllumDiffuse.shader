Shader "LaikaBoss/LightmappedSelfIllumDiffuse" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_LightmapTex ("Base (RGB)", 2D) = "white" {}
		_LMpower ("LM power", Float ) = 5.0 
	}
	
	SubShader {
		Lighting Off
		
		Tags { "Queue" = "Geometry-3" "RenderType" = "Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Unlit
		
		sampler2D _MainTex;
		
		sampler2D _LightmapTex;
		float _LMpower;
		
		float4 _Color;
		
		struct Input {
			float2 uv_MainTex;
			float2 uv2_LightmapTex;
		};
		
		half4 LightingUnlit (SurfaceOutput s, half3 lightDir, half atten) 
		{
           return float4 (0,0,0,s.Alpha); 
        }
        
        void surf (Input IN, inout SurfaceOutput o) {
			half4 colorMap = tex2D (_MainTex, IN.uv_MainTex);
			half4 lm = tex2D (_LightmapTex, IN.uv2_LightmapTex);

			float3 em = DecodeLightmap(lm);
			
			o.Emission =  colorMap.rgb * em * _LMpower;
			o.Alpha = 1;
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
