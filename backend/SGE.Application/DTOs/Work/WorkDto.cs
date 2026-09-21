namespace SGE.Application.DTOs.Work;

public class WorkDto
{
    public Guid Id { get; set; }

    public Guid? CompanyId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool IsActive { get; set; }
}
