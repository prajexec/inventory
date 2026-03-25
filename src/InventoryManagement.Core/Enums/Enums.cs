namespace InventoryManagement.Core.Enums
{
    public enum OrderStatus
    {
        Pending = 0,
        Approved = 1,
        Delivered = 2,
        Cancelled = 3
    }

    public enum SalesOrderStatus
    {
        Draft = 0,
        Confirmed = 1,
        Shipped = 2,
        Completed = 3,
        Returned = 4,
        Cancelled = 5
    }

    public enum MovementType
    {
        StockIn = 0,
        StockOut = 1,
        Adjustment = 2,
        Transfer = 3,
        Return = 4
    }

    public enum PaymentMethod
    {
        Cash = 0,
        BankTransfer = 1,
        CreditCard = 2,
        Cheque = 3,
        Other = 4
    }

    public enum PaymentStatus
    {
        Pending = 0,
        Partial = 1,
        Paid = 2,
        Overdue = 3
    }

    public enum TransferStatus
    {
        Pending = 0,
        InTransit = 1,
        Completed = 2,
        Cancelled = 3
    }

    public enum NotificationType
    {
        LowStock = 0,
        ExpiringProduct = 1,
        PendingPurchaseOrder = 2,
        System = 3
    }
}
