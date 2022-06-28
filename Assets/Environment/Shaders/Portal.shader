Shader "Custom/Portal"
{
    Properties
    {
        _InactiveColour ("Inactive Colour", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }
        LOD 100
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2_f
            {
                float4 vertex : SV_POSITION;
                float4 screen_pos : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _InactiveColour;
            int displayMask; // set to 1 to display texture, otherwise will draw test colour


            v2_f vert(const appdata v)
            {
                v2_f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.screen_pos = ComputeScreenPos(o.vertex);
                return o;
            }

            fixed4 frag(v2_f i) : SV_Target
            {
                const float2 uv = i.screen_pos.xy / i.screen_pos.w;
                const fixed4 portal_col = tex2D(_MainTex, uv);
                return portal_col * displayMask + _InactiveColour * (1 - displayMask);
            }
            ENDCG
        }
    }
    Fallback "Standard" // for shadows
}