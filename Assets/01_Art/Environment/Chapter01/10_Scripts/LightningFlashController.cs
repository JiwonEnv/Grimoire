using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class LightningFlashController : MonoBehaviour
{
    [Header("번개 전용 요소")]
    [SerializeField] private Volume flashVolume;
    [SerializeField] private Light flashLight;

    [Header("번개 밝기")]
    [Min(0f)]
    [SerializeField] private float flashLightIntensity = 1.2f;

    [Header("한 번의 번개 연출")]
    [Tooltip("첫 번째 번쩍임이 유지되는 시간")]
    [Min(0.01f)]
    [SerializeField] private float firstFlashDuration = 0.08f;

    [Tooltip("첫 번째와 두 번째 번쩍임 사이의 어두운 시간")]
    [Min(0f)]
    [SerializeField] private float flashGap = 0.06f;

    [Tooltip("두 번째 번쩍임이 유지되는 시간")]
    [Min(0.01f)]
    [SerializeField] private float secondFlashDuration = 0.16f;

    [Range(0f, 1f)]
    [Tooltip("두 번째 번쩍임의 밝기 비율")]
    [SerializeField] private float secondFlashStrength = 0.75f;

    [Header("번개 발생 랜덤 간격")]
    [Tooltip("다음 번개가 발생하기까지의 최소 대기 시간")]
    [Min(0f)]
    [SerializeField] private float randomIntervalMin = 3f;

    [Tooltip("다음 번개가 발생하기까지의 최대 대기 시간")]
    [Min(0f)]
    [SerializeField] private float randomIntervalMax = 8f;

    [Header("시작 설정")]
    [Tooltip("Play 직후 기다리지 않고 바로 한 번 번쩍입니다.")]
    [SerializeField] private bool flashImmediatelyOnStart = true;

    private Coroutine lightningRoutine;

    private void OnEnable()
    {
        SetFlash(0f);
        lightningRoutine = StartCoroutine(AutoLightningSequence());
    }

    private IEnumerator AutoLightningSequence()
    {
        if (flashImmediatelyOnStart)
            yield return FlashSequence();

        while (true)
        {
            float minInterval = Mathf.Min(randomIntervalMin, randomIntervalMax);
            float maxInterval = Mathf.Max(randomIntervalMin, randomIntervalMax);

            float randomWaitTime = Random.Range(minInterval, maxInterval);
            yield return new WaitForSeconds(randomWaitTime);

            yield return FlashSequence();
        }
    }

    private IEnumerator FlashSequence()
    {
        // 첫 번째 강한 섬광
        yield return Flash(1f, firstFlashDuration);

        // 두 섬광 사이의 암전
        SetFlash(0f);
        yield return new WaitForSeconds(flashGap);

        // 두 번째 약한 섬광
        yield return Flash(secondFlashStrength, secondFlashDuration);

        SetFlash(0f);
    }

    private IEnumerator Flash(float strength, float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            float normalizedTime =
                Mathf.Clamp01(elapsedTime / duration);

            // 즉시 밝아진 후 서서히 어두워짐
            float fade = 1f - normalizedTime;

            SetFlash(fade * strength);
            yield return null;
        }

        SetFlash(0f);
    }

    private void SetFlash(float strength)
    {
        strength = Mathf.Clamp01(strength);

        if (flashVolume != null)
            flashVolume.weight = strength;

        if (flashLight != null)
            flashLight.intensity =
                flashLightIntensity * strength;
    }

    private void OnDisable()
    {
        if (lightningRoutine != null)
        {
            StopCoroutine(lightningRoutine);
            lightningRoutine = null;
        }

        SetFlash(0f);
    }

    private void OnValidate()
    {
        randomIntervalMin = Mathf.Max(0f, randomIntervalMin);
        randomIntervalMax =
            Mathf.Max(randomIntervalMin, randomIntervalMax);
    }
}