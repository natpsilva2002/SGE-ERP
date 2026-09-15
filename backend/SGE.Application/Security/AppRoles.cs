namespace SGE.Application.Security;

public static class AppRoles
{
    public const string Requester = "Requester";
    public const string Approver = "Approver";
    public const string Buyer = "Buyer";
    public const string Warehouse = "Warehouse";
    public const string Finance = "Finance";
    public const string Admin = "Admin";

    public const string RequesterOrAdmin = Requester + "," + Admin;
    public const string ApproverOrAdmin = Approver + "," + Admin;
    public const string BuyerOrAdmin = Buyer + "," + Admin;
    public const string QuotationManagers = Buyer + "," + Approver + "," + Admin;
    public const string WarehouseOrAdmin = Warehouse + "," + Admin;
    public const string FinanceOrAdmin = Finance + "," + Admin;
    public const string PurchaseRequestCreators = Warehouse + "," + Approver + "," + Admin;
    public const string PurchaseRequestReaders = Requester + "," + Approver + "," + Buyer + "," + Warehouse + "," + Admin;
    public const string MaterialRequestCreators = Warehouse + "," + Approver + "," + Admin;
    public const string ServiceRequestCreators = Approver + "," + Admin;
    public const string SupplierReaders = Buyer + "," + Approver + "," + Warehouse + "," + Finance + "," + Admin;
    public const string ReceiptReaders = Warehouse + "," + Finance + "," + Approver + "," + Buyer + "," + Admin;
    public const string PaymentReaders = Finance + "," + Approver + "," + Admin;
    public const string PurchaseOrderReaders = Requester + "," + Buyer + "," + Approver + "," + Warehouse + "," + Finance + "," + Admin;
}
