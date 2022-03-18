Shader "Hidden/StillHighlightShader"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_Color("Color", Color) = (1,1,1,1)
		_DistanceX("Distance X Paramater", float) = 0.05
		_DistanceY("Distance Y Paramater", float) = 0.05

	}
		SubShader
		{
			// No culling or depth
			Cull Off ZWrite Off ZTest Always

			Tags
			{
				"Queue" = "Transparent"
			}

			Pass
			{
				Blend SrcAlpha OneMinusSrcAlpha

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

				v2f vert(appdata v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.uv = v.uv;
					return o;
				}

				sampler2D _MainTex;
				float4 _Color;
				float4 _MainTex_TexelSize;
				float _DistanceX;
				float _DistanceY;

				fixed4 frag(v2f i) : SV_Target
				{
					float4 color = tex2D(_MainTex, i.uv) * _Color; // the color of the texture * the color of the image.color

					float DEGREES_TO_CHECK = 360;
					float DIRECTIONS_COUNT = 4;
					float STEPS_SIZE = DEGREES_TO_CHECK / DIRECTIONS_COUNT;

					int ITERATION_COUNT = 20;
					float X_DISTANCE_VAL = _MainTex_TexelSize.x * 5;
					float Y_DISTANCE_VAL = _MainTex_TexelSize.y * 5;

					float strengthOfGreen = ITERATION_COUNT;
					[loop]
					for (float rads = 0; rads <= DEGREES_TO_CHECK + 1; rads += STEPS_SIZE)
					{
						float2 dir = float2(X_DISTANCE_VAL * sin(rads), Y_DISTANCE_VAL * cos(rads));
						
						float currEdgeDistance = 0;

						[loop]
						for (int j = 1; j <= ITERATION_COUNT; j += 1)
						{
							currEdgeDistance += tex2D(_MainTex, i.uv + dir * j).a;
						}
						strengthOfGreen = min(strengthOfGreen, currEdgeDistance);
					}

					strengthOfGreen = 1 - (strengthOfGreen / ITERATION_COUNT);

					float distFromEdgeX = 1 - (min(min(i.uv.x, 1 - i.uv.x), _DistanceX) / _DistanceX);
					float distFromEdgeY = 1 - (min(min(i.uv.y, 1 - i.uv.y), _DistanceY) / _DistanceY);

					float distFromEdge = max(distFromEdgeX, distFromEdgeY);


					//strengthOfGreen = max(strengthOfGreen, 1 - (distFromEdge * 20));
					strengthOfGreen = max(strengthOfGreen, distFromEdge);

					float strengthOfColor = 1 - strengthOfGreen;

					return color * strengthOfColor + float4(0, strengthOfGreen, 0, color.a);
				}
				ENDCG
			}
		}
}
