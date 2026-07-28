using UnityEngine;

/// <summary>
/// 건물의 "모양"(프리팹 조합 + 고정 치수)을 정의하는 데이터 에셋.
/// 프로젝트 전체에서 보통 1개만 만들어서 모든 BuildingGenerator가 공유한다.
/// Residential/Commercial 분기, 9m/12m 폭, 색/패턴 등 "건물마다 달라지는 값"은
/// 전부 BuildingGenerator(씬 컴포넌트) 쪽 책임 — 이 SO는 "이 프로젝트에서 쓸 수 있는
/// 모든 프리팹/에셋의 창고" 역할만 한다. 9m/12m처럼 실제 모델이 다른 경우는
/// 이 SO가 두 에셋을 다 들고 있고, Generator가 widthMeters로 그중 하나를 골라 쓴다.
/// </summary>
[CreateAssetMenu(fileName = "NewBuildingStyle", menuName = "Cyberpunk/Building Style SO")]
public class BuildingStyleSO : ScriptableObject
{
    [Header("모듈 프리팹 (LBF 피봇, 폭 3m 기준 모델)")]
    public GameObject middlePrefab;
    public GameObject roofPrefab;
    [Tooltip("Roof 돌출부(트림시트) — Front/Back은 FloorLine처럼 X축 타일링되는 부착물")]
    public GameObject roofOverhangPrefab;
    [Tooltip("좌/우 측면용 돌출부. Front/Back 코너와 안 맞물리는 모자란 부분을 덮기 위해 " +
             "건물 깊이(6m)보다 긴 6.6m 고정 길이 프리팹 — 폭 스케일 없이 그대로 붙임")]
    public GameObject roofOverhangSidePrefab;
    public const float roofOverhangSideLength = 6.6f;
    public GameObject floorLinePrefab;

    [Header("고정 치수 (모델 사이즈 종합표 기준)")]
    public float baseModuleWidth = 3f;   // 모델 원본 폭(스케일 1일 때)
    public float middleHeight = 3f;
    public float roofHeight = 0.6f;
    [Tooltip("Y축 UV 타일링 기준이 되는 모듈 높이. Lobby/Middle/Roof가 같은 머티리얼을 쓸 때, " +
             "이 값 대비 자기 높이의 비율로 _TilingScale.y를 계산해서 텍스처 밀도를 통일함")]
    public float baseModuleHeight = 3f;
    [Tooltip("RoofOverhang(트림시트) 전면 돌출량 — FloorLine처럼 X축 타일링되는 별도 부착물의 Z 위치 보정값")]
    public float roofFrontOverhang = 0.3f;
    public float roofOverhangHeight = 0.6f;
    public float floorLineHeight = 0.3f;
    public float buildingDepth = 6f;     // 깊이 고정값 — Front/Back 면 좌표 계산에 사용

    // =========================================================
    // STEP 2-2 보강 — Lobby (2종류: Residential / Shop)
    // =========================================================
    [Header("Lobby — 공통")]
    [Tooltip("Lobby 본체 박스 (LBF 피봇, 폭 3m 기준). 폭 스케일은 Middle과 동일하게 widthScale 적용")]
    public GameObject lobbyPrefab;
    public float lobbyHeight = 3.8f;

    [Tooltip("Lobby 좌우 코너 기둥. 코너에 일부 매몰되는 형태 — 피봇/사이즈는 동일하나 " +
             "주거형/상가형 텍스처(석재 vs 금속 트림시트)가 달라서 프리팹을 분리함")]
    public GameObject pillarPrefabResidential;
    public GameObject pillarPrefabShop;

    [Header("기둥 — Middle용 프리팹 (실제 사용 여부는 Generator의 useLobbyPillar/stackPillarOnMiddle로 결정)")]
    [Tooltip("Middle용 기둥 프리팹 — Lobby용과 높이가 다를 수 있어 별도로 둠")]
    public GameObject middlePillarPrefabResidential;
    public GameObject middlePillarPrefabShop;

    [Header("Lobby — Residential (주거형)")]
    [Tooltip("c 슬롯 본체: 계단. 항상 가운데. LBK 피봇")]
    public GameObject lobbyStepPrefab;
    [Tooltip("계단 위에 배치되는 문. Step의 자식으로 생성됨")]
    public GameObject lobbyDoorPrefab;
    public float lobbyDoorStepWidth = 2.0f;
    [Tooltip("b 슬롯: 로비벨트 — 가변형(스케일+타일링). c를 기준으로 좌우 대칭 배치, 남는 폭만큼 스케일")]
    public GameObject lobbyBeltPrefab;

    [Header("Lobby — Shop (상점형)")]
    [Tooltip("b 슬롯: 쇼윈도, 3m × 2.9m × 0.1m, 추가형(타일링). LBK 피봇")]
    public GameObject showWindowPrefab;
    [Tooltip("c 슬롯: 쇼윈도문, 추가형. LBK 피봇")]
    public GameObject showWindowDoorPrefab;

