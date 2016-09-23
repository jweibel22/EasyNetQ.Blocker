using System;
using System.Linq;

namespace EasyNetQ.Blocker.Framework.MessageMatching
{
    public class ProcessMatcher<T> where T : class
    {
        private readonly ISingleMessageMatcher<T> messageToReturn;
        private readonly IMessageBus actionExecutor;
        private readonly Action action;
        private readonly ActionExecutionConfig config;
        private readonly AsserterFactory asserterFactory;

        internal ProcessMatcher(ISingleMessageMatcher<T> messageToReturn, IMessageBus actionExecutor, ActionExecutionConfig config, AsserterFactory asserterFactory)
        {
            this.messageToReturn = messageToReturn;
            this.actionExecutor = actionExecutor;
            this.config = config;
            this.asserterFactory = asserterFactory;
        }

        internal ProcessMatcher(ISingleMessageMatcher<T> messageToReturn, IMessageBus actionExecutor, Action action, ActionExecutionConfig config, AsserterFactory asserterFactory)
        {
            this.messageToReturn = messageToReturn;
            this.actionExecutor = actionExecutor;
            this.action = action;
            this.config = config;
            this.asserterFactory = asserterFactory;
        }

        public T When(params IMessageMatcher[] endOfProcessSignals)
        {
            using (var yy = new BlockingAction(action, endOfProcessSignals.Union(new[] { messageToReturn }), actionExecutor, config.AssertOnMatchers, config.LogToConsole))
            {
                yy.Execute();
            }

            var asserter = asserterFactory.Create();

            if (config.AssertOnMatchers)
            {                
                foreach (var matcher in endOfProcessSignals.Where(m => m.IsMatched))
                {
                    matcher.AssertOk(asserter);
                }

                foreach (var matcher in endOfProcessSignals.Where(m => !m.IsMatched))
                {
                    matcher.AssertOk(asserter);
                }
            }

            messageToReturn.AssertOk(asserter);

            asserter.ThrowIfFailed();

            return messageToReturn.Match;
        }
    }
}
