// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/HighlighShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_Color("Color", Color) = (1,1,1,1)
	    _LengthParam("Length Paramater", Float) = 1
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

            v2f vert (appdata v)
            {
                v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
			float4 _Color;
			float _LengthParam;

            fixed4 frag (v2f i) : SV_Target
            {
				float4 color = tex2D(_MainTex, i.uv) * _Color; // the color of the texture * the color of the image.color
				
				float t = abs(sin(_Time.z)); // goes from 1 -> 0 -> 1

				float l = length(float2(0.5, 0.5) - i.uv) * _LengthParam; // goes from 0 -> 1

				color = (color * (1 - (t * l))) + float4(0, t * l, 0, color.a * t * l);
				
				return color;
            }
            ENDCG
        }
    }
}
