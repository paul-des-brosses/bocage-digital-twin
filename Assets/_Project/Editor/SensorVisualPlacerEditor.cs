using Bocage.Presentation.Scene.Sensors;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Bocage.Editor.Presentation
{
    /// <summary>
    /// Custom inspector for <see cref="SensorVisualPlacer"/>. Mirrors the
    /// workflow exposed for <c>SceneAssembler</c>:
    /// <list type="bullet">
    ///   <item>
    ///     <c>Rebuild from Placement</c> — clears the spawn root and
    ///     re-instantiates one sprite per placement.
    ///   </item>
    ///   <item>
    ///     <c>Capture Scene → Placement</c> — reads back the current
    ///     children of the spawn root (transforms, sortings, sprite ref)
    ///     and writes them into the placement asset. Metadata fields
    ///     (type, online status, observed variable, deferred-until tag)
    ///     are read from each child's <see cref="SensorMetadataTag"/>
    ///     so the round-trip preserves them across editor sessions.
    ///   </item>
    /// </list>
    /// </summary>
    [CustomEditor(typeof(SensorVisualPlacer))]
    public sealed class SensorVisualPlacerEditor : UnityEditor.Editor
    {
        private SerializedProperty _placementProp;

        private void OnEnable()
        {
            _placementProp = serializedObject.FindProperty("placement");
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(8);
            EditorGUILayout.LabelField("Sensor Placement Workflow", EditorStyles.boldLabel);

            var placer = (SensorVisualPlacer)target;
            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                using (new EditorGUI.DisabledScope(_placementProp.objectReferenceValue == null))
                {
                    if (GUILayout.Button("Rebuild from Placement (Edit Mode)", GUILayout.Height(26)))
                    {
                        if (ConfirmDestructiveRebuild(placer))
                        {
                            Undo.RegisterFullObjectHierarchyUndo(placer.gameObject, "Rebuild sensor placement");
                            placer.RebuildInEditor();
                            MarkSceneDirty(placer);
                        }
                    }

                    if (GUILayout.Button("Capture Scene → Placement", GUILayout.Height(26)))
                    {
                        int captured = CaptureFromScene(placer);
                        Debug.Log($"[SensorVisualPlacerEditor] captured {captured} sensors into {_placementProp.objectReferenceValue.name}");
                    }
                }
            }

            EditorGUILayout.HelpBox(
                "Workflow:\n" +
                "1. Press 'Rebuild from Placement' to spawn sensor sprites from the asset.\n" +
                "2. Manipulate them in the Scene view (W = move, R = scale).\n" +
                "3. Press 'Capture Scene → Placement' to write your changes back to the asset.\n" +
                "Metadata fields (type, online status, observed variable, deferred-until) are preserved across round-trips via SensorMetadataTag.",
                MessageType.Info);
        }

        private static bool ConfirmDestructiveRebuild(SensorVisualPlacer placer)
        {
            var parent = GetSpawnRoot(placer);
            if (parent == null || parent.childCount == 0) return true;

            return EditorUtility.DisplayDialog(
                "Rebuild from Placement",
                $"This will delete the {parent.childCount} existing sensor child(ren) under '{parent.name}' and re-spawn from the placement asset. " +
                "Any unsaved scene-view edits will be lost. Continue?",
                "Rebuild",
                "Cancel");
        }

        private int CaptureFromScene(SensorVisualPlacer placer)
        {
            var placement = _placementProp.objectReferenceValue as SensorPlacementDefinition;
            if (placement == null)
            {
                EditorUtility.DisplayDialog("Capture failed", "No SensorPlacementDefinition asset is assigned.", "OK");
                return 0;
            }

            var parent = GetSpawnRoot(placer);
            if (parent == null)
            {
                EditorUtility.DisplayDialog("Capture failed", "No spawn root resolved.", "OK");
                return 0;
            }

            var soDef = new SerializedObject(placement);
            var sensorsProp = soDef.FindProperty("sensors");
            sensorsProp.arraySize = parent.childCount;

            int captured = 0;
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                var sr = child.GetComponent<SpriteRenderer>();
                var meta = child.GetComponent<SensorMetadataTag>();

                if (sr == null)
                {
                    Debug.LogWarning($"[SensorVisualPlacerEditor] child '{child.name}' has no SpriteRenderer, captured as empty entry");
                }

                var elementProp = sensorsProp.GetArrayElementAtIndex(i);
                elementProp.FindPropertyRelative("id").stringValue = child.name;

                // Transforms + render fields.
                elementProp.FindPropertyRelative("sprite").objectReferenceValue = sr != null ? sr.sprite : null;
                var posProp = elementProp.FindPropertyRelative("worldPosition");
                posProp.vector2Value = new Vector2(child.localPosition.x, child.localPosition.y);

                Vector3 localScale = child.localScale;
                bool flipX = localScale.x < 0f;
                elementProp.FindPropertyRelative("scale").vector2Value =
                    new Vector2(Mathf.Abs(localScale.x), localScale.y);
                elementProp.FindPropertyRelative("flipX").boolValue = flipX;

                if (sr != null)
                {
                    elementProp.FindPropertyRelative("sortingLayerName").stringValue = sr.sortingLayerName;
                    elementProp.FindPropertyRelative("sortingOrderInLayer").intValue = sr.sortingOrder;
                }

                // Metadata fields — preserved from the tag if present, else
                // left as-is (the inspector may have authored them directly
                // on the SO without a SceneAssembler round-trip).
                if (meta != null)
                {
                    elementProp.FindPropertyRelative("displayName").stringValue = meta.DisplayName;
                    elementProp.FindPropertyRelative("type").enumValueIndex = (int)meta.Type;
                    elementProp.FindPropertyRelative("onlineStatus").enumValueIndex = (int)meta.OnlineStatus;
                    elementProp.FindPropertyRelative("observedModelVariable").stringValue = meta.ObservedModelVariable;
                    elementProp.FindPropertyRelative("deferredUntilStep").stringValue = meta.DeferredUntilStep;
                }

                captured++;
            }

            soDef.ApplyModifiedProperties();
            EditorUtility.SetDirty(placement);
            AssetDatabase.SaveAssetIfDirty(placement);
            return captured;
        }

        private static Transform GetSpawnRoot(SensorVisualPlacer placer)
        {
            var so = new SerializedObject(placer);
            var explicitRoot = so.FindProperty("spawnRoot").objectReferenceValue as Transform;
            return explicitRoot != null ? explicitRoot : placer.transform;
        }

        private static void MarkSceneDirty(SensorVisualPlacer placer)
        {
            EditorUtility.SetDirty(placer);
            if (placer.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(placer.gameObject.scene);
            }
        }
    }
}
