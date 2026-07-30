// WindSway.hlsl
// Shader Graph Custom Function Node용 — SG_WindSway Sub Graph 안에서 사용
// STEP 2-6b 1장(바람 흔들림 코어 커널) — 천/깃발/차양/현수막/풀/매달린 전선 등
// "고정점 거리 기반 가중치 × sine + 노이즈 난류" 방식으로 정점을 흔든다.
//
// Custom Function Node 설정:
//   Type   : File
//   Source : 이 파일
//   Name   : WindSway   (Shader Graph가 자동으로 _float 접미사를 붙여 찾음)
//
// Inputs (그래프 슬롯 순서 — 이름이 아니라 순서로 바인딩됨, 9.3절/17.2절과 동일 원칙 — 반드시 아래 순서 그대로 추가할 것)
//   VertexWorldPos : Vector3  ← Position(Object) → Transform(Object→World)
//   PivotWorldPos  : Vector3  ← Vector3(0,0,0)   → Transform(Object→World)
//   Weight         : Float    ← 대상별로 소스가 다름 (버텍스 컬러 R채널 / 로컬 Y좌표 Remap 등, 1.5절 참고)
//   WindDirection  : Vector2  ← _WindDirection Property
//   WindSpeed      : Float    ← _WindSpeed Property
//   WindStrength   : Float    ← _WindStrength Property
//   NoiseScale     : Float    ← _WindNoiseScale Property
//   Turbulence     : Float    ← _WindTurbulence Property (0~1)
//   Time           : Float    ← Time(Node).Time
//
// Outputs
//   DisplacedWorldPos : Vector3
//
// 주의(★ 1.6절 트러블슈팅과 동일 원칙):
//   - VertexWorldPos는 정점마다 달라야 하는 값이라 Position(World)/정점별 Transform 결과를 그대로 써야 하고,
//     PivotWorldPos는 오브젝트 전체에서 고정된 값 1개(개체 간 위상차용)라 반드시 Vector3(0,0,0)을
//     Object→World로 Transform한 결과를 써야 한다. 이 둘을 바꿔 넣으면
//     "메쉬 전체가 한 덩어리로 움직이거나" 반대로 "위상차가 전혀 없는" 증상이 나온다 (1.2절 참고).
//   - 이 함수의 출력은 World Space이므로, 그래프에서 반드시 Transform(World→Object)로
//     되돌린 뒤 Vertex Position 소켓에 연결해야 한다. 빼먹으면 오브젝트가 씬 원점 쪽으로
//     순간이동한 것처럼 보인다.

float WindSway_Hash21(float2 p)
{
    float h = dot(p, float2(127.1, 311.7));
    return frac(sin(h) * 43758.5453123) * 2.0 - 1.0; // -1~1 범위
}

void WindSway_float(
    float3 VertexWorldPos, float3 PivotWorldPos, float Weight,
    float2 WindDirection, float WindSpeed, float WindStrength,
    float NoiseScale, float Turbulence, float Time,
    out float3 DisplacedWorldPos)
{
    float2 dir = normalize(WindDirection + 1e-5); // 0벡터 입력 시 NaN 방지

    // 위상(Phase) — 피봇 좌표 기반. 같은 프리팹을 여러 개 배치해도
    // 서로 다른 시점에 흔들리게 하기 위함 (17.2절 오브젝트 좌표 시드와 동일 원칙).
    // 피봇 좌표가 큰 값일 수 있어 정밀도 손실 방지를 위해 소수 계수를 곱해 스케일을 낮춘다
    // (13.5절 원인 3 — GPU float32 정밀도 문제와 동일 이유).
    float phase = dot(PivotWorldPos.xz * 0.37, float2(12.9898, 78.233));

    float wave = sin(Time * WindSpeed + phase);

    // 보조 난류 — 정점 좌표 기반이라 메쉬 위를 흐르는 느낌을 준다.
    // Time을 살짝 섞어서 난류 패턴 자체도 서서히 흘러가게 함.
    float2 noiseUV = VertexWorldPos.xz * NoiseScale + Time * 0.1;
    float noise = WindSway_Hash21(noiseUV);

    // Turbulence=0이면 규칙적인 sine 그대로, 1이면 노이즈로 완전히 뒤덮인 불규칙한 흔들림
    float combined = lerp(wave, wave * noise, saturate(Turbulence));

    float3 offset = float3(dir.x, 0, dir.y) * combined * saturate(Weight) * WindStrength;
    DisplacedWorldPos = VertexWorldPos + offset;
}
