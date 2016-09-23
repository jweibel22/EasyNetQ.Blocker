using System;
using System.Collections.Generic;

namespace EasyNetQ.Blocker.Framework.MessageMatching
{
    public class ConfirmationMatcher<T> : ISingleMessageMatcher<ConsumerConfirmation> where T : class
    {
        private interface IMatchingStrategy
        {
            bool IsMatched(ConsumerConfirmation confirmation);
        }

        private class SimpleMatch : IMatchingStrategy
        {
            private readonly string consumerName;

            public SimpleMatch(string consumerName)
            {
                this.consumerName = consumerName;
            }

            public virtual bool IsMatched(ConsumerConfirmation msg)
            {
                return msg.ConsumerName == consumerName && msg.MessageType.Substring(0, msg.MessageType.IndexOf(':')) == typeof(T).FullName;
            }
        }

        private class MatchByCorrelationId : SimpleMatch
        {
            private readonly string correlationId;

            public MatchByCorrelationId(string consumerName, string correlationId) : base(consumerName)
            {
                this.correlationId = correlationId;
            }

            public override bool IsMatched(ConsumerConfirmation msg)
            {
                return base.IsMatched(msg) && msg.MessageCorrelationId == correlationId;
            }
        }

        private class MatchByMessageMatcher : SimpleMatch
        {
            private readonly ISingleMessageMatcher<T> matcher;

            public MatchByMessageMatcher(string consumerName, ISingleMessageMatcher<T> matcher) : base(consumerName)
            {
                this.matcher = matcher;
            }

            public override bool IsMatched(ConsumerConfirmation msg)
            {
                var result = base.IsMatched(msg) && matcher.IsMatched && matcher.Properties.MessageIdPresent && matcher.Properties.MessageId == msg.MessageId;
                return result;
            }
        }

        private readonly string consumerName;
        private readonly IMatchingStrategy matchingStrategy;
        private readonly string description;
        private readonly IList<IMessageMatcher> happensBefore = new List<IMessageMatcher>();
        private readonly IList<IMessageMatcher> happensAfter = new List<IMessageMatcher>();

        private ConfirmationMatcher(IMatchingStrategy matchingStrategy, string consumerName, string description)
        {
            this.matchingStrategy = matchingStrategy;
            this.consumerName = consumerName;
            this.description = description;
            this.Timeout = TimeSpan.Zero;
        }

        /// <summary>
        /// Matches on the confirmation of any message of the given type from the given consumer
        /// </summary>
        /// <param name="consumerName"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public static ConfirmationMatcher<T> Any(string consumerName, string description = "")
        {
            return new ConfirmationMatcher<T>(new SimpleMatch(consumerName), consumerName, description);
        }

        /// <summary>
        /// Matches on the confirmation of a message of the given type with the given CorrelationId from the given consumer
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="consumerName"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public static ConfirmationMatcher<T> Single(string correlationId, string consumerName, string description = "")
        {
            return new ConfirmationMatcher<T>(new MatchByCorrelationId(consumerName, correlationId), consumerName, description);
        }

        /// <summary>
        /// Matches when a confirmation is received from the given consumer on the message matched by the given matcher
        /// </summary>
        /// <param name="matcher"></param>
        /// <param name="consumerName"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public static ConfirmationMatcher<T> Single(ISingleMessageMatcher<T> matcher, string consumerName, string description = "")
        {
            return new ConfirmationMatcher<T>(new MatchByMessageMatcher(consumerName, matcher), consumerName, description);
        }

        public ConfirmationMatcher<T> WaitFor(TimeSpan timeout)
        {
            Timeout = timeout;
            return this;
        }

        public ConfirmationMatcher<T> HappensBefore(IMessageMatcher matcher)
        {
            happensBefore.Add(matcher);
            return this;
        }

        public ConfirmationMatcher<T> HappensAfter(IMessageMatcher matcher)
        {
            happensAfter.Add(matcher);
            return this;
        }

        public void TryMatch(object m, MessageProperties properties, TimeSpan timePassed)
        {
            var msg = m as ConsumerConfirmation;

            if (msg == null)
            {
                return;
            }

            if (IsMatched)
                return;

            IsMatched = matchingStrategy.IsMatched(msg);

            if (IsMatched)
            {
                Console.WriteLine("Confirmation matched. Consumer: {0}, Message: {1}", consumerName, typeof(T).Name);
                Match = msg;
                MatchedAt = timePassed;
                this.Properties = properties;
            }
        }

        public override string ToString()
        {
            return String.Format("ConsumerName: {1}\r\nMessageType: {0}\r\nDescription: {2}", typeof (T).FullName, consumerName, description);
        }

        public bool IsMatched { get; private set; }

        public TimeSpan Timeout { get; private set; }

        public Type MessageType
        {
            get { return typeof(ConsumerConfirmation); }
        }

        public bool IsOk
        {
            get
            {
                var asserter = new Asserter();
                AssertOk(asserter);
                return asserter.IsOk;
            }
        }

        public void AssertOk(Asserter asserter)
        {
            asserter.IsTrue(IsMatched && (Timeout == TimeSpan.Zero || MatchedAt < Timeout), 
                String.Format("Expected consumer confirmation was not received in time (MatchTime: {0}, Timeout: {1}) :\r\n{2}", MatchedAt, Timeout, ToString()));

            if (IsMatched)
            {
                asserter.IsTrue(Match.Succeeded, String.Format("Consumer has failed\r\n{0}\r\n\r\nThe error was:\r\n{1}", ToString(), Match.ErrorMessage));

                foreach (var matcher in happensBefore)
                {
                    asserter.IsTrue(MatchedAt < matcher.MatchedAt,
                        String.Format("The partial ordering of the message occurences were not as expected. Expected: {0} < {1}", ToString(), matcher.ToString()));
                }

                foreach (var matcher in happensAfter)
                {
                    asserter.IsTrue(matcher.MatchedAt < MatchedAt,
                        String.Format("The partial ordering of the message occurences were not as expected. Expected: {0} < {1}", matcher.ToString(), ToString()));
                }
            }
        }

        public ConsumerConfirmation Match { get; private set; }
        public MessageProperties Properties { get; private set; }
        public TimeSpan MatchedAt { get; private set; }
    }
}