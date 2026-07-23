using UnityEngine;

/// <summary>
/// STEP 2-2 — Lobby / Middle / Roof / FloorLine 조립 + 부착물(Fire Escape, Window,
/// Signboard, OutdoorUnit, Pillar, Door) 생성기.
/// 프리팹을 직접 들고 있지 않고 BuildingStyleSO를 참조해서 조립한다.
/// 같은 BuildingGenerator라도 연결된 Style이 다르면 완전히 다른 건물이 나온다.
///
/// 좌표축 가정 (모델 좌표축이 다르면 부호만 뒤집으면 됨):
///   X+ : 오른쪽 (Right)
///   Y+ : 위쪽
///   Z+ : 뒤쪽 (Back).  Front 면은 Z = 0.
/// </summary>
public class BuildingGenerator : MonoBehaviour
{
    [Header("이 건물이 사용할 스타일 (필수)")]
    public BuildingStyleSO style;

    [Header("건물 기본 설정")]
    [Tooltip("이 건물의 종류 — Style은 1개로 공유하고, 이 값으로 Residential/Commercial 분기")]
    public BuildingType buildingType = BuildingType.Residential;
    [Tooltip("건물 폭(m). 반드시 3의 배수 (현재 9 또는 12 사용)")]
    public int widthMeters = 9;

    [Tooltip("층수 — 직접 입력 (Lobby 제외, Middle 반복 횟수)")]
    public int floorCount = 5;

    public bool useFloorLine = true;
    public bool useFireEscape = true;
    public bool useWindow = true;
    public bool useOutdoorUnit = true;
    [Tooltip("Lobby 좌우 코너에 기둥을 세울지 (특히 Residential에서 옵션)")]
    public bool useLobbyPillar = true;
    [Tooltip("켜면 Middle 각 층마다도 기둥을 쌓아서, Lobby부터 옥상까지 이어지는 기둥처럼 보이게 함")]
    public bool stackPillarOnMiddle = false;

    [Tooltip("실외기/창문 랜덤 배치에 사용할 시드. 같은 시드면 같은 건물에서 항상 같은 패턴이 나옴")]
    public int randomSeed = 0;

    [Header("Commercial Lobby — 슬롯 패턴 (Residential이면 무시됨)")]
    [Tooltip("내부 슬롯(기둥 제외) 패턴. 길이는 widthMeters/baseModuleWidth와 맞춰서 직접 채운다. " +
             "예) 9m(3슬롯): [ShowWindow, ShowWindowDoor, ShowWindow]")]
    public LobbySlotType[] shopLobbyPattern;

    [Header("Residential Lobby — Door/Step 정렬 보정")]
    [Tooltip("lobbyDoorPrefab의 실제 폭(m). Step 위에서 Door를 가로 중앙 정렬할 때 사용")]
    public float lobbyDoorWidth = 1.2f;
    [Tooltip("lobbyStepPrefab의 실제 높이(m). Door를 이 높이만큼 띄워서 계단 상판 위에 올림")]
    public float lobbyStepHeight = 1.1f;

    [Header("건물 톤 — 이 건물 인스턴스의 색 후보 (생성할 때 바로 지정)")]
    [Tooltip("Lobby에 적용할 색 후보")]
    public Color[] lobbyColorPalette;
    [Tooltip("Middle + RoofCore에 공통으로 적용할 색 후보")]
    public Color[] bodyColorPalette;

    [Header("Dirt / Noise 패턴 조정 — 색은 그대로 두고 벗겨짐·노이즈 패턴만 바꿔보고 싶을 때 사용")]
    [Tooltip("체크하면 randomSeed와 무관하게 dirtSeed 값으로 Dirt 패턴만 따로 재계산")]
    public bool overrideDirtSeed = false;
    public int dirtSeed = 0;
    [Tooltip("체크하면 randomSeed와 무관하게 noiseSeed 값으로 Noise 패턴만 따로 재계산")]
    public bool overrideNoiseSeed = false;
    public int noiseSeed = 0;

    [Header("Window Glass — 동적 점등 분위기 (STEP 2-4)")]
    [Tooltip("꺼진 방 텍스처 — 비워두면 머티리얼의 기본 텍스처(_OffTex Default) 그대로 사용")]
    public Texture2D windowOffTex;
    [Tooltip("켜진 방 발광색 후보 — 창문마다 이 중 하나를 무작위로 선택 (한 건물에 여러 색 불이 섞여 보이게 함). " +
             "비워두면 windowLitColor 단일 색을 모든 창문에 동일하게 사용")]
    [ColorUsage(true, true)]
    public Color[] windowLitColorPalette;
    [Tooltip("windowLitColorPalette가 비어있을 때 쓰는 단일 발광색 (HDR 권장)")]
    [ColorUsage(true, true)]
    public Color windowLitColor = Color.white;
    [Range(0f, 1f)] public float windowLitChance = 0.4f;
    [Range(0f, 1f)] public float windowFlickerChance = 0.15f;
    [Tooltip("점등 패턴이 통째로 바뀌는 주기(초)")]
    public float windowChangeInterval = 8f;
    public float windowFlickerSpeed = 4f;

