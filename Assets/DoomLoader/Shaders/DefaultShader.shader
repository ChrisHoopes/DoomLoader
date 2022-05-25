Shader "Doom/Default" 
{
	Properties
	{
		[PerRenderData]_SectorLight("Sector Light", Color) = (1,1,1,1)
		[PerRenderData]_MainTex("Albedo (RGB)", 2D) = "white" {}
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque" }

		CGPROGRAM
		#pragma surface surf SimpleSpecular vertex:vert fullforwardshadows noambient
		#pragma target 3.0

		struct Input 
		{
			float2 uv_MainTex;
		};

		struct SurfaceOutputCustom
		{
			fixed3 Albedo;
			fixed3 Normal;
			fixed3 Emission;
			fixed Alpha;
		};

		void vert(inout appdata_full v, out Input o)
		{
			UNITY_INITIALIZE_OUTPUT(Input,o);
		}

		sampler2D _MainTex;
		uniform float4 _SectorLight;

		half4 LightingSimpleSpecular(SurfaceOutputCustom s, half3 lightDir, half3 viewDir, half atten)
		{
			half4 c;

			half diff = max(0, dot(s.Normal, lightDir));

			c.rgb = s.Albedo * _LightColor0.rgb * diff * atten;
			c.a = 1;
			return c;
		}

		void surf(Input IN, inout SurfaceOutputCustom o)
		{
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
			o.Albedo = c.rgb;
			o.Emission = c.rgb * _SectorLight * unity_AmbientSky;
		}

		ENDCG
	}

	FallBack "Diffuse"
}