# EasyNetQ.Blocker
Framework for writing deterministic system tests with EasyNetQ

[![NuGet status](https://img.shields.io/nuget/v/EasyNetQ.Blocker.Framework.png?maxAge=2592000)](https://www.nuget.org/packages/EasyNetQ.Blocker.Framework)

The purpose of this framework is to make it easier to write tests that validates the behaviour of a deployed distributed asynchronous system. With the recent rise in popularity of the micro services architecture, this practice is becoming increasingly more important.

As a general note, I don't recommend testing business logic with these kinds of tests. Business logic and other behaviour that is local to a specific service belongs inside that service. Besides, testing all possible combinations of state and workflows across all services is simply not feasible.  The purpose of the system tests is to validate that the infrastructure is working and that services can interact. Thus, usually only the "sunshine scenarios" are tested with a system wide test. Please read the WIKI for a more in depth discussion about the intentions behind this repo

##Quick intro

First spin up a new bus and connect it with an ActionExecutor:

            bus = RabbitHutch.CreateBus("host=localhost");                
            executor = new ActionExecutor(new RabbitMessageBus(bus, "MyQueueName"));
            
            //Throw NUnit's AssertionException when assertions fail           
            executor.OnFailedAssertionThrow<AssertionException>(); 
            
Next, execute an action and block until certain messages are detected

      executor
            .Do(() => bus.Publish(new PurchaseProduct
            {
                ProductId = productId,
                CustomerId = customerId
            }))
            .Return()
            .When(MessageMatcher<CustomerBilled>.Any(),
                  MessageMatcher<ReceiptWasSent>.Any().WaitFor(TimeSpan.FromSeconds(5)));

If the messages are not detected within the given timeout, the NUnit test will fail. For more fine grained control you can do your assertions yourself by using the Try() method instead of the Do() method

            executor
                .Try(() => messagePublisher.Publish(new PurchaseProduct
                {
                    ProductId = productId,
                    CustomerId = customerId
                }))
                .Return()
                .When(customerBilled, receiptWasSent.WaitFor(TimeSpan.FromSeconds(5)));

            Assert.IsFalse(customerBilled.IsMatched, "the message was not expected");
            Assert.IsFalse(receiptWasSent.IsMatched, "the message was not expected");

You can also add assertions on the messages that were detected

      var customerBilled = MessageMatcher<CustomerBilled>.Any();
      executor
            .Do(() => bus.Publish(new PurchaseProduct
            {
                ProductId = productId,
                CustomerId = customerId
            }))
            .Return()
            .When(customerBilled,
                  MessageMatcher<ReceiptWasSent>.Any().WaitFor(TimeSpan.FromSeconds(5)));

      Assert.That(customerBilled.Match.Price, Is.Equal.To(10));

It's possible to assert that a given service received and processed a given message

            executor
                .Do(() => bus.Publish(new PurchaseProduct { 
                      ProductId = productId, 
                      CustomerId = customerId 
                 }))
                .Return()
                .When(ConfirmationMatcher<PurchaseProduct>.Any("MyService").WaitFor(TimeSpan.FromSeconds(5)));
                
This will fail the NUnit test if the given service never received the message or if processing of the message failed. The error and stack trace from the service will be shown in the "failed" message  

It's possible to mock out a service. Declare the service as always with EasyNetQ:

    public class Crm : IConsume<ProductPurchased>
    {
        private readonly IMockBus bus;

        public Crm(IMockBus bus)
        {
            this.bus = bus;
        }

        public void Consume(ProductPurchased message)
        {
            bus.Publish(new ReceiptWasSent
            {
                CustomerId = message.CustomerId,
                ProductId = message.ProductId
            });
        }
    }

and connect your action executor to a MockBus

            bus = new MockBus();
            executor = new ActionExecutor(bus);
            executor.OnFailedAssertionThrow<AssertionException>();
            
            crm = new Crm(bus);
            bus.Consume<ProductPurchased>("crm", crm);
            
            
Please see the Samples projects from the source code for more in depth examples            
