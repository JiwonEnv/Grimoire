// RoadDamageBlend.hlsl
// STEP 2-5 도로 버텍스 컬러 시스템 — Shader Graph Custom Function Node용
// DirtBlend.hlsl(8~9절)과 동일한 패턴: Shader Graph는 이름이 아니라 "슬롯 순서"로
// 이 함수에 인자를 바인딩하므로, 그래프 쪽 입출력 슬롯 순서를 이 함수 파라미터
// 순서와 반드시 동일하게 맞출 것 (9.3절 트러블슈팅 참고).
//
// 입력 순서:
//   BaseColor, CrackColor, DirtColor,
//   BaseNormal, DamageNormal,
//   BaseSmoothness, WetSmoothness,
//   DamageMask(R), DirtMask(G), PuddleMask(B),
//   DamageThreshold, DirtThreshold, PuddleThreshold
//
// 출력 순서:
//   OutColor, OutNormal, OutSmoothness
//
// 웅덩이 반사 강도는 별도 Reflectance/Specular 출력 없이 Smoothness만으로 처리한다.
// 물의 실제 F0(기본 반사율)는 약 0.02~0.04로, URP Lit의 기본 비금속 반사율(0.04)과
// 거의 같다. Metallic은 항상 0으로 고정하고(물은 비금속), Smoothness를 올리는 것만으로
// Fresnel 효과가 비스듬한 각도에서 알아서 강한 반사를 만들어준다 — Reflection Probe와
// 결합하면 그것으로 충분하다 (6.5절).

void RoadDamageBlend_float(
    float3 BaseColor, float3 CrackColor, float3 DirtColor,
    float3 BaseNormal, float3 DamageNormal,
    float BaseSmoothness, float WetSmoothness,
    float DamageMask, float DirtMask, float PuddleMask,
    float DamageThreshold, float DirtThreshold, float PuddleThreshold,
    out float3 OutColor, out float3 OutNormal, out float OutSmoothness)
{
    // threshold 미만은 0으로 잘라서, 같은 모듈이어도 인스턴스마다
    // 보이는 영역을 다르게 만들 수 있게 함 (위치 다양성 확보 — 9절 방식과 동일 사고)
    float damageVisible = step(DamageThreshold, DamageMask);
    float dirtVisible    = step(DirtThreshold,    DirtMask);
    float puddleVisible  = step(PuddleThreshold,  PuddleMask);

    float damage  = DamageMask  * damageVisible;
    float dirt    = DirtMask    * dirtVisible;
    float puddle  = PuddleMask  * puddleVisible;

    // ① 균열 블렌드 (BaseColor)
    float3 color = lerp(BaseColor, CrackColor, damage);

    // ② 오염 블렌드 (어둡게 곱연산, BaseColor 위에 추가로 적용)
    color = lerp(color, color * DirtColor, dirt);

    // ③ 노멀맵 블렌드 (파손 단차/자갈) — damage 마스크 재사용
    float3 normal = lerp(BaseNormal, DamageNormal, damage);

    // ④ 웅덩이 Smoothness 상승 — puddle 마스크 (Reflection Probe 반사는 이것만으로 충분)
    float smoothness = lerp(BaseSmoothness, WetSmoothness, puddle);

    OutColor = color;
    OutNormal = normal;
    OutSmoothness = smoothness;
}
