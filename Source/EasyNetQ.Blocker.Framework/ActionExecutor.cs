using System;

namespace EasyNetQ.Blocker.Framework
{
    public class ActionExecutor : IActionExecutor
    {
        private readonly IMessageBus bus;
        private readonly AsserterFactory asserterFactory;

        public bool LogToConsole { get; set; }

        public ActionExecutor(IMessageBus bus)
        {
            this.bus = bus;
            this.asserterFactory = new AsserterFactory();
        }

        public void OnFailedAssertionThrow<T>() where T: Exception
        {
            asserterFactory.ThrowWhenFailed = s => (T)Activator.CreateInstance(typeof(T), s);
        }

        public IAwaitable Do(Action action)
        {
            return new ActionExecution(bus, action, new ActionExecutionConfig {AssertOnMatchers = true, LogToConsole = LogToConsole}, asserterFactory);
        }

        public IAwaitable Try(Action action)
        {
            return new ActionExecution(bus, action, new ActionExecutionConfig { AssertOnMatchers = false, LogToConsole = LogToConsole }, asserterFactory);
        }
    }
}
