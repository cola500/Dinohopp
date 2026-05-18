using UnityEngine;

/// <summary>
/// Subtle blink for the dino's eye. Squashes Eye_White + Eye_Pupil on the Y axis
/// briefly, with a random delay between blinks. Purely cosmetic — no physics.
/// </summary>
public class DinoBlink : MonoBehaviour
{
    [Tooltip("Eye_White transform. Auto-resolved to 'Visual/Eye_White' if left null.")]
    public Transform eyeWhite;
    [Tooltip("Eye_Pupil transform. Auto-resolved to 'Visual/Eye_Pupil' if left null.")]
    public Transform eyePupil;

    [Header("Timing")]
    [Tooltip("Minimum seconds between blinks.")]
    public float minInterval = 3f;
    [Tooltip("Maximum seconds between blinks.")]
    public float maxInterval = 6f;
    [Tooltip("Total duration of one blink (close + open).")]
    public float blinkDuration = 0.10f;

    [Header("Look")]
    [Range(0f, 1f)]
    [Tooltip("Eye Y scale at the closed peak. 0 = fully shut, 1 = no blink.")]
    public float closedScaleY = 0.10f;
    [Tooltip("How far the pupil drifts forward (+X) when the game is playing.")]
    public float lookForwardX = 0.04f;
    [Tooltip("Smoothing for the pupil look. Smaller = snappier.")]
    public float lookSmoothing = 0.18f;

    float nextBlinkTime;
    float blinkTimer;
    Vector3 baseScaleWhite;
    Vector3 baseScalePupil;
    Vector3 basePosPupil;
    Vector3 pupilVel;
    DinoController controllerForLook;

    void Awake()
    {
        if (eyeWhite == null) eyeWhite = transform.Find("Visual/Eye_White");
        if (eyePupil == null) eyePupil = transform.Find("Visual/Eye_Pupil");
        if (eyeWhite != null) baseScaleWhite = eyeWhite.localScale;
        if (eyePupil != null)
        {
            baseScalePupil = eyePupil.localScale;
            basePosPupil   = eyePupil.localPosition;
        }
        controllerForLook = GetComponent<DinoController>();
        ScheduleNext();
    }

    void Update()
    {
        if (blinkTimer > 0f)
        {
            blinkTimer -= Time.deltaTime;
            if (blinkTimer <= 0f)
            {
                RestoreOpen();
                ScheduleNext();
            }
            else
            {
                float t = Mathf.Clamp01(blinkTimer / blinkDuration);
                // sin(t * π) gives 0 -> 1 -> 0 over t = 1 -> 0.5 -> 0.
                float pulse = Mathf.Sin(t * Mathf.PI);
                float yScale = Mathf.Lerp(1f, closedScaleY, pulse);
                ApplyEyeYScale(yScale);
            }
        }
        else if (Time.time >= nextBlinkTime)
        {
            blinkTimer = blinkDuration;
        }

        // Pupil look — drifts forward (+X) when playing, centers when paused.
        if (eyePupil != null)
        {
            bool playing = controllerForLook != null && controllerForLook.controlsEnabled;
            Vector3 target = basePosPupil + (playing ? new Vector3(lookForwardX, 0f, 0f) : Vector3.zero);
            eyePupil.localPosition = Vector3.SmoothDamp(
                eyePupil.localPosition, target, ref pupilVel, lookSmoothing);
        }
    }

    void ApplyEyeYScale(float yScale)
    {
        if (eyeWhite != null)
            eyeWhite.localScale = new Vector3(baseScaleWhite.x, baseScaleWhite.y * yScale, baseScaleWhite.z);
        if (eyePupil != null)
            eyePupil.localScale = new Vector3(baseScalePupil.x, baseScalePupil.y * yScale, baseScalePupil.z);
    }

    void RestoreOpen()
    {
        if (eyeWhite != null) eyeWhite.localScale = baseScaleWhite;
        if (eyePupil != null) eyePupil.localScale = baseScalePupil;
    }

    void ScheduleNext()
    {
        nextBlinkTime = Time.time + Random.Range(minInterval, maxInterval);
    }
}
