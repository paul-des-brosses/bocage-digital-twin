using Bocage.Presentation.Bindings;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    public class ConsoleLogBufferTests
    {
        [Test]
        public void NewBuffer_IsEmpty()
        {
            var buffer = new ConsoleLogBuffer(4);
            Assert.AreEqual(0, buffer.Lines.Count);
            Assert.AreEqual(4, buffer.Capacity);
        }

        [Test]
        public void Add_OrdersNewestFirst()
        {
            var buffer = new ConsoleLogBuffer(4);
            buffer.Add("a");
            buffer.Add("b");
            buffer.Add("c");
            Assert.AreEqual(3, buffer.Lines.Count);
            Assert.AreEqual("c", buffer.Lines[0]);
            Assert.AreEqual("b", buffer.Lines[1]);
            Assert.AreEqual("a", buffer.Lines[2]);
        }

        [Test]
        public void Add_BeyondCapacity_DropsOldest()
        {
            var buffer = new ConsoleLogBuffer(2);
            buffer.Add("a");
            buffer.Add("b");
            buffer.Add("c");
            Assert.AreEqual(2, buffer.Lines.Count);
            Assert.AreEqual("c", buffer.Lines[0]);
            Assert.AreEqual("b", buffer.Lines[1]);
        }

        [Test]
        public void Capacity_ClampedToAtLeastOne()
        {
            var buffer = new ConsoleLogBuffer(0);
            Assert.AreEqual(1, buffer.Capacity);
            buffer.Add("a");
            buffer.Add("b");
            Assert.AreEqual(1, buffer.Lines.Count);
            Assert.AreEqual("b", buffer.Lines[0]);
        }

        [Test]
        public void Add_NullBecomesEmptyString()
        {
            var buffer = new ConsoleLogBuffer(2);
            buffer.Add(null);
            Assert.AreEqual(1, buffer.Lines.Count);
            Assert.AreEqual(string.Empty, buffer.Lines[0]);
        }
    }
}
