using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 데칼 아틀라스 1벌(웅덩이/균열/그래피티 등)의 스펙.
/// 이름으로 구분하지 않고 DecalStyleSO.atlases[] 배열의 인덱스로만 참조한다.
/// </summary>
[System.Serializable]
public class AtlasEntry
{
    public Texture2D baseColorAtlas;
    public Texture2D normalAtlas;                       // 비우면 셰이더 Default(평평한 노멀)로 폴백
    public Vector2Int gridSize = new Vector2Int(3, 2);   // 이 아틀라스만의 격자 (아틀라스마다 다르게 가능)
    [Range(0f, 1f)] public float smoothnessBoost = 0.6f;
    [Range(0f, 1f)] public float normalBlend = 0.5f;

    public int DesignCount => gridSize.x * gridSize.y;
}

/// <summary>
/// 데칼 스타일 데이터 SO — 아틀라스 목록 + 머티리얼 템플릿을 보관하고,
/// (아틀라스 인덱스, 디자인 인덱스) 조합마다 머티리얼을 1개씩만 생성/재사용하는 풀을 제공한다.
///
/// 주의: 이 SO는 프로젝트 전체에서 여러 DecalScatterer가 공유하는 애셋이다.
/// 따라서 ClearMaterialPool()은 어떤 Scatterer의 Generate()에서도 자동 호출하지 않는다
/// (한 Scatterer의 재실행이 다른 Scatterer가 참조 중인 캐시까지 비워버리는 부작용 방지).
/// 아틀라스 텍스처/격자를 교체해서 갱신이 필요할 때만, 이 에셋을 직접 선택해
/// 우클릭 → "머티리얼 풀 수동 초기화"를 사람이 실행한다.
/// </summary>
[CreateAssetMenu(fileName = "DecalStyle", menuName = "CyberpunkBG/Decal Style")]
public class DecalStyleSO : ScriptableObject
{
    [Header("아틀라스 목록 — 이름 대신 배열 인덱스로만 다룸")]
    public AtlasEntry[] atlases;

    [Header("머티리얼 템플릿 (SG_Decal 기반, 모든 아틀라스가 공유하는 원본)")]
    public Material decalMaterialTemplate;

    // 저장(직렬화) 안 됨 — 에디터 세션 동안만 유지되는 런타임 캐시.
    private Dictionary<(int atlasIdx, int designIdx), Material> _materialPool
        = new Dictionary<(int, int), Material>();

    /// <summary>
    /// (atlasIndex, designIndex) 조합에 해당하는 머티리얼을 반환한다.
    /// 이미 만든 적 있는 조합이면 캐시된 객체를 그대로 재사용하고,
    /// 없으면 decalMaterialTemplate을 복제해 새로 만들어 풀에 등록한다.
    /// </summary>
    public Material GetOrCreateMaterial(int atlasIndex, int designIndex)
    {
        if (atlases == null || atlasIndex < 0 || atlasIndex >= atlases.Length)
        {
            Debug.LogError($"{name}: 잘못된 atlasIndex({atlasIndex})입니다.");
            return decalMaterialTemplate;
        }

        var key = (atlasIndex, designIndex);
        if (_materialPool.TryGetValue(key, out var cached) && cached != null)
            return cached; // 이미 만든 조합이면 재사용 → 새 객체 안 만듦

        var atlas = atlases[atlasIndex];
        Material mat = new Material(decalMaterialTemplate);
        mat.SetTexture("_DecalAtlasTex", atlas.baseColorAtlas);
        mat.SetTexture("_DecalNormalTex", atlas.normalAtlas);
        mat.SetVector("_AtlasGridSize", new Vector4(atlas.gridSize.x, atlas.gridSize.y));
        mat.SetFloat("_AtlasIndex", designIndex);
        mat.SetFloat("_SmoothnessBoost", atlas.smoothnessBoost);
        mat.SetFloat("_NormalBlend", atlas.normalBlend);
        mat.enableInstancing = true; // Advanced Options의 "Enable GPU Instancing"과 동일 효과

        _materialPool[key] = mat;
        return mat;
    }

    // 자동 호출 금지 — 사람이 필요할 때만 수동 실행 (5.4절 이유 참고)
    [ContextMenu("머티리얼 풀 수동 초기화")]
    public void ClearMaterialPool() => _materialPool.Clear();
}
