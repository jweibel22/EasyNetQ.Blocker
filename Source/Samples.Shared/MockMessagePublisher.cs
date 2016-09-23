using EasyNetQ.Blocker.Framework;

namespace Samples.Shared
{
    public class MockMessagePublisher : IMessagePublisher
    {
        private MockBus bus;

        public MockMessagePublisher(MockBus bus)
        {
            this.bus = bus;
        }

        public void Publish<T>(T message) where T : class
        {
            bus.Publish(message);
        }
    }
}