using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;
using System.Reflection;

public class LightingPresetManager : MonoBehaviour
{
    [Header("Main Light")]
    public Light directionalLight;

    [Header("Global Volume")]
    public Volume globalVolume;

    [Header("SSAO Renderer Feature")]
    public ScriptableRendererFeature ssaoFeature;

    [Header("Presets")]
    public LightingPreset[] presets;

    [Header("Night Lights")]
    public Light[] nightLights;

    [Header("Auto Cycle")]
    public bool autoCycle = true;
    public float presetDuration = 10f;
    public float blendDuration = 2f;

    [Header("Current")]
    public int currentPresetIndex = 0;

    [Header("GUI")]
    public bool showDebugUI = true;
    public bool draggableGUI = true;
    public KeyCode toggleKey = KeyCode.F6;
    public Rect guiRect = new Rect(20, 20, 410, 260);

    private Bloom bloom;
    private ColorAdjustments colorAdjustments;
    private Vignette vignette;
    private ChromaticAberration chromaticAberration;
    private DepthOfField depthOfField;
    private MotionBlur motionBlur;

    private float cycleTimer = 0f;
    private bool isBlending = false;
    private Coroutine blendCoroutine;

    private GUIStyle titleStyle;
    private GUIStyle textStyle;
    private GUIStyle buttonStyle;
    private GUIStyle boxStyle;

