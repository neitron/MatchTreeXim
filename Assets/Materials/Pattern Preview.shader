Shader "CS/Foil Preview"
{
	Properties
	{
		[PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
		_FoilTex("Foil Texture", 2D) = "white" {}
		_FoilSpec("Foil Spec mask", 2D) = "black" {}
		_SpecFactor("Spec factor", Range(0.0, 1.0)) = 0.3
		_Color("Tint", Color) = (1,1,1,1)
	}

		SubShader
		{
			Tags
			{
				"Queue" = "Transparent"
				"IgnoreProjector" = "True"
				"RenderType" = "Transparent"
				"PreviewType" = "Plane"
				"CanUseSpriteAtlas" = "True"
			}

			Cull Off
			Lighting Off
			ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha

			Pass
			{
				CGPROGRAM
					#pragma vertex vert
					#pragma fragment frag
					#pragma target 2.0

					#include "UnityCG.cginc"

					struct appdata_t
					{
						float4 vertex   : POSITION;
						float4 color    : COLOR;
						float2 texcoord : TEXCOORD0;
						UNITY_VERTEX_INPUT_INSTANCE_ID
					};

					struct v2f
					{
						float4 vertex   : SV_POSITION;
						fixed4 color : COLOR;
						float2 texcoord  : TEXCOORD0;
						float2 foilUV  : TEXCOORD2;
						float2 specUV  : TEXCOORD3;
						float4 worldPosition : TEXCOORD1;
						UNITY_VERTEX_OUTPUT_STEREO
					};

					fixed4 _Color;

					sampler2D _FoilTex;
					float4 _FoilTex_ST;

					sampler2D _FoilSpec;
					float4 _FoilSpec_ST;

					v2f vert(appdata_t v)
					{
						v2f OUT;
						UNITY_SETUP_INSTANCE_ID(v);
						UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
						OUT.worldPosition = v.vertex;
						OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

						OUT.texcoord = v.texcoord;

						OUT.foilUV = TRANSFORM_TEX(v.texcoord, _FoilTex);
						OUT.specUV = TRANSFORM_TEX(v.texcoord, _FoilSpec);

						OUT.color = v.color * _Color;
						return OUT;
					}

					sampler2D _MainTex;
					float _SpecFactor;
					fixed4 frag(v2f IN) : SV_Target
					{
						half4 mainC = tex2D(_MainTex, IN.texcoord);
						half4 spec = tex2D(_FoilSpec, IN.specUV + float2(0.0f, sin(_Time.x * 5))) * _SpecFactor;
						half4 color = tex2D(_FoilTex, IN.foilUV)* IN.color;

						return (color + spec.r) * mainC.r;
					}
				ENDCG
			}
	}
}
