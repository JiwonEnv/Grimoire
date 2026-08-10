using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public class LightBeamController : MonoBehaviour
{
    [Header("Style")]
    [SerializeField]
    private LightBeamStyleSO style;

    [Header("Renderer References")]
    [SerializeField]
    private Renderer beamRenderer;

    [SerializeField]
    private Renderer emissionCoreRenderer;

    [Header("Optional Light")]
    [SerializeField]
    private Light spotLight;

    private MaterialPropertyBlock beamBlock;
    private MaterialPropertyBlock coreBlock;

    private static readonly int BaseMapID =
        Shader.PropertyToID("_BaseMap");

    private static readonly int BaseColorID =
        Shader.PropertyToID("_BaseColor");

    private static readonly int IntensityID =
        Shader.PropertyToID("_Intensity");

    private static readonly int OpacityID =
        Shader.PropertyToID("_Opacity");

    private void OnEnable()
    {
        ApplyStyle();
    }

    private void OnValidate()
    {
        ApplyStyle();
    }

    [ContextMenu("Apply Style")]
    public void ApplyStyle()
    {
        if (style == null)
            return;

        ApplyBeam();
        ApplyEmissionCore();
        ApplyRealLight();
    }

    private void ApplyBeam()
    {
        if (beamRenderer == null)
            return;

        beamBlock ??= new MaterialPropertyBlock();

        beamRenderer.GetPropertyBlock(beamBlock);

        beamBlock.SetTexture(BaseMapID, style.beamTexture);
        beamBlock.SetColor(BaseColorID, style.beamColor);
        beamBlock.SetFloat(IntensityID, style.intensity);
        beamBlock.SetFloat(OpacityID, style.opacity);

        beamRenderer.SetPropertyBlock(beamBlock);
    }

    private void ApplyEmissionCore()
    {
        if (emissionCoreRenderer == null)
            return;

        coreBlock ??= new MaterialPropertyBlock();

        emissionCoreRenderer.GetPropertyBlock(coreBlock);

        coreBlock.SetTexture(BaseMapID, style.beamTexture);
        coreBlock.SetColor(BaseColorID, style.beamColor);

        coreBlock.SetFloat(
            IntensityID,
            style.intensity * style.coreIntensityMultiplier
        );

        coreBlock.SetFloat(
            OpacityID,
            style.coreOpacity
        );

        emissionCoreRenderer.SetPropertyBlock(coreBlock);
    }

    private void ApplyRealLight()
    {
        if (spotLight == null)
            return;

        spotLight.gameObject.SetActive(style.useRealLight);

        if (!style.useRealLight)
            return;

        spotLight.color = style.lightColor;
        spotLight.intensity = style.lightIntensity;
        spotLight.range = style.lightRange;
        spotLight.spotAngle = style.spotAngle;

        spotLight.shadows = style.useShadows
            ? LightShadows.Soft
            : LightShadows.None;
    }
}