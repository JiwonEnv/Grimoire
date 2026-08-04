using UnityEngine;

/// <summary>
/// 케이블/전선 오브젝트 하나에 붙여서, CableLit 머티리얼을 인스턴스로 복제하고
/// 이 오브젝트만의 AnchorA/AnchorB(월드 좌표)를 셰이더에 전달하는 컴포넌트.
///
/// 왜 인스턴스 머티리얼인가 (MPB를 안 쓰는 이유):
/// - _AnchorA/_AnchorB는 셰이더 Property라 "머티리얼 객체당 값 하나"임 (데칼 KB 5.1절과 동일 원칙)
/// - 공유 머티리얼 하나를 여러 케이블에 그대로 씌우면 전부 같은 좌표를 참조해 겹쳐 보임
/// - MPB로도 개별화는 가능하지만, STEP 2-5(9절)에서 이미 겪은 대로
///   Play 모드 진입/종료 시 MPB 값이 사라지는 문제가 있어 이 프로젝트에서는
///   "두 번 이상 덮어써야 하는 Property가 있는 모듈"은 인스턴스 머티리얼로 통일하기로 함
///   (Glass/Signboard/WindowGlass와 같은 패턴, 이번엔 Cable도 여기 포함)
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(MeshRenderer))]
public class CableGenerator : MonoBehaviour
{
    [Header("머티리얼 템플릿 (CableLit 셰이더, 프로젝트 공유 원본)")]
    public Material sharedCableMaterial;

    [Header("앵커 — 오브젝트 Transform과 무관하게 월드 좌표 직접 지정 (3.1절/3.3절 원칙)")]
    [Tooltip("씬에 배치한 빈 GameObject를 끌어다 놓으면 그 Position을 앵커로 사용합니다.")]
    public Transform anchorA;
    public Transform anchorB;

    [Header("Sag (정적 처짐)")]
    [Min(0f)] public float sagAmount = 0.5f;
    [Tooltip("처짐이 가장 깊은 지점의 위치. 0.5=정중앙(기본), 0에 가까울수록 AnchorA 쪽으로 치우침")]
    [Range(0f, 1f)] public float sagPeakT = 0.5f;

    [Header("메쉬 스펙 — 실제 사용하는 케이블 메쉬의 값과 반드시 일치해야 함")]
    [Tooltip("케이블 메쉬가 로컬 X축을 따라 뻗은 실제 길이(m). 메쉬를 만들 때 정한 값 그대로 입력.")]
    [Min(0.0001f)] public float meshLength = 1f;

    [Header("Wind (동적 흔들림) — SG_WindSway 파라미터")]
    public float windSpeed = 1.5f;
    [Tooltip("실배포값. 테스트 시 0.3~0.5로 올려서 효과 유무부터 확인 권장 (11절 방식)")]
    public float windStrength = 0.1f;
    public float noiseScale = 0.5f;
    [Range(0f, 1f)] public float turbulence = 0.5f;

    [Header("표면")]
    public Texture2D baseTex;
    public Color baseColorTint = Color.white;
    public Texture2D normalTex;
    public Texture2D ormTex;

    [Header("실시간 갱신")]
    [Tooltip("켜두면 AnchorA/B를 옮길 때마다 케이블이 매 프레임 자동으로 따라옵니다. " +
             "배치가 끝나 더 이상 안 움직일 예정이면 꺼서 불필요한 연산을 아낄 수 있습니다. " +
             "머티리얼을 다시 만드는 게 아니라 좌표 2개만 갱신하는 거라 비용은 작습니다.")]
    public bool liveFollowAnchors = true;

    private MeshRenderer _renderer;
    private Material _instanceMat;

    // Play 모드 재진입 시에도 항상 재적용되도록 (STEP 2-5 9절 트러블슈팅 1번과 동일 이유)
    // [ExecuteAlways] 덕분에 Edit 모드에서 컴포넌트를 붙이는 순간에도 호출됨
    private void Awake()
    {
        Generate();
    }

