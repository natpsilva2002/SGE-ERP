namespace SGE.Application.DTOs.Dashboard;

public class DashboardDto
{
    public string Role { get; set; } = string.Empty;

    public IReadOnlyCollection<DashboardCardDto> Cards { get; set; } =
        Array.Empty<DashboardCardDto>();

    public IReadOnlyCollection<DashboardEntryDto> Pending { get; set; } =
        Array.Empty<DashboardEntryDto>();

    public IReadOnlyCollection<DashboardEntryDto> RecentActivities { get; set; } =
        Array.Empty<DashboardEntryDto>();
}

public class DashboardCardDto
{
    public string Key { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public int? Value { get; set; }

    public decimal? Amount { get; set; }

    public string? Route { get; set; }
}

public class DashboardEntryDto
{
    public string Type { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public string? Responsible { get; set; }

    public string? Status { get; set; }

    public string? Route { get; set; }
}
