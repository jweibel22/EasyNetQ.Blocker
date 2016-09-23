using System;
using EasyNetQ.Blocker.Framework;
using EasyNetQ.Blocker.Framework.MessageMatching;
using NUnit.Framework;
using Samples.Shared;

namespace Samples.Tests
{
    abstract class SharedTests
    {
        protected ActionExecutor executor;        
        protected IMessagePublisher messagePublisher;

        protected string consumerServiceName = "Samples.Service";
        protected int customerId = 1;
        protected int productId = 1;

        private MessageMatcher<CustomerBilled> customerBilled;
        private MessageMatcher<ReceiptWasSent> receiptWasSent;
        private MessageMatcher<ProductPurchased> productPurchased;
        private ConfirmationMatcher<PurchaseProduct> purchaseProductConfirmation;

        public virtual void Setup()
        {
            customerBilled = MessageMatcher<CustomerBilled>.Any(msg => msg.CustomerId == customerId && msg.ProductId == productId);
            receiptWasSent = MessageMatcher<ReceiptWasSent>.Any(msg => msg.CustomerId == customerId && msg.ProductId == productId);
            productPurchased = MessageMatcher<ProductPurchased>.Any(msg => msg.CustomerId == customerId && msg.ProductId == productId);
            purchaseProductConfirmation = ConfirmationMatcher<PurchaseProduct>.Any(consumerServiceName);
        }

        [Test(Description = "Check that customer gets billed when a product is purchased")]
        public void CustomerIsBilled()
        {
            executor
                .Try(() => messagePublisher.Publish(new PurchaseProduct { ProductId = productId, CustomerId = customerId }))
                .Return()
                .When(customerBilled, receiptWasSent.WaitFor(TimeSpan.FromSeconds(5)));

            Assert.IsFalse(customerBilled.IsMatched);
            Assert.IsFalse(receiptWasSent.IsMatched);
        }


        [Test(Description = "Check that purchasing of the product actually succeeds")]
        public void ProductPurchasedEventOccurs()
        {
            executor
                .Try(() => messagePublisher.Publish(new PurchaseProduct { ProductId = productId, CustomerId = customerId }))
                .Return()
                .When(productPurchased.WaitFor(TimeSpan.FromSeconds(5)));

            Assert.IsFalse(productPurchased.IsMatched);
        }

        [Test(Description = "Check that the Stock service is able to handle the PurchaseProduct command")]
        public void ProductPurchaseSucceeds()
        {
            executor
                .Try(() => messagePublisher.Publish(new PurchaseProduct { ProductId = productId, CustomerId = customerId }))
                .Return()
                .When(purchaseProductConfirmation.WaitFor(TimeSpan.FromSeconds(5)));

            Assert.IsTrue(purchaseProductConfirmation.IsMatched);

            var asserter = new Asserter();
            purchaseProductConfirmation.AssertOk(asserter);
            Assert.IsFalse(asserter.IsOk);
            Console.WriteLine(asserter);
        }

        protected void StockUpItem()
        {
            executor
                .Do(() => messagePublisher.Publish(new ProductReceivedFromSupplier { ProductId = productId, NumberOfItems = 1 }))
                .Return()
                .When(ConfirmationMatcher<ProductReceivedFromSupplier>.Any(consumerServiceName).WaitFor(TimeSpan.FromSeconds(5)));
        }

        [Test(Description = "Remember to stock up on the item before purchasing!")]
        public void CustomerIsBilledWhenWeRememberToStockUp()
        {
            StockUpItem();

            executor
                .Do(() => messagePublisher.Publish(new PurchaseProduct { ProductId = productId, CustomerId = customerId }))
                .Return()
                .When(  purchaseProductConfirmation, 
                        productPurchased,
                        receiptWasSent,
                        customerBilled.WaitFor(TimeSpan.FromSeconds(5)));
        }

        [Test(Description = "Make sure that we're charging the customer")]
        public void WeAreChargingTheCustomer()
        {
            StockUpItem();

            executor
                .Do(() => messagePublisher.Publish(new PurchaseProduct { ProductId = productId, CustomerId = customerId }))
                .Return()
                .When(  purchaseProductConfirmation, 
                        productPurchased, 
                        customerBilled.WaitFor(TimeSpan.FromSeconds(5)));

            Assert.That(customerBilled.Match.Price, Is.AtLeast(0M));
        }

        [Test(Description = "Check that the CRM service receives and processes the ProductPurchased event that corresponds to the product purchased matcher")]
        public void CrmProcessesOurSpecificProductPurchasedEvent()
        {
            StockUpItem();

            executor
                .Do(() => messagePublisher.Publish(new PurchaseProduct { ProductId = productId, CustomerId = customerId }))
                .Return()
                .When(  purchaseProductConfirmation, 
                        productPurchased, 
                        ConfirmationMatcher<ProductPurchased>.Single(productPurchased, consumerServiceName).WaitFor(TimeSpan.FromSeconds(5)));
        }

        [Test(Description = "Sanity check that the ProductPurchased event happens before the CustomerBilled event")]
        public void MakeSureCustomerBillingHappensAsAConsequenceOfProductPurchased()
        {
            StockUpItem();

            executor
                .Try(() => messagePublisher.Publish(new PurchaseProduct { ProductId = productId, CustomerId = customerId }))
                .Return()
                .When(  purchaseProductConfirmation, 
                        productPurchased.HappensAfter(customerBilled), 
                        customerBilled.WaitFor(TimeSpan.FromSeconds(5)));

            Assert.That(productPurchased.IsMatched);
            var asserter = new Asserter();
            productPurchased.AssertOk(asserter);
            Assert.IsFalse(asserter.IsOk);
            Console.WriteLine(asserter);
        }
    }

}
