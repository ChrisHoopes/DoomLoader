Shader "Doom/TransparentOmniBillboard" 
{
	Properties
	{
		[PerRenderData]_MainTex("Texture Image", 2D) = "white" {}
		[PerRenderData]_ScaleX("Scale X", Float) = 1.0
		[PerRenderData]_ScaleY("Scale Y", Float) = 1.0
		[PerRenderData]_Illumination("Self Illumination", Float) = 0.0
		[PerRenderData]_UvTransform("UV Transform", Vector) = (0,0,0,0)
		[PerRenderData]_Alpha("Alpha Multiplier", Float) = 0.5
	}

	SubShader
	{
		Tags
		{
			"DisableBatching" = "True"
		}

		Pass
		{
			Tags{ "LightMode" = "ForwardBase" }

			Blend SrcAlpha OneMinusSrcAlpha
			CGPROGRAM

			#pragma vertex vert  
			#pragma fragment frag

			#include "UnityCG.cginc"

			uniform sampler2D _MainTex;
			uniform float _ScaleX;
			uniform float _ScaleY;
			uniform float _Illumination;
			uniform float4 _UvTransform;
			uniform float _Alpha;

			struct vertexInput
			{
				float4 vertex : POSITION;
				float4 tex : TEXCOORD0;
			};

			struct vertexOutput
			{
				float4 pos : SV_POSITION;
				float4 tex : TEXCOORD0;
				float depth : TEXCOORD1;
			};

			vertexOutput vert(vertexInput input)
			{
				vertexOutput output;

				input.vertex.x *= _ScaleX;
				input.vertex.y *= _ScaleY;

				output.tex = input.tex;
				output.tex.x = lerp(output.tex.x, 1 - output.tex.x, _UvTransform.x);
				output.tex.y = lerp(output.tex.y, 1 - output.tex.y, _UvTransform.y);

				//rotate towards camera
				output.pos = mul(UNITY_MATRIX_P,
					mul(UNITY_MATRIX_MV, float4(0.0, 0.0, 0.0, 1.0))
					+ float4(input.vertex.x, input.vertex.y, 0.0, 0.0));

				//remove camera slope from depth
				float4 rootPos = mul(UNITY_MATRIX_P,
					mul(UNITY_MATRIX_MV, float4(0.0, 1.0, 0.0, 1.0))
					+ float4(input.vertex.x, input.vertex.y, 0.0, 0.0));

				output.depth = rootPos.z / rootPos.w;

				return output;
			}

			void frag(vertexOutput input, out float4 oFragment : COLOR0, out float depth : DEPTH0)
			{
				float4 color = tex2D(_MainTex, float2(input.tex.xy));
				
				color.a *= _Alpha;
				color.rgb *= _Illumination;
				
				oFragment = color;
				depth = input.depth;
			}

			ENDCG
		}
	}
}