using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace OttoEngine.Editor
{
    /// <summary>
    /// Builds the WebGL version into WebGL/ at the project root, which the GitHub Pages workflow publishes.
    /// Menu: Otto Engine > Build WebGL. Headless:
    /// Unity -batchmode -quit -projectPath . -buildTarget WebGL -executeMethod OttoEngine.Editor.BuildWebGL.Build
    /// </summary>
    public static class BuildWebGL
    {
        private const string Output = "WebGL";

        [MenuItem("Otto Engine/Build WebGL")]
        public static void Build()
        {
            // full-window page (Assets/WebGLTemplates/FullWindow); gzip with a JavaScript fallback, because GitHub
            // Pages does not send Content-Encoding headers for pre-compressed files
            PlayerSettings.WebGL.template = "PROJECT:FullWindow";
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.nameFilesAsHashes = false;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;

            if (Directory.Exists(Output)) Directory.Delete(Output, true);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = Output,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            });
            var summary = report.summary;
            Debug.Log($"[OttoEngine] WebGL build {summary.result}: {summary.totalSize / 1e6:F1} MB, {summary.totalTime.TotalSeconds:F0} s -> {Output}/");
            if (Application.isBatchMode && summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }
    }
}
