using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Tiny state machine for Dinohopp: StartScreen -> Playing -> (Retry | Success) -> Playing.
/// Owns the dino reset, fall detection, and UI panel toggling. Input
/// (space/click/touch) only transitions states when NOT in Playing.
/// </summary>
public class GameManager : MonoBehaviour
{
    public enum State { StartScreen, Playing, Success, Retry }

    public static GameManager Instance { get; private set; }
    public State CurrentState { get; private set; }

    [Header("References (auto-resolved if left empty)")]
    public DinoController dino;
    public CameraFollow2D cameraFollow;

    [Header("UI Panels")]
    public GameObject startScreenPanel;
    public GameObject retryPanel;
    public GameObject successPanel;

    [Header("Rules")]
    [Tooltip("If the dino's Y falls below this, treat it as a fall and show Retry.")]
    public float fallThresholdY = -7f;

    [Header("Fall feedback")]
    [Tooltip("Cartoony 'oops' clip played once when the dino falls into the pit.")]
    public AudioClip fallClip;
    [Range(0f, 1f)]
    public float fallVolume = 0.65f;

    Vector3 dinoStartPos;
    Rigidbody2D dinoRb;
    AudioSource sfxSource;

    void Awake()
    {
        Instance = this;

        if (dino == null)
        {
            var found = GameObject.FindWithTag("Player");
            if (found != null) dino = found.GetComponent<DinoController>();
        }
        if (dino != null)
        {
            dinoStartPos = dino.transform.position;
            dinoRb = dino.GetComponent<Rigidbody2D>();
        }

        if (cameraFollow == null && Camera.main != null)
            cameraFollow = Camera.main.GetComponent<CameraFollow2D>();

        sfxSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        SetState(State.StartScreen);
    }

    void Update()
    {
        if (CurrentState == State.Playing)
        {
            if (dino != null && dino.transform.position.y < fallThresholdY)
            {
                // Cartoony "oops" — played BEFORE reset/state-change so the trigger is
                // bound to the single fall event (manual retry-button presses won't fire it).
                if (sfxSource != null && fallClip != null)
                    sfxSource.PlayOneShot(fallClip, fallVolume);
                ResetDino();
                SetState(State.Retry);
            }
            return;
        }

        // Non-Playing states: a single tap/click/space starts (or restarts) the level.
        if (ActionPressedThisFrame())
        {
            ResetDino();
            SetState(State.Playing);
        }
    }

    /// <summary>Called by Goal when the dino enters its trigger.</summary>
    public void OnGoalReached()
    {
        if (CurrentState != State.Playing) return;
        if (dinoRb != null) dinoRb.linearVelocity = Vector2.zero;

        // Vary the success message depending on whether VIOLA was fully collected.
        if (successPanel != null)
        {
            var txt = successPanel.GetComponent<UnityEngine.UI.Text>();
            if (txt != null)
            {
                bool allLetters = LetterCollectionManager.Instance != null
                                  && LetterCollectionManager.Instance.AllCollected;
                txt.text = allLetters
                    ? "Du hittade hela VIOLA!\n\nTryck SPACE\neller på skärmen"
                    : "Bra jobbat!\n\nTryck SPACE\neller på skärmen";
            }
        }

        SetState(State.Success);
    }

    void ResetDino()
    {
        if (dino == null) return;
        dino.transform.position = dinoStartPos;
        if (dinoRb != null)
        {
            dinoRb.linearVelocity = Vector2.zero;
            dinoRb.angularVelocity = 0f;
        }
        if (cameraFollow != null) cameraFollow.SnapToTarget();

        // Fresh V-I-O-L-A row every restart — covers both fall-to-retry and play-again.
        if (LetterCollectionManager.Instance != null)
            LetterCollectionManager.Instance.ResetCollection();
    }

    void SetState(State s)
    {
        CurrentState = s;
        if (dino != null) dino.SetControlsEnabled(s == State.Playing);
        if (startScreenPanel != null) startScreenPanel.SetActive(s == State.StartScreen);
        if (retryPanel       != null) retryPanel.SetActive(s == State.Retry);
        if (successPanel     != null) successPanel.SetActive(s == State.Success);
    }

    static bool ActionPressedThisFrame()
    {
        var kb = Keyboard.current;
        if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame))
            return true;
        var m = Mouse.current;
        if (m != null && m.leftButton.wasPressedThisFrame) return true;
        var t = Touchscreen.current;
        if (t != null && t.primaryTouch.press.wasPressedThisFrame) return true;
        return false;
    }
}
