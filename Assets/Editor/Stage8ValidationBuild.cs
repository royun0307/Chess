using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

// A separate instrumented player. The normal build never enables this assembly.
public static class Stage8ValidationBuild
{
    public static void Run()
    {
        Directory.CreateDirectory("Validation/Stage8");
        const string scenePath = "Assets/Tests/Player/Stage8ValidationScene.unity";
        var setup = EditorSceneManager.GetSceneManagerSetup();
        bool created = false;
        int exitCode = 1;
        try
        {
            if (File.Exists(scenePath)) throw new InvalidOperationException("Temporary validation scene already exists; inspect it before rebuilding");
            if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone) != ScriptingImplementation.Mono2x)
                throw new InvalidOperationException("Stage 8 requires Windows Mono");
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
            var type = Type.GetType("Stage8PlayerValidation, Chess.Validation.Player", true);
            new GameObject("Stage8 player validation").AddComponent(type);
            if (!EditorSceneManager.SaveScene(scene, scenePath, true)) throw new IOException("Cannot save validation scene copy");
            created = true;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { scenePath },
                locationPathName = "Validation/Stage8/Windows64/Chess.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development,
                extraScriptingDefines = new[] { "STAGE8_PLAYER_VALIDATION" }
            });
            File.WriteAllText("Validation/Stage8/build-summary.txt",
                $"Result: {report.summary.result}\nBackend: Mono2x\nErrors: {report.summary.totalErrors}\nWarnings: {report.summary.totalWarnings}\nDuration: {report.summary.totalTime}\nInstrumented: STAGE8_PLAYER_VALIDATION\n");
            exitCode = report.summary.result == BuildResult.Succeeded ? 0 : 1;
        }
        catch (Exception e)
        {
            File.WriteAllText("Validation/Stage8/build-summary.txt", e.ToString());
        }
        finally
        {
            // MainScene is never saved. Only our temporary copy is removed.
            if (setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup);
            else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (created) AssetDatabase.DeleteAsset(scenePath);
        }
        EditorApplication.Exit(exitCode);
    }
}
