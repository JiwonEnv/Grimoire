using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

[System.Serializable]
public class VFXEffectData
{
    [Header("Effect Info")]
    public string effectName = "Rain";

    [Header("VFX Object")]
    public VisualEffect visualEffect;

    [Header("VFX Graph Property Names")]
    public string spawnRateProperty = "SpawnRate";
    public string effectPowerProperty = "EffectPower";

    [Header("Control Values")]
    public float baseSpawnRate = 1200f;

    [Range(0f, 3f)]
    public float effectPower = 1f;

    [Header("Runtime State")]
    public bool isOn = true;
}

public class VFXEffectManager : MonoBehaviour
{
    [Header("VFX List")]
    public List<VFXEffectData> effects = new List<VFXEffectData>();

    [Header("GUI")]
    public bool showGUI = true;
    public bool draggableGUI = true;
    public KeyCode toggleKey = KeyCode.F7;

    public Rect guiRect = new Rect(360, 20, 330, 380);

    private Vector2 scrollPos;

    private void Start()
    {
        ApplyAllEffects();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            showGUI = !showGUI;
        }
    }

    private void ApplyAllEffects()
    {
        foreach (var effect in effects)
        {
            ApplyEffect(effect);
        }
    }

    private void ApplyEffect(VFXEffectData effect)
    {
        if (effect == null || effect.visualEffect == null)
            return;

        float finalSpawnRate = effect.isOn
            ? effect.baseSpawnRate * effect.effectPower
            : 0f;

        if (!effect.isOn || effect.effectPower <= 0f)
        {
            SetFloatSafe(effect.visualEffect, effect.spawnRateProperty, 0f);
            SetFloatSafe(effect.visualEffect, effect.effectPowerProperty, 0f);
            effect.visualEffect.Stop();
            return;
        }

        effect.visualEffect.Play();

        SetFloatSafe(effect.visualEffect, effect.spawnRateProperty, finalSpawnRate);
        SetFloatSafe(effect.visualEffect, effect.effectPowerProperty, effect.effectPower);
    }

    private void SetFloatSafe(VisualEffect vfx, string propertyName, float value)
    {
        if (vfx == null)
            return;

        if (string.IsNullOrEmpty(propertyName))
            return;

        if (vfx.HasFloat(propertyName))
        {
            vfx.SetFloat(propertyName, value);
        }
    }

    private void OnGUI()
    {
        if (!showGUI)
            return;

        guiRect = GUI.Window(
            GetInstanceID(),
            guiRect,
            DrawGUIWindow,
            "CH20 VFX Manager"
        );
    }

    private void DrawGUIWindow(int windowID)
    {
        GUILayout.Space(5);

        GUILayout.Label("VFX Control Panel");

        GUILayout.Space(5);

        scrollPos = GUILayout.BeginScrollView(
            scrollPos,
            GUILayout.Width(guiRect.width - 15),
            GUILayout.Height(guiRect.height - 65)
        );

        for (int i = 0; i < effects.Count; i++)
        {
            DrawEffectGUI(effects[i]);
            GUILayout.Space(10);
        }

        GUILayout.EndScrollView();

        GUILayout.Label(toggleKey + " : Show / Hide");

        if (draggableGUI)
        {
            GUI.DragWindow(new Rect(0, 0, guiRect.width, 25));
        }
    }

    private void DrawEffectGUI(VFXEffectData effect)
    {
        if (effect == null)
            return;

        GUILayout.BeginVertical("box");

        GUILayout.Label(effect.effectName);

        if (effect.visualEffect == null)
        {
            GUILayout.Label("VisualEffect is missing.");
            GUILayout.EndVertical();
            return;
        }

        GUILayout.BeginHorizontal();

        string buttonText = effect.isOn ? "ON" : "OFF";

        if (GUILayout.Button(buttonText, GUILayout.Width(70)))
        {
            effect.isOn = !effect.isOn;
            ApplyEffect(effect);
        }

        GUILayout.Label("Final Spawn : " + GetFinalSpawnRate(effect).ToString("F0"));

        GUILayout.EndHorizontal();

        GUILayout.Space(5);

        GUILayout.Label("Base Spawn Rate : " + effect.baseSpawnRate.ToString("F0"));

        effect.baseSpawnRate = GUILayout.HorizontalSlider(
            effect.baseSpawnRate,
            0f,
            3000f
        );

        GUILayout.Space(5);

        GUILayout.Label("Effect Power : " + effect.effectPower.ToString("F1"));

        effect.effectPower = GUILayout.HorizontalSlider(
            effect.effectPower,
            0f,
            3f
        );

        GUILayout.Space(5);

        if (GUILayout.Button("Apply"))
        {
            ApplyEffect(effect);
        }

        ApplyEffect(effect);

        GUILayout.EndVertical();
    }

    private float GetFinalSpawnRate(VFXEffectData effect)
    {
        if (effect == null)
            return 0f;

        if (!effect.isOn)
            return 0f;

        return effect.baseSpawnRate * effect.effectPower;
    }
}