using Microsoft.EntityFrameworkCore;
using SGE.Application.DTOs.Dashboard;
using SGE.Application.Interfaces.Repositories.Dashboard;
using SGE.Application.Security;
using SGE.Domain.Entities.Purchasing;
using SGE.Domain.Enums;
using SGE.Persistence.Contexts;

namespace SGE.Persistence.Repositories.Dashboard;

public class DashboardRepository : IDashboardRepository
{
    private readonly SgeDbContext _context;

    public DashboardRepository(SgeDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardDto> GetAsync(string role)
    {
        var requests = await _context.PurchaseRequests
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Include(x => x.RequestedByUser)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        var quotations = await _context.Quotations
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Include(x => x.PurchaseRequest)
                .ThenInclude(x => x.RequestedByUser)
            .OrderByDescending(x => x.QuotationDate)
            .ToListAsync();

        var purchaseOrders = await _context.PurchaseOrders
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Include(x => x.Quotation)
                .ThenInclude(x => x.PurchaseRequest)
                    .ThenInclude(x => x.RequestedByUser)
            .Include(x => x.Supplier)
            .Include(x => x.Items)
                .ThenInclude(x => x.Item)
            .OrderByDescending(x => x.IssueDate)
            .ToListAsync();

        var serviceOrders = await _context.ServiceOrders
            .AsNoTracking()
            .AsSplitQuery()
            .Where(x => !x.IsDeleted)
            .Include(x => x.PurchaseRequest)
                .ThenInclude(x => x.RequestedByUser)
            .Include(x => x.Work)
            .Include(x => x.Supplier)
            .Include(x => x.Measurements)
            .Include(x => x.Payments)
                .ThenInclude(x => x.PaidByUser)
            .Include(x => x.AdvancePaymentRequests)
                .ThenInclude(x => x.Payments)
            .Include(x => x.AdvancePaymentRequests)
                .ThenInclude(x => x.RequestedByUser)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        var receipts = await _context.Receipts
            .AsNoTracking()
            .Include(x => x.ReceivedByUser)
            .Include(x => x.PurchaseOrder)
            .OrderByDescending(x => x.ReceiptDate)
            .Take(12)
            .ToListAsync();

        var payments = await _context.Payments
            .AsNoTracking()
            .Include(x => x.PaidByUser)
            .Include(x => x.PurchaseOrder)
            .OrderByDescending(x => x.PaymentDate)
            .Take(12)
            .ToListAsync();

        var isAdmin = role == AppRoles.Admin;
        var isBuyer = role == AppRoles.Buyer;
        var isWarehouse = role == AppRoles.Warehouse;
        var isFinance = role == AppRoles.Finance;

        var cards = new List<DashboardCardDto>();
        var pending = new List<DashboardEntryDto>();

        var requestsInProgress = requests.Count(x => x.Status != PurchaseRequestStatus.Finished &&
            x.Status != PurchaseRequestStatus.Cancelled && x.Status != PurchaseRequestStatus.Rejected);
        var quotationsWaitingApproval = quotations.Count(x =>
            x.Status == QuotationStatus.WaitingApproval ||
            x.Status == QuotationStatus.WaitingSecondApproval);
        var openPurchaseOrders = purchaseOrders.Count(x => x.Status != PurchaseOrderStatus.Cancelled &&
            x.Status != PurchaseOrderStatus.Completed && x.Status != PurchaseOrderStatus.Received);
        var serviceInExecution = serviceOrders.Count(x =>
            x.ExecutionStatus == ServiceOrderExecutionStatus.Released ||
            x.ExecutionStatus == ServiceOrderExecutionStatus.InProgress);

        if (isAdmin)
        {
            cards.Add(Card("requests", "Solicitações em andamento", requestsInProgress, "/app/solicitacoes"));
            cards.Add(Card("quotation-approval", "Cotações aguardando aprovação", quotationsWaitingApproval, "/app/cotacoes"));
            cards.Add(Card("purchase-orders", "Ordens de compra em aberto", openPurchaseOrders, "/app/ordens-compra"));
            cards.Add(Card("service-orders", "Ordens de serviço em execução", serviceInExecution, "/app/ordens-servico"));
            AddFinancialCards(cards, purchaseOrders, serviceOrders);
        }
        else if (isBuyer)
        {
            var waitingQuotation = requests.Count(x => x.Status == PurchaseRequestStatus.WaitingQuotation);
            var quotationsInProgress = quotations.Count(x => x.Status == QuotationStatus.Draft) +
                requests.Count(x => x.Status == PurchaseRequestStatus.QuotationInProgress);

            cards.Add(Card("waiting-quotation", "Solicitações aguardando cotação", waitingQuotation, "/app/solicitacoes"));
            cards.Add(Card("quotations-in-progress", "Cotações em andamento", quotationsInProgress, "/app/cotacoes"));
            cards.Add(Card("quotation-approval", "Cotações aguardando aprovação", quotationsWaitingApproval, "/app/cotacoes"));
            cards.Add(Card("purchase-orders", "Ordens de compra geradas", purchaseOrders.Count, "/app/ordens-compra"));

            pending.AddRange(requests.Where(x => x.Status == PurchaseRequestStatus.WaitingQuotation)
                .Take(8).Select(x => Entry("Solicitação sem cotação", x.Number, x.Description,
                    x.CreatedAt, UserName(x.RequestedByUser), "Aguardando cotação", "/app/solicitacoes/" + x.Id)));
            pending.AddRange(quotations.Where(x => x.Status == QuotationStatus.Draft)
                .Take(8).Select(x => Entry("Cotação editável", x.Number, x.PurchaseRequest.Description,
                    x.QuotationDate, UserName(x.PurchaseRequest.RequestedByUser), "Em andamento", "/app/cotacoes/" + x.Id)));
        }
        else if (isWarehouse)
        {
            var awaitingReceipt = purchaseOrders.Count(HasPendingReceipt);
            var partialReceipts = purchaseOrders.Count(x => x.Status == PurchaseOrderStatus.PartiallyReceived);
            var divergence = purchaseOrders.Count(HasReceivingDivergence);

            cards.Add(Card("requests", "Solicitações em andamento", requestsInProgress, "/app/solicitacoes"));
            cards.Add(Card("awaiting-receipt", "OCs aguardando recebimento", awaitingReceipt, "/app/ordens-compra"));
            cards.Add(Card("partial-receipts", "Recebimentos parciais", partialReceipts, "/app/recebimentos"));
            cards.Add(Card("receiving-divergence", "Recebimentos com divergência", divergence, "/app/recebimentos"));

            pending.AddRange(purchaseOrders.Where(HasPendingReceipt).Take(8)
                .Select(x => Entry("OC aguardando recebimento", x.Number,
                    x.Supplier?.TradeName ?? x.Supplier?.CorporateName ?? "Fornecedor não informado",
                    x.IssueDate, null, "Aguardando recebimento", "/app/ordens-compra/" + x.Id)));
            pending.AddRange(purchaseOrders.Where(HasReceivingDivergence).Take(8)
                .Select(x => Entry("Recebimento com divergência", x.Number,
                    x.Supplier?.TradeName ?? x.Supplier?.CorporateName ?? "Fornecedor não informado",
                    x.IssueDate, null, "Divergência", "/app/ordens-compra/" + x.Id)));
        }
        else if (isFinance)
        {
            var purchasePending = purchaseOrders.Where(x => x.AmountPending > 0).Sum(x => x.AmountPending);
            var servicePending = serviceOrders.Where(x => x.AmountPending > 0).Sum(x => x.AmountPending);
            var paid = purchaseOrders.Sum(x => x.AmountPaid) + serviceOrders.Sum(x => x.AmountPaid);
            var pendingPayments = purchaseOrders.Count(x => x.AmountPending > 0) +
                serviceOrders.Count(x => x.AmountPending > 0);
            var measuredAvailable = serviceOrders.Sum(AvailableMeasuredAmount);
            var approvedAdvances = serviceOrders.SelectMany(x => x.AdvancePaymentRequests)
                .Count(x => x.Status == ServiceAdvancePaymentStatus.Approved &&
                    x.Payments.Sum(payment => payment.Amount) < x.Amount);

            cards.Add(Card("pending-payments", "Pagamentos pendentes", pendingPayments, "/app/financeiro"));
            cards.Add(MoneyCard("pending-value", "Valor pendente", purchasePending + servicePending, "/app/financeiro"));
            cards.Add(MoneyCard("paid-value", "Valor pago", paid, "/app/financeiro"));
            cards.Add(MoneyCard("measured-available", "Valores disponíveis de medições", measuredAvailable, "/app/ordens-servico"));
            cards.Add(Card("approved-advances", "Antecipações aprovadas aguardando pagamento", approvedAdvances, "/app/ordens-servico"));

            pending.AddRange(purchaseOrders.Where(x => x.AmountPending > 0).Take(8)
                .Select(x => Entry("OC aguardando pagamento", x.Number,
                    x.Supplier?.TradeName ?? x.Supplier?.CorporateName ?? "Fornecedor não informado",
                    x.IssueDate, null, "Pendente", "/app/ordens-compra/" + x.Id)));
            pending.AddRange(serviceOrders.Where(x => x.AmountPending > 0).Take(8)
                .Select(x => Entry("OS com saldo financeiro", x.Number, x.ServiceDescription,
                    x.CreatedAt, UserName(x.PurchaseRequest.RequestedByUser), "Pendente", "/app/ordens-servico/" + x.Id)));
            pending.AddRange(serviceOrders.SelectMany(x => x.AdvancePaymentRequests
                    .Where(a => a.Status == ServiceAdvancePaymentStatus.Approved &&
                        a.Payments.Sum(payment => payment.Amount) < a.Amount)
                    .Select(a => Entry("Antecipação aprovada aguardando pagamento", x.Number,
                        a.Observation ?? "Solicitação de antecipação", a.RequestedAt,
                        UserName(a.RequestedByUser), "Aprovada", "/app/ordens-servico/" + x.Id)))
                .Take(8));
        }

        var recent = BuildRecentActivities(role, requests, quotations, purchaseOrders, serviceOrders, receipts, payments);

        return new DashboardDto
        {
            Role = role,
            Cards = cards,
            Pending = pending.OrderByDescending(x => x.Date).Take(12).ToList(),
            RecentActivities = recent
        };
    }

    private static List<DashboardEntryDto> BuildRecentActivities(
        string role,
        IReadOnlyCollection<PurchaseRequest> requests,
        IReadOnlyCollection<Quotation> quotations,
        IReadOnlyCollection<PurchaseOrder> purchaseOrders,
        IReadOnlyCollection<ServiceOrder> serviceOrders,
        IReadOnlyCollection<Receipt> receipts,
        IReadOnlyCollection<Payment> payments)
    {
        var entries = new List<DashboardEntryDto>();
        var canSeePayments = role == AppRoles.Admin || role == AppRoles.Finance;

        entries.AddRange(requests.Take(6).Select(x => Entry("Solicitação criada", x.Number, x.Description,
            x.CreatedAt, UserName(x.RequestedByUser), x.Status.ToString(), "/app/solicitacoes/" + x.Id)));
        entries.AddRange(quotations.Take(6).Select(x => Entry("Cotação atualizada", x.Number,
            x.PurchaseRequest.Description, x.QuotationDate, UserName(x.PurchaseRequest.RequestedByUser),
            x.Status.ToString(), "/app/cotacoes/" + x.Id)));
        entries.AddRange(purchaseOrders.Take(6).Select(x => Entry("Ordem de compra gerada", x.Number,
            x.Supplier?.TradeName ?? x.Supplier?.CorporateName ?? "Fornecedor não informado",
            x.IssueDate, UserName(x.Quotation.PurchaseRequest.RequestedByUser),
            x.Status.ToString(), "/app/ordens-compra/" + x.Id)));
        entries.AddRange(serviceOrders.Take(6).Select(x => Entry("Ordem de serviço", x.Number,
            x.ServiceDescription, x.CreatedAt, UserName(x.PurchaseRequest.RequestedByUser),
            x.ExecutionStatus.ToString(), "/app/ordens-servico/" + x.Id)));
        if (canSeePayments)
        {
            entries.AddRange(serviceOrders.SelectMany(x => x.Payments.Select(payment => Entry(
                "Pagamento de serviço", x.Number, payment.Amount.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("pt-BR")),
                payment.PaymentDate, UserName(payment.PaidByUser), payment.Status.ToString(),
                "/app/ordens-servico/" + x.Id))));
        }
        entries.AddRange(receipts.Take(6).Select(x => Entry("Recebimento registrado",
            x.PurchaseOrder.Number, x.InvoiceNumber ?? "Nota fiscal não informada", x.ReceiptDate,
            UserName(x.ReceivedByUser), x.Status.ToString(), "/app/ordens-compra/" + x.PurchaseOrderId)));

        if (canSeePayments)
        {
            entries.AddRange(payments.Take(6).Select(x => Entry("Pagamento registrado",
                x.PurchaseOrder.Number, x.Amount.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("pt-BR")),
                x.PaymentDate, UserName(x.PaidByUser), x.Status.ToString(), "/app/financeiro")));
        }

