using UnityEngine;

[RequireComponent(typeof(DinoController))]
[RequireComponent(typeof(AudioSource))]
public class DinoFeedback : MonoBehaviour
{
    // TODO: Drop short WAV/MP3 placeholder sounds into Assets/Audio/ and assign
    // them in the Inspector. Until then, jump/landing are silent (no errors).
    [Header("Audio")]
    [Tooltip("Played when the dino jumps. Leave empty for silence.")]
    public AudioClip jumpClip;
    [Tooltip("Played when the dino lands. Leave empty for silence.")]
    public AudioClip landClip;
    [Range(0f, 1f)]
    [Tooltip("Volume scale for one-shot SFX.")]
    public float volume = 0.6f;

    [Header("Landing Squash")]
    [Tooltip("Duration of the squash pulse on landing.")]
    public float squashDuration = 0.15f;
    [Tooltip("Peak scale at impact. x = sideways stretch, y = flatten. Keep subtle.")]
    public Vector2 squashScale = new Vector2(1.12f, 0.88f);

    [Header("Jump Stretch")]
    [Tooltip("Quick lift-off stretch when the dino jumps.")]
    public float stretchDuration = 0.12f;
    [Tooltip("Peak scale on take-off. x = slimmer, y = taller. Keep subtle.")]
    public Vector2 stretchScale = new Vector2(0.90f, 1.12f);

    [Header("Joy Bounce")]
    [Tooltip("Round pop fired by LetterCollectionManager when the dino picks up a letter.")]
    public float joyDuration = 0.20f;
    [Tooltip("Uniform-ish puff up. Both axes positive for a happy ‘yay!’ feel.")]
    public Vector2 joyScale = new Vector2(1.18f, 1.18f);

    DinoController controller;
    AudioSource audioSource;
    Vector3 baseScale;

    // Generalised single-pulse animator. New trigger overrides previous.
    Vector2 activePeak;
    float   activeDuration;
    float   activeTimer;

    void Awake()
    {
        controller = GetComponent<DinoController>();
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // pure 2D
        baseScale = transform.localScale;
    }

    void OnEnable()
    {
        controller.OnJump += HandleJump;
        controller.OnLand += HandleLand;
    }

    void OnDisable()
    {
        controller.OnJump -= HandleJump;
        controller.OnLand -= HandleLand;
    }

    void HandleJump()
    {
        if (jumpClip != null) audioSource.PlayOneShot(jumpClip, volume);
        Trigger(stretchScale, stretchDuration);
    }

    void HandleLand()
    {
        if (landClip != null) audioSource.PlayOneShot(landClip, volume);
        Trigger(squashScale, squashDuration);
    }

    /// <summary>Called by LetterCollectionManager when a letter is picked up.</summary>
    public void TriggerJoyBounce()
    {
        Trigger(joyScale, joyDuration);
    }

    void Trigger(Vector2 peak, float duration)
    {
        activePeak = peak;
        activeDuration = duration;
        activeTimer = duration;
    }

    void Update()
    {
        if (activeTimer > 0f)
        {
            activeTimer -= Time.deltaTime;
            float t = Mathf.Clamp01(activeTimer / activeDuration);
            // Sin from 0 -> 1 -> 0 so we ease into the peak and back.
            float pulse = Mathf.Sin(t * Mathf.PI);
            float sx = Mathf.Lerp(1f, activePeak.x, pulse);
            float sy = Mathf.Lerp(1f, activePeak.y, pulse);
            transform.localScale = new Vector3(baseScale.x * sx, baseScale.y * sy, baseScale.z);
        }
        else
        {
            transform.localScale = baseScale;
        }
    }
}