    // ★ Anchor를 옮겨도 케이블이 안 움직이는 문제 대응
    //   Generate()는 "호출된 순간의" Anchor 좌표를 딱 한 번 머티리얼에 박아넣을 뿐이라,
    //   그 뒤 Anchor Transform을 옮겨도 자동 반영되지 않음 (15절 원칙 6번과 동일 이유).
    //   여기서는 머티리얼을 다시 만들지 않고 좌표 2개만 매 프레임 가볍게 갱신해서 실시간으로 따라오게 함.
    private void LateUpdate()
    {
        if (!liveFollowAnchors) return;
        if (_instanceMat == null || anchorA == null || anchorB == null) return;

        _instanceMat.SetVector("_AnchorA", anchorA.position);
        _instanceMat.SetVector("_AnchorB", anchorB.position);
    }

    [ContextMenu("Generate")]
    public void Generate()
    {
        if (sharedCableMaterial == null)
        {
            Debug.LogError($"{name}: sharedCableMaterial이 비어있습니다.");
            return;
        }
        if (anchorA == null || anchorB == null)
        {
            Debug.LogError($"{name}: anchorA/anchorB가 비어있습니다.");
            return;
        }

        if (_renderer == null) _renderer = GetComponent<MeshRenderer>();

        // 인스턴스 머티리얼 생성 — sharedMaterial이 아니라 이 오브젝트 전용 복제본
        _instanceMat = new Material(sharedCableMaterial);
        _renderer.sharedMaterial = _instanceMat;

        ApplyProperties();

        // 다른 곳에서 걸어둔 MPB가 우선순위를 가져가는 걸 방지 (STEP 2-5 9절 세 번째 버그와 동일 패턴)
        _renderer.SetPropertyBlock(null);
    }

    private void ApplyProperties()
    {
        _instanceMat.SetVector("_AnchorA", anchorA.position);
        _instanceMat.SetVector("_AnchorB", anchorB.position);
        _instanceMat.SetFloat("_SagAmount", sagAmount);
        _instanceMat.SetFloat("_SagPeakT", sagPeakT);
        _instanceMat.SetFloat("_MeshLength", meshLength);

        _instanceMat.SetFloat("_WindSpeed", windSpeed);
        _instanceMat.SetFloat("_WindStrength", windStrength);
        _instanceMat.SetFloat("_NoiseScale", noiseScale);
        _instanceMat.SetFloat("_Turbulence", turbulence);

        if (baseTex != null) _instanceMat.SetTexture("_BaseTex", baseTex);
        _instanceMat.SetColor("_BaseColorTint", baseColorTint);
        if (normalTex != null) _instanceMat.SetTexture("_NormalTex", normalTex);
        if (ormTex != null) _instanceMat.SetTexture("_ORMTex", ormTex);
    }

    // 에디터에서 앵커를 옮기거나 값을 바꿀 때 씬 뷰에서 바로 확인하고 싶으면 사용
    [ContextMenu("Regenerate (앵커/값 변경 후 강제 갱신)")]
    public void Regenerate()
    {
        Generate();
    }

    private void OnDrawGizmosSelected()
    {
        if (anchorA == null || anchorB == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(anchorA.position, anchorB.position);
        Gizmos.DrawSphere(anchorA.position, 0.05f);
        Gizmos.DrawSphere(anchorB.position, 0.05f);
    }

    // [ExecuteAlways]로 Edit 모드에서도 Generate()가 반복 호출될 수 있어,
    // 오브젝트가 파괴될 때 만들어둔 인스턴스 머티리얼도 같이 정리 (메모리 누수 방지)
    private void OnDestroy()
    {
        if (_instanceMat == null) return;

        if (Application.isPlaying)
            Destroy(_instanceMat);
        else
            DestroyImmediate(_instanceMat);
    }
}
