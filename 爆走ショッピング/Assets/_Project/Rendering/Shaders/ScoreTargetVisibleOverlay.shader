Shader "Custom/ScoreTargetVisibleOverlay"
{
    // スコアターゲットを壁越しに表示するための半透明色です。
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 0, 0, 0.7)
    }

    SubShader
    {
        Tags
        {
            // URP 用の透明オーバーレイとして最後に描画します。
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Overlay"
        }

        Pass
        {
            Name "VisibleOverlay"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            Cull Back
            // 深度を書かず、常に手前に描画して壁越しに見えるようにします。
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                // オブジェクト空間の頂点をクリップ空間へ変換します。
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // スクリプト側から更新される色と透明度をそのまま出力します。
                return _BaseColor;
            }
            ENDHLSL
        }
    }
}
