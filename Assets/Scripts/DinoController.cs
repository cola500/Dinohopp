using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class DinoController : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Constant rightward auto-run speed.")]
    public float runSpeed = 2.5f;

    [Header("Jump")]
    [Tooltip("Upward velocity applied on jump.")]
    public float jumpForce = 8f;

    [Header("Ground Check")]
    [Tooltip("Empty transform placed at the dino's feet. If null, uses bottom of collider.")]
    public Transform groundCheck;
    [Tooltip("Radius of the ground-check overlap circle.")]
    public float groundCheckRadius = 0.12f;
    [Tooltip("Which layers count as ground. Auto-set to Everything at Awake if left empty.")]
    public LayerMask groundLayers;

    [Header("Jump Forgiveness")]
    [Tooltip("Grace period after leaving ground during which a jump press still counts. " +
             "Higher = more forgiving (good for small kids). Lower = more precise (good for skilled players).")]
    public float coyoteTime = 0.18f;
    [Tooltip("How long a jump press is remembered before landing. " +
             "Higher = more forgiving (good for small kids). Lower = more precise (good for skilled players).")]
    public float jumpBufferTime = 0.18f;

    [Header("Control")]
    [Tooltip("If false, the dino doesn't auto-run or accept jump input. GameManager toggles this between states.")]
    public bool controlsEnabled = false;

    /// <summary>Raised the frame the dino actually jumps (after coyote/buffer checks).</summary>
    public event System.Action OnJump;
    /// <summary>Raised the frame the dino transitions from airborne to grounded.</summary>
    public event System.Action OnLand;

    /// <summary>True when the ground-check found a non-self collider this frame.</summary>
    public bool IsGrounded => isGrounded;

    Rigidbody2D rb;
    Collider2D col;
    bool isGrounded;
    bool wasGrounded;
    float coyoteTimer;
    float jumpBufferTimer;
    int enabledOnFrame = -1;

    /// <summary>
    /// Toggle auto-run + jump input. Suppresses the first frame's input so the
    /// "Tap to start" press isn't immediately re-interpreted as a jump.
    /// </summary>
    public void SetControlsEnabled(bool value)
    {
        if (value && !controlsEnabled) enabledOnFrame = Time.frameCount;
        controlsEnabled = value;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        rb.freezeRotation = true;

        // Safety net: an unset LayerMask defaults to 0 ("Nothing"),
        // which would silently break the ground check. Fall back to everything.
        if (groundLayers.value == 0) groundLayers = Physics2D.AllLayers;

        // Assume starting grounded so we don't fire a spurious OnLand on frame 1.
        wasGrounded = true;
    }

    void Update()
    {
        isGrounded = CheckGrounded();

        // When paused (StartScreen/Retry/Success) we still fire landing feedback so
        // visuals stay alive, but skip everything input-related.
        if (!controlsEnabled || Time.frameCount == enabledOnFrame)
        {
            if (!wasGrounded && isGrounded) OnLand?.Invoke();
            wasGrounded = isGrounded;
            return;
        }

        // Coyote: top up while grounded, drain while airborne.
        if (isGrounded) coyoteTimer = coyoteTime;
        else coyoteTimer -= Time.deltaTime;

        // Buffer: latch on press, drain otherwise.
        if (JumpPressedThisFrame()) jumpBufferTimer = jumpBufferTime;
        else jumpBufferTimer -= Time.deltaTime;

        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            Jump();
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
        }

        if (!wasGrounded && isGrounded) OnLand?.Invoke();
        wasGrounded = isGrounded;
    }

    void FixedUpdate()
    {
        var v = rb.linearVelocity;
        v.x = controlsEnabled ? runSpeed : 0f;
        rb.linearVelocity = v;
    }

    void Jump()
    {
        var v = rb.linearVelocity;
        v.y = jumpForce;
        rb.linearVelocity = v;
        OnJump?.Invoke();
    }

    bool CheckGrounded()
    {
        Vector2 origin = groundCheck != null
            ? (Vector2)groundCheck.position
            : new Vector2(col.bounds.center.x, col.bounds.min.y - 0.02f);

        var hits = Physics2D.OverlapCircleAll(origin, groundCheckRadius, groundLayers);
        for (int i = 0; i < hits.Length; i++)
        {
            var h = hits[i];
            if (h == null) continue;
            // Ignore the dino's own colliders.
            if (h.attachedRigidbody == rb) continue;
            if (h == col) continue;
            return true;
        }
        return false;
    }

    bool JumpPressedThisFrame()
    {
        var kb = Keyboard.current;
        if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame))
            return true;

        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            return true;

        var touch = Touchscreen.current;
        if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
            return true;

        return false;
    }

    void OnDrawGizmosSelected()
    {
        Vector2 origin;
        if (groundCheck != null)
        {
            origin = groundCheck.position;
        }
        else
        {
            var c = GetComponent<Collider2D>();
            if (c == null) return;
            origin = new Vector2(c.bounds.center.x, c.bounds.min.y);
        }
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(origin, groundCheckRadius);
    }
}
