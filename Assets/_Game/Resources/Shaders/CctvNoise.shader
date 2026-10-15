// 감시카메라 화면이 지지직거리는 듯한 노이즈를 입히는 후처리 셰이더.
// CctvNoise(PostProcessEffectSettings)가 이 셰이더로 화면 전체를 다시 그린다.
//
// 이미지 파일 없이 수식으로만 만든다. 세기(_Intensity)가 0이면 화면이 그대로이고, 1에 가까울수록 모두 강해진다.
//   가로 떨림   화면의 가로줄 몇 개가 무작위로 옆으로 밀린다
//   색 번짐     빨강과 파랑이 서로 반대쪽으로 어긋난다
//   지지직 입자 매 순간 바뀌는 흑백 점
//   주사선      옛날 모니터 같은 가는 가로줄
//   흐르는 띠   위에서 아래로 천천히 내려가는 밝은 띠
//
// 빌드에도 포함되도록 Resources 폴더에 둔다. (코드가 Shader.Find로 이름을 찾아 쓰기 때문)
Shader "Hidden/Custom/CctvNoise"
{
    HLSLINCLUDE

        #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"

        TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
        float _Intensity;
        float _NoiseTime;
        float4 _Resolution;     // (가로 픽셀, 세로 픽셀, 0, 0)
        float _LineJitter;
        float _ColorSplit;
        float _Static;
        float _Scanline;

        // 0~1 사이의 의사 난수. 같은 입력이면 항상 같은 값이 나온다.
        float Hash(float2 p)
        {
            return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
        }

        float4 Frag(VaryingsDefault i) : SV_Target
        {
            float2 uv = i.texcoord;
            float k = _Intensity;
            // 노이즈 모양이 1초에 24번 바뀌게 한다. (매 프레임 바뀌면 너무 부드러워 보인다)
            float tick = floor(_NoiseTime * 24.0);

            // 가로 떨림: 화면을 가는 가로줄로 나누고, 세기가 셀수록 더 많은 줄이 더 크게 밀린다.
            float row = floor(uv.y * 120.0);
            float shifted = step(1.0 - 0.35 * k, Hash(float2(row, tick)));
            uv.x += (Hash(float2(row + 7.3, tick)) - 0.5) * _LineJitter * k * shifted;

            // 색 번짐: 빨강은 오른쪽, 파랑은 왼쪽 자리의 색을 가져온다.
            float split = _ColorSplit * k;
            float3 color;
            color.r = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(split, 0)).r;
            color.g = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).g;
            color.b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(split, 0)).b;

            // 감시카메라처럼 색을 조금 뺀다.
            float luminance = dot(color, float3(0.299, 0.587, 0.114));
            color = lerp(color, luminance.xxx, 0.5 * k);

            // 지지직 입자: 2픽셀 크기의 흑백 점
            float2 pixel = floor(i.texcoord * _Resolution.xy / 2.0);
            float grain = Hash(pixel + tick * 1.37);
            color = lerp(color, grain.xxx, _Static * k);

            // 주사선
            float scan = 0.5 + 0.5 * sin(i.texcoord.y * _Resolution.y * 1.2);
            color *= 1.0 - _Scanline * k * scan;

            // 흐르는 띠: 위에서 아래로 내려가는 밝은 띠
            float bandY = 1.0 - frac(_NoiseTime * 0.25);
            float band = 1.0 - smoothstep(0.0, 0.05, abs(i.texcoord.y - bandY));
            color += band * 0.12 * k;

            return float4(color, 1.0);
        }

    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM
                #pragma vertex VertDefault
                #pragma fragment Frag
            ENDHLSL
        }
    }
}
