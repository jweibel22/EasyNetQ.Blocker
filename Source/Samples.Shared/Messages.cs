namespace Samples.Shared
{
    public class ProductReceivedFromSupplier
    {
        public int ProductId { get; set; }

        public int NumberOfItems { get; set; }
    }

    public class PurchaseProduct
    {
        public int ProductId { get; set; }

        public int CustomerId { get; set; }
    }

    public class ProductPurchased
    {
        public int ProductId { get; set; }

        public int CustomerId { get; set; }
    }

    public class CustomerBilled
    {
        public int ProductId { get; set; }

        public int CustomerId { get; set; }

        public decimal Price { get; set; }
    }

    public class ReceiptWasSent
    {
        public int ProductId { get; set; }

        public int CustomerId { get; set; }
    }

}
