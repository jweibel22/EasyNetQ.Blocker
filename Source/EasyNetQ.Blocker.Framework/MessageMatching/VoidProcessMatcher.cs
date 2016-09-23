using System;
using System.Linq;

namespace EasyNetQ.Blocker.Framework.MessageMatching
{
    public class VoidProcessMatcher
    {
        private readonly IMessageBus actionExecutor;
        private readonly Action action;
        private readonly ActionExecutionConfig config;
        private readonly AsserterFactory asserterFactory;

        internal VoidProcessMatcher(IMessageBus actionExecutor, ActionExecutionConfig config, AsserterFactory asserterFactory)
        {
            this.actionExecutor = actionExecutor;
            this.config = config;
            this.asserterFactory = asserterFactory;
        }

        internal VoidProcessMatcher(IMessageBus actionExecutor, Action action, ActionExecutionConfig config, AsserterFactory asserterFactory)
        {
            this.actionExecutor = actionExecutor;
            this.action = action;
            this.config = config;
            this.asserterFactory = asserterFactory;
        }

        public void When(params IMessageMatcher[] endOfProcessSignals)
        {
            using (var yy = new BlockingAction(action, endOfProcessSignals, actionExecutor, config.AssertOnMatchers, config.LogToConsole))
            {
                yy.Execute();
            }

            if (config.AssertOnMatchers)
            {
                var asserter = asserterFactory.Create();

                foreach (var matcher in endOfProcessSignals.Where(m => m.IsMatched))
                {
                    matcher.AssertOk(asserter);
                }

                foreach (var matcher in endOfProcessSignals.Where(m => !m.IsMatched))
                {
                    matcher.AssertOk(asserter);
                }

                asserter.ThrowIfFailed();
            }
        }
    }
}