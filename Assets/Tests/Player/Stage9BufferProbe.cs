using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

// Diagnostic player only: compare normal shutdown with releasing one known package buffer.
// Reflection is intentionally confined to this validation assembly, not gameplay code.
public sealed class Stage9BufferProbe : MonoBehaviour
{
    private IEnumerator Start()
    {
        Application.runInBackground = true;
        yield return new WaitForSecondsRealtime(3);
        try
        {
            var type = Type.GetType("UnityEngine.U2D.Animation.GpuDeformationSystem, Unity.2D.Animation.Runtime", true);
            var field = type.GetField("s_FallbackBuffer", BindingFlags.Static | BindingFlags.NonPublic);
            var buffer = field?.GetValue(null) as ComputeBuffer;
            if (buffer == null || !buffer.IsValid()) throw new Exception("Expected live 2D Animation fallback buffer");
            Debug.Log($"Stage9: fallback valid; count={buffer.count}; stride={buffer.stride}");
            bool release = Array.IndexOf(Environment.GetCommandLineArgs(), "-stage9-release-buffer") >= 0;
            if (release)
            {
                var cleanup = type.GetMethod("ClearFallbackBuffer", BindingFlags.Static | BindingFlags.NonPublic);
                if (cleanup == null) throw new MissingMethodException(type.FullName, "ClearFallbackBuffer");
                cleanup.Invoke(null, null);
                if (buffer.IsValid() || field.GetValue(null) != null) throw new Exception("Fallback cleanup failed");
                Debug.Log("Stage9: only fallback buffer released using package cleanup");
            }
            else Debug.Log("Stage9: baseline leaves package buffer untouched");
            Debug.Log("Stage9: probe passed; inspect shutdown log for ComputeBuffer warning");
            Application.Quit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Application.Quit(1);
        }
    }
}
