using Bocage.Indicators.Hero;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// Unit tests for the <see cref="InvestmentHorizonIndicator"/> payback
    /// latch (chantier E5 / ADR #50, refondu E8). The indicator no longer
    /// integrates: it is fed the NET tech value (the Hero KPI value =
    /// cumulative operational gain minus total action investment) and
    /// latches the first day the NET reaches break-even, provided an
    /// investment exists to amortise.
    /// </summary>
    public sealed class InvestmentHorizonIndicatorTests
    {
        [Test]
        public void Starts_idle_with_no_horizon()
        {
            var indicator = new InvestmentHorizonIndicator();
            Assert.AreEqual(-1, indicator.HorizonReachedOnDay);
            Assert.IsFalse(indicator.IsHorizonReached);
            Assert.AreEqual(0.0, indicator.HorizonYears, 1e-9);
        }

        [Test]
        public void No_latch_while_total_investment_is_zero()
        {
            // A positive NET with no investment is pure free-slider value,
            // not a payback. The horizon stays « Sans objet ».
            var indicator = new InvestmentHorizonIndicator();
            for (int day = 0; day < 100; day++)
            {
                indicator.Update(netTechValueEurosPerHa: 200.0, totalInvestmentEurosPerHa: 0.0, currentDay: day);
            }
            Assert.IsFalse(indicator.IsHorizonReached);
            Assert.AreEqual(-1, indicator.HorizonReachedOnDay);
        }

        [Test]
        public void No_latch_while_net_stays_negative()
        {
            // Investment exists but the NET never climbs back to 0 → no payback.
            var indicator = new InvestmentHorizonIndicator();
            for (int day = 1; day <= 200; day++)
            {
                indicator.Update(netTechValueEurosPerHa: -50.0, totalInvestmentEurosPerHa: 150.0, currentDay: day);
            }
            Assert.IsFalse(indicator.IsHorizonReached);
        }

        [Test]
        public void Latches_on_first_day_net_reaches_breakeven()
        {
            // NET climbs linearly from -149 to +50, crossing 0 at day 150.
            var indicator = new InvestmentHorizonIndicator();
            for (int day = 1; day <= 200; day++)
            {
                double net = day - 150.0; // 0 on day 150
                indicator.Update(net, totalInvestmentEurosPerHa: 150.0, currentDay: day);
            }
            Assert.IsTrue(indicator.IsHorizonReached);
            Assert.AreEqual(150, indicator.HorizonReachedOnDay);
            Assert.AreEqual(150.0 / InvestmentHorizonIndicator.DaysPerYear, indicator.HorizonYears, 1e-9);
        }

        [Test]
        public void Latches_immediately_when_prebanked_gains_already_cover_the_bill()
        {
            // Days 1-50: NET = +200 but no investment yet → no latch (free
            // slider value, nothing to amortise). Day 51: an investment lands,
            // NET = +50 (gross 200 − bill 150) ≥ 0 → portfolio is already in
            // the black, payback latches on day 51.
            var indicator = new InvestmentHorizonIndicator();
            for (int day = 1; day <= 50; day++)
            {
                indicator.Update(netTechValueEurosPerHa: 200.0, totalInvestmentEurosPerHa: 0.0, currentDay: day);
            }
            Assert.IsFalse(indicator.IsHorizonReached);

            indicator.Update(netTechValueEurosPerHa: 50.0, totalInvestmentEurosPerHa: 150.0, currentDay: 51);
            Assert.IsTrue(indicator.IsHorizonReached);
            Assert.AreEqual(51, indicator.HorizonReachedOnDay);
        }

        [Test]
        public void Horizon_stays_latched_even_if_net_regresses()
        {
            // Reaches break-even at day 150, then the NET drops back below 0
            // (a bad late decision). The first-day latch must NOT clear.
            var indicator = new InvestmentHorizonIndicator();
            for (int day = 1; day <= 150; day++)
            {
                double net = day - 150.0;
                indicator.Update(net, totalInvestmentEurosPerHa: 150.0, currentDay: day);
            }
            Assert.AreEqual(150, indicator.HorizonReachedOnDay);

            for (int day = 151; day <= 200; day++)
            {
                indicator.Update(netTechValueEurosPerHa: -80.0, totalInvestmentEurosPerHa: 150.0, currentDay: day);
            }
            Assert.IsTrue(indicator.IsHorizonReached, "Horizon flag must stay true once reached");
            Assert.AreEqual(150, indicator.HorizonReachedOnDay, "HorizonReachedOnDay must not move once latched");
        }

        [Test]
        public void Reset_wipes_latched_horizon()
        {
            var indicator = new InvestmentHorizonIndicator();
            indicator.Update(netTechValueEurosPerHa: 10.0, totalInvestmentEurosPerHa: 150.0, currentDay: 42);
            Assert.IsTrue(indicator.IsHorizonReached);

            indicator.Reset();
            Assert.AreEqual(-1, indicator.HorizonReachedOnDay);
            Assert.IsFalse(indicator.IsHorizonReached);
        }
    }
}
