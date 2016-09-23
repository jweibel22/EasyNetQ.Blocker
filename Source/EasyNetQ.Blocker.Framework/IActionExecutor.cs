using System;

namespace EasyNetQ.Blocker.Framework
{
    /// <summary>
    /// Executes the given action and returns an IAwaitable can be used to wait for results
    /// </summary>
    public interface IActionExecutor
    {
        /// <summary>
        /// Executes the given action and asserts that all matchers are matched
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        IAwaitable Do(Action action);

        /// <summary>
        /// Executes the given action but does not assert that all matchers are matched
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        IAwaitable Try(Action action);
    }
}