using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using EasyNetQ.Blocker.Framework.MessageMatching;
using Newtonsoft.Json;

namespace EasyNetQ.Blocker.Framework
{
    public class BlockingAction: IMessageListener, IDisposable
    {
        private IEnumerable<IMessageMatcher> messageMatchers;
        private bool abortOnFailure;
        private Action action;
        private readonly bool logToConsole;

        IDictionary<Type, ManualResetEvent> waitHandles;
        DateTimeOffset startTime = DateTimeOffset.Now;
        private IMessageBus bus;

        public BlockingAction(Action action, IEnumerable<IMessageMatcher> messageMatchers, IMessageBus bus, bool abortOnFailure, bool logToConsole)
        {
            this.action = action;
            this.abortOnFailure = abortOnFailure;
            this.messageMatchers = messageMatchers;
            this.logToConsole = logToConsole;
            this.bus = bus;

            waitHandles =
                InterestedIn
                    .Select(type => new KeyValuePair<Type, ManualResetEvent>(type, new ManualResetEvent(false)))
                    .ToDictionary(kv => kv.Key, kv => kv.Value);

            bus.Subscribe(this);
        }

        public IEnumerable<IMessageMatcher> MessageMatchers
        {
            get { return messageMatchers; }
            set { messageMatchers = value; }
        }

        public void OnMessage(IMessage<object> msg)
        {
            var now = DateTimeOffset.Now;
            var messageMatcherByType = messageMatchers.Where(mm => mm.MessageType == msg.Body.GetType());

            var waitHandle = waitHandles[msg.Body.GetType()];
            try
            {
                if (logToConsole && msg.Body.GetType() != typeof(ConsumerConfirmation))
                {
                    Console.WriteLine(msg.Body.GetType().FullName + "\r\n" + JsonConvert.SerializeObject(msg.Body, Formatting.Indented));
                }

                foreach (var messageMatcher in messageMatcherByType)
                {
                    messageMatcher.TryMatch(msg.Body, msg.Properties, now.Subtract(startTime));
                }

                if (messageMatcherByType.All(mm => mm.IsMatched))
                {
                    waitHandle.Set();
                }

                if (abortOnFailure && messageMatcherByType.Any(mm => mm.IsMatched && !mm.IsOk))
                {
                    foreach (ManualResetEvent wh in waitHandles.Values)
                    {
                        wh.Set();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Handling of message failed. " + ex.Message);
            }
        }

        public IEnumerable<Type> InterestedIn
        {
            get { return messageMatchers.Select(mm => mm.MessageType).Distinct(); }
        }

        public void Execute()
        {
            if (action != null)
            {
                action();
            }
            if (messageMatchers.Any())
            {
                if (messageMatchers.All(mm => mm.Timeout == TimeSpan.Zero))
                {
                    throw new Exception("At least one matcher must specify a timeout. Set a timeout by using the WaitFor() method on the matcher");
                }

                WaitHandle.WaitAll(waitHandles.Values.ToArray(), messageMatchers.Max(mm => (int) mm.Timeout.TotalMilliseconds));
            }
        }

        public void Dispose()
        {
            bus.Unsubscribe(this);
        }
    }
}