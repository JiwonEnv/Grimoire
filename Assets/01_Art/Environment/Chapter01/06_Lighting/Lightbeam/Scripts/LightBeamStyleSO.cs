using UnityEngine;

[CreateAssetMenu(
    fileName = "SO_LightBeamStyle",
    menuName = "Environment/Lighting/Light Beam Style"
)]
public class LightBeamStyleSO : ScriptableObject
{
    [Header("Beam Visual")]
    public Texture2D beamTexture;

    [ColorUsage(true, true)]
    public Color beamColor = Color.white;

    [Min(0f)]
    public float intensity = 2f;

    [Range(0f, 1f)]
    public float opacity = 0.1f;

    [Header("Emission Core")]
    [Min(0f)]
    public float coreIntensityMultiplier = 2f;

    [Range(0f, 1f)]
    public float coreOpacity = 0.5f;

    [Header("Optional Real Light")]
    public bool useRealLight;

    [ColorUsage(true, true)]
    public Color lightColor = Color.white;

    [Min(0f)]
    public float lightIntensity = 1000f;

    [Min(0f)]
    public float lightRange = 10f;

    [Range(1f, 179f)]
    public float spotAngle = 45f;

    public bool useShadows;
}