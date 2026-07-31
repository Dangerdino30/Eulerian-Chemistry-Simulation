Shader "Unlit/UI"
{
    Properties
    {
        _CursorSquareOpacity ("CursorSquareOpacity", float) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        
        Blend SrcAlpha OneMinusSrcAlpha 
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "CustomShader.cginc"

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

            float _CursorSquareOpacity;
            uniform float4 _MousePos;
            uniform int _MousePosX;
            uniform int _MousePosY;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            float3 BlueGreenRedGradient(float c)
            {
                return float3(2*c-1,1-abs(0.5-c)*2 ,1-2*c);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = fixed4(0,0,0,0);
                
                int2 cell = int2(floor(i.uv.x * _GridWidth), floor(i.uv.y * _GridHeight));
                
                if (cell.x == _MousePosX && cell.y == _MousePosY)
                    col = fixed4(1,1,1,_CursorSquareOpacity);

                return col;
            }
            ENDCG
        }
    }
}
