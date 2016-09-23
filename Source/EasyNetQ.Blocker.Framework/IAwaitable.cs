using EasyNetQ.Blocker.Framework.MessageMatching;

namespace EasyNetQ.Blocker.Framework
{
    public interface IAwaitable
    {
        ProcessMatcher<T> Return<T>(ISingleMessageMatcher<T> messageToReturn) where T : class;
        VoidProcessMatcher Return();        
    }
}