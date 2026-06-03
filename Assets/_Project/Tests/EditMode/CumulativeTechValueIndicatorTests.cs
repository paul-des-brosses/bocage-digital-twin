using Bocage.Indicators.Hero;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    public class CumulativeTechValueIndicatorTests
    {
        private const double Tol = 1e-9;

        [Test]
        public void NewIndicator_IsZero()
        {
            var acc = new CumulativeTechValueIndicator();
            Assert.AreEqual(0.0, acc.CumulativeEurosPerHa, Tol);
        }

        [Test]
        public void Update_AddsDailyShareOfAnnualisedGap()
        {
            var acc = new CumulativeTechValueIndicator();
            // real beats shadow by 365 €/ha/yr → one day banks 365/365 = 1 €/ha.
            acc.Update(realProfitAnnualised: 1365.0, shadowProfitAnnualised: 1000.0);
            Assert.AreEqual(1.0, acc.CumulativeEurosPerHa, Tol);
        }

        [Test]
        public void Update_Accumulates_AndPlateausWhenGapCloses()
        {
            var acc = new CumulativeTechValueIndicator();
            acc.Update(1365.0, 1000.0); // +1 €/ha
            acc.Update(1365.0, 1000.0); // +1 €/ha → 2
            Assert.AreEqual(2.0, acc.CumulativeEurosPerHa, Tol);
            // Gap closes back to 0 (transient action erased by the rules):
            // the cumulative plateaus, it does NOT collapse.
            acc.Update(1000.0, 1000.0);
            acc.Update(1000.0, 1000.0);
            Assert.AreEqual(2.0, acc.CumulativeEurosPerHa, Tol);
        }

        [Test]
        public void Update_RealEqualsShadow_StaysZero()
        {
            var acc = new CumulativeTechValueIndicator();
            acc.Update(1000.0, 1000.0);
            acc.Update(500.0, 500.0);
            Assert.AreEqual(0.0, acc.CumulativeEurosPerHa, Tol);
        }

        [Test]
        public void Update_RealBelowShadow_GoesNegative()
        {
            var acc = new CumulativeTechValueIndicator();
            acc.Update(635.0, 1000.0); // −365/365 = −1 €/ha
            Assert.AreEqual(-1.0, acc.CumulativeEurosPerHa, Tol);
        }

        [Test]
        public void Reset_WipesToZero()
        {
            var acc = new CumulativeTechValueIndicator();
            acc.Update(1365.0, 1000.0);
            acc.Reset();
            Assert.AreEqual(0.0, acc.CumulativeEurosPerHa, Tol);
        }

        [Test]
        public void Normalize_MapsBoundsAndClamps()
        {
            Assert.AreEqual(0.0, CumulativeTechValueIndicator.Normalize(CumulativeTechValueIndicator.MinEurosPerHectare), Tol);
            Assert.AreEqual(1.0, CumulativeTechValueIndicator.Normalize(CumulativeTechValueIndicator.MaxEurosPerHectare), Tol);
            // 0 €/ha sits at 0.25 on the [-500, +1500] gauge.
            Assert.AreEqual(0.25, CumulativeTechValueIndicator.Normalize(0.0), Tol);
            Assert.AreEqual(0.0, CumulativeTechValueIndicator.Normalize(-9999.0), Tol);
            Assert.AreEqual(1.0, CumulativeTechValueIndicator.Normalize(9999.0), Tol);
        }
    }
}
