using System;
using EasyNetQ.Blocker.Framework;
using EasyNetQ.Blocker.Framework.MessageMatching;
using NUnit.Framework;
using Samples.Shared;

namespace Samples.Tests
{
    /// <summary>
    /// Runs the test suite as a unit test by mocking out the messaging infrastructure
    /// </summary>
    class UnitTests : SharedTests
    {
        private MockBus bus;
        private Crm crm;
        private Accounting accounting;
        private Stock stock;        

        [SetUp]
        public override void Setup()
        {
            base.Setup();

            bus = new MockBus();
            messagePublisher = new MockMessagePublisher(bus);
            executor = new ActionExecutor(bus);
            executor.LogToConsole = true;
            executor.OnFailedAssertionThrow<AssertionException>();

            crm = new Crm(messagePublisher);
            accounting = new Accounting(messagePublisher);
            stock = new Stock(messagePublisher);

            bus.Consume<ProductPurchased>(consumerServiceName, crm);
            bus.Consume<ProductPurchased>(consumerServiceName, accounting);

            bus.Consume<PurchaseProduct>(consumerServiceName, stock);
            bus.Consume<ProductReceivedFromSupplier>(consumerServiceName, stock);
        }

        [Test(Description = "Ensure that only a single ProductPurchased event happens when we purchase one product")]

        public void MakeSureOnlyASingleProductPurchasedEventOccurs()
        {
            var productPurchased = MessageMatcher<ProductPurchased>.Single(msg => msg.CustomerId == customerId && msg.ProductId == productId);

            //oops two stock services are processing orders now!
            bus.Consume<PurchaseProduct>(consumerServiceName, new Stock(messagePublisher));

            StockUpItem();

            executor
                .Try(() => messagePublisher.Publish(new PurchaseProduct { ProductId = productId, CustomerId = customerId }))
                .Return()
                .When(  ConfirmationMatcher<PurchaseProduct>.Any(consumerServiceName), 
                        productPurchased, 
                        MessageMatcher<CustomerBilled>.Any(msg => msg.CustomerId == customerId && msg.ProductId == productId).WaitFor(TimeSpan.FromSeconds(45)));

            Assert.That(productPurchased.IsMatched);
            var asserter = new Asserter();
            productPurchased.AssertOk(asserter);
            Assert.IsFalse(asserter.IsOk);
            Console.WriteLine(asserter);
        }


        [TearDown]
        public void TearDown()
        {
            bus.Dispose();
        }
    }
}