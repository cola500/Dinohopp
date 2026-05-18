using UnityEngine;

/// <summary>
/// Tiny "alive" animator for the dino's Visual child. Two modes:
///   - Paused (StartScreen/Retry/Success): slow breathing scale on Y.
///   - Running on ground: small vertical bob giving a footstep feel.
///   - Airborne: snaps back to neutral so the squash/stretch on the root reads cleanly.
///
/// Operates on the Visual child only — never touches the root collider/Rigidbody2D.
/// </summary>
[RequireComponent(typeof(DinoController))]
public class DinoLocomotion : MonoBehaviour
{
    [Tooltip("The dino's visual root. Auto-found at 'Visual' if null.")]
    public Transform visual;

    [Header("Idle Breathing")]
    [Tooltip("Cycles per second when the dino is paused.")]
    public float breathFrequency = 0.45f;
    [Tooltip("Peak Y-scale gain at the top of a breath. Tiny.")]
    public float breathAmount = 0.04f;

    [Header("Running Bob")]
    [Tooltip("Footstep frequency in Hz while auto-running.")]
    public float runBobFrequency = 4.0f;
    [Tooltip("Peak Y-offset of the visual at the top of a bob, in world units.")]
    public float runBobAmount = 0.04f;

    DinoController controller;
    Vector3 visualBaseScale;
    Vector3 visualBasePos;

    void Awake()
    {
        controller = GetComponent<DinoController>();
        if (visual == null) visual = transform.Find("Visual");
        if (visual != null)
        {
            visualBaseScale = visual.localScale;
            visualBasePos = visual.localPosition;
        }
    }

    void Update()
    {
        if (visual == null) return;

        bool playing  = controller != null && controller.controlsEnabled;
        bool grounded = controller != null && controller.IsGrounded;

        if (!playing)
        {
            // Idle breathing: gentle Y-scale wobble, neutral position.
            float breath = Mathf.Sin(Time.time * breathFrequency * Mathf.PI * 2f) * breathAmount;
            visual.localPosition = visualBasePos;
            visual.localScale = new Vector3(
                visualBaseScale.x,
                visualBaseScale.y * (1f + breath),
                visualBaseScale.z);
            return;
        }

        if (grounded)
        {
            // Running footstep bob: always-positive Y offset, neutral scale.
            float bob = Mathf.Abs(Mathf.Sin(Time.time * runBobFrequency * Mathf.PI)) * runBobAmount;
            visual.localPosition = visualBasePos + new Vector3(0f, bob, 0f);
            visual.localScale = visualBaseScale;
            return;
        }

        // Airborne — let the root's squash/stretch carry the motion. Neutral here.
        visual.localPosition = visualBasePos;
        visual.localScale = visualBaseScale;
    }
}