    [Header("ShowWindow Glass Emission — 아틀라스 (9m/12m 별도 텍스처, Style 1개가 둘 다 보유)")]
    [Tooltip("9m 패턴(3슬롯)용 아틀라스. 한 줄 = 가게 내부 풍경 1세트(3슬롯 연속)")]
    public Texture2D showWindowAtlas9m;
    [Tooltip("12m 패턴(4슬롯)용 아틀라스. 한 줄 = 가게 내부 풍경 1세트(4슬롯 연속)")]
    public Texture2D showWindowAtlas12m;
    [Tooltip("showWindowAtlas9m의 세로 디자인 종류 수")]
    public int showWindowAtlasRows9m = 6;
    [Tooltip("showWindowAtlas12m의 세로 디자인 종류 수")]
    public int showWindowAtlasRows12m = 6;

    [Header("Signboard (간판) — Shop 전용, 9m/12m 프리팹 둘 다 보유")]
    public GameObject signboardPrefab9m;
    public GameObject signboardPrefab12m;
    public float signboardHeight = 0.9f;
    [Tooltip("간판 하단이 Lobby 바닥 기준 몇 m 지점에 오는지 (간판 하단=2.9m, 상단=2.9+0.9=3.8m로 Lobby 상단과 맞물림)")]
    public float signboardBottomY = 2.9f;

    [Header("Signboard Emission — 아틀라스 (9m/12m 별도 텍스처, 세로 7등분)")]
    [Tooltip("9m 폭 건물용 간판 아틀라스. 2048×2048, 세로 7등분 — 디자인 7종")]
    public Texture2D signboardAtlas9m;
    [Tooltip("12m 폭 건물용 간판 아틀라스. 9m용과 겹치지 않는 다른 7종")]
    public Texture2D signboardAtlas12m;
    [Tooltip("signboardAtlas9m의 세로 디자인 종류 수 (기본 7)")]
    public int signboardAtlasRows9m = 7;
    [Tooltip("signboardAtlas12m의 세로 디자인 종류 수 (기본 7)")]
    public int signboardAtlasRows12m = 7;

    // =========================================================
    // STEP 2-2 보강 — Fire Escape (외벽 비상계단)
    // =========================================================
    [Header("Fire Escape — 외벽 철제 비상계단")]
    [Tooltip("발판/플랫폼. 6m × 0.9m × 1.0m. LBK 피봇")]
    public GameObject feLandingPrefab;
    [Tooltip("층간 대각선 계단. 1.0m × 3.0m × 1.8m. 최상층 아래로는 (N-1)개만 생성")]
    public GameObject feStairPrefab;
    [Tooltip("건물 우측 끝에서 안쪽으로 띄우는 여백(초안값, 실제 배치 후 튠)")]
    public float feRightMargin = 0.5f;
    [Tooltip("Middle 바닥 기준 Landing을 위로 띄우는 높이(초안값)")]
    public float feLandingYOffset = 0.4f;
    public const float feLandingWidth = 6f;

    // =========================================================
    // STEP 2-2 보강 — Window (면별 on/off, 모듈당 1개 중앙)
    // =========================================================
    [Header("Window — 모듈(3m)당 1개, 중앙 고정")]
    public GameObject windowPrefab;
    [Tooltip("Window 모델 폭(W). LBK 피봇 보정(슬롯 중앙 정렬)에 사용. 스펙표 기준 1.0m")]
    public float windowWidth = 1.0f;
    public bool frontHasWindow = true;
    public bool backHasWindow = false;
    public bool leftHasWindow = true;
    public bool rightHasWindow = true;
    [Tooltip("Middle 층 중앙 기준 Y 보정값. 음수면 아래로 내려감")]
    public float windowVerticalOffset = -0.3f;

    // =========================================================
    // STEP 2-2 보강 — OutdoorUnit (실외기)
    // =========================================================
    [Header("OutdoorUnit — Window 있는 면에서만, 모듈마다 확률 배치")]
    public GameObject outdoorUnitPrefab;
    [Range(0f, 1f)]
    public float outdoorUnitSpawnChance = 0.3f;
    [Tooltip("Window 기준 (X, Y) 오프셋 — 보통 창문 아래쪽이라 Y는 음수")]
    public Vector2 outdoorUnitOffset = new Vector2(0.3f, -0.3f);

    [Header("재질 (색상/Dirt/Tiling — 별도 SO, STEP 2-3)")]
    [Tooltip("프로젝트 전체에서 공유하는 모듈→재질 매핑 테이블 (보통 한 개만 만들어서 모든 BuildingStyleSO가 같이 참조)")]
    public BuildingMaterialStyleSO materialStyle;

    [Header("향후 확장용 (지금은 안 써도 됨)")]
    [Tooltip("건물당 Dirt 베리에이션 시드 범위")]
    public Vector2 dirtSeedRange = new Vector2(0f, 1f);
}

public enum BuildingType
{
    Residential,
    Commercial
}

public enum LobbySlotType
{
    ShowWindow,
    ShowWindowDoor
}
