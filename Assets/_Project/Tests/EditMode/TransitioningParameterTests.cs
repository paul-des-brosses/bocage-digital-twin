using Bocage.SimulationCore;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    public sealed class TransitioningParameterTests
    {
        [Test]
        public void InitialValueIsCurrent()
        {
            var p = TransitioningParameter.ForDouble(3.5);
            Assert.AreEqual(3.5, p.Current);
            Assert.IsFalse(p.IsTransitioning);
        }

        [Test]
        public void LinearInterpolationReachesMidPointAtHalfDuration()
        {
            var p = TransitioningParameter.ForDouble(0.0);
            p.SetTarget(10.0, durationInDays: 10);

            for (int i = 0; i < 5; i++) p.Tick();

            Assert.AreEqual(5.0, p.Current, 1e-9);
            Assert.IsTrue(p.IsTransitioning);
        }

        [Test]
        public void ReachesTargetAfterFullDuration()
        {
            var p = TransitioningParameter.ForDouble(2.0);
            p.SetTarget(8.0, durationInDays: 7);

            for (int i = 0; i < 7; i++) p.Tick();

            Assert.AreEqual(8.0, p.Current, 1e-9);
            Assert.IsFalse(p.IsTransitioning);
        }

        [Test]
        public void StaysAtTargetAfterTransitionCompletes()
        {
            var p = TransitioningParameter.ForDouble(0.0);
            p.SetTarget(1.0, durationInDays: 3);

            for (int i = 0; i < 100; i++) p.Tick();

            Assert.AreEqual(1.0, p.Current, 1e-9);
            Assert.IsFalse(p.IsTransitioning);
        }

        [Test]
        public void RetargettingMidTransitionStartsFromCurrent()
        {
            var p = TransitioningParameter.ForDouble(0.0);
            p.SetTarget(10.0, durationInDays: 10);
            for (int i = 0; i < 5; i++) p.Tick();
            // Current ≈ 5.0 — now retarget to 0, should land back at 0 in 10 days.
            p.SetTarget(0.0, durationInDays: 10);

            for (int i = 0; i < 10; i++) p.Tick();

            Assert.AreEqual(0.0, p.Current, 1e-9);
        }

        [Test]
        public void DurationOfOneSnapsToTargetInOneTick()
        {
            var p = TransitioningParameter.ForDouble(0.0);
            p.SetTarget(42.0, durationInDays: 1);

            p.Tick();

            Assert.AreEqual(42.0, p.Current, 1e-9);
            Assert.IsFalse(p.IsTransitioning);
        }
    }
}
