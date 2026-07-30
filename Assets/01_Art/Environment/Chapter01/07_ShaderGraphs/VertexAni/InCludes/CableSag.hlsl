// CableSag.hlsl
// Shader Graph Custom Function Node용
// 오브젝트 로컬 좌표(Position, Space=Object)를 입력받아,
// "케이블 진행 방향 성분(스파인)"과 "단면 반지름 성분(두께)"을 분리해서 계산한다.
// (STEP 2-6b 3장 — Type 2(양끝 고정) 대상의 정적 성분. 동적 흔들림은 SG_WindSway가 별도로 담당)
//
// ★ v2 — 메쉬가 얇은 곡선으로 뭉개지는 버그 수정 (반지름 보존)
// ★ v3 — 처짐 정점 위치(SagPeakT) 파라미터화
//   기존엔 4T(1-T) 공식이라 항상 정중앙(T=0.5)에서만 최대로 처짐.
//   전신주 사이 간격마다 처짐 위치가 다르게 보이도록(전부 똑같이 중앙만 축 처지면
//   반복 패턴이 너무 티 남) SagPeakT(0~1)로 처짐 정점 위치를 자유롭게 지정 가능하게 함.
//   SagPeakT=0.5로 두면 기존 4T(1-T) 공식과 정확히 동일한 결과 (하위 호환).
//
// ★ 전제조건 (모델링 규칙)
//   - 케이블 메쉬는 로컬 X축을 따라 직선으로 뻗어있어야 함
//   - 피봇은 메쉬 정중앙 — 로컬 x 범위가 -MeshLength/2 ~ +MeshLength/2
//     (한쪽 끝 피봇이면 T 계산식에서 +0.5 오프셋을 제거할 것, 아래 T 계산 참고)
//   - 단면(원통 반지름)은 로컬 Y/Z 평면에 있어야 함
//
// Custom Function Node 설정:
//   Type   : File
//   Source : 이 파일
//   Name   : CableSag
//
// Inputs (그래프 슬롯 순서 — 이름이 아니라 순서로 바인딩됨, 반드시 아래 순서 그대로 추가할 것)
//   ObjectPos  : Vector3  ← Position(Space=Object) — 로컬 x=진행방향, y/z=단면 반지름
//   AnchorA    : Vector3  ← _AnchorA Property (월드 좌표, 케이블 시작점)
//   AnchorB    : Vector3  ← _AnchorB Property (월드 좌표, 케이블 끝점)
//   SagAmount  : Float    ← _SagAmount Property (처짐 깊이, m)
//   MeshLength : Float    ← _MeshLength Property (메쉬가 로컬 X축으로 뻗은 실제 길이, m)
//   SagPeakT   : Float    ← _SagPeakT Property (0~1, 처짐이 가장 깊은 지점의 위치. 0.5=정중앙, 신규)
//
// Outputs
//   SaggedWorldPos : Vector3  → SG_WindSway의 VertexWorldPos 입력으로 그대로 연결
//   T              : Float    → Weight 계산 및 위상 기준에 재사용
//   CableDir       : Vector3  → WindDirectionXZ를 케이블 진행축에 수직으로 계산할 때 재사용 (3.4절)

void CableSag_float(
    float3 ObjectPos, float3 AnchorA, float3 AnchorB, float SagAmount, float MeshLength,
    float SagPeakT,
    out float3 SaggedWorldPos, out float T, out float3 CableDir)
{
    // ① 진행 방향 성분 — 로컬 X를 0~1로 정규화
    //    피봇이 정중앙이라 x 범위가 -MeshLength/2 ~ +MeshLength/2 이므로 +0.5 오프셋 필요
    //    (만약 피봇이 한쪽 끝이라면 x 범위가 0~MeshLength이므로 이 줄을
    //     T = saturate(ObjectPos.x / max(MeshLength, 0.0001)); 로 되돌릴 것)
    T = saturate(ObjectPos.x / max(MeshLength, 0.0001) + 0.5);

    // ② 스파인(중심선) 위치 — 반지름 없는 "선"으로서의 케이블 경로
    float3 spine = lerp(AnchorA, AnchorB, T);

    // 비대칭 처짐 곡선 — SagPeakT 지점에서 최대(1), 양 끝(T=0, T=1)에서 0
    // SagPeakT=0.5일 때 기존 4T(1-T)와 수학적으로 동일한 결과가 나오도록 설계됨
    float peak = clamp(SagPeakT, 0.001, 0.999);
    float leftWidth = peak;
    float rightWidth = 1.0 - peak;
    float d = (T < peak)
        ? (peak - T) / max(leftWidth, 1e-4)   // 왼쪽: 정점에서 0, 왼쪽 끝(T=0)에서 1
        : (T - peak) / max(rightWidth, 1e-4); // 오른쪽: 정점에서 0, 오른쪽 끝(T=1)에서 1
    float sagCurve = 1.0 - d * d;

    spine += float3(0, -1, 0) * SagAmount * sagCurve;

    // ③ 케이블 진행축(새 커브의 접선 방향) — Anchor 두 점을 잇는 직선 방향으로 근사
    //    (정밀하게는 위치마다 접선이 살짝 다르지만, Sag가 완만하면 이 근사로 충분)
    float3 dir = normalize(AnchorB - AnchorA + 1e-5);

    // ④ 새 진행축 기준 단면 좌표계(right/up) 재구성
    //    원래 로컬 Y/Z가 향하던 방향을, 회전된 새 진행축에 맞게 다시 세움
    float3 worldUp = float3(0, 1, 0);
    float3 right = normalize(cross(worldUp, dir) + float3(1e-5, 0, 0));
    float3 up = cross(dir, right);

    // ⑤ 원래 반지름 성분(로컬 y, z)을 새 단면 좌표계에 투영해서 스파인에 더함
    //    → 정점이 스파인 한 점으로 뭉개지지 않고, 케이블 두께가 그대로 유지됨
    float3 radial = right * ObjectPos.y + up * ObjectPos.z;

    SaggedWorldPos = spine + radial;
    CableDir = dir;
}