    private void Start()
    {
        SetupPostProcess();
        ApplyPresetInstant(currentPresetIndex);

        if (autoCycle)
            StartCoroutine(AutoCycleRoutine());
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            showDebugUI = !showDebugUI;
        }
    }

    private void SetupPostProcess()
    {
        if (globalVolume == null || globalVolume.profile == null)
        {
            Debug.LogWarning("Global Volume 또는 Profile이 없습니다.");
            return;
        }

        globalVolume.profile.TryGet(out bloom);
        globalVolume.profile.TryGet(out colorAdjustments);
        globalVolume.profile.TryGet(out vignette);
        globalVolume.profile.TryGet(out chromaticAberration);
        globalVolume.profile.TryGet(out depthOfField);
        globalVolume.profile.TryGet(out motionBlur);
    }

    private IEnumerator AutoCycleRoutine()
    {
        while (true)
        {
            cycleTimer = 0f;

            while (cycleTimer < presetDuration)
            {
                cycleTimer += Time.deltaTime;
                yield return null;
            }

            int nextIndex = currentPresetIndex + 1;
            if (nextIndex >= presets.Length)
                nextIndex = 0;

            yield return StartCoroutine(BlendToPreset(nextIndex));
        }
    }

    public void ChangePresetByButton(int index)
    {
        if (index < 0 || index >= presets.Length) return;
        if (index == currentPresetIndex) return;

        if (blendCoroutine != null)
            StopCoroutine(blendCoroutine);

        cycleTimer = 0f;
        blendCoroutine = StartCoroutine(BlendToPreset(index));
    }

    private IEnumerator BlendToPreset(int nextIndex)
    {
        if (presets == null || presets.Length == 0) yield break;
        if (nextIndex < 0 || nextIndex >= presets.Length) yield break;
        if (directionalLight == null) yield break;

        isBlending = true;

        LightingPreset to = presets[nextIndex];

        if (to.skyboxMaterial != null)
            RenderSettings.skybox = to.skyboxMaterial;

        SetNightLights(to.nightLightsOn);

        float timer = 0f;

        Color startSunColor = directionalLight.color;
        float startSunIntensity = directionalLight.intensity;
        Quaternion startSunRotation = directionalLight.transform.rotation;

        Color startAmbientColor = RenderSettings.ambientLight;
        Color startFogColor = RenderSettings.fogColor;
        float startFogDensity = RenderSettings.fogDensity;

        float startBloomIntensity = bloom != null ? bloom.intensity.value : 0f;
        float startBloomThreshold = bloom != null ? bloom.threshold.value : 1f;

        float startExposure = colorAdjustments != null ? colorAdjustments.postExposure.value : 0f;
        float startContrast = colorAdjustments != null ? colorAdjustments.contrast.value : 0f;
        float startSaturation = colorAdjustments != null ? colorAdjustments.saturation.value : 0f;
        Color startColorFilter = colorAdjustments != null ? colorAdjustments.colorFilter.value : Color.white;

        float startVignetteIntensity = vignette != null ? vignette.intensity.value : 0f;
        float startVignetteSmoothness = vignette != null ? vignette.smoothness.value : 0.3f;

        float startChromatic = chromaticAberration != null ? chromaticAberration.intensity.value : 0f;

        float startMotionBlur = motionBlur != null ? motionBlur.intensity.value : 0f;

        float startDofStart = depthOfField != null ? depthOfField.gaussianStart.value : 10f;
        float startDofEnd = depthOfField != null ? depthOfField.gaussianEnd.value : 40f;
        float startDofMaxRadius = depthOfField != null ? depthOfField.gaussianMaxRadius.value : 1f;

        float startSSAOIntensity = GetSSAOFloat("Intensity", "intensity", "m_Intensity");
        float startSSAORadius = GetSSAOFloat("Radius", "radius", "m_Radius");
        float startSSAODirect = GetSSAOFloat("DirectLightingStrength", "directLightingStrength", "m_DirectLightingStrength");

        Quaternion targetSunRotation = Quaternion.Euler(to.sunRotation);

        while (timer < blendDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / blendDuration);

            directionalLight.color = Color.Lerp(startSunColor, to.sunColor, t);
            directionalLight.intensity = Mathf.Lerp(startSunIntensity, to.sunIntensity, t);
            directionalLight.transform.rotation = Quaternion.Slerp(startSunRotation, targetSunRotation, t);

            RenderSettings.ambientLight = Color.Lerp(startAmbientColor, to.ambientColor, t);

            RenderSettings.fog = to.useFog;
            RenderSettings.fogColor = Color.Lerp(startFogColor, to.fogColor, t);
            RenderSettings.fogDensity = Mathf.Lerp(startFogDensity, to.fogDensity, t);

            ApplyPostProcessBlend(
                to,
                t,
                startBloomIntensity,
                startBloomThreshold,
                startExposure,
                startContrast,
                startSaturation,
                startColorFilter,
                startVignetteIntensity,
                startVignetteSmoothness,
                startChromatic,
                startMotionBlur,
                startDofStart,
                startDofEnd,
                startDofMaxRadius
            );

            ApplySSAOBlend(
                to,
                t,
                startSSAOIntensity,
                startSSAORadius,
                startSSAODirect
            );

            yield return null;
        }

        ApplyPresetInstant(nextIndex);

        isBlending = false;
        blendCoroutine = null;
    }

    public void ApplyPresetInstant(int index)
    {
        if (presets == null || presets.Length == 0) return;
        if (index < 0 || index >= presets.Length) return;
        if (directionalLight == null) return;

        LightingPreset preset = presets[index];

        currentPresetIndex = index;

        directionalLight.color = preset.sunColor;
        directionalLight.intensity = preset.sunIntensity;
        directionalLight.transform.rotation = Quaternion.Euler(preset.sunRotation);

        if (preset.skyboxMaterial != null)
            RenderSettings.skybox = preset.skyboxMaterial;

        RenderSettings.ambientLight = preset.ambientColor;

        RenderSettings.fog = preset.useFog;
        RenderSettings.fogColor = preset.fogColor;
        RenderSettings.fogDensity = preset.fogDensity;

        SetNightLights(preset.nightLightsOn);
        ApplyPostProcessInstant(preset);
        ApplySSAOInstant(preset);

        DynamicGI.UpdateEnvironment();
    }

    private void ApplyPostProcessInstant(LightingPreset preset)
    {
        if (bloom != null)
        {
            bloom.intensity.value = preset.bloomIntensity;
            bloom.threshold.value = preset.bloomThreshold;
        }

        if (colorAdjustments != null)
        {
            colorAdjustments.postExposure.value = preset.postExposure;
            colorAdjustments.contrast.value = preset.contrast;
            colorAdjustments.saturation.value = preset.saturation;
            colorAdjustments.colorFilter.value = preset.colorFilter;
        }

        if (vignette != null)
        {
            vignette.intensity.value = preset.vignetteIntensity;
            vignette.smoothness.value = preset.vignetteSmoothness;
        }

        if (chromaticAberration != null)
        {
            chromaticAberration.intensity.value = preset.chromaticAberrationIntensity;
        }

        if (motionBlur != null)
        {
            motionBlur.intensity.value = preset.motionBlurIntensity;
        }

        if (depthOfField != null)
        {
            depthOfField.mode.value = preset.useDepthOfField
                ? DepthOfFieldMode.Gaussian
                : DepthOfFieldMode.Off;

            depthOfField.gaussianStart.value = preset.dofStart;
            depthOfField.gaussianEnd.value = preset.dofEnd;
            depthOfField.gaussianMaxRadius.value = preset.dofMaxRadius;
        }
    }

    private void ApplyPostProcessBlend(
        LightingPreset to,
        float t,
        float startBloomIntensity,
        float startBloomThreshold,
        float startExposure,
        float startContrast,
        float startSaturation,
        Color startColorFilter,
        float startVignetteIntensity,
        float startVignetteSmoothness,
        float startChromatic,
        float startMotionBlur,
        float startDofStart,
        float startDofEnd,
        float startDofMaxRadius)
    {
        if (bloom != null)
        {
            bloom.intensity.value = Mathf.Lerp(startBloomIntensity, to.bloomIntensity, t);
            bloom.threshold.value = Mathf.Lerp(startBloomThreshold, to.bloomThreshold, t);
        }

        if (colorAdjustments != null)
        {
            colorAdjustments.postExposure.value = Mathf.Lerp(startExposure, to.postExposure, t);
            colorAdjustments.contrast.value = Mathf.Lerp(startContrast, to.contrast, t);
            colorAdjustments.saturation.value = Mathf.Lerp(startSaturation, to.saturation, t);
            colorAdjustments.colorFilter.value = Color.Lerp(startColorFilter, to.colorFilter, t);
        }

        if (vignette != null)
        {
            vignette.intensity.value = Mathf.Lerp(startVignetteIntensity, to.vignetteIntensity, t);
            vignette.smoothness.value = Mathf.Lerp(startVignetteSmoothness, to.vignetteSmoothness, t);
        }

        if (chromaticAberration != null)
        {
            chromaticAberration.intensity.value = Mathf.Lerp(startChromatic, to.chromaticAberrationIntensity, t);
        }

        if (motionBlur != null)
        {
            motionBlur.intensity.value = Mathf.Lerp(startMotionBlur, to.motionBlurIntensity, t);
        }

        if (depthOfField != null)
        {
            depthOfField.mode.value = to.useDepthOfField
                ? DepthOfFieldMode.Gaussian
                : DepthOfFieldMode.Off;

            depthOfField.gaussianStart.value = Mathf.Lerp(startDofStart, to.dofStart, t);
            depthOfField.gaussianEnd.value = Mathf.Lerp(startDofEnd, to.dofEnd, t);
            depthOfField.gaussianMaxRadius.value = Mathf.Lerp(startDofMaxRadius, to.dofMaxRadius, t);
        }
    }

    private void ApplySSAOInstant(LightingPreset preset)
    {
        if (ssaoFeature == null) return;

        ssaoFeature.SetActive(preset.ssaoEnabled);

        SetSSAOFloat(preset.ssaoIntensity, "Intensity", "intensity", "m_Intensity");
        SetSSAOFloat(preset.ssaoRadius, "Radius", "radius", "m_Radius");
        SetSSAOFloat(preset.ssaoDirectLightingStrength, "DirectLightingStrength", "directLightingStrength", "m_DirectLightingStrength");
    }

    private void ApplySSAOBlend(
        LightingPreset to,
        float t,
        float startIntensity,
        float startRadius,
        float startDirect)
    {
        if (ssaoFeature == null) return;

        ssaoFeature.SetActive(to.ssaoEnabled);

        SetSSAOFloat(Mathf.Lerp(startIntensity, to.ssaoIntensity, t), "Intensity", "intensity", "m_Intensity");
        SetSSAOFloat(Mathf.Lerp(startRadius, to.ssaoRadius, t), "Radius", "radius", "m_Radius");
        SetSSAOFloat(Mathf.Lerp(startDirect, to.ssaoDirectLightingStrength, t), "DirectLightingStrength", "directLightingStrength", "m_DirectLightingStrength");
    }

    private object GetSSAOSettingsObject()
    {
        if (ssaoFeature == null) return null;

        FieldInfo field = ssaoFeature.GetType().GetField("m_Settings", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        if (field == null)
            field = ssaoFeature.GetType().GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        if (field == null)
            return null;

        return field.GetValue(ssaoFeature);
    }

    private float GetSSAOFloat(params string[] possibleNames)
    {
        object settings = GetSSAOSettingsObject();
        if (settings == null) return 0f;

        System.Type type = settings.GetType();

        foreach (string name in possibleNames)
        {
            FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field != null && field.FieldType == typeof(float))
                return (float)field.GetValue(settings);

            PropertyInfo prop = type.GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (prop != null && prop.PropertyType == typeof(float))
                return (float)prop.GetValue(settings);
        }

        return 0f;
    }

    private void SetSSAOFloat(float value, params string[] possibleNames)
    {
        object settings = GetSSAOSettingsObject();
        if (settings == null) return;

        System.Type type = settings.GetType();

        foreach (string name in possibleNames)
        {
            FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field != null && field.FieldType == typeof(float))
            {
                field.SetValue(settings, value);
                return;
            }

            PropertyInfo prop = type.GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (prop != null && prop.PropertyType == typeof(float) && prop.CanWrite)
            {
                prop.SetValue(settings, value);
                return;
            }
        }
    }

    private void SetNightLights(bool isOn)
    {
        foreach (Light light in nightLights)
        {
            if (light != null)
                light.enabled = isOn;
        }
    }

    private void OnGUI()
    {
        if (!showDebugUI) return;
        if (presets == null || presets.Length == 0) return;

        InitGUIStyles();

        guiRect = GUI.Window(
            GetInstanceID(),
            guiRect,
            DrawLightingGUIWindow,
            "CH18 Lighting Manager"
        );
    }

    private void DrawLightingGUIWindow(int windowID)
    {
        LightingPreset current = presets[currentPresetIndex];

        GUILayout.Label("Lighting + Post Process Preset", titleStyle);
        GUILayout.Space(8);

        GUILayout.Label("Current Preset : " + current.presetName, textStyle);
        GUILayout.Label("Remain Time : " + GetRemainTime().ToString("F1") + "s", textStyle);
        GUILayout.Label("Blend State : " + (isBlending ? "Blending" : "Waiting"), textStyle);
        GUILayout.Label("Auto Cycle : " + (autoCycle ? "ON" : "OFF"), textStyle);

        GUILayout.Space(8);

        Rect rect = GUILayoutUtility.GetRect(guiRect.width - 40, 18);
        GUI.Box(rect, "");
        GUI.Box(new Rect(rect.x, rect.y, rect.width * GetProgress01(), rect.height), "");

        GUILayout.Space(12);

        GUILayout.BeginHorizontal();

        for (int i = 0; i < presets.Length; i++)
        {
            if (GUILayout.Button(presets[i].presetName, buttonStyle, GUILayout.Height(32)))
                ChangePresetByButton(i);
        }

        GUILayout.EndHorizontal();

        GUILayout.Space(8);

        autoCycle = GUILayout.Toggle(autoCycle, "Auto Cycle");
        GUILayout.Label(toggleKey + " : Show / Hide", textStyle);

        if (draggableGUI)
        {
            GUI.DragWindow(new Rect(0, 0, guiRect.width, 25));
        }
    }

    private void InitGUIStyles()
    {
        if (titleStyle != null) return;

        titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 18;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = Color.white;

        textStyle = new GUIStyle(GUI.skin.label);
        textStyle.fontSize = 14;
        textStyle.normal.textColor = Color.white;

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 13;

        boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.padding = new RectOffset(15, 15, 15, 15);
    }

    public float GetProgress01()
    {
        if (presetDuration <= 0f) return 0f;
        return Mathf.Clamp01(cycleTimer / presetDuration);
    }

    public float GetRemainTime()
    {
        return Mathf.Max(0f, presetDuration - cycleTimer);
    }
}