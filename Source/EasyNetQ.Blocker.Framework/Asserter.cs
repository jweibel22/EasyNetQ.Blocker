using System;
using System.Collections.Generic;
using System.Linq;

namespace EasyNetQ.Blocker.Framework
{
    public class Asserter
    {
        private readonly Func<string, Exception> _throwWhenFailed;
        private IList<string> errors = new List<string>();

        public Asserter() : this(s => new Exception(s))
        {
            
        }

        public Asserter(Func<string, Exception> throwWhenFailed)
        {
            this._throwWhenFailed = throwWhenFailed;
        }

        public void IsTrue(bool condition, string message)
        {
            if (!condition)
            {
                errors.Add(message);
            }
        }

        public bool IsOk
        {
            get { return !errors.Any(); }
        }

        public void ThrowIfFailed()
        {
            if (!IsOk)
            {
                throw _throwWhenFailed(errors.First());
            }
        }

        public override string ToString()
        {
            return String.Join("\r\n", errors);
        }
    }
}