using SGE.Domain.Common;

namespace SGE.Domain.Entities.Companies;
public class Work : BaseSoftDeleteEntity
{
    public Guid CompanyId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public DateTime StartDate { get; private set; }

    public DateTime? EndDate { get; private set; }

    public Company Company { get; private set; } = null!;

    private Work()
    {
    }

    public Work(
        Guid companyId,
        string code,
        string name,
        string? description,
        DateTime startDate)
    {
        CompanyId = companyId;
        Code = code;
        Name = name;
        Description = description;
        StartDate = startDate;
    }

    public void Finish(DateTime endDate)
    {
        EndDate = endDate;
    }

    public void Update(
        string code,
        string name,
        string? description)
    {
        Code = code;
        Name = name;
        Description = description;
    }
}