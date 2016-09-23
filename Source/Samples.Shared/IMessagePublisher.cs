namespace Samples.Shared
{
    public interface IMessagePublisher
    {
        void Publish<T>(T message) where T : class;
    }
}