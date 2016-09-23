using System;
using System.Reflection;
using System.Threading.Tasks;
using EasyNetQ.Consumer;
using EasyNetQ.Interception;
using EasyNetQ.Producer;
using EasyNetQ.Topology;

namespace EasyNetQ.Blocker
{
    public class BusThatSendsConfirmations : RabbitAdvancedBus
    {
        public BusThatSendsConfirmations(IConnectionFactory connectionFactory, IConsumerFactory consumerFactory, IEasyNetQLogger logger, 
            IClientCommandDispatcherFactory clientCommandDispatcherFactory, IPublishConfirmationListener confirmationListener, IEventBus eventBus, 
            IHandlerCollectionFactory handlerCollectionFactory, IContainer container, ConnectionConfiguration connectionConfiguration, 
            IProduceConsumeInterceptor produceConsumeInterceptor, IMessageSerializationStrategy messageSerializationStrategy, IConventions conventions, 
            AdvancedBusEventHandlers advancedBusEventHandlers) : base(connectionFactory, consumerFactory, logger, clientCommandDispatcherFactory,
                confirmationListener, eventBus, handlerCollectionFactory, container, connectionConfiguration, produceConsumeInterceptor, messageSerializationStrategy, 
                conventions, advancedBusEventHandlers)
        {
        }

        public override IDisposable Consume(IQueue queue, Func<byte[], MessageProperties, MessageReceivedInfo, Task> onMessage, Action<IConsumerConfiguration> configure)
        {
            var bus = Container.Resolve<IBus>();

            return base.Consume(queue, (bytes, prop, info) =>
            {
                var handlerTask = onMessage(bytes, prop, info);

                handlerTask.ContinueWith(task =>
                {                    
                    bus.Publish(new ConsumerConfirmation
                    {
                        ConsumerName = Assembly.GetEntryAssembly().GetName().Name,
                        MessageCorrelationId = prop.CorrelationId,
                        MessageId = prop.MessageId,
                        MessageType = prop.Type,
                        Succeeded = !task.IsFaulted,
                        ErrorMessage = task.Exception == null ? "" : task.Exception.ToString(),
                        Timestamp = DateTimeOffset.Now
                    });
                });

                return handlerTask;
            }, configure);
        }
    }
}