    [Header("ShowWindow Glass — 발광 색/강도 (Commercial 전용)")]
    [Tooltip("유리 발광 색 (HDR 권장 — Bloom과 연계)")]
    [ColorUsage(true, true)]
    public Color showWindowEmissionColor = Color.white;
    [Tooltip("유리 발광 강도")]
    public float showWindowEmissionIntensity = 1.5f;
    [Tooltip("체크하면 랜덤 대신 showWindowDesignIndex 번째 가게 디자인(세트)을 강제로 사용 (테스트/디버깅용)")]
    public bool overrideShowWindowDesign = false;
    [Tooltip("0부터 시작 — 0이면 아틀라스 맨 위 칸(1번째 디자인 세트)")]
    public int showWindowDesignIndex = 0;

    [Header("Signboard — 발광 색/강도 + 디자인 직접 지정 (Commercial 전용)")]
    [Tooltip("간판 발광 색 (HDR 권장 — 네온 느낌)")]
    [ColorUsage(true, true)]
    public Color signboardEmissionColor = Color.white;
    [Tooltip("간판 발광 강도")]
    public float signboardEmissionIntensity = 1.5f;
    [Tooltip("체크하면 랜덤 대신 signboardDesignIndex 번째 디자인을 강제로 사용 (테스트/디버깅용)")]
    public bool overrideSignboardDesign = false;
    [Tooltip("0부터 시작 — 0이면 아틀라스 맨 위 칸(1번째 디자인)")]
    public int signboardDesignIndex = 0;

    private float WidthScale => style == null ? 1f : widthMeters / style.baseModuleWidth;
    private int ModuleCount => style == null ? 0 : Mathf.RoundToInt(widthMeters / style.baseModuleWidth);
    private int DepthModuleCount => style == null ? 0 : Mathf.RoundToInt(style.buildingDepth / style.baseModuleWidth);

    [Header("내부 상태 (건드리지 마세요)")]
    [Tooltip("Generate()가 한 번이라도 실행됐는지 — Play 진입 시 재생성 여부 판단용")]
    [SerializeField, HideInInspector] private bool _hasGenerated = false;

    /// <summary>
    /// BuildingPlacement가 Plot 정보를 가지고 런타임에 호출하는 진입점.
    /// </summary>
    public void Configure(BuildingStyleSO newStyle, int newWidthMeters, int newFloorCount)
    {
        style = newStyle;

        if (style != null && newWidthMeters % (int)style.baseModuleWidth != 0)
        {
            Debug.LogWarning($"[BuildingGenerator] widthMeters({newWidthMeters})는 " +
                              $"{style.baseModuleWidth}의 배수가 아님. 가장 가까운 배수로 보정함.");
            newWidthMeters = Mathf.RoundToInt(newWidthMeters / style.baseModuleWidth) * (int)style.baseModuleWidth;
        }

        widthMeters = newWidthMeters;
        floorCount = newFloorCount;
        Generate();
    }

