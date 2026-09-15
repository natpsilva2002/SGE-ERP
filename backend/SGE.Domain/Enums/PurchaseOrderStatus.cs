namespace SGE.Domain.Enums;

public enum PurchaseOrderStatus
{
    Open = 1,
    Approved = 2,
    Sent = 3,
    PartiallyReceived = 4,
    Received = 5,
    PartiallyCompleted = 6,
    Completed = 7,
    Cancelled = 8,
    WaitingSecondApproval = 9
}
