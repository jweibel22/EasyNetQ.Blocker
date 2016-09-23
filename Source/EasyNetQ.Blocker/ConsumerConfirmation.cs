using System;

namespace EasyNetQ.Blocker
{
    public class ConsumerConfirmation
    {
        public string ConsumerName { get; set; }

        public string MessageId { get; set; }

        public string MessageCorrelationId { get; set; }

        public string MessageType { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public string ErrorMessage { get; set; }

        public bool Succeeded { get; set; }
    }
}
