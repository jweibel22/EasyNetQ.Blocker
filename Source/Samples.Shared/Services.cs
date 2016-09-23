using System;
using System.Collections.Generic;
using EasyNetQ.AutoSubscribe;

namespace Samples.Shared
{
    public class Crm : IConsume<ProductPurchased>
    {
        private readonly IMessagePublisher bus;

        public Crm(IMessagePublisher bus)
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

    public class Accounting : IConsume<ProductPurchased>
    {
        private readonly IMessagePublisher bus;

        public Accounting(IMessagePublisher bus)
        {
            this.bus = bus;
        }

        public void Consume(ProductPurchased message)
        {
            bus.Publish(new CustomerBilled
            {
                CustomerId = message.CustomerId,
                ProductId = message.ProductId,
                Price = 10M
            });
        }
    }

    public class Stock : IConsume<PurchaseProduct>, IConsume<ProductReceivedFromSupplier>
    {
        private readonly IMessagePublisher bus;

        private readonly IDictionary<int, int> stock = new Dictionary<int, int>();

        public Stock(IMessagePublisher bus)
        {
            this.bus = bus;
        }

        public void Consume(PurchaseProduct message)
        {
            if (!stock.ContainsKey(message.ProductId) || stock[message.ProductId] == 0)
            {
                throw new Exception("Item not in stock");
            }

            stock[message.ProductId] -= 1;
            bus.Publish(new ProductPurchased
            {
                CustomerId = message.CustomerId,
                ProductId = message.ProductId
            });
        }

        public void Consume(ProductReceivedFromSupplier message)
        {
            if (!stock.ContainsKey(message.ProductId))
            {
                stock[message.ProductId] = 0;
            }

            stock[message.ProductId] += message.NumberOfItems;
        }
    }
}
