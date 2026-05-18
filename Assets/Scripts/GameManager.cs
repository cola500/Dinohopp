using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// State machine + level switcher for Dinohopp.
/// States: StartScreen → Playing → (Retry | Success) → Playing.
///
/// Levels are built into the scene by SceneBuilder as separate roots. GameManager
/// toggles SetActive between them on success. Retry stays on the current level.
/// </summary>
public class GameManager : MonoBehaviour
{
    public enum State { StartScreen, Playing, Success, Retry }

    public static GameManager Instance { get; private set; }
    public State CurrentState { get; private set; }

    [Header("References (auto-resolved if left empty)")]
    public DinoController dino;
    public CameraFollow2D cameraFollow;

    [Header("Levels")]
    [Tooltip("Levels in play order. Index 0 is the first level; success on the last loops back to 0.")]
    public LevelInfo[] levels;

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
    [Range(0f, 1f)] public float fallVolume = 0.65f;

    public int CurrentLevelIndex { get; private set; }

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
        if (dino != null) dinoRb = dino.GetComponent<Rigidbody2D>();

        if (cameraFollow == null && Camera.main != null)
            cameraFollow = Camera.main.GetComponent<CameraFollow2D>();

        sfxSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        ActivateLevel(0);
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

        if (ActionPressedThisFrame()) HandleActionFromNonPlaying();
    }

    void HandleActionFromNonPlaying()
    {
        switch (CurrentState)
        {
            case State.StartScreen:
            case State.Retry:
                // Same level, fresh run.
                ResetDino();
                SetState(State.Playing);
                break;
            case State.Success:
                // Advance to the next level, or loop back to 0 after the last.
                int next = (CurrentLevelIndex < levels.Length - 1) ? CurrentLevelIndex + 1 : 0;
                ActivateLevel(next);
                ResetDino();
                SetState(State.Playing);
                break;
        }
    }

    /// <summary>Called by Goal when the dino enters its trigger.</summary>
    public void OnGoalReached()
    {
        if (CurrentState != State.Playing) return;
        if (dinoRb != null) dinoRb.linearVelocity = Vector2.zero;

        if (successPanel != null)
        {
            var txt = successPanel.GetComponent<UnityEngine.UI.Text>();
            if (txt != null)
            {
                bool allLetters = LetterCollectionManager.Instance != null
                                  && LetterCollectionManager.Instance.AllCollected;
                bool isLastLevel = (CurrentLevelIndex >= levels.Length - 1);
                string word = (levels != null && CurrentLevelIndex < levels.Length && levels[CurrentLevelIndex] != null)
                    ? levels[CurrentLevelIndex].wordToCollect : "";

                if (isLastLevel)
                {
                    txt.text = allLetters
                        ? $"Du hittade hela {word}!\nDu klarade Dinohopp!\n\nTryck SPACE\nför att börja om"
                        : "Du klarade Dinohopp!\n\nTryck SPACE\nför att börja om";
                }
                else
                {
                    txt.text = allLetters
                        ? $"Du hittade hela {word}!\n\nTryck SPACE\nför nästa bana"
                        : "Bra jobbat! Nästa bana!\n\nTryck SPACE";
                }
            }
        }

        SetState(State.Success);
    }

    /// <summary>Toggle level roots, apply level config (sky, dino start, word, colors).</summary>
    void ActivateLevel(int index)
    {
        if (levels == null || levels.Length == 0) return;
        index = Mathf.Clamp(index, 0, levels.Length - 1);
        CurrentLevelIndex = index;

        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i] != null) levels[i].gameObject.SetActive(i == index);
        }

        var info = levels[index];
        if (info == null) return;

        if (Camera.main != null) Camera.main.backgroundColor = info.skyColor;
        dinoStartPos = info.dinoStartPosition;

        if (LetterCollectionManager.Instance != null)
            LetterCollectionManager.Instance.SetWord(info.wordToCollect, info.letterColors);
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

        // Fresh letter row every restart — covers fall-to-retry, play-again, and level switch.
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
