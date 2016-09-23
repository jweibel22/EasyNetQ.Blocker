using System;
using System.Collections.Generic;
using System.Linq;
using EasyNetQ.Topology;

namespace EasyNetQ.Blocker.Framework
{
    public class RabbitMessageBus : IMessageBus, IDisposable
    {
        private class AttachedListener
        {
            public IMessageListener Listener { get; set; }

            public IList<IExchange> Exchanges { get; set; }
        }

        private IQueue queue;
        private IBus bus;
        private IDisposable consumer;
        private AttachedListener _attachedListener;

        public RabbitMessageBus(IBus bus, string queueName)
        {
            this.bus = bus;
            queue = bus.Advanced.QueueDeclare(queueName, autoDelete: true);
            bus.Advanced.QueuePurge(queue);

            Action<IMessage<object>, MessageReceivedInfo> onMessage = (msg, info) =>
            {
                if (_attachedListener != null)
                {
                    _attachedListener.Listener.OnMessage(msg);
                }
            };

            consumer = bus.Advanced.Consume(queue, x => x.Add(onMessage));
        }

        public void Subscribe(IMessageListener listener)
        {
            if (_attachedListener != null)
            {
                throw new Exception("Only 1 listener at a time");
            }

            _attachedListener = new AttachedListener
            {
                Listener = listener,
                Exchanges = listener.InterestedIn
                           .Select(t => bus.Advanced.Conventions.ExchangeNamingConvention(t))
                           .Select(name => bus.Advanced.ExchangeDeclare(name, ExchangeType.Topic))
                           .ToList()
            };

            foreach (var exchange in _attachedListener.Exchanges)
            {
                bus.Advanced.Bind(exchange, queue, "#");
            }
        }

        public void Unsubscribe(IMessageListener listener)
        {
            if (_attachedListener.Listener != listener)
            {
                throw new Exception("Unknown listener");
            }
            
            foreach (var exchange in _attachedListener.Exchanges)
            {
                bus.Advanced.BindingDelete(new Binding(queue, exchange, "#"));
            }

            bus.Advanced.QueuePurge(queue);

            _attachedListener = null;
        }

        public void Dispose()
        {
            consumer.Dispose();
        }
    }
}