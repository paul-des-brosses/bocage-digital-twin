using Bocage.Presentation.Scene.Fauna;
using NUnit.Framework;
using UnityEngine;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// Pure-compute tests for <see cref="FaunaTraversalMotion"/>:
    /// linear X interpolation between trajectory endpoints + direction
    /// mirroring, exercised via <see cref="FaunaTraversalMotion.SamplePositionAt"/>
    /// without running a Unity Update loop. Vertical bob is disabled
    /// (amplitude 0) so the X check stays clean; bob amplitude is a
    /// stylistic add-on, not a load-bearing model variable.
    /// </summary>
    public sealed class FaunaTraversalMotionTests
    {
        [Test]
        public void LinearX_LeftToRight_MatchesLerpEndpoints()
        {
            var go = NewMotionGameObject();
            var motion = go.GetComponent<FaunaTraversalMotion>();
            motion.Configure(new Sprite[0], framesPerSecond: 0f, new TrajectoryDefinition
            {
                leftPoint = new Vector2(-10f, 0f),
                rightPoint = new Vector2(10f, 0f),
                durationSec = 5f,
                verticalBobAmplitude = 0f,
                verticalBobFrequencyHz = 0f,
            });

            var p0 = motion.SamplePositionAt(0f, FaunaTraversalMotion.Direction.LeftToRight);
            var pHalf = motion.SamplePositionAt(2.5f, FaunaTraversalMotion.Direction.LeftToRight);
            var pEnd = motion.SamplePositionAt(5f, FaunaTraversalMotion.Direction.LeftToRight);

            Assert.AreEqual(-10f, p0.x, 0.001f);
            Assert.AreEqual(0f, pHalf.x, 0.001f);
            Assert.AreEqual(10f, pEnd.x, 0.001f);
            Assert.AreEqual(0f, p0.y, 0.001f, "Y is zero when bob amplitude is zero.");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void DirectionReversal_PositionMirroredAcrossLerp()
        {
            var go = NewMotionGameObject();
            var motion = go.GetComponent<FaunaTraversalMotion>();
            motion.Configure(new Sprite[0], framesPerSecond: 0f, new TrajectoryDefinition
            {
                leftPoint = new Vector2(-10f, 0f),
                rightPoint = new Vector2(10f, 0f),
                durationSec = 5f,
                verticalBobAmplitude = 0f,
                verticalBobFrequencyHz = 0f,
            });

            // RtL: at t=0 the bird is at the right endpoint, at t=duration at the left.
            var p0 = motion.SamplePositionAt(0f, FaunaTraversalMotion.Direction.RightToLeft);
            var pEnd = motion.SamplePositionAt(5f, FaunaTraversalMotion.Direction.RightToLeft);

            Assert.AreEqual(10f, p0.x, 0.001f);
            Assert.AreEqual(-10f, pEnd.x, 0.001f);

            Object.DestroyImmediate(go);
        }

        private static GameObject NewMotionGameObject()
        {
            var go = new GameObject("test_traversal_motion");
            go.AddComponent<SpriteRenderer>();
            go.AddComponent<FaunaTraversalMotion>();
            return go;
        }
    }
}
