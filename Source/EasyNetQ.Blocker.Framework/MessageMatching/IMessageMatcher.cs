using System;

namespace EasyNetQ.Blocker.Framework.MessageMatching
{
    public interface IMessageMatcher
    {
        void TryMatch(object msg, MessageProperties properties, TimeSpan timePassed);

        bool IsMatched { get; }

        Type MessageType { get; }

        void AssertOk(Asserter asserter);

        bool IsOk { get; }

        TimeSpan Timeout { get; }

        TimeSpan MatchedAt { get; }
    }
}