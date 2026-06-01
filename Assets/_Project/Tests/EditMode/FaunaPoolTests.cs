using System.Reflection;
using Bocage.Presentation.Scene.Fauna;
using NUnit.Framework;
using UnityEngine;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// EditMode coverage for <see cref="FaunaPool"/>: at Awake, the pool
    /// pre-instantiates exactly one GameObject per (species, trajectory)
    /// pair declared in its <see cref="FaunaPlacementDefinition"/>, all
    /// disabled so the spawn driver can activate them probabilistically
    /// without further <c>Instantiate</c> calls (CLAUDE.md §6).
    /// </summary>
    public sealed class FaunaPoolTests
    {
        [Test]
        public void PreInstantiates_StaticAppearance_OneActiveGameObjectAtStaticPosition()
        {
            var sp = MakeSpecies(id: "test_static", trajectoryCount: 0);
            SetPrivateField(sp, "motionMode", FaunaMotionMode.StaticAppearance);
            SetPrivateField(sp, "staticPosition", new Vector2(3.5f, -1.2f));
            SetPrivateField(sp, "fadeDurationSec", 1.0f);

            var placement = ScriptableObject.CreateInstance<FaunaPlacementDefinition>();
            SetPrivateField(placement, "species", new[] { sp });

            var go = new GameObject("test_static_pool");
            var pool = go.AddComponent<FaunaPool>();
            SetPrivateField(pool, "placement", placement);
            pool.Rebuild();

            Assert.AreEqual(1, pool.PooledSprites.Count,
                "StaticAppearance species creates exactly 1 GameObject regardless of trajectory count.");

            var p = pool.PooledSprites[0];
            Assert.IsNull(p.TraversalMotion);
            Assert.IsNotNull(p.StaticAppearance);
            Assert.IsTrue(p.GameObject.activeSelf,
                "Static-mode GameObject stays active — alpha (not SetActive) controls visibility.");
            Assert.AreEqual(3.5f, p.GameObject.transform.localPosition.x, 0.001f);
            Assert.AreEqual(-1.2f, p.GameObject.transform.localPosition.y, 0.001f);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(placement);
            Object.DestroyImmediate(sp);
        }

        [Test]
        public void PreInstantiates_OneGameObjectPerTrajectory_AllDisabled()
        {
            var species1 = MakeSpecies(id: "test_a", trajectoryCount: 2);
            var species2 = MakeSpecies(id: "test_b", trajectoryCount: 1);
            var placement = ScriptableObject.CreateInstance<FaunaPlacementDefinition>();
            SetPrivateField(placement, "species", new[] { species1, species2 });

            // EditMode does NOT auto-fire the Awake lifecycle on
            // SetActive(true), so we inject the placement via reflection
            // and trigger the build explicitly via the public Rebuild()
            // method (which is also what Awake calls in normal runtime).
            var go = new GameObject("test_fauna_pool");
            var pool = go.AddComponent<FaunaPool>();
            SetPrivateField(pool, "placement", placement);
            pool.Rebuild();

            Assert.AreEqual(3, pool.PooledSprites.Count,
                "Pool size must equal sum of trajectory counts across species (2 + 1).");

            foreach (var p in pool.PooledSprites)
            {
                Assert.IsNotNull(p.GameObject);
                Assert.IsFalse(p.GameObject.activeSelf,
                    "All pre-instantiated sprites must start disabled — activation is the binding's job.");
            }

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(placement);
            Object.DestroyImmediate(species1);
            Object.DestroyImmediate(species2);
        }

        private static FaunaSpeciesDefinition MakeSpecies(string id, int trajectoryCount)
        {
            var sp = ScriptableObject.CreateInstance<FaunaSpeciesDefinition>();
            SetPrivateField(sp, "id", id);
            SetPrivateField(sp, "frames", new Sprite[0]);
            SetPrivateField(sp, "framesPerSecond", 6f);
            SetPrivateField(sp, "appearanceThreshold", 0.3f);
            SetPrivateField(sp, "spawnRateAtMaxBiodiv", 0.1f);
            SetPrivateField(sp, "sortingLayerName", "");
            SetPrivateField(sp, "sortingOrderInLayer", 0);

            var trajectories = new TrajectoryDefinition[trajectoryCount];
            for (int i = 0; i < trajectoryCount; i++)
            {
                trajectories[i] = new TrajectoryDefinition
                {
                    leftPoint = new Vector2(-1f, 0f),
                    rightPoint = new Vector2(1f, 0f),
                    durationSec = 1f,
                    verticalBobAmplitude = 0f,
                    verticalBobFrequencyHz = 0f,
                };
            }
            SetPrivateField(sp, "trajectories", trajectories);
            return sp;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var fi = target.GetType().GetField(fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi, $"Field '{fieldName}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }
    }
}
