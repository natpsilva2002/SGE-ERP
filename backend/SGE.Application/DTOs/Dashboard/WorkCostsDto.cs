namespace SGE.Application.DTOs.Dashboard;

public class WorkCostsDto
{
    public Guid? WorkId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public IReadOnlyCollection<DashboardWorkOptionDto> Works { get; set; } = [];
    public WorkCostsSummaryDto Summary { get; set; } = new();
    public IReadOnlyCollection<WorkCostEvolutionDto> Evolution { get; set; } = [];
    public IReadOnlyCollection<WorkSupplierCostDto> Suppliers { get; set; } = [];
    public IReadOnlyCollection<WorkTopCostDto> TopCosts { get; set; } = [];
    public IReadOnlyCollection<WorkCostDetailDto> Details { get; set; } = [];
    public IReadOnlyCollection<WorkPaymentDto> Payments { get; set; } = [];
}

public class DashboardWorkOptionDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class WorkCostsSummaryDto
{
    public decimal Contracted { get; set; }
    public decimal Paid { get; set; }
    public decimal Pending { get; set; }
    public decimal Materials { get; set; }
    public decimal Services { get; set; }
}

public class WorkCostEvolutionDto
{
    public DateTime Period { get; set; }
    public decimal Materials { get; set; }
    public decimal Services { get; set; }
    public decimal Total { get; set; }
}

public class WorkSupplierCostDto
{
    public Guid? SupplierId { get; set; }
    public string Supplier { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Contracted { get; set; }
    public decimal Paid { get; set; }
    public decimal Pending { get; set; }
    public decimal Share { get; set; }
    public string? Route { get; set; }
}

public class WorkTopCostDto
{
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Route { get; set; }
}

public class WorkCostDetailDto
{
    public string Type { get; set; } = string.Empty;
    public string Document { get; set; } = string.Empty;
    public string Supplier { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Contracted { get; set; }
    public decimal Paid { get; set; }
    public decimal Pending { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string? Route { get; set; }
}

public class WorkPaymentDto
{
    public DateTime Date { get; set; }
    public string Work { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Document { get; set; } = string.Empty;
    public string Supplier { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string Responsible { get; set; } = string.Empty;
    public string? Route { get; set; }
}
