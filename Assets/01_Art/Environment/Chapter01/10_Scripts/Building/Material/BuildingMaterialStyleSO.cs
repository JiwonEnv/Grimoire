using UnityEngine;

/// <summary>
/// STEP 2-3 — 건물 모듈별 재질(베이스 머티리얼 + 색 팔레트 + Dirt 설정)을 관리하는 데이터 에셋.
/// BuildingStyleSO(조립 방법)와 분리해서, "모양"과 "보이는 방식"을 독립적으로 조합할 수 있게 한다.
///
/// 사용 예: 같은 9m 주거형 BuildingStyleSO에 MaterialStyle만 바꿔 끼우면
///          완전히 다른 색/질감의 건물이 나온다.
/// </summary>
[CreateAssetMenu(fileName = "NewBuildingMaterialStyle", menuName = "Cyberpunk/Building Material Style SO")]
public class BuildingMaterialStyleSO : ScriptableObject
{
    // 프로젝트 전체에서 공유하는 단일 모듈→재질 매핑 테이블.
    // 건물 톤(Lobby/Body 색)은 여기서 다루지 않음 — BuildingStyleSO 쪽 책임.
    public MaterialEntry[] entries;

    /// <summary>
    /// moduleId에 해당하는 MaterialEntry를 찾아 반환. 없으면 null.
    /// </summary>
    public MaterialEntry GetEntry(ModuleId id)
    {
        if (entries == null) return null;

        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].moduleId == id)
                return entries[i];
        }

        Debug.LogWarning($"[BuildingMaterialStyleSO] ModuleId '{id}'에 대한 MaterialEntry가 없음.");
        return null;
    }

    /// <summary>
    /// entries에 빠져있는 ModuleId를 전부 찾아서, 기본값(useDirt/useNoise=true)으로 행을 추가한다.
    /// 이미 있는 ModuleId는 건드리지 않음 — 누락분만 채워주는 용도.
    /// baseMaterial은 비워둔 채로 추가되니, 추가 후 직접 머티리얼을 연결해줘야 함.
    /// </summary>
    [ContextMenu("누락된 ModuleId 전부 채우기")]
    public void FillMissingModuleIds()
    {
        var existing = new System.Collections.Generic.HashSet<ModuleId>();
        if (entries != null)
        {
            foreach (var e in entries)
                existing.Add(e.moduleId);
        }

        var toAdd = new System.Collections.Generic.List<MaterialEntry>();
        foreach (ModuleId id in System.Enum.GetValues(typeof(ModuleId)))
        {
            if (existing.Contains(id)) continue;
            toAdd.Add(new MaterialEntry { moduleId = id });
        }

        if (toAdd.Count == 0)
        {
            Debug.Log("[BuildingMaterialStyleSO] 빠진 ModuleId 없음 — 전부 채워져 있음.");
            return;
        }

        var merged = new MaterialEntry[(entries?.Length ?? 0) + toAdd.Count];
        if (entries != null) entries.CopyTo(merged, 0);
        toAdd.CopyTo(merged, entries?.Length ?? 0);
        entries = merged;

        Debug.Log($"[BuildingMaterialStyleSO] {toAdd.Count}개 ModuleId 추가됨: " +
                  string.Join(", ", toAdd.ConvertAll(e => e.moduleId.ToString())));
    }

    /// <summary>
    /// 전체 ModuleId가 빠짐없이 채워져 있는지, baseMaterial이 비어있는 entry가 있는지 점검.
    /// </summary>
    [ContextMenu("entries 점검 (누락/빈 머티리얼 확인)")]
    public void ValidateEntries()
    {
        if (entries == null || entries.Length == 0)
        {
            Debug.LogWarning("[BuildingMaterialStyleSO] entries가 비어있음.");
            return;
        }

        var seen = new System.Collections.Generic.HashSet<ModuleId>();
        foreach (var e in entries)
        {
            if (!seen.Add(e.moduleId))
                Debug.LogWarning($"[BuildingMaterialStyleSO] ModuleId '{e.moduleId}' 중복됨.");
            if (e.baseMaterial == null)
                Debug.LogWarning($"[BuildingMaterialStyleSO] ModuleId '{e.moduleId}'의 baseMaterial이 비어있음.");
        }

        foreach (ModuleId id in System.Enum.GetValues(typeof(ModuleId)))
        {
            if (!seen.Contains(id))
                Debug.LogWarning($"[BuildingMaterialStyleSO] ModuleId '{id}'가 entries에 없음.");
        }

        Debug.Log("[BuildingMaterialStyleSO] 점검 완료.");
    }
}

/// <summary>
/// 건물을 구성하는 모듈 종류. 새 모듈(부착물)이 추가되면 여기에 항목만 추가하면 됨.
/// </summary>
public enum ModuleId
{
    // Core
    Lobby, Middle, RoofCore, FloorLine,
    // Attachment
    PillarResidential, PillarShop, RoofOverhang,
    WindowFrame, WindowGlass,
    ShowWindowFrame, ShowWindowGlass,
    ShowWindowDoorFrame, ShowWindowDoorGlass,
    Signboard, LobbyBelt,
    OutdoorUnit, Door, Step, FE_Landing, FE_Stair
}

public enum DirtMode
{
    Fixed,
    Random
}

/// <summary>
/// 모듈 하나(예: Middle)가 가질 수 있는 재질 설정 한 묶음.
/// </summary>
[System.Serializable]
public class MaterialEntry
{
    [Tooltip("이 엔트리가 적용될 모듈")]
    public ModuleId moduleId;

