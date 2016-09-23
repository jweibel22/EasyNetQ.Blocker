using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using EasyNetQ.AutoSubscribe;

namespace EasyNetQ.Blocker.Framework
{
    public class MockBus : IMessageBus, IDisposable
    {
        private class ConsumerAndName
        {
            public object Consumer { get; set; }

            public string Name { get; set; }
        }

        private class MessageWithProperties
        {
            public object Message { get; set; }

            public MessageProperties Properties { get; set; }
        }

        private readonly IList<ConsumerAndName> consumers = new List<ConsumerAndName>();
        private readonly TypeNameSerializer typeNameSerializer = new TypeNameSerializer();

        private readonly BlockingCollection<MessageWithProperties> messages = new BlockingCollection<MessageWithProperties>();

        private readonly Thread worker;
        private bool isDisposed;

        private readonly IList<IMessageListener> listeners = new List<IMessageListener>();

        public MockBus()
        {
            worker = new Thread(Run);
            worker.Start();
        }

        private void Run()
        {
            foreach (var message in messages.GetConsumingEnumerable())
            {
                if (isDisposed)
                {
                    break;
                }

                Handle(message);
            }
        }

        public void Publish<T>(T message) where T : class
        {
            var properties = new MessageProperties();
            properties.Type = typeNameSerializer.Serialize(message.GetType());
            properties.MessageId = Guid.NewGuid().ToString();
            //TODO: set correlation Id

            messages.Add(new MessageWithProperties
            {
                Message = message,
                Properties = properties
            });
        }

        private void Handle(MessageWithProperties messageWithProperties)
        {
            lock (this)
            {
                var handleMethod = GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).Single(x => x.Name == "InternalHandle").MakeGenericMethod(messageWithProperties.Message.GetType());

                handleMethod.Invoke(this, new object[] {messageWithProperties.Message, messageWithProperties.Properties });
            }
        }

        private void InternalHandle<T>(T message, MessageProperties properties) where T : class
        {
            var handlerType = typeof(IConsume<>).MakeGenericType(message.GetType());

            var subscribers = consumers.Where(x => handlerType.IsAssignableFrom(x.Consumer.GetType())).ToList();

            foreach (var consumerAndName in subscribers)
            {
                var consumer = (IConsume<T>)consumerAndName.Consumer;

                Exception exception = null;

                try
                {
                    consumer.Consume(message);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }

                Publish(new ConsumerConfirmation
                {
                    ConsumerName = consumerAndName.Name,
                    ErrorMessage = exception != null ? exception.ToString() : "",
                    MessageType = properties.Type,
                    Succeeded = exception == null,
                    Timestamp = DateTimeOffset.Now,
                    MessageCorrelationId = properties.CorrelationId,
                    MessageId = properties.MessageId
                });
            }

            var msg = new Message<T>(message, properties);

            foreach (var listener in listeners)
            {
                if (listener.InterestedIn.Contains(message.GetType()))
                {
                    listener.OnMessage(msg);
                }                
            }
        }

        public void Consume<T>(string name, IConsume<T> consumer) where T : class
        {
            lock(this)
            {
                if (!consumers.Any(c => c.Consumer == consumer))
                {
                    consumers.Add(new ConsumerAndName
                    {
                        Name = name,
                        Consumer = consumer
                    });
                }
            }            
        }        

        public void Subscribe(IMessageListener listener)
        {
            lock (this)
            {
                listeners.Add(listener);
            }
        }

        public void Unsubscribe(IMessageListener listener)
        {
            lock (this)
            {
                listeners.Remove(listener);
            }
        }

        public void Dispose()
        {
            isDisposed = true;
            messages.CompleteAdding();
            worker.Join();
        }
    }
}