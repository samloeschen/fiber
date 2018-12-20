// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Sam Loeschen/Vertex Color Unlit" {
	Properties{
	}
	SubShader{
		Blend SrcAlpha OneMinusSrcAlpha
		Tags { "RenderType"="Transparent" }

		Cull Off
		Pass{

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			struct vertexInput {
				half4 vertex : POSITION;
				float4 color : TEXCOORD1;
			};

			struct vertexOutput {
				half4 pos : SV_POSITION;
				float4 color : TEXCOORD1;
			};

			vertexOutput vert(vertexInput input) {
				vertexOutput output;
				output.pos = UnityObjectToClipPos(input.vertex);
				output.color = input.color;
				return output;
			}

			float4 frag(vertexOutput input) : COLOR {
				return input.color;
				return half4(pow(input.color.rgb, 2.16), 1);
			}
			ENDCG
		}
	}
}