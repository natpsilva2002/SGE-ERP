using Microsoft.EntityFrameworkCore;
using SGE.Application.DTOs.Dashboard;
using SGE.Application.Interfaces.Repositories.Dashboard;
using SGE.Domain.Entities.Purchasing;
using SGE.Domain.Enums;
using SGE.Persistence.Contexts;

namespace SGE.Persistence.Repositories.Dashboard;

public class WorkCostsRepository : IWorkCostsRepository
{
    private readonly SgeDbContext _context;

    public WorkCostsRepository(SgeDbContext context)
    {
        _context = context;
    }

    public async Task<WorkCostsDto> GetAsync(Guid? workId, DateTime? startDate, DateTime? endDate)
    {
        var works = await _context.Works
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Code)
            .Select(x => new DashboardWorkOptionDto { Id = x.Id, Code = x.Code, Name = x.Name })
            .ToListAsync();

        DateTime? start = startDate.HasValue
            ? DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc)
            : null;
        DateTime? endExclusive = endDate.HasValue
            ? DateTime.SpecifyKind(endDate.Value.Date.AddDays(1), DateTimeKind.Utc)
            : null;

        var purchaseOrders = await _context.PurchaseOrders
            .AsNoTracking()
            .AsSplitQuery()
            .Where(x => !x.IsDeleted && x.Status != PurchaseOrderStatus.Cancelled)
            .Include(x => x.Quotation)
                .ThenInclude(x => x.PurchaseRequest)
                    .ThenInclude(x => x.Work)
            .Include(x => x.Supplier)
            .Include(x => x.Items).ThenInclude(x => x.Item)
            .Include(x => x.Payments).ThenInclude(x => x.PaidByUser)
            .Where(x => (!start.HasValue || x.IssueDate >= start.Value) &&
                        (!endExclusive.HasValue || x.IssueDate < endExclusive.Value))
            .ToListAsync();

        var serviceOrders = await _context.ServiceOrders
            .AsNoTracking()
            .AsSplitQuery()
            .Where(x => !x.IsDeleted && x.ExecutionStatus != ServiceOrderExecutionStatus.Cancelled)
            .Include(x => x.Work)
            .Include(x => x.Supplier)
            .Include(x => x.Payments).ThenInclude(x => x.PaidByUser)
            .Include(x => x.Amendments)
            .Where(x => (!start.HasValue || x.CreatedAt >= start.Value) &&
                        (!endExclusive.HasValue || x.CreatedAt < endExclusive.Value))
            .ToListAsync();

        if (workId.HasValue)
        {
            purchaseOrders = purchaseOrders.Where(x => x.Quotation.PurchaseRequest.WorkId == workId.Value).ToList();
            serviceOrders = serviceOrders.Where(x => x.WorkId == workId.Value).ToList();
        }

        var materialRecords = purchaseOrders.Select(x => new CostRecord(
            "Material", x.Id, x.Number, x.SupplierId, SupplierName(x.Supplier),
            x.Quotation.PurchaseRequest.WorkId, x.Quotation.PurchaseRequest.Work.Name,
            x.TotalValue, x.Payments.Sum(p => p.Amount),
            x.Payments.Where(p => InRange(p.PaymentDate, start, endExclusive)).Sum(p => p.Amount),
            x.IssueDate, StatusLabel(x.Status), "/app/ordens-compra/" + x.Id));

        var serviceRecords = serviceOrders.Select(x => new CostRecord(
            "Serviço", x.Id, x.Number, x.SupplierId, SupplierName(x.Supplier), x.WorkId, x.Work.Name,
            x.CurrentContractedValue, x.Payments.Sum(p => p.Amount),
            x.Payments.Where(p => InRange(p.PaymentDate, start, endExclusive)).Sum(p => p.Amount),
            x.CreatedAt, StatusLabel(x.ExecutionStatus), "/app/ordens-servico/" + x.Id));

        var records = materialRecords.Concat(serviceRecords).ToList();
        var totalContracted = records.Sum(x => x.Contracted);

        var evolution = purchaseOrders
            .SelectMany(order => order.Payments.Where(p => InRange(p.PaymentDate, start, endExclusive))
                .Select(p => new WorkCostEvolutionDto { Period = MonthStart(p.PaymentDate), Materials = p.Amount, Total = p.Amount }))
            .Concat(serviceOrders.SelectMany(order => order.Payments.Where(p => InRange(p.PaymentDate, start, endExclusive))
                .Select(p => new WorkCostEvolutionDto { Period = MonthStart(p.PaymentDate), Services = p.Amount, Total = p.Amount })))
            .GroupBy(x => x.Period)
            .OrderBy(x => x.Key)
            .Select(x => new WorkCostEvolutionDto
            {
                Period = x.Key,
                Materials = x.Sum(y => y.Materials),
                Services = x.Sum(y => y.Services),
                Total = x.Sum(y => y.Total)
            })
            .ToList();

        var supplierCosts = records
            .GroupBy(x => new { x.SupplierId, x.Supplier, x.Type })
            .Select(x => new WorkSupplierCostDto
            {
                SupplierId = x.Key.SupplierId,
                Supplier = x.Key.Supplier,
                Type = x.Key.Type,
                Contracted = x.Sum(y => y.Contracted),
                Paid = x.Sum(y => y.PaidInPeriod),
                Pending = x.Sum(y => y.Pending),
                Share = totalContracted <= 0 ? 0 : Math.Round(x.Sum(y => y.Contracted) / totalContracted * 100, 2),
                Route = x.Key.Type == "Material" ? "/app/ordens-compra" : "/app/ordens-servico"
            })
            .OrderByDescending(x => x.Contracted)
            .ToList();

        var topCosts = purchaseOrders
            .SelectMany(order => order.Items.Select(item => new WorkTopCostDto
            {
                Description = item.Item.Description, Type = "Material", Amount = item.TotalValue,
                Route = "/app/ordens-compra/" + order.Id
            }))
            .Concat(serviceOrders.Select(order => new WorkTopCostDto
            {
                Description = order.ServiceDescription, Type = "Serviço", Amount = order.CurrentContractedValue,
                Route = "/app/ordens-servico/" + order.Id
            }))
            .GroupBy(x => new { x.Description, x.Type })
            .Select(x => new WorkTopCostDto
            {
                Description = x.Key.Description, Type = x.Key.Type, Amount = x.Sum(y => y.Amount), Route = x.First().Route
            })
            .OrderByDescending(x => x.Amount)
            .Take(10)
            .ToList();

        var details = records
            .OrderByDescending(x => x.Contracted)
            .Select(x => new WorkCostDetailDto
            {
                Type = x.Type,
                Document = x.Document,
                Supplier = x.Supplier,
                Description = x.Type == "Material" ? "Ordem de compra de materiais" : "Ordem de serviço",
                Contracted = x.Contracted,
                Paid = x.PaidInPeriod,
                Pending = x.Pending,
                Status = x.Status,
                Date = x.Date,
                Route = x.Route
            })
            .ToList();

        var payments = purchaseOrders
            .SelectMany(order => order.Payments.Where(p => InRange(p.PaymentDate, start, endExclusive))
                .Select(p => new WorkPaymentDto
                {
                    Date = p.PaymentDate, Work = order.Quotation.PurchaseRequest.Work.Name, Type = "Material",
                    Document = order.Number, Supplier = SupplierName(order.Supplier), Amount = p.Amount,
                    PaymentMethod = p.PaymentMethod.ToString(), Responsible = UserName(p.PaidByUser),
                    Route = "/app/ordens-compra/" + order.Id
                }))
            .Concat(serviceOrders.SelectMany(order => order.Payments.Where(p => InRange(p.PaymentDate, start, endExclusive))
                .Select(p => new WorkPaymentDto
                {
                    Date = p.PaymentDate, Work = order.Work.Name, Type = "Serviço", Document = order.Number,
                    Supplier = SupplierName(order.Supplier), Amount = p.Amount,
                    PaymentMethod = p.PaymentMethod.ToString(), Responsible = UserName(p.PaidByUser),
                    Route = "/app/ordens-servico/" + order.Id
                })))
            .OrderByDescending(x => x.Date)
            .ToList();

        return new WorkCostsDto
        {
            WorkId = workId,
            StartDate = startDate,
            EndDate = endDate,
            Works = works,
            Summary = new WorkCostsSummaryDto
            {
                Contracted = totalContracted,
                Paid = records.Sum(x => x.PaidInPeriod),
                Pending = records.Sum(x => x.Pending),
                Materials = materialRecords.Sum(x => x.Contracted),
                Services = serviceRecords.Sum(x => x.Contracted)
            },
            Evolution = evolution,
            Suppliers = supplierCosts,
            TopCosts = topCosts,
            Details = details,
            Payments = payments
        };
    }

    private static bool InRange(DateTime date, DateTime? start, DateTime? endExclusive) =>
        (!start.HasValue || date >= start.Value) && (!endExclusive.HasValue || date < endExclusive.Value);

    private static DateTime MonthStart(DateTime date) => new(date.Year, date.Month, 1, 0, 0, 0, DateTimeKind.Utc);

    private static string SupplierName(SGE.Domain.Entities.Companies.Supplier supplier) =>
        string.IsNullOrWhiteSpace(supplier.TradeName) ? supplier.CorporateName : supplier.TradeName;

    private static string UserName(SGE.Domain.Entities.Administration.User? user) =>
        user == null ? "Não informado" : $"{user.FirstName} {user.LastName}".Trim();

    private static string StatusLabel(PurchaseOrderStatus status) => status switch
    {
        PurchaseOrderStatus.Approved => "Aprovada",
        PurchaseOrderStatus.Sent => "Enviada",
        PurchaseOrderStatus.PartiallyReceived => "Recebimento parcial",
        PurchaseOrderStatus.Received => "Recebida integralmente",
        PurchaseOrderStatus.PartiallyCompleted => "Concluída parcialmente",
        PurchaseOrderStatus.Completed => "Concluída",
        _ => status.ToString()
    };

    private static string StatusLabel(ServiceOrderExecutionStatus status) => status switch
    {
        ServiceOrderExecutionStatus.WaitingContract => "Aguardando contrato",
        ServiceOrderExecutionStatus.Released => "Liberada",
        ServiceOrderExecutionStatus.InProgress => "Em execução",
        ServiceOrderExecutionStatus.Completed => "Concluída",
        _ => status.ToString()
    };

    private sealed record CostRecord(
        string Type, Guid Id, string Document, Guid SupplierId, string Supplier, Guid WorkId, string Work,
        decimal Contracted, decimal PaidAllTime, decimal PaidInPeriod, DateTime Date, string Status, string Route)
    {
        public decimal Pending => Math.Max(Contracted - PaidAllTime, 0);
    }
}
