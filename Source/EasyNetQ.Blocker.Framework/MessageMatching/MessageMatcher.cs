using System;
using System.Collections.Generic;

namespace EasyNetQ.Blocker.Framework.MessageMatching
{
    public class MessageMatcher<T> : ISingleMessageMatcher<T> where T : class
    {
        private readonly Func<T, bool> selector;

        private readonly bool failOnMultipleMatches;
        

        private readonly IList<IMessageMatcher> happensBefore = new List<IMessageMatcher>();
        private readonly IList<IMessageMatcher> happensAfter = new List<IMessageMatcher>();

        private MessageMatcher(Func<T, bool> selector, bool failOnMultipleMatches)
        {
            this.selector = selector;
            this.failOnMultipleMatches = failOnMultipleMatches;        
            this.Timeout = TimeSpan.Zero;
        }

        /// <summary>
        /// Matches any message of the given type that satisifies the given predicate
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static MessageMatcher<T> Any(Func<T, bool> predicate)
        {
            return new MessageMatcher<T>(predicate, false);
        }

        public static MessageMatcher<T> Single(Func<T, bool> predicate)
        {
            return new MessageMatcher<T>(predicate, true);
        }

        /// <summary>
        /// Matches any message of the given type
        /// </summary>
        /// <returns></returns>
        public static MessageMatcher<T> Any()
        {
            return new MessageMatcher<T>(msg => true, false);
        }

        public static MessageMatcher<T> Single()
        {
            return new MessageMatcher<T>(msg => true, true);
        }

        public MessageMatcher<T> WaitFor(TimeSpan timeout)
        {
            Timeout = timeout;
            return this;
        }

        public MessageMatcher<T> HappensBefore(IMessageMatcher matcher)
        {
            happensBefore.Add(matcher);
            return this;
        }

        public MessageMatcher<T> HappensAfter(IMessageMatcher matcher)
        {
            happensAfter.Add(matcher);
            return this;
        }

        public T Match { get; private set; }

        public MessageProperties Properties { get; private set; }

        public bool IsOk
        {
            get
            {
                var asserter = new Asserter();
                AssertOk(asserter);
                return asserter.IsOk;
            }
        }

        public TimeSpan Timeout { get; private set; }

        public void TryMatch(object msg, MessageProperties properties, TimeSpan timePassed)
        {
            var message = msg as T;

            if (message == null)
            {
                return;
            }

            if (IsMatched)
            {
                if (selector(message))
                {
                    Console.WriteLine("Message matched: {0}", typeof(T).Name);
                    NumberOfMatches++;
                }
            }                
            else if (selector(message))
            {
                Console.WriteLine("Message matched: {0}", typeof(T).Name);
                Match = message;
                Properties = properties;
                MatchedAt = timePassed;
                IsMatched = true;
                NumberOfMatches++;
            }
        }

        public bool IsMatched { get; private set; }

        public int NumberOfMatches { get; private set; }

        public TimeSpan MatchedAt { get; private set; }

        public Type MessageType
        {
            get { return typeof (T); }
        }

        public void AssertOk(Asserter asserter)
        {
            asserter.IsTrue(IsMatched && (Timeout == TimeSpan.Zero || MatchedAt < Timeout), "Expected message was not received in time:\\r\\n " + ToString());

            if (IsMatched && failOnMultipleMatches)
            {
                asserter.IsTrue(NumberOfMatches == 1, NumberOfMatches + " matching messages were found, but only 1 was expected:\r\n " + ToString());    
            }

            foreach (var matcher in happensBefore)
            {
                asserter.IsTrue(MatchedAt < matcher.MatchedAt, String.Format("The partial ordering of the message occurences were not as expected. Expected: {0} < {1}", ToString(), matcher.ToString()));
            }

            foreach (var matcher in happensAfter)
            {
                asserter.IsTrue(matcher.MatchedAt < MatchedAt, String.Format("The partial ordering of the message occurences were not as expected. Expected: {0} < {1}", matcher.ToString(), ToString()));
            }
        }
    }
}