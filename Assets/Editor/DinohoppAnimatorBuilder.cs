using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Builds the dino's AnimationClips and AnimatorController from the sprites that
/// <see cref="DinohoppDinoSpriteBuilder"/> generates. Re-runnable: existing assets
/// are overwritten so iterating on the sprite art or animation tuning is one click.
///
/// State machine is intentionally tiny — four states, three parameters, AnyState
/// transitions. No blend trees, no layers, no overrides. Tweaking conditions or
/// frame rates happens by editing the "Tunables" constants below and re-running
/// <c>Tools/Dinohopp/Build Dino Animator</c>.
/// </summary>
public static class DinohoppAnimatorBuilder
{
    const string AnimDir          = "Assets/Animation";
    const string ControllerPath   = "Assets/Animation/DinoAnimator.controller";
    const string IdleClipPath     = "Assets/Animation/Dino_Idle.anim";
    const string RunClipPath      = "Assets/Animation/Dino_Run.anim";
    const string JumpClipPath     = "Assets/Animation/Dino_Jump.anim";
    const string FallClipPath     = "Assets/Animation/Dino_Fall.anim";

    // ---- Tunables ----
    // Idle: mostly the open-eye sprite, with a brief blink baked into the clip so we
    // don't need a separate Blink state or layer. Loops every IdleLoopSec.
    const float IdleLoopSec       = 3.2f;
    const float BlinkStartSec     = 2.85f;
    const float BlinkDurationSec  = 0.12f;

    // Run: two-frame loop. Period chosen to feel like a steady jog at runSpeed=1.8.
    const float RunFrameSec       = 0.13f;

    // Animator parameter names — kept as public consts so DinoAnimatorBridge can
    // reference them without typo risk.
    public const string ParamIsGrounded      = "IsGrounded";
    public const string ParamHorizontalSpeed = "HorizontalSpeed";
    public const string ParamVerticalSpeed   = "VerticalSpeed";

