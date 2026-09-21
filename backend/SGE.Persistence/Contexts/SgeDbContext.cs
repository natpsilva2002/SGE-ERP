using Microsoft.EntityFrameworkCore;

using SGE.Domain.Entities.Administration;

using SGE.Domain.Entities.Catalog;
using SGE.Domain.Entities.Purchasing;
using SGE.Domain.Entities.Companies;

namespace SGE.Persistence.Contexts;

public class SgeDbContext : DbContext
{
    public SgeDbContext(DbContextOptions<SgeDbContext> options)
        : base(options)
    {
    }

    // Administration
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();

    // Companies
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Work> Works => Set<Work>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();

    // Catalog
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<UnitOfMeasure> UnitOfMeasures => Set<UnitOfMeasure>();

    // Purchasing
    public DbSet<PurchaseRequest> PurchaseRequests => Set<PurchaseRequest>();
    public DbSet<PurchaseRequestItem> PurchaseRequestItems => Set<PurchaseRequestItem>();

    public DbSet<Quotation> Quotations => Set<Quotation>();
    public DbSet<QuotationItem> QuotationItems => Set<QuotationItem>();

    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<Receipt> Receipts => Set<Receipt>();
    public DbSet<ReceiptItem> ReceiptItems => Set<ReceiptItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<ServiceOrder> ServiceOrders => Set<ServiceOrder>();
    public DbSet<ServiceMeasurement> ServiceMeasurements => Set<ServiceMeasurement>();
    public DbSet<ServiceOrderPayment> ServiceOrderPayments => Set<ServiceOrderPayment>();
    public DbSet<ServiceAdvancePaymentRequest> ServiceAdvancePaymentRequests => Set<ServiceAdvancePaymentRequest>();
    public DbSet<ServiceOrderAttachment> ServiceOrderAttachments => Set<ServiceOrderAttachment>();
    public DbSet<PaymentAttachment> PaymentAttachments => Set<PaymentAttachment>();
    public DbSet<QuotationAttachment> QuotationAttachments => Set<QuotationAttachment>();
    public DbSet<ServiceOrderPaymentAttachment> ServiceOrderPaymentAttachments => Set<ServiceOrderPaymentAttachment>();
    public DbSet<ServiceMeasurementAttachment> ServiceMeasurementAttachments => Set<ServiceMeasurementAttachment>();

    public DbSet<Approval> Approvals => Set<Approval>();
    public DbSet<ApprovalHistory> ApprovalHistories => Set<ApprovalHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SgeDbContext).Assembly);
    }
}
