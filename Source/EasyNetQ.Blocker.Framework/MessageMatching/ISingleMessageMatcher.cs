namespace EasyNetQ.Blocker.Framework.MessageMatching
{
    public interface ISingleMessageMatcher<out T> : IMessageMatcher where T : class
    {
        T Match { get; }

        MessageProperties Properties { get; }     
    }
}