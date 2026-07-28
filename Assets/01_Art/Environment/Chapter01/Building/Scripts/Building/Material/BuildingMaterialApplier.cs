using UnityEngine;

/// <summary>
/// STEP 2-3 — Renderer에 ModuleId 기반 재질(색/Dirt/Tiling)을 MaterialPropertyBlock으로 적용.
/// 머티리얼 애셋 자체는 늘리지 않으므로 GPU Instancing이 유지된다.
///
/// BuildingLit 셰이더(또는 Shader Graph)에 다음 프로퍼티가 노출되어 있어야 함:
///   _BaseColorTint (Color)
///   _DirtIntensity (Float, 0~1)
///   _DirtSeed      (Float — Dirt 마스크 노이즈 좌표 오프셋용)
///   _NoiseIntensity (Float, 0~1 — Dirt와 별개의 미세 색 얼룩 강도)
///   _NoiseSeed      (Float — Noise 좌표 오프셋용)
///   _TilingScale   (Vector4 — xy만 사용. widthScale/높이비율 보정용 UV 타일링)
///   _AtlasOffsetScale (Vector4 — xy=UV offset, zw=UV tile. Signboard/ShowWindowGlass 등 아틀라스 텍스처용.
///                       atlasVariantCount=1인 모듈은 (0,0,1,1)이 와서 풀텍스처 그대로 나옴)
/// </summary>
public static class BuildingMaterialApplier
{
    private static readonly int BaseColorTintId = Shader.PropertyToID("_BaseColorTint");
    private static readonly int DirtIntensityId = Shader.PropertyToID("_DirtIntensity");
    private static readonly int DirtSeedId = Shader.PropertyToID("_DirtSeed");
    private static readonly int DirtSmoothnessId = Shader.PropertyToID("_DirtSmoothness");
    private static readonly int DirtMetallicId = Shader.PropertyToID("_DirtMetallic");
    private static readonly int NoiseIntensityId = Shader.PropertyToID("_NoiseIntensity");
    private static readonly int NoiseSeedId = Shader.PropertyToID("_NoiseSeed");
    private static readonly int TilingScaleId = Shader.PropertyToID("_TilingScale");
    private static readonly int AtlasOffsetScaleId = Shader.PropertyToID("_AtlasOffsetScale");

    // ShowWindow Glass Emission 전용 (BuildingGlassEmission 셰이더 그래프)
    private static readonly int EmissionTexId = Shader.PropertyToID("_EmissionTex");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private static readonly int EmissionIntensityId = Shader.PropertyToID("_EmissionIntensity");

    // Window Glass — STEP 2-4 동적 점등 (WindowLit 셰이더 그래프)
    private static readonly int OffTexId = Shader.PropertyToID("_OffTex");
    private static readonly int LitColorId = Shader.PropertyToID("_LitColor");
    private static readonly int LitChanceId = Shader.PropertyToID("_LitChance");
    private static readonly int FlickerChanceId = Shader.PropertyToID("_FlickerChance");
    private static readonly int ChangeIntervalId = Shader.PropertyToID("_ChangeInterval");
    private static readonly int FlickerSpeedId = Shader.PropertyToID("_FlickerSpeed");

    /// <summary>
    /// instance(모듈 GameObject) 아래의 모든 Renderer에 재질 스타일을 적용.
    /// buildingSeed: 건물 하나당 고정된 시드 — 색 선택(overrideColor가 없을 때)에 사용.
    /// dirtSeedOverride / noiseSeedOverride: null이면 buildingSeed 기반 entrySeed를 그대로 쓰고,
    ///     값이 있으면 그 값으로 Dirt/Noise만 독립적으로 재계산 — Generate 단에서 패턴만 바꿔보고 싶을 때 사용.
    /// tilingScale: X=widthScale(폭 늘어난 비율), Y=모듈 높이 비율 보정. Attachment는 (1,1)로 전달.
    /// overrideColor: Lobby/Body처럼 건물 단위로 미리 정해둔 색을 그대로 강제할 때 사용.
    /// </summary>
    /// <summary>
    /// C#의 int 시드(해시 조합이라 절댓값이 수억~수십억까지 커짐)를 그대로 GPU float에 넘기면,
    /// float32 정밀도(~7자리) 한계 때문에 UV에 더해지는 작은 소수(0~1) 부분이 묻혀서 사라짐
    /// (예: -2048532640 같은 값에 0.3을 더해봐야 그 차이가 표현이 안 됨).
    /// 그래서 셰이더에 넘기기 직전, 작은 범위(0~999)로 접어서(mod) 보내야 함.
    /// 같은 시드는 항상 같은 결과로 접히므로 재현성은 그대로 유지됨.
    /// </summary>
    private static float SeedToShaderFloat(int seed)
    {
        const int range = 1000;
        int wrapped = ((seed % range) + range) % range; // C#의 %는 음수를 음수로 반환하므로 보정
        return wrapped;
    }

