using Bocage.Presentation.Scene.Fauna;
using UnityEditor;
using UnityEngine;

namespace Bocage.Presentation.Editor.Scene.Fauna
{
    /// <summary>
    /// Custom Editor for <see cref="FaunaPool"/>. Provides interactive
    /// in-Scene placement of fauna species:
    /// <list type="bullet">
    /// <item>Drag handles in Scene view to move trajectory endpoints
    /// (<c>leftPoint</c> / <c>rightPoint</c>) and the heron's static
    /// position. Changes are written back into the species SOs via
    /// SerializedObject so they're persisted to disk (visible in
    /// Inspector + git diff).</item>
    /// <item>Preview sprites are spawned in Edit Mode at the
    /// trajectory midpoint (for Traversal) or at staticPosition (for
    /// StaticAppearance) so you can SEE where each species sits
    /// without entering Play Mode.</item>
    /// <item>Inspector "Rebuild Preview" button re-spawns the preview
    /// after any SO change made outside the drag flow.</item>
    /// </list>
    /// <para>
    /// In Play Mode the editor disables itself — runtime spawn is
    /// handled by <c>FaunaPoolBinding</c> as usual.
    /// </para>
    /// </summary>
    [CustomEditor(typeof(FaunaPool))]
    public sealed class FaunaPoolEditor : UnityEditor.Editor
    {
        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                RebuildPreview((FaunaPool)target);
            }
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(
                    "Play Mode actif — l'éditeur de placement est inactif. " +
                    "Sors de Play pour drag les positions.",
                    MessageType.Info);
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Placement Editor", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Sélectionne ce GameObject → des poignées colorées (flèches X/Y) " +
                "apparaissent dans Scene view sur chaque endpoint de trajectoire et " +
                "sur la position du héron. Drag-les à la souris pour repositionner. " +
                "Les modifs sont écrites dans les SO (visible Inspector + git diff). " +
                "Le bouton ci-dessous re-spawn les sprites preview si tu as modifié " +
                "le placement depuis un autre Inspector.",
                MessageType.Info);

            if (GUILayout.Button("Rebuild Preview Now"))
            {
                RebuildPreview((FaunaPool)target);
            }
        }

        private void OnSceneGUI()
        {
            var pool = (FaunaPool)target;
            var placement = GetPlacement(pool);
            if (placement == null) return;

            var speciesList = placement.Species;
            for (int s = 0; s < speciesList.Count; s++)
            {
                var sp = speciesList[s];
                if (sp == null) continue;

                if (sp.MotionMode == FaunaMotionMode.StaticAppearance)
                {
                    DrawStaticHandle(pool, sp);
                }
                else
                {
                    DrawTraversalHandles(pool, sp);
                }
            }
        }

        // ----- Static appearance --------------------------------------------

        private void DrawStaticHandle(FaunaPool pool, FaunaSpeciesDefinition sp)
        {
            Handles.color = ColorForSpecies(sp);
            Vector3 worldPos = new Vector3(sp.StaticPosition.x, sp.StaticPosition.y, 0f);

            Handles.Label(worldPos + Vector3.up * 0.6f, sp.Id + " (static)");

            EditorGUI.BeginChangeCheck();
            Vector3 newPos = Handles.PositionHandle(worldPos, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(sp, "Move " + sp.Id + " static position");
                var so = new SerializedObject(sp);
                so.FindProperty("staticPosition").vector2Value = new Vector2(newPos.x, newPos.y);
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(sp);
                SyncPreviewPosition(pool, sp);
            }

            DrawScaleHandle(pool, sp, worldPos);
        }

        // ----- Traversal trajectories ---------------------------------------

        private void DrawTraversalHandles(FaunaPool pool, FaunaSpeciesDefinition sp)
        {
            Handles.color = ColorForSpecies(sp);
            var trajs = sp.Trajectories;
            for (int t = 0; t < trajs.Count; t++)
            {
                Vector3 left = new Vector3(trajs[t].leftPoint.x, trajs[t].leftPoint.y, 0f);
                Vector3 right = new Vector3(trajs[t].rightPoint.x, trajs[t].rightPoint.y, 0f);

                Handles.DrawLine(left, right);
                string label = sp.Id + "[" + t + "]";
                Handles.Label(left + Vector3.up * 0.3f, label + " L");
                Handles.Label(right + Vector3.up * 0.3f, label + " R");

                EditorGUI.BeginChangeCheck();
                Vector3 newLeft = Handles.PositionHandle(left, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    SetTrajectoryField(sp, t, "leftPoint", new Vector2(newLeft.x, newLeft.y));
                    SyncPreviewPosition(pool, sp);
                }

                EditorGUI.BeginChangeCheck();
                Vector3 newRight = Handles.PositionHandle(right, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    SetTrajectoryField(sp, t, "rightPoint", new Vector2(newRight.x, newRight.y));
                    SyncPreviewPosition(pool, sp);
                }
            }

            // One scale handle per species, anchored at the midpoint of
            // the first trajectory (worldScale is per-species, not per-traj).
            if (trajs.Count > 0)
            {
                Vector2 mid = (trajs[0].leftPoint + trajs[0].rightPoint) * 0.5f;
                DrawScaleHandle(pool, sp, new Vector3(mid.x, mid.y, 0f));
            }
        }

        // ----- Scale handle (shared by both modes) --------------------------

        private void DrawScaleHandle(FaunaPool pool, FaunaSpeciesDefinition sp, Vector3 anchor)
        {
            Handles.color = ColorForSpecies(sp);
            float size = HandleUtility.GetHandleSize(anchor) * 0.5f;
            // Offset above the position handle so the two don't fight.
            Vector3 handlePos = anchor + Vector3.up * size * 3f;

            Handles.DrawDottedLine(anchor, handlePos, 3f);
            Handles.Label(handlePos + Vector3.up * 0.3f,
                sp.Id + " scale: " + sp.WorldScale.ToString("0.00"));

            EditorGUI.BeginChangeCheck();
            float newScale = Handles.ScaleValueHandle(
                sp.WorldScale,
                handlePos,
                Quaternion.identity,
                size,
                Handles.CubeHandleCap,
                0.05f);
            if (EditorGUI.EndChangeCheck())
            {
                SetScale(sp, newScale);
                SyncPreviewScale(pool, sp);
            }
        }

        private static void SetScale(FaunaSpeciesDefinition sp, float value)
        {
            value = Mathf.Max(0.05f, value);
            Undo.RecordObject(sp, "Scale " + sp.Id);
            var so = new SerializedObject(sp);
            so.FindProperty("worldScale").floatValue = value;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(sp);
        }

        private static void SyncPreviewScale(FaunaPool pool, FaunaSpeciesDefinition sp)
        {
            var pooled = pool.PooledSprites;
            float scale = sp.WorldScale;
            for (int i = 0; i < pooled.Count; i++)
            {
                var ps = pooled[i];
                if (ps == null || ps.Species != sp || ps.GameObject == null) continue;
                ps.GameObject.transform.localScale = new Vector3(scale, scale, 1f);
            }
        }

        // ----- Internals ----------------------------------------------------

        private static void SetTrajectoryField(FaunaSpeciesDefinition sp, int trajIdx, string fieldName, Vector2 value)
        {
            Undo.RecordObject(sp, "Move " + sp.Id + " trajectory " + trajIdx + " " + fieldName);
            var so = new SerializedObject(sp);
            var trajs = so.FindProperty("trajectories");
            var traj = trajs.GetArrayElementAtIndex(trajIdx);
            traj.FindPropertyRelative(fieldName).vector2Value = value;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(sp);
        }

        private static FaunaPlacementDefinition GetPlacement(FaunaPool pool)
        {
            var so = new SerializedObject(pool);
            var p = so.FindProperty("placement");
            return p?.objectReferenceValue as FaunaPlacementDefinition;
        }

        /// <summary>
        /// Spawn / re-spawn the preview sprites in Edit Mode. Forces
        /// renderer alpha = 1 (overriding FaunaStaticAppearance.Awake
        /// which sets it to 0) and SetActive(true) on all pooled
        /// GameObjects so the artist can see them.
        /// </summary>
        private static void RebuildPreview(FaunaPool pool)
        {
            if (pool == null) return;
            pool.Rebuild();
            var pooled = pool.PooledSprites;
            for (int i = 0; i < pooled.Count; i++)
            {
                var ps = pooled[i];
                if (ps?.GameObject == null) continue;
                ps.GameObject.SetActive(true);
                ForceAlphaOne(ps.GameObject);
                ApplyPreviewPosition(ps);
            }
        }

        /// <summary>
        /// Move the preview GameObjects belonging to one species to the
        /// new SO values (called after a Handle drag). No Rebuild — keeps
        /// drag latency low.
        /// </summary>
        private static void SyncPreviewPosition(FaunaPool pool, FaunaSpeciesDefinition sp)
        {
            if (pool == null || sp == null) return;
            var pooled = pool.PooledSprites;
            for (int i = 0; i < pooled.Count; i++)
            {
                var ps = pooled[i];
                if (ps == null || ps.Species != sp || ps.GameObject == null) continue;
                ApplyPreviewPosition(ps);
            }
        }

        private static void ApplyPreviewPosition(PooledSprite ps)
        {
            var sp = ps.Species;
            if (sp.MotionMode == FaunaMotionMode.StaticAppearance)
            {
                ps.GameObject.transform.localPosition = new Vector3(
                    sp.StaticPosition.x, sp.StaticPosition.y, 0f);
            }
            else if (ps.TrajectoryIndex < sp.TrajectoryCount)
            {
                var t = sp.Trajectories[ps.TrajectoryIndex];
                Vector2 mid = (t.leftPoint + t.rightPoint) * 0.5f;
                ps.GameObject.transform.localPosition = new Vector3(mid.x, mid.y, 0f);
            }
        }

        private static void ForceAlphaOne(GameObject go)
        {
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) return;
            var c = sr.color;
            c.a = 1f;
            sr.color = c;
        }

        private static Color ColorForSpecies(FaunaSpeciesDefinition sp)
        {
            unchecked
            {
                int h = sp != null && sp.Id != null ? sp.Id.GetHashCode() : 0;
                float r = ((h * 374761393) & 0xFF) / 255f;
                float g = ((h * 668265263) & 0xFF) / 255f;
                float b = ((h * 1274126177) & 0xFF) / 255f;
                return new Color(r, g, b, 0.95f);
            }
        }
    }
}
