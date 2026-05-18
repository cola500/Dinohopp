using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Minimal WebGL build pipeline for Dinohopp. Two menus:
///   Tools/Dinohopp/Configure WebGL Build  — applies sensible mobile-friendly defaults
///   Tools/Dinohopp/Build WebGL            — builds to Builds/WebGL/
///
/// First-time use: run Configure once, then Build. Subsequent builds just hit Build.
/// </summary>
public static class DinohoppBuildSettings
{
    const string BuildPath  = "Builds/WebGL";
    const string ScenePath  = "Assets/Scenes/DinohoppPrototype.unity";

    [MenuItem("Tools/Dinohopp/Configure WebGL Build")]
    public static void ConfigureWebGL()
    {
        // ---- Player settings ----
        PlayerSettings.runInBackground = true;
        PlayerSettings.defaultScreenWidth  = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.defaultIsNativeResolution = false;

        // ---- WebGL-specific ----
        // Disabled compression = most compatible with cheap static hosts (no special
        // Content-Encoding headers needed). Bigger build size but it Just Works on
        // GitHub Pages, itch.io, plain Apache/nginx, etc. Switch to Gzip later if
        // size matters and your host sets the right headers.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.dataCaching       = true;
        PlayerSettings.WebGL.exceptionSupport  = WebGLExceptionSupport.None; // smallest build
        PlayerSettings.WebGL.linkerTarget      = WebGLLinkerTarget.Wasm;
        PlayerSettings.WebGL.template          = "PROJECT:DinohoppMobile";
        PlayerSettings.WebGL.threadsSupport    = false; // best browser compatibility

        // Audio: standard 2D-game-friendly defaults; nothing exotic needed.

        // Make sure the prototype scene is the only scene in build settings,
        // and that it's at build index 0 (so it loads on app startup).
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, enabled: true),
        };

        // Switch active build target so subsequent builds don't trigger a
        // platform-switch warning the first time.
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
        {
            Debug.Log("[Dinohopp] Switching active build target → WebGL (may take a moment).");
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
        }

        Debug.Log("[Dinohopp] WebGL build settings configured. Run 'Tools/Dinohopp/Build WebGL' next.");
    }

    [MenuItem("Tools/Dinohopp/Build WebGL")]
    public static void BuildWebGL()
    {
        // Resolve absolute path so the build lands inside the project root
        // (Builds/ as a sibling of Assets/).
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string output = Path.Combine(projectRoot, BuildPath);
        Directory.CreateDirectory(output);

        var options = new BuildPlayerOptions
        {
            scenes           = new[] { ScenePath },
            locationPathName = output,
            target           = BuildTarget.WebGL,
            options          = BuildOptions.None,
        };

        Debug.Log($"[Dinohopp] Building WebGL → {output}");
        var report = BuildPipeline.BuildPlayer(options);
        var s = report.summary;

        if (s.result == BuildResult.Succeeded)
        {
            float mb = s.totalSize / (1024f * 1024f);
            Debug.Log($"[Dinohopp] WebGL build OK — {mb:F1} MB, {s.totalTime.TotalSeconds:F0} s. " +
                      $"Output: {output}");
        }
        else
        {
            Debug.LogError($"[Dinohopp] WebGL build failed: {s.result} ({s.totalErrors} errors).");
        }
    }
}
