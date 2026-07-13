using UnityEngine;

public class EnvironmentTimeController : MonoBehaviour
{
    [Header("Time")]
    [Range(0, 24)]
    public float currentTime = 12f;
    public float timeSpeed = 30f;

    [Header("Sun")]
    public Light sunLight;
    public Vector3 sunRotationOffset;
    public Gradient sunColor;
    public AnimationCurve sunIntensity;

    [Header("Ambient")]
    public Gradient ambientColor;

    [Header("Fog")]
    public bool useFog = true;
    public Gradient fogColor;
    public AnimationCurve fogDensity;

    [Header("Night Lights")]
    public Light[] nightLights;
    public float nightStartTime = 18f;
    public float nightEndTime = 6f;

    private void Update()
    {
        UpdateTime();
        UpdateSun();
        UpdateAmbient();
        UpdateFog();
        UpdateNightLights();
    }

    private void UpdateTime()
    {
        currentTime += Time.deltaTime * (24f / timeSpeed);

        if (currentTime >= 24f)
            currentTime = 0f;
    }

    private void UpdateSun()
    {
        if (sunLight == null) return;

        float timePercent = currentTime / 24f;

        float sunX = Mathf.Lerp(-90f, 270f, timePercent);
        float sunY = Mathf.Lerp(-45f, 45f, Mathf.Sin(timePercent * Mathf.PI));

        sunLight.transform.rotation = Quaternion.Euler(
            sunRotationOffset.x + sunX,
            sunRotationOffset.y + sunY,
            sunRotationOffset.z
        );

        sunLight.color = sunColor.Evaluate(timePercent);
        sunLight.intensity = sunIntensity.Evaluate(timePercent);
    }

    private void UpdateAmbient()
    {
        float timePercent = currentTime / 24f;
        RenderSettings.ambientLight = ambientColor.Evaluate(timePercent);
    }

    private void UpdateFog()
    {
        if (!useFog) return;

        float timePercent = currentTime / 24f;

        RenderSettings.fog = true;
        RenderSettings.fogColor = fogColor.Evaluate(timePercent);
        RenderSettings.fogDensity = fogDensity.Evaluate(timePercent);
    }

    private void UpdateNightLights()
    {
        bool isNight = currentTime >= nightStartTime || currentTime <= nightEndTime;

        foreach (Light light in nightLights)
        {
            if (light != null)
                light.enabled = isNight;
        }
    }
}