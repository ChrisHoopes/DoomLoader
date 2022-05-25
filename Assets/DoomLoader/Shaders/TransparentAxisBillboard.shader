Shader "Doom/TransparentAxisBillboard" 
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
			};

			vertexOutput vert(vertexInput input)
			{
				vertexOutput output;

				input.vertex.x *= _ScaleX;
				input.vertex.y *= _ScaleY;

				output.tex = input.tex;
				output.tex.x = lerp(output.tex.x, 1 - output.tex.x, _UvTransform.x);
				output.tex.y = lerp(output.tex.y, 1 - output.tex.y, _UvTransform.y);


				//rotate towards camera keeping y = up
				float3 local = float3(input.vertex.x, input.vertex.y, 0);
				float3 offset = input.vertex.xyz - local;

				float3 upVector = half3(0, 1, 0);
				float3 forwardVector = UNITY_MATRIX_IT_MV[2].xyz;
				float3 rightVector = normalize(cross(forwardVector, upVector));

				float3 position = 0;
				position += local.x * rightVector;
				position += local.y * upVector;
				position += local.z * forwardVector;

				float4 rotated = float4(offset + position, 1);
				output.pos = UnityObjectToClipPos(rotated);
				//-----


				return output;
			}

			float4 frag(vertexOutput input) : COLOR
			{
				float4 color = tex2D(_MainTex, float2(input.tex.xy));

				color.a *= _Alpha;
				color.rgb *= _Illumination;

				return color;
			}

			ENDCG
		}
	}
}