Shader "Unlit/UVWave"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "black" {}
		
		_SpeedX("SpeedX", float) = 3.0
		_SpeedY("SpeedY", float) = 3.0
		_Scale("Scale", float) = 100
		_TileX("TileX", float) = 5
		_TileY("TileY", float) = 5

    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

			float _SpeedX;
			float _SpeedY;
			float _Scale;
			float _TileX;
			float _TileY;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
				float2 uv = i.uv;
				uv.x += sin((i.uv.x + i.uv.y) * _TileX + _Time.g * _SpeedX) / _Scale;
				uv.y += cos(i.uv.y*_TileY + _Time.g * _SpeedY) / _Scale;

				fixed4 col = tex2D(_MainTex, uv);

				// apply fog
                //UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