    [ContextMenu("Generate")]
    public void Generate()
    {
        if (style == null)
        {
            Debug.LogWarning("[BuildingGenerator] style이 비어있어 생성 불가.");
            return;
        }

        Clear();

        Random.InitState(randomSeed);

        // 건물 하나당 색은 두 가지만 결정 — Lobby색, Body색(Middle+RoofCore 공통).
        // 팔레트는 BuildingStyleSO(이 건물의 톤)에서 가져오고, materialStyle은 모듈→재질 매핑만 담당.
        // 그 외 모듈(트림시트/유리/간판/프랍 등)은 각자의 MaterialEntry 설정을 그대로 따름.
        Color? lobbyColor = PickPaletteColor(lobbyColorPalette, 1);
        Color? bodyColor = PickPaletteColor(bodyColorPalette, 2);

        float widthScale = WidthScale;
        float currentY = 0f;

        // ---------------------------------------------------
        // 1. Lobby (+ Lobby 전용 부착물)
        // ---------------------------------------------------
        GameObject lobbyInstance = SpawnModule(style.lobbyPrefab, currentY, widthScale, "Lobby");
        ApplyMaterial(lobbyInstance, ModuleId.Lobby, new Vector2(widthScale, style.lobbyHeight / style.baseModuleHeight), lobbyColor);
        if (lobbyInstance != null)
            GenerateLobbyAttachments(lobbyInstance.transform);
        currentY += style.lobbyHeight;

        // Lobby ↔ Middle_00 경계 — Shop은 Signboard가 그 자리를 채우므로 제외,
        // Residential은 FloorLine이 없으면 허전하므로 추가
        if (useFloorLine && buildingType == BuildingType.Residential)
        {
            GameObject lobbyFloorLine = SpawnModule(style.floorLinePrefab, currentY, widthScale, "FloorLine_Lobby");
            ApplyMaterial(lobbyFloorLine, ModuleId.FloorLine, new Vector2(widthScale, 1f));
        }

        // ---------------------------------------------------
        // 2. Middle 반복 (+ FloorLine, Window, OutdoorUnit, FireEscape)
        // ---------------------------------------------------
        for (int floor = 0; floor < floorCount; floor++)
        {
            GameObject middleInstance = SpawnModule(style.middlePrefab, currentY, widthScale, $"Middle_{floor:00}");
            ApplyMaterial(middleInstance, ModuleId.Middle, new Vector2(widthScale, style.middleHeight / style.baseModuleHeight), bodyColor);
            Transform middleParent = middleInstance != null ? middleInstance.transform : transform;

            if (useWindow)
                GenerateWindowsForFloor(middleParent, floor);

            bool isLastFloor = floor == floorCount - 1;

            if (useFireEscape)
                GenerateFireEscapeForFloor(middleParent, floor, isLastFloor);

            if (stackPillarOnMiddle)
                GenerateMiddlePillars(middleParent);

            currentY += style.middleHeight;

            if (useFloorLine && !isLastFloor)
            {
                // FloorLine은 층 사이에 별도 공간을 차지하는 모듈이 아니라
                // 경계선에 그대로 덧대는 트림(장식 띠)이다.
                // 그래서 currentY를 올리지 않고, 같은 경계 높이에 겹쳐서 붙인다.
                GameObject floorLine = SpawnModule(style.floorLinePrefab, currentY, widthScale, $"FloorLine_{floor:00}");
                ApplyMaterial(floorLine, ModuleId.FloorLine, new Vector2(widthScale, 1f));
            }
        }

        // ---------------------------------------------------
        // 3. RoofCore + RoofOverhang(트림시트) — FloorLine과 같은 패턴으로
        //    돌출부를 별도 부착물로 분리. RoofCore는 Middle과 같은 6m 깊이.
        // ---------------------------------------------------
        GameObject roofInstance = SpawnModule(style.roofPrefab, currentY, widthScale, "RoofCore");
        ApplyMaterial(roofInstance, ModuleId.RoofCore, new Vector2(widthScale, style.roofHeight / style.baseModuleHeight), bodyColor);

        if (style.roofOverhangPrefab != null)
        {
            // Front — LBK 피봇이라 별도 Z오프셋 없이 그대로 붙으면 돌출 방향이 맞음
            GameObject overhangFront = SpawnModule(style.roofOverhangPrefab, currentY, widthScale, "RoofOverhang_Front");
            ApplyMaterial(overhangFront, ModuleId.RoofOverhang, new Vector2(widthScale, 1f));

            // Back — 같은 프리팹을 180도 회전해서 반대편에 붙임.
            // 180도 회전하면 모델이 펼쳐지는 방향(local +X)도 같이 뒤집히므로,
            // 컨테이너 X를 0이 아니라 widthMeters(우측 끝)에서 시작해야 폭 전체를 정확히 덮음
            // (실측으로 확인됨 — Front처럼 X=0 그대로 두면 반대 방향으로 빗나감).
            GameObject overhangBack = SpawnModule(style.roofOverhangPrefab, currentY, widthScale, "RoofOverhang_Back",
                style.buildingDepth, Quaternion.Euler(0f, 180f, 0f));
            if (overhangBack != null)
            {
                Vector3 backPos = overhangBack.transform.localPosition;
                backPos.x = widthMeters;
                overhangBack.transform.localPosition = backPos;
            }
            ApplyMaterial(overhangBack, ModuleId.RoofOverhang, new Vector2(widthScale, 1f));
        }

        if (style.roofOverhangSidePrefab != null)
        {
            // Left/Right — Front/Back 코너와 겹치는 부분까지 포함한 고정 길이(6.6m) 프리팹, 폭 스케일 없음.
            // 실측으로 확인된 값: Left/Right는 회전 방향이 반대일 뿐 아니라, Z 시작점도 서로 다른 코너에서 시작함
            // (모델이 피봇 기준으로 한쪽 방향으로만 뻗는 구조라, 회전 180도 차이만으로는 안 맞고 시작 코너 자체가 바뀜).
            const float sideZFront = -0.3f;                 // Right가 사용하는 시작점 (Front 코너 쪽으로 살짝 더 나감)
            float sideZBack = style.buildingDepth + 0.3f;    // Left가 사용하는 시작점 (Back 코너 쪽으로 살짝 더 나감)

            GameObject overhangLeft = SpawnAttachment(style.roofOverhangSidePrefab, transform,
                new Vector3(0f, currentY, sideZBack), Quaternion.Euler(0f, 90f, 0f), "RoofOverhang_Left");
            ApplyMaterial(overhangLeft, ModuleId.RoofOverhang, Vector2.one);

            GameObject overhangRight = SpawnAttachment(style.roofOverhangSidePrefab, transform,
                new Vector3(widthMeters, currentY, sideZFront), Quaternion.Euler(0f, -90f, 0f), "RoofOverhang_Right");
            ApplyMaterial(overhangRight, ModuleId.RoofOverhang, Vector2.one);
        }

        _hasGenerated = true;
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    /// <summary>
    /// Play 진입 시(또는 빌드 실행 시) 자동 호출됨.
    /// MaterialPropertyBlock은 직렬화되지 않으므로, 에디터에서 Generate()로 적용해둔
    /// 색/Dirt/Emission 등이 Play를 누르는 순간 전부 사라진다.
    /// Generate()는 Clear() 후 동일한 시드로 처음부터 다시 조립하는 완전 재현 가능한
    /// 함수이므로(Random.InitState(randomSeed) + CombineSeed 기반), 이미 한 번
    /// 생성된 적이 있다면(_hasGenerated) Play 시작 시 그대로 다시 호출해서 화면에
    /// 보이던 것과 동일한 결과를 재현한다.
    /// </summary>
    private void Awake()
    {
        if (_hasGenerated && style != null)
        {
            Generate();
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Play 모드 종료 시 Unity가 "Play 시작 직전 상태"로 자동 복원하는데,
    /// 이 복원 과정도 직렬화 기반이라 MaterialPropertyBlock은 복원 대상에서
    /// 빠진다. Awake()로 Play 진입은 해결했지만, Play 종료 시 한 번 더 같은
    /// 문제가 재발하므로, Edit 모드로 돌아온 직후 Generate()를 한 번 더 호출해서
    /// 막는다.
    /// </summary>
    private void OnEnable()
    {
        UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private void OnDisable()
    {
        UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    private void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
    {
        if (state == UnityEditor.PlayModeStateChange.EnteredEditMode && _hasGenerated && style != null)
        {
            // ★ 중요: EnteredEditMode가 떴다고 바로 Generate()를 부르면,
            // Unity 자체의 "Play 시작 직전 상태로 복원" 작업이 이 시점에
            // 아직 안 끝나서 그 다음에 한 번 더 덮어써버리는 타이밍 문제가 있다.
            // 그래서 한 프레임(editor update 1틱) 늦춰서, Unity의 복원이
            // 완전히 끝난 뒤에 다시 Generate()를 호출한다.
            UnityEditor.EditorApplication.delayCall += DelayedRegenerateAfterPlayStop;
        }
    }

    private void DelayedRegenerateAfterPlayStop()
    {
        // 이 콜백이 실행되는 시점엔 오브젝트가 이미 파괴됐을 수도 있으므로 null 체크 필수
        if (this == null) return;
        if (_hasGenerated && style != null)
        {
            Generate();
        }
    }
#endif

    // =========================================================
    // Lobby 부착물 — Residential / Shop 분기
    // =========================================================
    private void GenerateLobbyAttachments(Transform lobbyParent)
    {
        if (buildingType == BuildingType.Residential)
            GenerateResidentialLobby(lobbyParent);
        else
            GenerateShopLobby(lobbyParent);

        if (useLobbyPillar)
        {
            GameObject pillarPrefab = buildingType == BuildingType.Residential
                ? style.pillarPrefabResidential
                : style.pillarPrefabShop;
            ModuleId pillarModuleId = buildingType == BuildingType.Residential
                ? ModuleId.PillarResidential
                : ModuleId.PillarShop;

            if (pillarPrefab != null)
            {
                // Pillar_L: 메쉬가 비대칭이라 코너 좌표(0,0)에서 보정값(-0.1, 0.2) 적용 — 실측 고정값
                GameObject pillarL = SpawnAttachment(pillarPrefab, lobbyParent, new Vector3(-0.1f, 0f, 0.2f), Quaternion.identity, "Pillar_L");
                ApplyMaterial(pillarL, pillarModuleId, Vector2.one);

                // Pillar_R: 단순 이동/회전이 아니라 메쉬 자체가 비대칭(노치)이라 좌우 거울 반전이 필요.
                // X축 음수 스케일로 미러링 + 위치는 좌측과 동일한 오프셋 패턴을 미러링.
                GameObject pillarR = SpawnAttachment(pillarPrefab, lobbyParent,
                    new Vector3(widthMeters + 0.1f, 0f, 0.2f), Quaternion.identity, "Pillar_R");
                if (pillarR != null)
                {
                    Vector3 s = pillarR.transform.localScale;
                    s.x = -s.x;
                    pillarR.transform.localScale = s;
                }
                ApplyMaterial(pillarR, pillarModuleId, Vector2.one);
            }
        }
    }

    /// <summary>
    /// a(기둥, 옵션) - b(LobbyBelt, 가변) - c(계단+문, 3m 고정, 항상 중앙) - b - a
    /// c가 중앙 고정이므로, 좌우 LobbyBelt 폭 = (widthMeters - 3) / 2
    /// </summary>
    private void GenerateResidentialLobby(Transform lobbyParent)
    {
        float doorStepWidth = style.lobbyDoorStepWidth;
        float beltTotalWidth = widthMeters - doorStepWidth;
        float beltEachWidth = beltTotalWidth / 2f;

        float doorStepX = (widthMeters - doorStepWidth) / 2f;
        GameObject step = SpawnAttachment(style.lobbyStepPrefab, lobbyParent, new Vector3(doorStepX, 0f, 0f), Quaternion.identity, "Lobby_Step");
        ApplyMaterial(step, ModuleId.Step, Vector2.one);

        if (step != null && style.lobbyDoorPrefab != null)
        {
            // Door는 Step의 자식 — 둘 다 LBK 피봇이라 Vector3.zero로 두면 Step의
            // 좌측-하단 코너에 Door가 그대로 겹쳐져서 "왼쪽으로 치우치고 바닥에 깔린" 것처럼 보임.
            // Step 위에 Door를 중앙 정렬 + 계단 높이만큼 띄워서 올려야 함:
            //   X = (Step 폭 - Door 폭) / 2   → 가로 중앙 정렬
            //   Y = Step 높이                 → 계단 상판 위로 올리기
            float doorLocalX = (doorStepWidth - lobbyDoorWidth) * 0.5f;
            Vector3 doorLocalPos = new Vector3(doorLocalX, lobbyStepHeight, 0f);

            GameObject door = SpawnAttachment(style.lobbyDoorPrefab, step.transform, doorLocalPos, Quaternion.identity, "Door");
            ApplyMaterial(door, ModuleId.Door, Vector2.one);
        }

        if (style.lobbyBeltPrefab != null && beltEachWidth > 0f)
        {
            // 가변형 — 원본 폭(baseModuleWidth) 기준으로 스케일, Tiling 보정은 셰이더 파트(STEP 2-3)에서 처리
            float beltScale = beltEachWidth / style.baseModuleWidth;
            GameObject beltL = SpawnScaledAttachment(style.lobbyBeltPrefab, lobbyParent, new Vector3(0f, 0f, 0f), beltScale, "LobbyBelt_L");
            ApplyMaterial(beltL, ModuleId.LobbyBelt, new Vector2(beltScale, 1f));
            GameObject beltR = SpawnScaledAttachment(style.lobbyBeltPrefab, lobbyParent, new Vector3(doorStepX + doorStepWidth, 0f, 0f), beltScale, "LobbyBelt_R");
            ApplyMaterial(beltR, ModuleId.LobbyBelt, new Vector2(beltScale, 1f));
        }
    }

    /// <summary>
    /// 코너 기둥을 제외한 내부 슬롯을 shopLobbyPattern 배열 순서대로 3m씩 채운다.
    /// 패턴 배열 길이는 ModuleCount와 맞춰서 에디터에서 직접 지정.
    /// </summary>
    private void GenerateShopLobby(Transform lobbyParent)
    {
        if (shopLobbyPattern == null || shopLobbyPattern.Length == 0)
        {
            Debug.LogWarning("[BuildingGenerator] shopLobbyPattern이 비어있어 Shop Lobby 부착물을 건너뜀.");
        }
        else
        {
            // Style은 9m/12m 에셋을 둘 다 들고 있고, widthMeters로 이 건물에 맞는 쪽을 자동 선택한다.
            bool isNarrow = widthMeters <= 9;
            Texture2D showWindowAtlas = isNarrow ? style.showWindowAtlas9m : style.showWindowAtlas12m;
            int showWindowAtlasRows = isNarrow ? style.showWindowAtlasRows9m : style.showWindowAtlasRows12m;

            for (int i = 0; i < shopLobbyPattern.Length; i++)
            {
                float slotX = i * style.baseModuleWidth;
                bool isWindow = shopLobbyPattern[i] == LobbySlotType.ShowWindow;
                GameObject prefab = isWindow ? style.showWindowPrefab : style.showWindowDoorPrefab;

                GameObject slot = SpawnAttachment(prefab, lobbyParent, new Vector3(slotX, 0f, 0f), Quaternion.identity, $"Lobby_Slot_{i:00}");

                ModuleId frameModuleId = isWindow ? ModuleId.ShowWindowFrame : ModuleId.ShowWindowDoorFrame;
                ModuleId glassModuleId = isWindow ? ModuleId.ShowWindowGlass : ModuleId.ShowWindowDoorGlass;

                ApplyMaterialToChild(slot, "Frame", frameModuleId, Vector2.one);
                ApplyMaterialToChild(slot, "Glass", glassModuleId, Vector2.one);

                // Glass에 "가게 내부 풍경" Emission 아틀라스 덮어씌우기.
                // setSeed=randomSeed(건물 시드)로 통일 — 같은 건물의 모든 슬롯이 같은 가게 풍경(세트)을 공유.
                if (slot != null)
                {
                    Transform glass = slot.transform.Find("Glass");
                    if (glass != null)
                    {
                        int? forcedShowWindowIndex = overrideShowWindowDesign ? showWindowDesignIndex : (int?)null;
                        BuildingMaterialApplier.ApplyShowWindowGlassEmission(
                            glass.gameObject,
                            patternLength: shopLobbyPattern.Length,
                            indexInPattern: i,
                            setSeed: randomSeed,
                            showWindowAtlas, showWindowAtlasRows,
                            showWindowEmissionColor, showWindowEmissionIntensity, forcedShowWindowIndex);
                    }
                }
            }
        }

        // Signboard — Style은 9m/12m 프리팹/아틀라스를 둘 다 들고 있고, widthMeters로 자동 선택.
        bool isNarrowSign = widthMeters <= 9;
        GameObject signboardPrefab = isNarrowSign ? style.signboardPrefab9m : style.signboardPrefab12m;
        Texture2D signboardAtlas = isNarrowSign ? style.signboardAtlas9m : style.signboardAtlas12m;
        int signboardAtlasRows = isNarrowSign ? style.signboardAtlasRows9m : style.signboardAtlasRows12m;

        GameObject signboard = SpawnAttachment(signboardPrefab, lobbyParent, new Vector3(0f, style.signboardBottomY, 0f), Quaternion.identity, "Signboard");
        ApplyMaterial(signboard, ModuleId.Signboard, Vector2.one);

        if (signboard != null)
        {
            int? forcedSignIndex = overrideSignboardDesign ? signboardDesignIndex : (int?)null;
            BuildingMaterialApplier.ApplySignboardEmission(
                signboard, randomSeed,
                signboardAtlas, signboardAtlasRows,
                signboardEmissionColor, signboardEmissionIntensity, forcedSignIndex);
        }
    }

    // =========================================================
    // Window — 모듈(3m)당 1개, 면 중앙. 면별 on/off만 적용
    // =========================================================
    private void GenerateWindowsForFloor(Transform middleParent, int floor)
    {
        if (style.windowPrefab == null) return;

        // Middle은 부모이므로 자식 좌표는 Middle 로컬 기준(0~middleHeight) — 절대 Y(floorY) 더하지 않음
        float windowY = style.middleHeight * 0.5f + style.windowVerticalOffset;

        if (style.frontHasWindow)
            GenerateWindowsOnFace(middleParent, Face.Front, floor, windowY);
        if (style.backHasWindow)
            GenerateWindowsOnFace(middleParent, Face.Back, floor, windowY);
        if (style.leftHasWindow)
            GenerateWindowsOnFace(middleParent, Face.Left, floor, windowY);
        if (style.rightHasWindow)
            GenerateWindowsOnFace(middleParent, Face.Right, floor, windowY);
    }

    private void GenerateWindowsOnFace(Transform middleParent, Face face, int floor, float windowY)
    {
        bool isFrontBack = face == Face.Front || face == Face.Back;
        int count = isFrontBack ? ModuleCount : DepthModuleCount;

        for (int i = 0; i < count; i++)
        {
            Vector3 centerPos = GetFaceCenterPosition(face, i, windowY);
            Quaternion rot = GetFaceRotation(face);

            // Window는 LBK 피봇(좌측 기준) — 슬롯 중앙에 피봇을 그대로 꽂으면
            // 창문 폭의 절반만큼 오른쪽으로 밀려 보인다. 회전된 로컬 -X(왼쪽) 방향으로
            // windowWidth/2만큼 보정해서, 실제 메쉬가 슬롯 중앙에 오도록 맞춘다.
            Vector3 pivotPos = centerPos - rot * (Vector3.right * (style.windowWidth * 0.5f));

            GameObject windowInstance = SpawnAttachment(style.windowPrefab, middleParent, pivotPos, rot, $"Window_{face}_{floor:00}_{i:00}");
            ApplyMaterialToChild(windowInstance, "Frame", ModuleId.WindowFrame, Vector2.one);
            ApplyMaterialToChild(windowInstance, "Glass", ModuleId.WindowGlass, Vector2.one);

            if (useWindow && windowInstance != null)
            {
                Transform glass = windowInstance.transform.Find("Glass");
                if (glass != null)
                {
                    Color pickedLitColor = PickWindowLitColor(windowInstance.name);
                    BuildingMaterialApplier.ApplyWindowLit(
                        glass.gameObject, windowOffTex, pickedLitColor,
                        windowLitChance, windowFlickerChance, windowChangeInterval, windowFlickerSpeed);
                }
            }

            if (useOutdoorUnit && style.outdoorUnitPrefab != null
                && Random.value < style.outdoorUnitSpawnChance)
            {
                SpawnOutdoorUnit(middleParent, face, pivotPos, rot, floor, i);
            }
        }
    }

    private void SpawnOutdoorUnit(Transform middleParent, Face face, Vector3 windowPos, Quaternion faceRot, int floor, int index)
    {
        // Window 기준 (X,Y) 오프셋을 "그 면의 로컬 좌표계" 기준으로 두고,
        // faceRot을 곱해서 월드로 변환 — Window 피봇 보정과 동일한 원리.
        // 면마다 축을 수동으로 바꿔주던 이전 방식은 회전 부호가 꼬이기 쉬워서 제거.
        Vector3 localOffset = new Vector3(style.outdoorUnitOffset.x, style.outdoorUnitOffset.y, 0f);
        Vector3 pos = windowPos + faceRot * localOffset;

        GameObject outdoorUnit = SpawnAttachment(style.outdoorUnitPrefab, middleParent, pos, faceRot, $"OutdoorUnit_{face}_{floor:00}_{index:00}");
        ApplyMaterial(outdoorUnit, ModuleId.OutdoorUnit, Vector2.one);
    }

    /// <summary>
    /// stackPillarOnMiddle이 켜져 있으면 Middle 층마다도 기둥을 좌우에 쌓아서,
    /// Lobby부터 옥상까지 이어지는 기둥처럼 보이게 한다. 코너 보정값은 Lobby 기둥과 동일하게 재사용.
    /// </summary>
    private void GenerateMiddlePillars(Transform middleParent)
    {
        GameObject pillarPrefab = buildingType == BuildingType.Residential
            ? style.middlePillarPrefabResidential
            : style.middlePillarPrefabShop;
        ModuleId pillarModuleId = buildingType == BuildingType.Residential
            ? ModuleId.PillarResidential
            : ModuleId.PillarShop;

        if (pillarPrefab == null) return;

        GameObject pillarL = SpawnAttachment(pillarPrefab, middleParent, new Vector3(-0.1f, 0f, 0.2f), Quaternion.identity, "Pillar_L");
        ApplyMaterial(pillarL, pillarModuleId, Vector2.one);

        GameObject pillarR = SpawnAttachment(pillarPrefab, middleParent,
            new Vector3(widthMeters + 0.1f, 0f, 0.2f), Quaternion.identity, "Pillar_R");
        if (pillarR != null)
        {
            Vector3 s = pillarR.transform.localScale;
            s.x = -s.x;
            pillarR.transform.localScale = s;
        }
        ApplyMaterial(pillarR, pillarModuleId, Vector2.one);
    }

    // =========================================================
    // Fire Escape — Landing(6m) + Stair, 우측 끝 기준, 정면(Front)
    // 최상층은 Landing만, 로비까지 안 내려옴 (Stair는 마지막 층 전까지만 생성)
    // =========================================================
    private void GenerateFireEscapeForFloor(Transform middleParent, int floor, bool isLastFloor)
    {
        if (style.feLandingPrefab == null) return;

        float landingX = widthMeters - style.feRightMargin - BuildingStyleSO.feLandingWidth;
        float landingY = style.feLandingYOffset; // Middle 로컬 기준 (부모가 이미 floorY에 위치)

        // Front 면 기준 배치 (실제 참조 이미지 확인 결과 — 정면 외벽에 노출되는 형태)
        Vector3 landingPos = new Vector3(landingX, landingY, 0f);
        GameObject landing = SpawnAttachment(style.feLandingPrefab, middleParent, landingPos, Quaternion.identity, $"FE_Landing_{floor:00}");
        ApplyMaterial(landing, ModuleId.FE_Landing, Vector2.one);

        // Stair는 "현재 층 Landing → 다음 층 Landing"을 잇는 모듈.
        // 최상층에서는 더 올라갈 곳이 없으므로 생성하지 않음.
        if (!isLastFloor && style.feStairPrefab != null)
        {
            Vector3 stairPos = new Vector3(landingX, landingY, 0f);
            GameObject stair = SpawnAttachment(style.feStairPrefab, middleParent, stairPos, Quaternion.identity, $"FE_Stair_{floor:00}");
            ApplyMaterial(stair, ModuleId.FE_Stair, Vector2.one);
        }
    }

    // =========================================================
    // 공통 유틸
    // =========================================================
    private enum Face { Front, Back, Left, Right }

    private Vector3 GetFaceCenterPosition(Face face, int index, float y)
    {
        float center = (index + 0.5f) * style.baseModuleWidth;

        switch (face)
        {
            case Face.Front: return new Vector3(center, y, 0f);
            case Face.Back: return new Vector3(center, y, style.buildingDepth);
            case Face.Left: return new Vector3(0f, y, center);
            case Face.Right: return new Vector3(widthMeters, y, center);
            default: return Vector3.zero;
        }
    }

    private Quaternion GetFaceRotation(Face face)
    {
        switch (face)
        {
            case Face.Front: return Quaternion.identity;
            case Face.Back: return Quaternion.Euler(0f, 180f, 0f);
            case Face.Left: return Quaternion.Euler(0f, 90f, 0f);
            case Face.Right: return Quaternion.Euler(0f, -90f, 0f);
            default: return Quaternion.identity;
        }
    }

    /// <summary>
    /// 빈 컨테이너(스케일 없음)를 만들고, 그 안에 실제 메쉬를 widthScale 적용해서 넣는다.
    /// 부착물(Window/Pillar 등)은 이 컨테이너의 자식으로 붙이면 되므로,
    /// 메쉬에 걸린 widthScale이 부착물 위치/크기에 전혀 영향을 주지 않는다.
    /// 반환값은 "컨테이너" Transform — 부착물의 부모로 사용.
    /// </summary>
    /// <summary>
    /// lobbyColorPalette / bodyColorPalette(Generator 자체 필드)에서 randomSeed 기반으로 색 하나를 일관되게 선택.
    /// 팔레트가 비어있으면 null(= ApplyMaterial에서 모듈별 기본 동작으로 처리).
    /// </summary>
    private Color? PickPaletteColor(Color[] palette, int roleOffset)
    {
        if (palette == null || palette.Length == 0) return null;
        var rng = new System.Random(randomSeed * 17 + roleOffset);
        return palette[rng.Next(palette.Length)];
    }

    private GameObject SpawnModule(GameObject prefab, float y, float widthScale, string objectName, float zOffset = 0f, Quaternion? rotation = null)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"[BuildingGenerator] '{objectName}' 프리팹이 비어있어 건너뜀.");
            return null;
        }

        GameObject container = new GameObject(objectName);
        container.transform.SetParent(transform, false);
        container.transform.localPosition = new Vector3(0f, y, zOffset);
        container.transform.localRotation = rotation ?? Quaternion.identity;
        // container.transform.localScale는 항상 1 유지 — 스케일은 메쉬 자식에만 적용

        GameObject mesh = Instantiate(prefab, container.transform);
        mesh.name = objectName + "_Mesh";
        mesh.transform.localPosition = Vector3.zero;
        mesh.transform.localRotation = Quaternion.identity;

        Vector3 scale = mesh.transform.localScale;
        scale.x = widthScale;
        mesh.transform.localScale = scale;

        return container;
    }

    /// <summary>
    /// 부착물(Attachment) 인스턴스화 — 폭 스케일을 적용하지 않는 고정 크기 모델용.
    /// </summary>
    private GameObject SpawnAttachment(GameObject prefab, Transform parent, Vector3 localPos, Quaternion localRot, string objectName)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"[BuildingGenerator] '{objectName}' 프리팹이 비어있어 건너뜀.");
            return null;
        }

