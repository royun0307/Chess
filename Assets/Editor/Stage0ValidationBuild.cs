using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class Stage0ValidationBuild
{
    public static void Run()
    {
        try
        {
            var backend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone);
            if (backend != ScriptingImplementation.Mono2x)
                throw new InvalidOperationException("Current Standalone backend is not Mono: " + backend);

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            var output = Path.GetFullPath("Validation/Stage0/Windows64/Chess.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            Debug.Log("Stage0: Windows x64; backend=" + backend + "; Development; scenes=" + string.Join(",", scenes));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            var summary = report.summary;
            File.WriteAllText("Validation/Stage0/build-summary.txt",
                "Result: " + summary.result + "\nBackend: " + backend +
                "\nTarget: " + summary.platform + "\nOptions: " + summary.options +
                "\nScenes: " + string.Join(",", scenes) + "\nOutput: " + summary.outputPath +
                "\nErrors: " + summary.totalErrors + "\nWarnings: " + summary.totalWarnings +
                "\nDuration: " + summary.totalTime + "\nBytes: " + summary.totalSize);
            EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            File.WriteAllText("Validation/Stage0/build-summary.txt", exception.ToString());
            EditorApplication.Exit(1);
        }
    }
}
