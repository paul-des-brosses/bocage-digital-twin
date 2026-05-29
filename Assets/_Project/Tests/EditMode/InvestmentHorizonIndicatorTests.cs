using Bocage.Indicators.Hero;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// Unit tests for the <see cref="InvestmentHorizonIndicator"/>
    /// accumulator introduced in chantier E5 / ADR #50. The indicator
    /// integrates the per-day share of <c>(realProfit − shadowProfit)</c>
    /// and latches the first day the cumul matches the cumulative
    /// total investment, exposing it as a horizon in years.
    /// </summary>
    public sealed class InvestmentHorizonIndicatorTests
    {
        [Test]
        public void Starts_idle_with_no_cumul_and_no_horizon()
        {
            var indicator = new InvestmentHorizonIndicator();
            Assert.AreEqual(0.0, indicator.CumulativeProfitDeltaEurosPerHa, 1e-9);
            Assert.AreEqual(-1, indicator.HorizonReachedOnDay);
            Assert.IsFalse(indicator.IsHorizonReached);
            Assert.AreEqual(0.0, indicator.HorizonYears, 1e-9);
        }

        [Test]
        public void Update_skipped_while_total_investment_is_zero()
        {
            // No investment yet — even if shadow underperforms by 100 €/ha/yr,
            // the indicator should NOT integrate. Pre-investment days don't
            // count toward the horizon.
            var indicator = new InvestmentHorizonIndicator();
            for (int day = 0; day < 100; day++)
            {
                indicator.Update(realProfitAnnualised: 500.0, shadowProfitAnnualised: 400.0,
                    totalInvestmentEurosPerHa: 0.0, currentDay: day);
            }
            Assert.AreEqual(0.0, indicator.CumulativeProfitDeltaEurosPerHa, 1e-9);
            Assert.IsFalse(indicator.IsHorizonReached);
        }

        [Test]
        public void Update_accumulates_per_day_share_once_investment_lands()
        {
            // Constant divergence of 365 €/ha/yr → 1 €/ha/day. After 50 days
            // the cumul should sit at 50 €/ha (within float noise).
            var indicator = new InvestmentHorizonIndicator();
            const double daily = 365.0; // /365 → 1 €/ha/day
            for (int day = 1; day <= 50; day++)
            {
                indicator.Update(realProfitAnnualised: daily, shadowProfitAnnualised: 0.0,
                    totalInvestmentEurosPerHa: 200.0, currentDay: day);
            }
            Assert.AreEqual(50.0, indicator.CumulativeProfitDeltaEurosPerHa, 1e-6);
            Assert.IsFalse(indicator.IsHorizonReached); // 50 < 200
        }

        [Test]
        public void Horizon_latches_on_first_crossing_day()
        {
            // 365 €/ha/yr divergence → 1 €/ha/day cumul. Investment 150 €/ha
            // → horizon reached on day 150.
            var indicator = new InvestmentHorizonIndicator();
            for (int day = 1; day <= 200; day++)
            {
                indicator.Update(realProfitAnnualised: 365.0, shadowProfitAnnualised: 0.0,
                    totalInvestmentEurosPerHa: 150.0, currentDay: day);
            }
            Assert.IsTrue(indicator.IsHorizonReached);
            Assert.AreEqual(150, indicator.HorizonReachedOnDay);
            Assert.AreEqual(150.0 / InvestmentHorizonIndicator.DaysPerYear,
                indicator.HorizonYears, 1e-9);
        }

        [Test]
        public void Horizon_stays_latched_even_if_cumul_regresses()
        {
            // Reaches the horizon at day 150 with cumul = 150, then real
            // underperforms → cumul drops back below 150. The first-day
            // latch should NOT clear.
            var indicator = new InvestmentHorizonIndicator();
            for (int day = 1; day <= 150; day++)
            {
                indicator.Update(realProfitAnnualised: 365.0, shadowProfitAnnualised: 0.0,
                    totalInvestmentEurosPerHa: 150.0, currentDay: day);
            }
            int latchedDay = indicator.HorizonReachedOnDay;
            Assert.AreEqual(150, latchedDay);

            // Now real falls below shadow — cumul shrinks.
            for (int day = 151; day <= 200; day++)
            {
                indicator.Update(realProfitAnnualised: 0.0, shadowProfitAnnualised: 365.0,
                    totalInvestmentEurosPerHa: 150.0, currentDay: day);
            }
            Assert.IsTrue(indicator.IsHorizonReached, "Horizon flag must stay true once reached");
            Assert.AreEqual(latchedDay, indicator.HorizonReachedOnDay,
                "HorizonReachedOnDay must not move once latched");
        }

        [Test]
        public void Reset_wipes_cumul_and_latched_horizon()
        {
            var indicator = new InvestmentHorizonIndicator();
            for (int day = 1; day <= 200; day++)
            {
                indicator.Update(realProfitAnnualised: 365.0, shadowProfitAnnualised: 0.0,
                    totalInvestmentEurosPerHa: 150.0, currentDay: day);
            }
            Assert.IsTrue(indicator.IsHorizonReached);

            indicator.Reset();
            Assert.AreEqual(0.0, indicator.CumulativeProfitDeltaEurosPerHa, 1e-9);
            Assert.AreEqual(-1, indicator.HorizonReachedOnDay);
            Assert.IsFalse(indicator.IsHorizonReached);
        }
    }
}
