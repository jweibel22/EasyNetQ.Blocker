using System;
using System.Collections.Generic;
using System.Linq;

namespace EasyNetQ.Blocker.Framework.MessageMatching
{
    public class AggregateMatcher<T> : IMessageMatcher where T : class
    {
        private readonly IEnumerable<ISingleMessageMatcher<T>> matchers;

        public AggregateMatcher(IEnumerable<ISingleMessageMatcher<T>> matchers)
        {
            this.matchers = matchers;
        }

        public void TryMatch(object msg, MessageProperties properties, TimeSpan timePassed)
        {
            var unmatched = matchers.FirstOrDefault(m => !m.IsMatched);

            if (unmatched != null)
            {
                unmatched.TryMatch(msg, properties, timePassed);    
            }            
        }

        public bool IsMatched
        {
            get { return matchers.All(m => m.IsMatched); }
        }

        public Type MessageType
        {
            get { return matchers.First().MessageType; }
        }

        public void AssertOk(Asserter asserter)
        {
            foreach (var matcher in matchers)
            {
                matcher.AssertOk(asserter);
            }
        }

        public bool IsOk
        {
            get { return matchers.All(m => m.IsOk); }
        }

        public IEnumerable<T> Matches
        {
            get { return matchers.Select(m => m.Match); }
        }

        public TimeSpan Timeout 
        {
            get
            {
                return matchers.Max(m => m.Timeout);
            }
        }

        public TimeSpan MatchedAt
        {
            get { return matchers.Max(m => m.MatchedAt); }
        }
    }
}