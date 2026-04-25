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

        Console.WriteLine($"[CIBuilder] Building WebGL: {scenes.Length} scene(s) -> {buildPath}");

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