        return entries.OrderByDescending(x => x.Date).Take(12).ToList();
    }

    private static void AddFinancialCards(
        ICollection<DashboardCardDto> cards,
        IEnumerable<PurchaseOrder> purchaseOrders,
        IEnumerable<ServiceOrder> serviceOrders)
    {
        cards.Add(MoneyCard("pending-value", "Valor pendente de pagamento",
            purchaseOrders.Sum(x => x.AmountPending) + serviceOrders.Sum(x => x.AmountPending), "/app/financeiro"));
        cards.Add(MoneyCard("paid-value", "Valor pago",
            purchaseOrders.Sum(x => x.AmountPaid) + serviceOrders.Sum(x => x.AmountPaid), "/app/financeiro"));
    }

    private static DashboardCardDto Card(string key, string label, int value, string route) =>
        new() { Key = key, Label = label, Value = value, Route = route };

    private static DashboardCardDto MoneyCard(string key, string label, decimal amount, string route) =>
        new() { Key = key, Label = label, Amount = amount, Route = route };

    private static DashboardEntryDto Entry(string type, string title, string description,
        DateTime date, string? responsible, string status, string route) => new()
    {
        Type = type,
        Title = title,
        Description = description,
        Date = date,
        Responsible = responsible,
        Status = status,
        Route = route
    };

    private static string? UserName(SGE.Domain.Entities.Administration.User? user)
    {
        if (user == null) return null;
        var name = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(name) ? user.Email : name;
    }

    private static bool HasPendingReceipt(PurchaseOrder order) =>
        order.Status != PurchaseOrderStatus.Cancelled && order.Items.Any(x => x.QuantityPending > 0);

    private static bool HasReceivingDivergence(PurchaseOrder order) =>
        order.Items.Any(x => x.QuantityReceived > x.QuantityOrdered);

    private static decimal AvailableMeasuredAmount(ServiceOrder order)
    {
        var approved = order.Measurements
            .Where(x => x.Status == ServiceMeasurementStatus.Approved)
            .Sum(x => x.Amount);
        var paid = order.Payments.Sum(x => x.Amount);
        return Math.Max(approved - paid, 0);
    }
}
