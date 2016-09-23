using EasyNetQ;

namespace Samples.Shared
{
    public class RabbitMessagePublisher : IMessagePublisher
    {
        private readonly IBus bus;

        public RabbitMessagePublisher(IBus bus)
        {
            this.bus = bus;
        }

        public void Publish<T>(T message) where T : class
        {
            bus.Publish(message);
        }
    }
}