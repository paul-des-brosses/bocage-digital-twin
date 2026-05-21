using Bocage.Presentation.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace Bocage.Editor.Presentation
{
    /// <summary>
    /// Custom inspector for <see cref="SimulationTraceRecorder"/> with a
    /// one-click "Export trace now" button. Lets the user dump the
    /// running trace to CSV at any point during Play, not only on
    /// destroy/stop. Useful when investigating a transient anomaly:
    /// run, reproduce, click Export, send the CSV.
    /// </summary>
    [CustomEditor(typeof(SimulationTraceRecorder))]
    public sealed class SimulationTraceRecorderEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(8);
            EditorGUILayout.LabelField("Trace export", EditorStyles.boldLabel);

            var recorder = (SimulationTraceRecorder)target;
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("Export trace now", GUILayout.Height(26)))
                {
                    string path = recorder.ExportToFile();
                    if (!string.IsNullOrEmpty(path))
                    {
                        EditorUtility.RevealInFinder(path);
                    }
                }
            }

            EditorGUILayout.HelpBox(
                "The trace is also exported automatically on Play stop if 'Auto Export On Destroy' is enabled.\n" +
                "The CSV lands in '{ProjectRoot}/Logs/trace-{timestamp}.csv'. Share it for analysis.",
                MessageType.Info);
        }
    }
}
