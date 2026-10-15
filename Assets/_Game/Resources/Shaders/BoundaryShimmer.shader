// 맵의 경계를 보여 주는 일렁이는 벽. (배틀그라운드의 자기장 벽 같은 효과)
// BoundaryShimmer 스크립트가 지형의 네 가장자리에 세운 벽 메시에 이 셰이더를 쓴다.
//
// 벽 뒤의 화면을 한 장 찍어 두고(GrabPass), 그것을 물결 모양으로 조금씩 밀어서 다시 그린다.
// 그래서 벽 너머의 숲이 아지랑이처럼 일렁여 보인다. 그 위에 색과 위로 흐르는 가는 줄무늬를 더한다.
//
// 메시의 UV는 미터 단위다. (u: 경계를 따라 잰 거리, v: 지형 바닥에서 잰 높이)
// 그래서 벽이 아무리 길어도 무늬의 크기가 일정하다.
//
// 세기는 두 가지로 정한다.
//   높이     위로 갈수록 옅어져서 하늘로 녹아들 듯 사라진다
//   거리     플레이어에게서 먼 곳은 _FarStrength만큼만 보이고, 가까울수록 또렷해진다
//
// 빌드에도 포함되도록 Resources 폴더에 둔다. (코드가 Shader.Find로 이름을 찾아 쓰기 때문)
Shader "Hidden/Custom/BoundaryShimmer"
{
    Properties
    {
        _Color ("색", Color) = (0.25, 0.55, 1, 1)
        _Height ("벽의 높이 (m)", Float) = 30
        _Glow ("빛나는 정도", Range(0, 2)) = 0.6
        _Distortion ("일렁이는 정도", Range(0, 0.05)) = 0.015
        _NearDistance ("또렷해지는 거리 (m)", Float) = 30
        _FarStrength ("멀리서 보이는 정도", Range(0, 1)) = 0.15
        _PlayerPos ("플레이어 위치", Vector) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags { "Queue" = "Transparent+10" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

        // 이름을 붙인 GrabPass는 한 프레임에 한 번만 화면을 찍는다.
        GrabPass { "_BoundaryGrab" }

        Pass
        {
            Cull Off
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            sampler2D _BoundaryGrab;
            fixed4 _Color;
            float _Height;
            float _Glow;
            float _Distortion;
            float _NearDistance;
            float _FarStrength;
            float4 _PlayerPos;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 grabPos : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                UNITY_FOG_COORDS(3)
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.grabPos = ComputeGrabScreenPos(o.pos);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float t = _Time.y;
                float2 p = i.uv;

                // 일렁임: 서로 다른 빠르기와 방향의 물결 두 겹을 겹쳐서 같은 모양이 반복되지 않게 한다.
                float waveA = sin(p.x * 0.35 + t * 1.7 + sin(p.y * 0.25 - t * 1.1) * 2.0);
                float waveB = sin(p.x * 0.9 - p.y * 0.6 + t * 2.3);
                float wave = (waveA + waveB) * 0.5;

                // 위로 흐르는 가는 줄무늬. 물결을 따라 휘어진다.
                float lines = pow(saturate(sin(p.y * 1.2 - t * 3.0 + wave * 0.8) * 0.5 + 0.5), 8.0);

                // 높이: 땅에서 1m 사이에 나타나고, 위로 갈수록 옅어진다.
                float heightFade = saturate(p.y + 1.0) * saturate(1.0 - p.y / _Height);
                heightFade *= heightFade;

                // 거리: 플레이어에게 가까운 부분일수록 또렷하다.
                float toPlayer = length(i.worldPos.xz - _PlayerPos.xz);
                float nearness = saturate(1.0 - toPlayer / _NearDistance);
                float strength = lerp(_FarStrength, 1.0, nearness) * heightFade;

                // 벽 뒤의 화면을 물결만큼 밀어서 가져온다.
                float2 screenUV = i.grabPos.xy / i.grabPos.w;
                float2 offset = float2(wave, waveA * waveB) * _Distortion * strength;
                fixed3 behind = tex2D(_BoundaryGrab, screenUV + offset).rgb;
                // 벽 너머가 살짝 그 색으로 물든다.
                behind = lerp(behind, behind * _Color.rgb * 1.5, 0.35 * strength);

                fixed3 glow = _Color.rgb * (0.25 + lines * 0.75 + (wave * 0.5 + 0.5) * 0.3) * strength * _Glow;
                // 안개 속에서는 빛도 함께 흐려진다. (벽 뒤의 화면은 이미 안개가 낀 채로 찍혀 있다)
                UNITY_APPLY_FOG_COLOR(i.fogCoord, glow, fixed4(0, 0, 0, 0));

                return fixed4(behind + glow, 1.0);
            }
            ENDCG
        }
    }
}