    [MenuItem("Tools/Dinohopp/Build Dino Animator")]
    public static void Build()
    {
        EnsureDir();

        var spriteIdle      = Load("Assets/Sprites/Dino/Dino_Idle.png");
        var spriteIdleBlink = Load("Assets/Sprites/Dino/Dino_Idle_Blink.png");
        var spriteRunA      = Load("Assets/Sprites/Dino/Dino_Run_A.png");
        var spriteRunB      = Load("Assets/Sprites/Dino/Dino_Run_B.png");
        var spriteJump      = Load("Assets/Sprites/Dino/Dino_Jump.png");
        var spriteFall      = Load("Assets/Sprites/Dino/Dino_Fall.png");

        if (spriteIdle == null || spriteIdleBlink == null || spriteRunA == null ||
            spriteRunB == null || spriteJump == null || spriteFall == null)
        {
            Debug.LogError("[Dinohopp] Missing dino sprites in Assets/Sprites/Dino/. " +
                           "Run 'Tools/Dinohopp/Build Dino Sprites' first.");
            return;
        }

        // ---- Clips ----
        var idleClip = BuildClip(IdleClipPath, IdleLoopSec, loop: true,
            new[] {
                Kf(0f,                                spriteIdle),
                Kf(BlinkStartSec,                     spriteIdleBlink),
                Kf(BlinkStartSec + BlinkDurationSec,  spriteIdle),
            });

        var runClip = BuildClip(RunClipPath, RunFrameSec * 2f, loop: true,
            new[] {
                Kf(0f,           spriteRunA),
                Kf(RunFrameSec,  spriteRunB),
            });

        var jumpClip = BuildClip(JumpClipPath, 0.25f, loop: false,
            new[] { Kf(0f, spriteJump) });

        var fallClip = BuildClip(FallClipPath, 0.25f, loop: false,
            new[] { Kf(0f, spriteFall) });

        // ---- Controller ----
        // Delete-then-create so a re-run starts from a clean slate (no stale states
        // or transitions left over from previous tuning).
        AssetDatabase.DeleteAsset(ControllerPath);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        ctrl.AddParameter(ParamIsGrounded,      AnimatorControllerParameterType.Bool);
        ctrl.AddParameter(ParamHorizontalSpeed, AnimatorControllerParameterType.Float);
        ctrl.AddParameter(ParamVerticalSpeed,   AnimatorControllerParameterType.Float);

        var sm = ctrl.layers[0].stateMachine;
        sm.entryPosition  = new Vector3(  0f,  0f, 0f);
        sm.anyStatePosition = new Vector3(0f, -120f, 0f);
        sm.exitPosition   = new Vector3(600f,  0f, 0f);

        var idleState = sm.AddState("Idle", new Vector3(260f,  60f, 0f));
        var runState  = sm.AddState("Run",  new Vector3(260f, 160f, 0f));
        var jumpState = sm.AddState("Jump", new Vector3(260f, 260f, 0f));
        var fallState = sm.AddState("Fall", new Vector3(260f, 360f, 0f));

        idleState.motion = idleClip;
        runState.motion  = runClip;
        jumpState.motion = jumpClip;
        fallState.motion = fallClip;

        sm.defaultState = idleState;

        // ---- AnyState transitions ----
        // Order matters: Unity evaluates AnyState transitions top-to-bottom and
        // takes the first whose conditions are met. We want airborne states to win
        // over grounded ones, and Jump (going up) to win over Fall (going down).
        AddAny(sm, jumpState, "Jump",
            (ParamIsGrounded,      AnimatorConditionMode.IfNot,   0f),
            (ParamVerticalSpeed,   AnimatorConditionMode.Greater, 0.1f));

        AddAny(sm, fallState, "Fall",
            (ParamIsGrounded,      AnimatorConditionMode.IfNot,   0f));

        AddAny(sm, runState, "Run",
            (ParamIsGrounded,      AnimatorConditionMode.If,      0f),
            (ParamHorizontalSpeed, AnimatorConditionMode.Greater, 0.1f));

        AddAny(sm, idleState, "Idle",
            (ParamIsGrounded,      AnimatorConditionMode.If,      0f),
            (ParamHorizontalSpeed, AnimatorConditionMode.Less,    0.1f));

        AssetDatabase.SaveAssets();
        Debug.Log($"[Dinohopp] Animator built → {ControllerPath} (Idle/Run/Jump/Fall, " +
                  $"params: {ParamIsGrounded}, {ParamHorizontalSpeed}, {ParamVerticalSpeed})");
    }

    // ---------- Helpers ----------

    static AnimationClip BuildClip(string path, float length, bool loop, ObjectReferenceKeyframe[] keys)
    {
        var clip = new AnimationClip { frameRate = 24 };
        var binding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");

        // Tail keyframe at `length` echoes the previous sprite — ensures Unity holds
        // the last frame's sprite through to the loop point instead of interpolating
        // back toward keyframe 0 early.
        var withTail = new ObjectReferenceKeyframe[keys.Length + 1];
        for (int i = 0; i < keys.Length; i++) withTail[i] = keys[i];
        withTail[keys.Length] = Kf(length, keys[keys.Length - 1].value);

        AnimationUtility.SetObjectReferenceCurve(clip, binding, withTail);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        settings.stopTime = length;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    static ObjectReferenceKeyframe Kf(float time, Object value)
        => new ObjectReferenceKeyframe { time = time, value = value };

    static void AddAny(AnimatorStateMachine sm, AnimatorState dest, string label,
                       params (string param, AnimatorConditionMode mode, float threshold)[] conditions)
    {
        var t = sm.AddAnyStateTransition(dest);
        t.name = label;
        t.hasExitTime = false;
        t.duration = 0.06f;
        t.canTransitionToSelf = false;
        foreach (var (p, m, th) in conditions)
            t.AddCondition(m, th, p);
    }

    static Sprite Load(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

    static void EnsureDir()
    {
        if (!AssetDatabase.IsValidFolder(AnimDir))
            AssetDatabase.CreateFolder("Assets", "Animation");
    }
}
