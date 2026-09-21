namespace SGE.Application.Security;

public static class AppRoles
{
    // Os nomes persistidos são os quatro perfis oficiais apresentados na interface.
    // Os nomes das constantes permanecem internos para evitar acoplamento de UI.
    public const string Admin = "Administrador";
    public const string Warehouse = "Almoxarife";
    public const string Buyer = "Compras";
    public const string Finance = "Financeiro";

    public static IReadOnlySet<string> OfficialRoles { get; } =
        new HashSet<string>(StringComparer.Ordinal)
        {
            Admin,
            Warehouse,
            Buyer,
            Finance
        };

    public const string BuyerOrAdmin = Buyer + "," + Admin;
    public const string QuotationManagers = Buyer + "," + Admin;
    public const string QuotationReaders = Buyer + "," + Finance + "," + Admin;
    public const string WarehouseOrAdmin = Warehouse + "," + Admin;
    public const string FinanceOrAdmin = Finance + "," + Admin;
    public const string PurchaseRequestCreators = Warehouse + "," + Buyer + "," + Admin;
    public const string PurchaseRequestReaders = Buyer + "," + Warehouse + "," + Finance + "," + Admin;
    public const string MaterialRequestCreators = Warehouse + "," + Buyer + "," + Admin;
    public const string ServiceRequestCreators = Warehouse + "," + Buyer + "," + Admin;
    public const string SupplierReaders = Buyer + "," + Finance + "," + Admin;
    public const string ReceiptReaders = Warehouse + "," + Finance + "," + Buyer + "," + Admin;
    public const string PaymentReaders = Finance + "," + Admin;
    public const string PurchaseOrderReaders = Buyer + "," + Warehouse + "," + Finance + "," + Admin;

    public static string Normalize(string? role)
    {
        return role?.Trim().ToLowerInvariant() switch
        {
            "administrador" or "admin" => Admin,
            "approver" => Admin,
            "compras" or "buyer" => Buyer,
            "almoxarife" or "warehouse" => Warehouse,
            "financeiro" or "finance" => Finance,
            "solicitante" or "requester" => Warehouse,
            _ => role?.Trim() ?? string.Empty
        };
    }
}
