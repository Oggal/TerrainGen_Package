

Shader "Custom/FadeToWhite" {
    Properties{
        _Color("Color", Color) = (1, 1, 1, 1)
    }

        SubShader{
            Tags {"Queue" = "Transparent" "RenderType" = "Transparent"}
            LOD 200

            CGPROGRAM
            #pragma surface surf Lambert

            struct Input {
                float3 worldNormal;
            };

            sampler2D _MainTex;

            fixed4 _Color;

            void surf(Input IN, inout SurfaceOutput o) {
                float angle = dot(IN.worldNormal, float3(0, 1, 0));
                angle = max((angle),0);
                o.Albedo = _Color.rgb * angle;
                o.Alpha = angle;
            }
            ENDCG
    }
        FallBack "Diffuse"
}