    [Tooltip("베이스 머티리얼 — BuildingLit 셰이더를 쓰는 모듈별 전용 머티리얼 (벽돌/시멘트/금속/유리 등 텍스처가 다름)")]
    public Material baseMaterial;

    [Tooltip("이 모듈이 가질 수 있는 색 후보 목록. 건물 생성 시 이 중 하나를 골라 틴트로 사용")]
    public Color[] colorPalette;

    [Header("Dirt — 페인트 벗겨짐")]
    [Tooltip("유리/그림(간판)처럼 Dirt 표현이 안 맞는 모듈은 꺼둠. Dirt 쓰는 모듈이 더 적어서 기본값을 꺼둠으로 변경 — " +
             "Core(Lobby/Middle 등) 같은 실제 쓰는 모듈만 직접 체크하면 됨")]
    public bool useDirt = false;
    public DirtMode dirtMode = DirtMode.Random;
    public float dirtIntensityFixed = 0.5f;
    public Vector2 dirtIntensityRandomRange = new Vector2(0.2f, 0.8f);
    [Tooltip("벗겨진 안쪽 면의 Smoothness 고정값. 전용 텍스처 없이 단일값으로 처리 (DirtBlend.hlsl의 DirtSmoothness 입력)")]
    public float dirtSmoothness = 0.2f;
    [Tooltip("벗겨진 안쪽 면의 Metallic 고정값 (DirtBlend.hlsl의 DirtMetallic 입력)")]
    public float dirtMetallic = 0f;

    [Header("Noise — 단색 방지용 미세 색 편차 (Dirt와 별개)")]
    public bool useNoise = true;
    public DirtMode noiseMode = DirtMode.Random;
    public float noiseIntensityFixed = 0.15f;
    public Vector2 noiseIntensityRandomRange = new Vector2(0.05f, 0.2f);

    [Header("Tiling")]
    [Tooltip("Core처럼 widthScale로 늘어나는 모듈에 곱해줄 기본 타일링 밀도")]
    public float baseTilingDensity = 1f;

    [Header("아틀라스 — Signboard/ShowWindowGlass처럼 한 텍스처에 여러 그림이 들어있는 경우")]
    [Tooltip("이 텍스처 안에 들어있는 변형 개수. 1이면 아틀라스 안 씀")]
    public int atlasVariantCount = 1;
    [Tooltip("아틀라스 그리드 (가로 칸 수, 세로 칸 수). 세로로만 쌓았으면 (1, N)")]
    public Vector2Int atlasGrid = new Vector2Int(1, 1);

    /// <summary>
    /// seed로 아틀라스 변형 하나를 골라서, 셰이더에 넘길 UV Offset/Scale(Vector4: xy=offset, zw=tile)을 계산.
    /// atlasVariantCount가 1이면 (0,0,1,1) — 즉 풀텍스처 그대로 사용.
    /// </summary>
    public Vector4 PickAtlasOffsetScale(int seed)
    {
        if (atlasVariantCount <= 1 || atlasGrid.x <= 0 || atlasGrid.y <= 0)
            return new Vector4(0f, 0f, 1f, 1f);

        var rng = new System.Random(seed);
        int index = rng.Next(atlasVariantCount);

        int col = index % atlasGrid.x;
        int row = index / atlasGrid.x;

        float tileX = 1f / atlasGrid.x;
        float tileY = 1f / atlasGrid.y;
        float offsetX = col * tileX;
        // 텍스처 V좌표는 보통 아래가 0이라, 위에서부터 순서대로 채우고 싶으면 row를 뒤집어줌
        float offsetY = 1f - tileY - (row * tileY);

        return new Vector4(offsetX, offsetY, tileX, tileY);
    }

    /// <summary>
    /// seed로 팔레트에서 색 하나를 일관되게 선택. 같은 seed면 항상 같은 색.
    /// </summary>
    public Color PickColor(int seed)
    {
        if (colorPalette == null || colorPalette.Length == 0)
            return Color.white;

        // System.Random을 따로 써서 UnityEngine.Random의 전역 상태(다른 랜덤 로직)와 섞이지 않게 함
        var rng = new System.Random(seed);
        int index = rng.Next(colorPalette.Length);
        return colorPalette[index];
    }

    /// <summary>
    /// seed로 Dirt 강도를 결정. Fixed면 항상 같은 값, Random이면 seed 기반으로 범위 내 값.
    /// </summary>
    public float PickDirtIntensity(int seed)
    {
        if (!useDirt) return 0f;
        if (dirtMode == DirtMode.Fixed) return dirtIntensityFixed;

        var rng = new System.Random(seed);
        double t = rng.NextDouble();
        return Mathf.Lerp(dirtIntensityRandomRange.x, dirtIntensityRandomRange.y, (float)t);
    }

    /// <summary>
    /// seed로 Noise 강도를 결정 (Dirt와 별개의 시드를 받아서 독립적으로 변화 가능).
    /// </summary>
    public float PickNoiseIntensity(int seed)
    {
        if (!useNoise) return 0f;
        if (noiseMode == DirtMode.Fixed) return noiseIntensityFixed;

        var rng = new System.Random(seed);
        double t = rng.NextDouble();
        return Mathf.Lerp(noiseIntensityRandomRange.x, noiseIntensityRandomRange.y, (float)t);
    }
}
