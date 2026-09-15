using SGE.Domain.Common;
using SGE.Domain.Entities.Administration;

namespace SGE.Domain.Entities.Companies;

public class Company : BaseSoftDeleteEntity
{
    public string CorporateName { get; private set; } = string.Empty;

    public string TradeName { get; private set; } = string.Empty;

    public string Document { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string Phone { get; private set; } = string.Empty;

    public ICollection<User> Users { get; private set; } = new List<User>();

    public ICollection<Work> Works { get; private set; } = new List<Work>();

    public ICollection<Supplier> Suppliers { get; private set; } = new List<Supplier>();

    private Company()
    {
    }

    public Company(
        string corporateName,
        string tradeName,
        string document,
        string email,
        string phone)
    {
        CorporateName = corporateName;
        TradeName = tradeName;
        Document = document;
        Email = email;
        Phone = phone;
    }

    public void Update(
        string corporateName,
        string tradeName,
        string email,
        string phone)
    {
        CorporateName = corporateName;
        TradeName = tradeName;
        Email = email;
        Phone = phone;
    }
}