        GameObject instance = Instantiate(prefab, parent);
        instance.name = objectName;
        instance.transform.localPosition = localPos;
        instance.transform.localRotation = localRot;
        return instance;
    }

    /// <summary>
    /// LobbyBelt처럼 가변 스케일이 필요한 부착물용 — X축만 스케일.
    /// </summary>
    private GameObject SpawnScaledAttachment(GameObject prefab, Transform parent, Vector3 localPos, float scaleX, string objectName)
    {
        GameObject instance = SpawnAttachment(prefab, parent, localPos, Quaternion.identity, objectName);
        if (instance == null) return null;

        Vector3 scale = instance.transform.localScale;
        scale.x = scaleX;
        instance.transform.localScale = scale;
        return instance;
    }

    /// <summary>
    /// <summary>
    /// 건물 전체 시드(randomSeed)와 모듈 고유 이름을 섞어서 "모듈마다 다른" 시드를 만든다.
    /// 같은 건물(같은 randomSeed)이면 항상 같은 결과가 나오고(재현 가능),
    /// 같은 건물 안에서도 Middle_00 vs Middle_01처럼 이름이 다르면 패턴이 달라진다.
    /// (전부 randomSeed 하나만 그대로 넘기면 모든 층의 Dirt/Noise가 똑같은 자리에 찍히는 버그가 있었음)
    /// </summary>
    private static int CombineSeed(int baseSeed, string salt)
    {
        unchecked
        {
            return baseSeed * 397 + salt.GetHashCode();
        }
    }

    /// <summary>
    /// 창문 인스턴스 이름으로 windowLitColorPalette에서 색 하나를 일관되게 선택.
    /// 팔레트가 비어있으면 windowLitColor 단일 색을 그대로 반환(기존 동작 유지).
    /// 같은 건물을 다시 Generate해도 창문마다 항상 같은 색이 나옴(재현성).
    /// </summary>
    private Color PickWindowLitColor(string windowName)
    {
        if (windowLitColorPalette == null || windowLitColorPalette.Length == 0)
            return windowLitColor;

        int seed = CombineSeed(randomSeed, windowName + "_litcolor");
        var rng = new System.Random(seed);
        int index = rng.Next(windowLitColorPalette.Length);
        return windowLitColorPalette[index];
    }

    /// <summary>
    /// BuildingMaterialApplier 호출 래퍼 — style.materialStyle이 없으면 조용히 건너뜀.
    /// </summary>
    private void ApplyMaterial(GameObject instance, ModuleId moduleId, Vector2 tilingScale, Color? overrideColor = null)
    {
        if (instance == null || style.materialStyle == null) return;

        int? dirtSeedOverride = overrideDirtSeed ? dirtSeed : (int?)null;
        int? noiseSeedOverride = overrideNoiseSeed ? noiseSeed : (int?)null;
        int instanceSeed = CombineSeed(randomSeed, instance.name);

        BuildingMaterialApplier.Apply(instance, style.materialStyle, moduleId, instanceSeed, tilingScale,
                                       overrideColor, dirtSeedOverride, noiseSeedOverride);

        Debug.Log($"[체크] {instance.name} seed={CombineSeed(randomSeed, instance.name)}");
    }

    /// <summary>
    /// Frame/Glass처럼 한 프리팹 안에 재질이 다른 자식이 섞여 있을 때,
    /// 자식 이름("Frame", "Glass")으로 찾아서 각각 다른 ModuleId 재질을 적용.
    /// 자식을 못 찾으면 조용히 건너뜀(모델 쪽에서 이름 규칙을 안 지켰을 경우 디버깅 포인트).
    /// </summary>
    private void ApplyMaterialToChild(GameObject instance, string childName, ModuleId moduleId, Vector2 tilingScale)
    {
        if (instance == null || style.materialStyle == null) return;

        Transform child = instance.transform.Find(childName);
        if (child == null)
        {
            Debug.LogWarning($"[BuildingGenerator] '{instance.name}'에서 자식 '{childName}'을 못 찾음. " +
                              "프리팹 안에 동일한 이름의 자식 오브젝트가 있는지 확인.");
            return;
        }

        int? dirtSeedOverride = overrideDirtSeed ? dirtSeed : (int?)null;
        int? noiseSeedOverride = overrideNoiseSeed ? noiseSeed : (int?)null;
        int instanceSeed = CombineSeed(randomSeed, instance.name + "_" + childName);

        BuildingMaterialApplier.Apply(child.gameObject, style.materialStyle, moduleId, instanceSeed, tilingScale,
                                       null, dirtSeedOverride, noiseSeedOverride);
    }

    [ContextMenu("Clear")]
    public void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(child);
                continue;
            }
#endif
            Destroy(child);
        }
    }
}
