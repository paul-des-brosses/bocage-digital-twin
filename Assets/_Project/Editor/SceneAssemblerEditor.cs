using Bocage.Presentation.Scene.Composition;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Bocage.Editor.Presentation
{
    /// <summary>
    /// Custom inspector for <see cref="SceneAssembler"/> with two
    /// workflow buttons that let the artist iterate on the scene
    /// composition without entering Play Mode:
    /// <list type="bullet">
    ///   <item>
    ///     <c>Rebuild from Composition</c> — clears the spawn root's
    ///     children and re-instantiates them from the assigned
    ///     <see cref="SceneCompositionDefinition"/>.
    ///   </item>
    ///   <item>
    ///     <c>Capture Scene → Composition</c> — reads back the current
    ///     children of the spawn root (positions, scales, sorting, sprite
    ///     refs) and writes them into the <see cref="SceneCompositionDefinition"/>
    ///     asset. Use after manipulating sprites directly in the Scene
    ///     view with Unity's transform handles (W = move, R = scale).
    ///   </item>
    /// </list>
    /// </summary>
    [CustomEditor(typeof(SceneAssembler))]
    public sealed class SceneAssemblerEditor : UnityEditor.Editor
    {
        private SerializedProperty _compositionProp;
        private SerializedProperty _spawnRootProp;

        private void OnEnable()
        {
            _compositionProp = serializedObject.FindProperty("composition");
            _spawnRootProp = serializedObject.FindProperty("spawnRoot");
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(8);
            EditorGUILayout.LabelField("Composition Workflow", EditorStyles.boldLabel);

            var assembler = (SceneAssembler)target;
            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                using (new EditorGUI.DisabledScope(_compositionProp.objectReferenceValue == null))
                {
                    if (GUILayout.Button("Rebuild from Composition (Edit Mode)", GUILayout.Height(26)))
                    {
                        if (ConfirmDestructiveRebuild(assembler))
                        {
                            Undo.RegisterFullObjectHierarchyUndo(assembler.gameObject, "Rebuild scene composition");
                            assembler.RebuildInEditor();
                            MarkSceneDirty(assembler);
                        }
                    }

                    if (GUILayout.Button("Capture Scene → Composition", GUILayout.Height(26)))
                    {
                        int captured = CaptureFromScene(assembler);
                        Debug.Log($"[SceneAssemblerEditor] captured {captured} elements into {_compositionProp.objectReferenceValue.name}");
                    }
                }
            }

            EditorGUILayout.HelpBox(
                "Workflow:\n" +
                "1. Press 'Rebuild from Composition' to spawn sprites from the asset.\n" +
                "2. Manipulate them in the Scene view (W = move, R = scale, sorting via inspector).\n" +
                "3. Press 'Capture Scene → Composition' to write your changes back to the asset.\n" +
                "Tip: the SceneAssembler is the source of truth at Play — never edit the spawned children at runtime.",
                MessageType.Info);
        }

        private static bool ConfirmDestructiveRebuild(SceneAssembler assembler)
        {
            var parent = GetSpawnRoot(assembler);
            if (parent == null || parent.childCount == 0) return true;

            return EditorUtility.DisplayDialog(
                "Rebuild from Composition",
                $"This will delete the {parent.childCount} existing child(ren) under '{parent.name}' and re-spawn from the composition asset. " +
                "Any unsaved scene-view edits will be lost. Continue?",
                "Rebuild",
                "Cancel");
        }

        private int CaptureFromScene(SceneAssembler assembler)
        {
            var composition = _compositionProp.objectReferenceValue as SceneCompositionDefinition;
            if (composition == null)
            {
                EditorUtility.DisplayDialog("Capture failed", "No SceneCompositionDefinition asset is assigned.", "OK");
                return 0;
            }

            var parent = GetSpawnRoot(assembler);
            if (parent == null)
            {
                EditorUtility.DisplayDialog("Capture failed", "No spawn root resolved (neither 'Spawn Root' nor 'this' transform is set).", "OK");
                return 0;
            }

            var soDef = new SerializedObject(composition);
            var elementsProp = soDef.FindProperty("elements");
            elementsProp.arraySize = parent.childCount;

            int captured = 0;
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                var sr = child.GetComponent<SpriteRenderer>();
                if (sr == null)
                {
                    Debug.LogWarning($"[SceneAssemblerEditor] child '{child.name}' has no SpriteRenderer, captured as empty entry");
                }

                var elementProp = elementsProp.GetArrayElementAtIndex(i);
                elementProp.FindPropertyRelative("id").stringValue = child.name;
                elementProp.FindPropertyRelative("sprite").objectReferenceValue = sr != null ? sr.sprite : null;

                var posProp = elementProp.FindPropertyRelative("worldPosition");
                posProp.vector2Value = new Vector2(child.localPosition.x, child.localPosition.y);

                // Recover non-uniform scale and flipX from the negative-X convention used by Rebuild.
                Vector3 localScale = child.localScale;
                bool flipX = localScale.x < 0f;
                elementProp.FindPropertyRelative("scale").vector2Value =
                    new Vector2(Mathf.Abs(localScale.x), localScale.y);
                elementProp.FindPropertyRelative("flipX").boolValue = flipX;

                if (sr != null)
                {
                    elementProp.FindPropertyRelative("sortingLayerName").stringValue = sr.sortingLayerName;
                    elementProp.FindPropertyRelative("sortingOrderInLayer").intValue = sr.sortingOrder;

                    // sharedMaterial is what an artist assigns via the
                    // Inspector. We don't try to detect "default sprite
                    // material" vs custom — Unity returns the canonical
                    // default-sprite material reference in either case, which
                    // is harmless to store: SceneAssembler treats only a
                    // null material as "use default".
                    elementProp.FindPropertyRelative("material").objectReferenceValue = sr.sharedMaterial;
                }

                captured++;
            }

            soDef.ApplyModifiedProperties();
            EditorUtility.SetDirty(composition);
            AssetDatabase.SaveAssetIfDirty(composition);
            return captured;
        }

        private static Transform GetSpawnRoot(SceneAssembler assembler)
        {
            // Mirror SceneAssembler's fallback logic: explicit spawnRoot or this.transform.
            var so = new SerializedObject(assembler);
            var explicitRoot = so.FindProperty("spawnRoot").objectReferenceValue as Transform;
            return explicitRoot != null ? explicitRoot : assembler.transform;
        }

        private static void MarkSceneDirty(SceneAssembler assembler)
        {
            EditorUtility.SetDirty(assembler);
            if (assembler.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(assembler.gameObject.scene);
            }
        }
    }
}