    public static void Apply(GameObject instance, BuildingMaterialStyleSO materialStyle,
                              ModuleId moduleId, int buildingSeed, Vector2? tilingScale = null,
                              Color? overrideColor = null, int? dirtSeedOverride = null, int? noiseSeedOverride = null)
    {
        if (instance == null || materialStyle == null) return;

        MaterialEntry entry = materialStyle.GetEntry(moduleId);
        if (entry == null) return;

        Vector2 tiling = tilingScale ?? Vector2.one;

        // 모듈 종류(moduleId)를 시드에 섞어서, 같은 건물 안에서도 모듈별로는 다른 색/Dirt/Noise가
        // 나오게 하되, 같은 건물을 다시 생성하면 항상 같은 결과가 나오게 함(재현성).
        int entrySeed = buildingSeed * 397 + (int)moduleId;
        int dirtSeed = dirtSeedOverride ?? entrySeed;
        int noiseSeed = noiseSeedOverride ?? entrySeed;

        Color color = overrideColor ?? entry.PickColor(entrySeed);
        float dirt = entry.PickDirtIntensity(dirtSeed);
        float noise = entry.PickNoiseIntensity(noiseSeed);
        Vector4 atlasOffsetScale = entry.PickAtlasOffsetScale(entrySeed);

        if (entry.baseMaterial != null)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
            MaterialPropertyBlock block = new MaterialPropertyBlock();

            foreach (Renderer renderer in renderers)
            {
                // 베이스 머티리얼 자체를 교체 (벽돌/시멘트/금속/유리 등 텍스처가 다른 경우)
                renderer.sharedMaterial = entry.baseMaterial;

                renderer.GetPropertyBlock(block);
                block.SetColor(BaseColorTintId, color);
                block.SetFloat(DirtIntensityId, dirt);
                block.SetFloat(DirtSeedId, SeedToShaderFloat(dirtSeed));
                block.SetFloat(DirtSmoothnessId, entry.dirtSmoothness);
                block.SetFloat(DirtMetallicId, entry.dirtMetallic);
                block.SetFloat(NoiseIntensityId, noise);
                block.SetFloat(NoiseSeedId, SeedToShaderFloat(noiseSeed));
                block.SetVector(TilingScaleId, new Vector4(tiling.x * entry.baseTilingDensity, tiling.y * entry.baseTilingDensity, 0f, 0f));
                block.SetVector(AtlasOffsetScaleId, atlasOffsetScale);
                renderer.SetPropertyBlock(block);
            }
        }
    }

    /// <summary>
    /// 쇼윈도/쇼윈도도어 Glass 자식에 "가게 내부 풍경" Emission 아틀라스를 적용.
    /// 기존 Apply()로 sharedMaterial(BuildingGlassEmission 셰이더 머티리얼)을 먼저 적용한 뒤,
    /// 이 메서드로 아틀라스 텍스처/UV/발광 강도를 덮어씀(override) — 두 호출이 한 세트로 같이 쓰임.
    ///
    /// BuildingStyleSO가 폭(9m/12m)별로 분리되어 있으므로, 호출하는 쪽은 그 Style 1개에 들어있는
    /// atlas/rows를 그대로 넘기면 됨 — 9m/12m 분기는 더 이상 이 함수 책임이 아님.
    ///
    /// patternLength: 그 건물의 shopLobbyPattern.Length (9m=3, 12m=4) — 가로 슬롯 개수 계산용으로 여전히 필요
    /// indexInPattern: 패턴 배열에서 이 슬롯의 인덱스 — 같은 세트 안에서의 가로 위치
    /// setSeed: "세트" 단위 시드 — 보통 buildingSeed 그대로 사용.
    ///          같은 건물이면 ShowWindow/ShowWindowDoor 전부 같은 세트(같은 가게 내부)가 나오도록
    ///          모듈 인덱스가 아니라 건물 시드 하나로 통일해서 받는다.
    /// forcedRowIndex: Signboard의 forcedRowIndex와 같은 용도 — 값이 있으면 랜덤 대신 그 세트(디자인 행)를
    ///                 강제로 사용 (테스트/디버깅용). 0부터 시작.
    /// </summary>
    public static void ApplyShowWindowGlassEmission(
        GameObject glassObj, int patternLength, int indexInPattern, int setSeed,
        Texture2D atlas, int rows,
        Color emissionColor, float emissionIntensity, int? forcedRowIndex = null)
    {
        if (glassObj == null) return;

        if (atlas == null || rows <= 0 || patternLength <= 0)
        {
            Debug.LogWarning($"[BuildingMaterialApplier] '{glassObj.name}' ShowWindow 아틀라스 설정이 비어있어 Emission 적용을 건너뜀.");
            return;
        }

        // 세로: 어떤 디자인 세트(가게 종류)를 쓸지 — forcedRowIndex가 있으면 그 값을 강제 사용,
        // 없으면 건물 시드로 결정 (같은 건물의 모든 슬롯이 같은 세트를 공유)
        int rowIndex;
        if (forcedRowIndex.HasValue)
        {
            rowIndex = Mathf.Clamp(forcedRowIndex.Value, 0, rows - 1);
        }
        else
        {
            var rng = new System.Random(setSeed);
            rowIndex = rng.Next(rows);
        }

        // 가로: 그 디자인 세트 안에서 자기 위치 — shopLobbyPattern의 배열 인덱스 그대로
        float tileX = 1f / patternLength;
        float tileY = 1f / rows;
        float offsetX = indexInPattern * tileX;
        float offsetY = 1f - tileY - (rowIndex * tileY);

        Renderer renderer = glassObj.GetComponent<Renderer>();
        if (renderer == null)
        {
            Debug.LogWarning($"[BuildingMaterialApplier] '{glassObj.name}'에 Renderer가 없어 Emission 적용을 건너뜀.");
            return;
        }

        // ★ MaterialPropertyBlock 대신 인스턴스 머티리얼 사용.
        // 이 모듈은 Apply()로 한 번, 여기서 또 한 번 — 같은 렌더러에 MPB를 두 번
        // 나눠 설정하는 구조였는데, 이게 Unity의 "Play 종료 시 자동 복원" 과정에서
        // 유독 깨지는 원인으로 확인됨(Lobby/Middle처럼 MPB를 한 번만 쓰는 모듈은 멀쩡함).
        // renderer.material은 최초 접근 시 sharedMaterial을 자동으로 복제해서
        // 인스턴스를 만들어주고, 이 인스턴스는 Unity 표준 직렬화 대상이라
        // Play 진입/종료 어느 쪽에서도 안전하게 보존된다.
        Material instMat = renderer.material;
        instMat.SetTexture(EmissionTexId, atlas);
        instMat.SetVector(AtlasOffsetScaleId, new Vector4(offsetX, offsetY, tileX, tileY));
        instMat.SetColor(EmissionColorId, emissionColor);
        instMat.SetFloat(EmissionIntensityId, emissionIntensity);

        // ★ Apply()가 이 렌더러에 먼저 MPB(_AtlasOffsetScale=(0,0,1,1) 등 기본값)를
        // 설정해둔 상태라, MPB가 항상 머티리얼 값보다 렌더링 우선순위가 높아서
        // 방금 인스턴스 머티리얼에 넣은 값이 가려져버린다. 이 렌더러는 이제부터
        // 인스턴스 머티리얼로만 값을 제어하므로, 남아있는 MPB를 통째로 지운다.
        renderer.SetPropertyBlock(null);
    }

    /// <summary>
    /// Signboard에 발광 아틀라스(세로 N등분 디자인 중 1개)를 적용.
    /// ShowWindowGlassEmission과 달리 "패턴 인덱스" 개념이 없음 — 모듈 1개당 디자인 1개를 통째로 고르면 끝.
    /// 가로 분할이 없으므로 tileX=1, offsetX=0으로 고정.
    ///
    /// BuildingStyleSO가 폭(9m/12m)별로 분리되어 있으므로, 9m/12m 분기는 더 이상 이 함수 책임이 아님 —
    /// 호출하는 쪽이 그 Style 1개에 들어있는 atlas/rows를 그대로 넘기면 됨.
    /// buildingSeed: 건물 시드 그대로 사용 — 같은 건물이면 항상 같은 간판 디자인(재현성).
    /// </summary>
    public static void ApplySignboardEmission(
        GameObject signboardObj, int buildingSeed,
        Texture2D atlas, int rows,
        Color emissionColor, float emissionIntensity, int? forcedRowIndex = null)
    {
        if (signboardObj == null) return;

        if (atlas == null || rows <= 0)
        {
            Debug.LogWarning($"[BuildingMaterialApplier] '{signboardObj.name}' Signboard 아틀라스 설정이 비어있어 Emission 적용을 건너뜀.");
            return;
        }

        int rowIndex;
        if (forcedRowIndex.HasValue)
        {
            // 0부터 시작하는 인덱스를 그대로 사용, 범위 밖이면 안전하게 클램프
            rowIndex = Mathf.Clamp(forcedRowIndex.Value, 0, rows - 1);
        }
        else
        {
            // 간판 전용 시드 오프셋(* 503) — 같은 buildingSeed를 공유하는 다른 아틀라스(쇼윈도 등)와
            // 우연히 같은 패턴 선택 결과가 나오지 않도록 모듈 종류별로 시드를 분리.
            var rng = new System.Random(buildingSeed * 503 + 1);
            rowIndex = rng.Next(rows);
        }

        float tileY = 1f / rows;
        float offsetY = 1f - tileY - (rowIndex * tileY);

        Renderer renderer = signboardObj.GetComponentInChildren<Renderer>();
        if (renderer == null)
        {
            Debug.LogWarning($"[BuildingMaterialApplier] '{signboardObj.name}'에 Renderer가 없어 Emission 적용을 건너뜀.");
            return;
        }

        // ★ ApplyShowWindowGlassEmission과 동일한 이유로 인스턴스 머티리얼 사용
        Material instMat = renderer.material;
        instMat.SetTexture(EmissionTexId, atlas);
        instMat.SetVector(AtlasOffsetScaleId, new Vector4(0f, offsetY, 1f, tileY));
        instMat.SetColor(EmissionColorId, emissionColor);
        instMat.SetFloat(EmissionIntensityId, emissionIntensity);

        // ★ ApplyShowWindowGlassEmission과 동일한 이유로 남은 MPB 클리어
        renderer.SetPropertyBlock(null);
    }

    /// <summary>
    /// STEP 2-4 — Window Glass에 동적 점등(WindowLit 셰이더) 분위기를 덮어씀.
    /// 기존 Apply()로 sharedMaterial(Mat_WindowLit)을 먼저 적용한 뒤, 이 메서드로
    /// 건물별 분위기(꺼진방 텍스처/켜진방 색/점등 확률 등)를 덮어씀 — 두 호출이 한 세트.
    ///
    /// 시드(어떤 창문이 켜질지)는 셰이더 안에서 오브젝트 피봇의 월드 좌표로 직접 계산하므로
    /// 여기서는 넘기지 않음 — 이 메서드는 "그 확률/색 자체"만 건물 단위로 바꿔주는 역할.
    /// offTex가 null이면 머티리얼에 미리 지정해둔 Default 텍스처가 그대로 쓰이므로 안전.
    /// </summary>
    public static void ApplyWindowLit(
        GameObject glassObj, Texture2D offTex, Color litColor,
        float litChance, float flickerChance, float changeInterval, float flickerSpeed)
    {
        if (glassObj == null) return;

        Renderer renderer = glassObj.GetComponent<Renderer>();
        if (renderer == null)
        {
            Debug.LogWarning($"[BuildingMaterialApplier] '{glassObj.name}'에 Renderer가 없어 WindowLit 적용을 건너뜀.");
            return;
        }

        // ★ ApplyShowWindowGlassEmission과 동일한 이유로 인스턴스 머티리얼 사용
        Material instMat = renderer.material;
        if (offTex != null) instMat.SetTexture(OffTexId, offTex);
        instMat.SetColor(LitColorId, litColor);
        instMat.SetFloat(LitChanceId, litChance);
        instMat.SetFloat(FlickerChanceId, flickerChance);
        instMat.SetFloat(ChangeIntervalId, changeInterval);
        instMat.SetFloat(FlickerSpeedId, flickerSpeed);

        // ★ ApplyShowWindowGlassEmission과 동일한 이유로 남은 MPB 클리어
        renderer.SetPropertyBlock(null);
    }
}
