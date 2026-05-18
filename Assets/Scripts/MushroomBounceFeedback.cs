using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MushroomBounceFeedback : MonoBehaviour
{
    [Tooltip("Tag the script looks for to detect the dino.")]
    public string dinoTag = "Player";

    [Tooltip("Cheerful message shown on landing.")]
    public string message = "Bra hopp!";

    [Tooltip("Peak squish strength. 1.15 = up to 15% sideways stretch and 15% vertical squash at impact.")]
    public float squishAmount = 1.15f;
    [Tooltip("Total time for the squish pulse.")]
    public float squishDuration = 0.2f;

    [Tooltip("Cooldown so rapid re-collisions don't spam feedback.")]
    public float retriggerCooldown = 0.5f;

    [Header("Audio")]
    [Tooltip("Played at the mushroom's position on landing. Leave empty for silence.")]
    public AudioClip bounceClip;
    [Range(0f, 1f)]
    [Tooltip("Volume scale for the bounce SFX.")]
    public float bounceVolume = 0.55f;

    [Header("Music")]
    [Range(0.5f, 2.0f)]
    [Tooltip("Pitch multiplier for this mushroom's bounce. 1.0 = original, 2.0 = one octave up. " +
             "Each mushroom in Dinohopp gets a different note so the level becomes a tiny instrument.")]
    public float pitch = 1f;

    Vector3 baseScale;
    float squishTimer;
    float lastTriggerTime = -999f;
    AudioSource audioSource;

    void Awake()
    {
        baseScale = transform.localScale;
        audioSource = GetComponent<AudioSource>();
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        if (!other.collider.CompareTag(dinoTag)) return;

        // Only count landings from above (contact normal points up from the mushroom).
        bool landedFromAbove = false;
        for (int i = 0; i < other.contactCount; i++)
        {
            if (other.GetContact(i).normal.y > 0.5f)
            {
                landedFromAbove = true;
                break;
            }
        }
        if (!landedFromAbove) return;

        if (Time.time - lastTriggerTime < retriggerCooldown) return;
        lastTriggerTime = Time.time;

        squishTimer = squishDuration;
        FeedbackText.Show(message);

        if (bounceClip != null)
        {
            if (audioSource != null)
            {
                // Per-mushroom AudioSource gives each cap its own voice — multiple
                // mushrooms can ring out in parallel without cutting each other off,
                // and we can pitch-shift independently.
                audioSource.pitch = pitch;
                audioSource.PlayOneShot(bounceClip, bounceVolume);
            }
            else
            {
                // Fallback when no AudioSource is attached: no pitch shift, but still audible.
                AudioSource.PlayClipAtPoint(bounceClip, transform.position, bounceVolume);
            }
        }
    }

    void Update()
    {
        if (squishTimer > 0f)
        {
            squishTimer -= Time.deltaTime;
            float t = Mathf.Clamp01(squishTimer / squishDuration);
            // Ping-pong: 0 -> 1 -> 0
            float pulse = Mathf.Sin(t * Mathf.PI);
            float amt = (squishAmount - 1f) * pulse;
            // Asymmetric: stretch sideways and squash flat — feels like a soft cap absorbing impact.
            transform.localScale = new Vector3(
                baseScale.x * (1f + amt),
                baseScale.y * (1f - amt),
                baseScale.z);
        }
        else
        {
            transform.localScale = baseScale;
        }
    }
}
