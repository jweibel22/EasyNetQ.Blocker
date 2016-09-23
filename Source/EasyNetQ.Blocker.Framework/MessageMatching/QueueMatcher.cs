using System;

namespace EasyNetQ.Blocker.Framework.MessageMatching
{
    public interface IQueueMatcher
    {
        Func<string, bool> QueueNamePredicate { get; }

        string ExchangeName { get; }

        TimeSpan Timeout { get; }
    }

    public class QueueMatcher<T> : IQueueMatcher where T : class
    {
        private readonly Func<string, bool> queueNamePredicate;

        public Func<string, bool> QueueNamePredicate
        {
            get { return queueNamePredicate; }
        }

        public static QueueMatcher<T> Any(Func<string, bool> queueNamePredicate)
        {
            return new QueueMatcher<T>(queueNamePredicate);
        }

        public static QueueMatcher<T> Any()
        {
            return new QueueMatcher<T>(name => true);
        }

        private QueueMatcher(Func<string, bool> queueNamePredicate)
        {
            this.queueNamePredicate = queueNamePredicate;
        }

        public QueueMatcher<T> WaitFor(TimeSpan timeout)
        {
            Timeout = timeout;
            return this;
        }

        public TimeSpan Timeout { get; private set; }

        public string ExchangeName
        {
            get
            {
                var type = typeof (T);
                return type.FullName + ":" + type.Assembly.GetName().Name;
            }
        }
    }
}
