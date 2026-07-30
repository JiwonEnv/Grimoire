// RiverFlow.hlsl
// Shader Graph Custom Function Node용
// 강물 표면의 진행파(Traveling Wave) — 모듈 타일을 이어붙여도 이음새가 안 보이도록
// 위상을 "오브젝트 피봇 좌표"가 아니라 "정점의 월드 좌표"에서 직접 뽑는다.
// (STEP 2-6b 4장 — 1~3장과 반대 방향의 원리: 오브젝트별로 다르게가 아니라
//  월드 좌표의 연속함수로 만들어서 모듈 경계에서 흐름이 끊기지 않게 함)
//
// Custom Function Node 설정:
//   Type   : File
//   Source : 이 파일
//   Name   : RiverFlow
//
// Inputs (그래프 슬롯 순서 — 이름이 아니라 순서로 바인딩됨, 반드시 아래 순서 그대로 추가할 것)
//   VertexWorldPos   : Vector3  ← Position(Space=World)
//   FlowDirectionXZ  : Vector2  ← _FlowDirectionXZ Property
//   FlowSpeed        : Float    ← _FlowSpeed Property
//   WaveAmplitude    : Float    ← _WaveAmplitude Property
//   WaveFrequency    : Float    ← _WaveFrequency Property
//   NoiseScale       : Float    ← _NoiseScale Property
//   Turbulence       : Float    ← _Turbulence Property
//   TimeValue        : Float    ← Time 노드의 Time 출력 (예약어 충돌 방지, 1.9절과 동일 이유)
//
// Outputs
//   DisplacedWorldPos : Vector3  → 원본 VertexWorldPos에 높이만 더한 결과
//                                  (3.9절 원칙 — "대체"가 아니라 "더하기")

float RiverFlow_Hash21(float2 p)
{
    float h = dot(p, float2(127.1, 311.7));
    return frac(sin(h) * 43758.5453123) * 2.0 - 1.0;
}

void RiverFlow_float(
    float3 VertexWorldPos, float2 FlowDirectionXZ, float FlowSpeed,
    float WaveAmplitude, float WaveFrequency, float NoiseScale, float Turbulence,
    float TimeValue,
    out float3 DisplacedWorldPos)
{
    float2 dir = normalize(FlowDirectionXZ + 1e-5);

    // 진행파 — 파동의 위상이 시간에 따라 dir 방향으로 흘러가도록
    // (제자리 진동인 1장 sin(Time*Speed+phase)와 달리, 좌표 자체가 위상에 곱해짐)
    float phase = dot(VertexWorldPos.xz, dir) * WaveFrequency;
    float wave = sin(phase - TimeValue * FlowSpeed);

    // 완전히 규칙적인 파동만은 아니게 노이즈를 섞음 (1장 Turbulence와 같은 목적)
    // 노이즈도 월드 좌표 기반이라야 모듈 경계에서 끊기지 않음
    float2 noiseUV = VertexWorldPos.xz * NoiseScale - TimeValue * FlowSpeed * 0.1 * dir;
    float noise = RiverFlow_Hash21(floor(noiseUV * 10.0) / 10.0);

    float combined = lerp(wave, wave * 0.6 + noise * 0.4, saturate(Turbulence));
    float height = combined * WaveAmplitude;

    // 원본 위치에 "더하기" — 통째로 대체하면 3장에서 겪은 것과 같은 뭉개짐 버그 재발
    DisplacedWorldPos = VertexWorldPos + float3(0, height, 0);
}
