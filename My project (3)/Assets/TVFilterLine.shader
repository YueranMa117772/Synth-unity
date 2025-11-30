Shader "Unlit/TVFilterLine"
{
    Properties
    {
        _Cutoff ("Cutoff (0-1)", Range(0,1)) = 0.5
        _LineColor ("Line Color", Color) = (1,1,1,1)
        _BGColor ("Background Color", Color) = (0,0,0,1)
        _Thickness ("Line Thickness", Range(0.001,0.1)) = 0.01
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _Cutoff;
            float4 _LineColor;
            float4 _BGColor;
            float _Thickness;

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // 斜线 → cutoff 变平
                float yLine = (uv.x < _Cutoff) ? uv.x : _Cutoff;

                float d = abs(uv.y - yLine);
                float alpha = step(d, _Thickness);

                return lerp(_BGColor, _LineColor, alpha);
            }
            ENDCG
        }
    }
}
