using System;

namespace EasyNetQ.Blocker.Framework
{
    internal class AsserterFactory
    {
        public Func<string, Exception> ThrowWhenFailed { get; set; }

        public AsserterFactory()
        {
            this.ThrowWhenFailed = s => new Exception(s);
        }

        public Asserter Create()
        {
            return new Asserter(ThrowWhenFailed);
        }
    }
}