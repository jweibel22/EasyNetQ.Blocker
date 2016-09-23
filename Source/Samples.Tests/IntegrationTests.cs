using EasyNetQ;
using EasyNetQ.Blocker.Framework;
using NUnit.Framework;
using Samples.Shared;

namespace Samples.Tests
{
    /// <summary>
    /// Runs the test suite as an integration test. Remember to fire up the Samples.Service console before executing the test!
    /// </summary>
    class IntegrationTests : SharedTests
    {
        private IBus bus;

        [SetUp]
        public override void Setup()
        {
            base.Setup();

            bus = RabbitHutch.CreateBus("host=vmdcvppt1");                
            messagePublisher = new RabbitMessagePublisher(bus);

            executor = new ActionExecutor(new RabbitMessageBus(bus, "XXXXXX"));
            executor.LogToConsole = true;
            executor.OnFailedAssertionThrow<AssertionException>();            
        }

        [TearDown]
        public void TearDown()
        {
            bus.Dispose();
        }
    }
}