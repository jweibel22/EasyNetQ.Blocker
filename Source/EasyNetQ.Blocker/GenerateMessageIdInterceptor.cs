using System;
using EasyNetQ.Interception;

namespace EasyNetQ.Blocker
{
    public class GenerateMessageIdInterceptor : IProduceConsumeInterceptor
    {
        public RawMessage OnProduce(RawMessage rawMessage)
        {
            rawMessage.Properties.MessageId = Guid.NewGuid().ToString();
            return rawMessage;
        }

        public RawMessage OnConsume(RawMessage rawMessage)
        {
            return rawMessage;
        }
    }
}