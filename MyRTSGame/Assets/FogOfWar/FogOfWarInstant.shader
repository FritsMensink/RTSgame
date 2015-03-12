Shader "Custom/FogOfWarInstant" {
	Properties {
		_Color("Main Color",Color ) = (1,1,1,1)
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}
	SubShader {
		Tags { "Queue" = "Transparent" "RenderType"="Transparent" "LightMode"="ForwardBase"}
		Blend SrcAlpha OneMinusSrcAlpha
		Lighting off
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Lambert
		
		fixed4 LightingNoLighting(SurfaceOutput s, fixed3 lightdir, float aten){
			fixed4 color;
			color.rgb = s.Albedo;
			color.a = s.Alpha;
			return color;
		}
		
		fixed4 _Color;
		sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex;
		};

		void surf (Input IN, inout SurfaceOutput o) {
			half4 baseColor = tex2D (_MainTex, IN.uv_MainTex);
			o.Albedo = _Color.rgb *baseColor.b;
			o.Alpha = _Color.a - baseColor.g;
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
