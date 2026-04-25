using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class CIBuilder
{
    public static void BuildWebGL()
    {
        string buildPath = "build/WebGL";
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "-buildPath")
            {
                buildPath = args[i + 1];
                break;
            }
        }

        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            Console.Error.WriteLine("[CIBuilder] No enabled scenes in EditorBuildSettings.");
            EditorApplication.Exit(1);
            return;
        }

        // Force WebGL build flags from code so the CI build does not depend on
        // the active Build Profile (Build Profile overrides are ignored when
        // BuildPipeline.BuildPlayer is invoked without setting the profile
        // active). Brotli + decompression fallback is required for GitHub
        // Pages, which cannot send the Content-Encoding: br header.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
        PlayerSettings.WebGL.decompressionFallback = true;

        Console.WriteLine($"[CIBuilder] Building WebGL: {scenes.Length} scene(s) -> {buildPath}");
        Console.WriteLine($"[CIBuilder] compressionFormat={PlayerSettings.WebGL.compressionFormat} decompressionFallback={PlayerSettings.WebGL.decompressionFallback}");

        BuildReport report = BuildPipeline.BuildPlayer(
            scenes,
            buildPath,
            BuildTarget.WebGL,
            BuildOptions.None
        );

        bool ok = report.summary.result == BuildResult.Succeeded;
        Console.WriteLine($"[CIBuilder] Result: {report.summary.result} | size: {report.summary.totalSize} B | duration: {report.summary.totalTime}");
        EditorApplication.Exit(ok ? 0 : 1);
    }
}
