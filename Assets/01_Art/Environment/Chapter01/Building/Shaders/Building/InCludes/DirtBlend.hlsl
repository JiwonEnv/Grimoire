// STEP 2-3 — Building Shader: Dirt(페인트 벗겨짐) 블렌딩
//
// Shader Graph에서 사용법:
//   1. Custom Function Node 추가
//   2. Type: File, 이 파일 경로 지정
//   3. Name: DirtBlend
//   4. 아래 함수 시그니처와 "정확히 같은 순서"로 Input/Output 슬롯 구성
//      (Shader Graph Custom Function Node는 이름이 아니라 슬롯 순서로 .hlsl 함수에 인자를 넘김 —
//       순서가 하나라도 어긋나면 타입 불일치로 컴파일 에러가 남)
//
//      Input  (순서대로): BaseColor(3), UnderLayerColor(3), DirtMaskSample(1), DirtSeed(1),
//                          BaseNormal(3), DirtNormal(3), DirtIntensity(1),
//                          BaseSmoothness(1), DirtSmoothness(1), BaseMetallic(1), DirtMetallic(1)
//      Output (순서대로): OutColor(3), OutNormal(3), OutSmoothness(1), OutMetallic(1)
//
// 컨셉: 벽 표면에 "벗겨짐 블롭" 마스크를 노이즈로 만들고, 그 마스크 강도에 따라
//       1) 색을 벽돌 BaseColor → 벗겨진 안쪽 색(UnderLayerColor)으로 블렌드
//       2) 노멀도 평평한 벽 BaseNormal → 거친 DirtNormal로 블렌드
//       3) Smoothness/Metallic도 같이 블렌드 — 벗겨진 안쪽(녹/시멘트 등)은 보통
//          광택이 낮고 Metallic도 원래 재질과 달라지는 경우가 많아서, 색만 바뀌고
//          광택은 그대로면 "스티커 붙인 것처럼" 부자연스러워 보임.
//       DirtIntensity(0~1, MaterialPropertyBlock에서 건물마다 다르게 전달)로 전체 강도를 조절.

void DirtBlend_float(
    float3 BaseColor, float3 UnderLayerColor,
    float DirtMaskSample,
    float DirtSeed,
    float3 BaseNormal, float3 DirtNormal,
    float DirtIntensity,
    float BaseSmoothness, float DirtSmoothness,
    float BaseMetallic, float DirtMetallic,
    out float3 OutColor, out float3 OutNormal, out float OutSmoothness, out float OutMetallic)
{
    float mask = saturate(DirtMaskSample - (1.0 - DirtIntensity));
    float blend = smoothstep(0.0, 0.15, mask);

    OutColor = lerp(BaseColor, UnderLayerColor, blend);
    OutNormal = lerp(BaseNormal, DirtNormal, blend);
    OutSmoothness = lerp(BaseSmoothness, DirtSmoothness, blend);
    OutMetallic = lerp(BaseMetallic, DirtMetallic, blend);
}

// half 버전 (모바일/URP half precision 빌드 대응)
void DirtBlend_half(
    half3 BaseColor, half3 UnderLayerColor,
    half DirtMaskSample,
    half DirtSeed,
    half3 BaseNormal, half3 DirtNormal,
    half DirtIntensity,
    half BaseSmoothness, half DirtSmoothness,
    half BaseMetallic, half DirtMetallic,
    out half3 OutColor, out half3 OutNormal, out half OutSmoothness, out half OutMetallic)
{
    half mask = saturate(DirtMaskSample - (1.0h - DirtIntensity));
    half blend = smoothstep(0.0h, 0.15h, mask);

    OutColor = lerp(BaseColor, UnderLayerColor, blend);
    OutNormal = lerp(BaseNormal, DirtNormal, blend);
    OutSmoothness = lerp(BaseSmoothness, DirtSmoothness, blend);
    OutMetallic = lerp(BaseMetallic, DirtMetallic, blend);
}
