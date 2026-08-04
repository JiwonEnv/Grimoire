using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전신주(Pole)를 일정 간격으로 자동 배치하고, 인접한 전신주끼리 케이블을
/// 자동으로 연결(CableGenerator)하는 컴포넌트.
///
/// 핵심 원리 — 왜 "간격이 자동으로 늘어나는가":
/// CableGenerator.anchorA/anchorB는 Transform 참조이고, 셰이더는 그 Transform의
/// 월드 좌표를 매번 읽어서 처짐/흔들림을 계산한다 (오브젝트 스케일과 무관, 3.3절 원칙).
/// 즉 전신주 사이 거리가 얼마든 케이블이 "그 거리에 맞춰 다시 계산"될 뿐이라,
/// 메쉬를 늘리거나 스케일할 필요가 전혀 없다 — 전신주 배치 좌표만 정해주면
/// 케이블은 자동으로 따라온다.
/// </summary>
public enum PoleLinePlacementMode { ByCount, ByTotalLength }

public class PoleLineGenerator : MonoBehaviour
{
    [Header("배치 방향 — 자유 벡터 (대각선 배치도 가능)")]
    [Tooltip("정규화해서 사용하므로 길이는 상관없음. 예: (1,0,0)=X축, (0,0,1)=Z축, (1,0,1)=대각선")]
    public Vector3 direction = Vector3.right;

    [Header("배치 기준")]
    public PoleLinePlacementMode placementMode = PoleLinePlacementMode.ByCount;
    [Tooltip("ByCount 모드에서 사용 — 전신주 개수")]
    [Min(2)] public int poleCount = 10;
    [Tooltip("ByTotalLength 모드에서 사용 — 전체 길이(m), 개수는 자동 계산")]
    [Min(1f)] public float totalLength = 100f;
    [Tooltip("전신주 사이 간격(m) — 두 모드 공통")]
    [Min(0.1f)] public float spacing = 10f;

    [Header("프리팹")]
    [Tooltip("자식 Transform으로 이름이 \"WireAttachPoint\"로 시작하는 오브젝트를 포함해야 함")]
    public GameObject polePrefab;
    [Tooltip("CableGenerator 컴포넌트가 붙어있는 케이블 프리팹")]
    public GameObject cablePrefab;
    public Material sharedCableMaterial;

    [Header("전선 처짐 랜덤화 — 케이블마다 다르게 뽑음")]
    public int randomSeed = 0;
    public float sagAmountMin = 0.4f;
    public float sagAmountMax = 0.8f;
    [Tooltip("처짐 정점 위치 랜덤 범위. 0.5=정중앙. 너무 0/1에 가까우면 부자연스러우니 0.3~0.7 권장")]
    [Range(0f, 1f)] public float sagPeakMin = 0.3f;
    [Range(0f, 1f)] public float sagPeakMax = 0.7f;

    private static int CombineSeed(int baseSeed, string salt)
    {
        // 13.5절과 동일한 시드 조합 방식 — 오브젝트 이름 대신 케이블 인덱스를 salt로 사용
        unchecked { return baseSeed * 397 + salt.GetHashCode(); }
    }

    [ContextMenu("Generate")]
    public void Generate()
    {
        Clear();

        if (polePrefab == null || cablePrefab == null)
        {
            Debug.LogError($"{name}: polePrefab/cablePrefab이 비어있습니다.");
            return;
        }

        Vector3 dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.right;

        int count = placementMode == PoleLinePlacementMode.ByCount
            ? poleCount
            : Mathf.Max(2, Mathf.RoundToInt(totalLength / spacing) + 1);

        var poles = new List<Transform>(count);

        // 1. 전신주 배치
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = transform.position + dir * (spacing * i);
            Quaternion rot = dir.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(dir) : Quaternion.identity;

            GameObject pole = Instantiate(polePrefab, pos, rot, transform);
            pole.name = $"Pole_{i:00}";
            poles.Add(pole.transform);
        }

        // 2. 인접 전신주끼리 케이블 연결
        for (int i = 0; i < poles.Count - 1; i++)
        {
            Transform attachA = FindWireAttachPoint(poles[i]);
            Transform attachB = FindWireAttachPoint(poles[i + 1]);

            if (attachA == null || attachB == null)
            {
                Debug.LogWarning($"{name}: Pole_{i:00} 또는 Pole_{(i + 1):00}에서 " +
                                  "\"WireAttachPoint\"로 시작하는 자식 오브젝트를 찾지 못했습니다. 이 구간은 건너뜁니다.");
                continue;
            }

            GameObject cableObj = Instantiate(cablePrefab, transform);
            cableObj.name = $"Cable_{i:00}";

            var cableGen = cableObj.GetComponent<CableGenerator>();
            if (cableGen == null)
            {
                Debug.LogError($"{name}: cablePrefab에 CableGenerator 컴포넌트가 없습니다.");
                continue;
            }

            cableGen.anchorA = attachA;
            cableGen.anchorB = attachB;
            if (sharedCableMaterial != null)
                cableGen.sharedCableMaterial = sharedCableMaterial;

            // 케이블 인덱스를 시드에 섞어서, 같은 배치는 항상 같은 결과로 재현되면서도
            // 케이블마다 서로 다른 처짐 값을 갖게 함 (13.5절 원칙과 동일)
            int seed = CombineSeed(randomSeed, $"cable_{i}");
            var rng = new System.Random(seed);
            cableGen.sagAmount = Mathf.Lerp(sagAmountMin, sagAmountMax, (float)rng.NextDouble());
            cableGen.sagPeakT = Mathf.Lerp(sagPeakMin, sagPeakMax, (float)rng.NextDouble());

            cableGen.Generate();
        }
    }

    /// <summary>
    /// 전신주 프리팹의 자식 중 이름이 "WireAttachPoint"로 시작하는 첫 번째 Transform을 찾는다.
    /// 전선을 여러 겹 걸고 싶으면(WireAttachPoint_0, _1, _2 등) 나중에 index 파라미터로 확장 가능.
    /// </summary>
    private Transform FindWireAttachPoint(Transform pole)
    {
        foreach (Transform child in pole)
        {
            if (child.name.StartsWith("WireAttachPoint"))
                return child;
        }
        return null;
    }

    [ContextMenu("Clear")]
    public void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i).gameObject;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(child);
            else
                Destroy(child);
#else
            Destroy(child);
#endif
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.right;
        int count = placementMode == PoleLinePlacementMode.ByCount
            ? poleCount
            : Mathf.Max(2, Mathf.RoundToInt(totalLength / spacing) + 1);

        Gizmos.color = Color.cyan;
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = transform.position + dir * (spacing * i);
            Gizmos.DrawWireSphere(pos, 0.3f);
            if (i > 0)
                Gizmos.DrawLine(transform.position + dir * (spacing * (i - 1)), pos);
        }
    }
}
