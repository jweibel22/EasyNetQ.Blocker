using System;
using System.Collections.Generic;

namespace EasyNetQ.Blocker.Framework
{
    public interface IMessageListener
    {
        void OnMessage(IMessage<object> msg);

        IEnumerable<Type> InterestedIn { get; }
    }
}