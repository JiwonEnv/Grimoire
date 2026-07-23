using UnityEngine;

/// <summary>
/// 창문/쇼윈도 유리 자식 오브젝트에 붙이는 마커.
/// STEP 2-4(창문 셰이더, 랜덤 점등)에서 건물 전체의 유리를 한 번에 모으는 용도.
///   var glasses = buildingRoot.GetComponentsInChildren&lt;GlassMarker&gt;();
/// Frame과 분리된 별도 Renderer를 가지므로, 점등 제어가 Frame에 영향을 주지 않는다.
/// </summary>
public class GlassMarker : MonoBehaviour
{
    [Tooltip("완전 발광(Window) vs 이미지 위 오버레이(ShowWindow) — STEP 2-4 점등 로직 분기용")]
    public GlassMode mode = GlassMode.FullEmission;
}

public enum GlassMode
{
    FullEmission,  // Window — 라이트 on일 때 완전히 발광
    Overlay        // ShowWindow — 라이트 on일 때 내부 이미지 위에 약하게 오버레이
}
