namespace EasyNetQ.Blocker.Framework
{
    public interface IMessageBus
    {        
        void Subscribe(IMessageListener listener);

        void Unsubscribe(IMessageListener listener);
    }
}