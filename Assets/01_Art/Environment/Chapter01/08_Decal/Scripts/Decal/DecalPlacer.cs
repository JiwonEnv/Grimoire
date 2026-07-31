using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 씬에서 위치/회전/크기를 직접(수동으로) 배치한 Decal Projector에 붙이는 가벼운 헬퍼.
/// DecalScatterer처럼 자동으로 위치를 랜덤 생성하지 않는다 — 오브젝트 배치는 사람이 직접 한다.
///
/// 대신 머티리얼만큼은 DecalStyleSO.GetOrCreateMaterial()을 그대로 경유하므로,
/// 같은 (atlasIndex, designIndex) 조합을 쓰는 데칼끼리는 항상 같은 머티리얼 객체를 공유한다.
/// → 손으로 몇 개를 배치하든 머티리얼 개수는 "실제로 사용한 디자인 조합 수" 이하로 유지된다.
/// </summary>
[RequireComponent(typeof(DecalProjector))]
public class DecalPlacer : MonoBehaviour
{
    [Header("스타일")]
    public DecalStyleSO style;

    [Header("이 데칼이 쓸 디자인 (인덱스로 지정)")]
    public int atlasIndex = 0;
    public int designIndex = 0;

    private DecalProjector _projector;

    // 인스펙터에서 값을 바꿀 때마다 즉시 반영 — 에디터에서 배치하면서 바로 확인 가능
    private void OnValidate()
    {
        Apply();
    }

    [ContextMenu("Apply")]
    public void Apply()
    {
        if (style == null || style.atlases == null || style.atlases.Length == 0) return;
        if (atlasIndex < 0 || atlasIndex >= style.atlases.Length) return;

        int maxDesign = style.atlases[atlasIndex].DesignCount;
        designIndex = Mathf.Clamp(designIndex, 0, maxDesign - 1);

        if (_projector == null) _projector = GetComponent<DecalProjector>();
        _projector.material = style.GetOrCreateMaterial(atlasIndex, designIndex);
    }
}
