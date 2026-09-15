namespace SGE.Domain.Enums;

public enum PurchaseRequestStatus
{
    Draft = 1,
    WaitingQuotation = 2,
    WaitingApproval = 3,
    Approved = 4,
    Rejected = 5,
    PurchaseOrderGenerated = 6,
    Finished = 7,
    Cancelled = 8,
    QuotationInProgress = 9
}
