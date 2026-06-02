using System;
using System.Collections.Generic;
using Bocage.Presentation.Scene.Sensors;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// EditMode coverage for the static <see cref="SensorClickedEventBus"/>
    /// (chantier E6 / ADR #53): subscribers are notified with the right
    /// <see cref="SensorType"/>, multiple subscribers fan out, and
    /// unsubscribed handlers no longer fire (important because the bus
    /// is static — leaks would survive Domain Reload-less play sessions).
    /// </summary>
    public sealed class SensorClickedEventBusTests
    {
        // Reset the static event between tests by attaching and detaching
        // every handler we add. If a previous test leaked we'd contaminate
        // the next one; the LocalCleanup helper makes leaks impossible.
        private readonly List<Action<SensorType>> _trackedHandlers = new List<Action<SensorType>>();

        [TearDown]
        public void DetachLeakedHandlers()
        {
            for (int i = 0; i < _trackedHandlers.Count; i++)
                SensorClickedEventBus.SensorClicked -= _trackedHandlers[i];
            _trackedHandlers.Clear();
        }

        private void Subscribe(Action<SensorType> handler)
        {
            SensorClickedEventBus.SensorClicked += handler;
            _trackedHandlers.Add(handler);
        }

        [Test]
        public void RaiseClicked_NotifiesSubscriberWithMatchingSensorType()
        {
            SensorType? received = null;
            Action<SensorType> handler = t => received = t;
            Subscribe(handler);

            SensorClickedEventBus.RaiseClicked(SensorType.WeatherStation);

            Assert.AreEqual(SensorType.WeatherStation, received);
        }

        [Test]
        public void RaiseClicked_FansOutToAllSubscribers()
        {
            int callsA = 0, callsB = 0;
            Action<SensorType> handlerA = _ => callsA++;
            Action<SensorType> handlerB = _ => callsB++;
            Subscribe(handlerA);
            Subscribe(handlerB);

            SensorClickedEventBus.RaiseClicked(SensorType.Piezometer);

            Assert.AreEqual(1, callsA);
            Assert.AreEqual(1, callsB);
        }

        [Test]
        public void RaiseClicked_WithNoSubscribers_IsSafe()
        {
            // Bus must be safe to call when no listener is wired (e.g.
            // a click that arrives before the inspection panel binding is
            // enabled). Any throw here would be a regression.
            Assert.DoesNotThrow(() => SensorClickedEventBus.RaiseClicked(SensorType.EddyTower));
        }

        [Test]
        public void UnsubscribedHandler_NoLongerFires()
        {
            int calls = 0;
            Action<SensorType> handler = _ => calls++;
            SensorClickedEventBus.SensorClicked += handler;
            SensorClickedEventBus.SensorClicked -= handler;

            SensorClickedEventBus.RaiseClicked(SensorType.AcousticSensor);

            Assert.AreEqual(0, calls, "Handlers detached before the raise must not be invoked.");
        }
    }
}
