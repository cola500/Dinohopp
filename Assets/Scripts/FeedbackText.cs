using UnityEngine;
using UnityEngine.UI;

public class FeedbackText : MonoBehaviour
{
    public static FeedbackText Instance { get; private set; }

    [Tooltip("How long the text stays fully visible before fading out.")]
    public float visibleDuration = 0.6f;
    [Tooltip("Fade out duration in seconds.")]
    public float fadeDuration = 0.4f;

    Text uiText;
    float timer;
    float startAlpha = 1f;

    void Awake()
    {
        Instance = this;
        uiText = GetComponent<Text>();
        SetAlpha(0f);
    }

    public static void Show(string message)
    {
        if (Instance == null) return;
        Instance.ShowInternal(message);
    }

    void ShowInternal(string message)
    {
        uiText.text = message;
        timer = visibleDuration + fadeDuration;
        SetAlpha(startAlpha);
    }

    void Update()
    {
        if (timer <= 0f) return;

        timer -= Time.deltaTime;

        if (timer > fadeDuration)
        {
            SetAlpha(startAlpha);
        }
        else if (timer > 0f)
        {
            SetAlpha(startAlpha * (timer / fadeDuration));
        }
        else
        {
            SetAlpha(0f);
        }
    }

    void SetAlpha(float a)
    {
        if (uiText == null) return;
        var c = uiText.color;
        c.a = a;
        uiText.color = c;
    }
}
