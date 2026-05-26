using UnityEngine;

/// <summary>
/// Feeds the dino's Animator with state from <see cref="DinoController"/> and
/// <see cref="Rigidbody2D"/> every frame. Three parameters, no allocations.
///
/// Parameter names must match those defined in
/// <c>DinohoppAnimatorBuilder</c> (IsGrounded, HorizontalSpeed, VerticalSpeed).
/// </summary>
[RequireComponent(typeof(DinoController))]
public class DinoAnimatorBridge : MonoBehaviour
{
    [Tooltip("Animator that owns the dino's state machine. Auto-found on the same GameObject or any child if null.")]
    public Animator animator;

    [Tooltip("SpriteRenderer to flip if the dino ever runs backwards. Auto-found in children if null.")]
    public SpriteRenderer spriteRenderer;

    // Cached parameter hashes — cheaper than the string lookups Animator does
    // internally each Set* call.
    static readonly int HashIsGrounded      = Animator.StringToHash("IsGrounded");
    static readonly int HashHorizontalSpeed = Animator.StringToHash("HorizontalSpeed");
    static readonly int HashVerticalSpeed   = Animator.StringToHash("VerticalSpeed");

    DinoController controller;
    Rigidbody2D    rb;

    void Awake()
    {
        controller = GetComponent<DinoController>();
        rb         = GetComponent<Rigidbody2D>();
        if (animator == null)       animator       = GetComponentInChildren<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void Update()
    {
        if (animator == null || rb == null) return;

        Vector2 v = rb.linearVelocity;

        animator.SetBool (HashIsGrounded,      controller.IsGrounded);
        animator.SetFloat(HashHorizontalSpeed, Mathf.Abs(v.x));
        animator.SetFloat(HashVerticalSpeed,   v.y);

        // The game is auto-run-right today, but the flip-on-negative-x is cheap
        // safety in case Johan ever adds left-running levels.
        if (spriteRenderer != null && Mathf.Abs(v.x) > 0.05f)
            spriteRenderer.flipX = v.x < 0f;
    }
}
