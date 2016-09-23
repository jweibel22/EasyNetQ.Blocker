using System;
using EasyNetQ.Blocker.Framework.MessageMatching;

namespace EasyNetQ.Blocker.Framework
{
    internal class ActionExecution : IAwaitable
    {
        private readonly IMessageBus actionExecutor;
        private readonly Action action;
        private readonly ActionExecutionConfig config;
        private readonly AsserterFactory asserterFactory;

        public ActionExecution(IMessageBus actionExecutor, Action action, ActionExecutionConfig config, AsserterFactory asserterFactory)
        {
            this.actionExecutor = actionExecutor;
            this.action = action;
            this.config = config;
            this.asserterFactory = asserterFactory;
        }

        public ProcessMatcher<T> Return<T>(ISingleMessageMatcher<T> messageToReturn) where T : class
        {
            return new ProcessMatcher<T>(messageToReturn, actionExecutor, action, config, asserterFactory);
        }

        public VoidProcessMatcher Return()
        {
            return new VoidProcessMatcher(actionExecutor, action, config, asserterFactory);
        }
    }
}