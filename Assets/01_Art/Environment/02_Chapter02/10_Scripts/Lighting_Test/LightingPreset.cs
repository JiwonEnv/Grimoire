using UnityEngine;

[System.Serializable]
public class LightingPreset
{
    [Header("Preset")]
    public string presetName;

    [Header("Skybox")]
    public Material skyboxMaterial;

    [Header("Sun Light")]
    public Color sunColor = Color.white;
    public float sunIntensity = 1f;
    public Vector3 sunRotation;

    [Header("Environment")]
    public Color ambientColor = Color.gray;

    [Header("Fog")]
    public bool useFog = true;
    public Color fogColor = Color.gray;
    public float fogDensity = 0.01f;

    [Header("Night Lights")]
    public bool nightLightsOn;

    [Header("SSAO")]
    public bool ssaoEnabled = true;
    public float ssaoIntensity = 1.5f;
    public float ssaoRadius = 0.25f;
    public float ssaoDirectLightingStrength = 0.5f;

    [Header("Bloom")]
    public float bloomIntensity = 0.5f;
    public float bloomThreshold = 1f;

    [Header("Color Adjustments")]
    public float postExposure = 0f;
    public float contrast = 0f;
    public float saturation = 0f;
    public Color colorFilter = Color.white;

    [Header("Vignette")]
    public float vignetteIntensity = 0f;
    public float vignetteSmoothness = 0.3f;

    [Header("Chromatic Aberration")]
    public float chromaticAberrationIntensity = 0f;

    [Header("Depth Of Field")]
    public bool useDepthOfField = false;
    public float dofStart = 10f;
    public float dofEnd = 40f;
    public float dofMaxRadius = 1f;

    [Header("Motion Blur")]
    public float motionBlurIntensity = 0f;
}