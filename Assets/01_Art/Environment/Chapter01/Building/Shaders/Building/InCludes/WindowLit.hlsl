// WindowLit.hlsl
// STEP 2-4 — Window Glass 전용 동적 점등. 그리드 없음 (창문 1개 = 칸 1개).
//
// ★ 중요: WorldPos 입력에는 Shader Graph의 "Position(World)" 노드를 직접 연결하면 안 됨
// (그건 픽셀마다 달라지는 표면 좌표라서, 창문 1개 안에서도 값이 미세하게 달라짐).
// 대신 "Transform" 노드로 Position(0,0,0)을 Object→World로 변환한, 오브젝트
// 피봇의 월드 좌표(메쉬 전체에서 동일한 값 1개)를 연결해야 함.
//
// 시드를 C#이 넘겨주지 않고 오브젝트 좌표에서 직접 뽑아내므로, 이 머티리얼을 아무
// 오브젝트의 Glass에든 그냥 꽂으면 바로 동작한다 (MaterialPropertyBlock/코드 불필요).

float Hash21(float2 p)
{
    float h = dot(p, float2(127.1, 311.7));
    return frac(sin(h) * 43758.5453123);
}

void WindowLit_float(
    float3 PivotWorldPos, float LitChance, float FlickerChance,
    float ChangeInterval, float FlickerSpeed, float Time,
    out float Brightness)
{
    // 오브젝트 피봇 좌표 자체가 이미 "창문마다 고유한 값" — 별도 floor/grid 불필요.
    float2 windowId = PivotWorldPos.xz + PivotWorldPos.y * 1.7;

    // 점등 사이클 번호 — ChangeInterval초마다 패턴이 통째로 바뀜
    float cycle = floor(Time / max(ChangeInterval, 0.001));

    // 이 창문이 "지금 사이클에" 켜져 있는지
    float litRand = Hash21(windowId + cycle * 13.37);
    bool isLit = litRand < LitChance;

    // 이 창문이 "깜빡이는 창문"인지는 사이클과 무관하게 고정 (windowId만 사용)
    float2 flickerHashInput = windowId * 1.93;
    float flickerRand = Hash21(flickerHashInput);
    float flickerPhase = Hash21(flickerHashInput + 7.77) * 6.2831; // 0~2π

    bool isFlicker = flickerRand < FlickerChance;

    Brightness = 0.0;
    if (isLit)
    {
        if (isFlicker)
        {
            float wave = sin(Time * FlickerSpeed + flickerPhase);
            Brightness = wave > 0.3 ? 1.0 : 0.0;
        }
        else
        {
            Brightness = 1.0;
        }
    }
}
