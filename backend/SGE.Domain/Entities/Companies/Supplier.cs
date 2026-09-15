using SGE.Domain.Common;
namespace SGE.Domain.Entities.Companies;
public class Supplier : BaseSoftDeleteEntity
{
    public Guid? CompanyId { get; private set; }

    public string CorporateName { get; private set; } = string.Empty;

    public string TradeName { get; private set; } = string.Empty;

    public string Document { get; private set; } = string.Empty;

    public string? StateRegistration { get; private set; }

    public string Email { get; private set; } = string.Empty;

    public string Phone { get; private set; } = string.Empty;

    public string ContactName { get; private set; } = string.Empty;

    public string Address { get; private set; } = string.Empty;

    public string Number { get; private set; } = string.Empty;

    public string? Complement { get; private set; }

    public string District { get; private set; } = string.Empty;

    public string City { get; private set; } = string.Empty;

    public string State { get; private set; } = string.Empty;

    public string ZipCode { get; private set; } = string.Empty;

    public string? PixKey { get; private set; }

    public string? Bank { get; private set; }

    public string? Agency { get; private set; }

    public string? Account { get; private set; }

    public bool IsActive { get; private set; } = true;

    public Company? Company { get; private set; }

    private Supplier()
    {
    }

    public Supplier(
        Guid? companyId,
        string corporateName,
        string tradeName,
        string document,
        string email,
        string phone,
        string contactName,
        string address,
        string number,
        string? complement,
        string district,
        string city,
        string state,
        string zipCode,
        string? stateRegistration = null,
        bool isActive = true)
    {
        CompanyId = companyId;
        CorporateName = corporateName;
        TradeName = tradeName;
        Document = document;
        StateRegistration = stateRegistration;
        Email = email;
        Phone = phone;
        ContactName = contactName;
        Address = address;
        Number = number;
        Complement = complement;
        District = district;
        City = city;
        State = state;
        ZipCode = zipCode;
        IsActive = isActive;
    }
    public void Update(
    string corporateName,
    string tradeName,
    string document,
    string? stateRegistration,
    string email,
    string phone,
    string contactName,
    string address,
    string number,
    string? complement,
    string district,
    string city,
    string state,
    string zipCode,
    bool isActive)
{
    CorporateName = corporateName;
    TradeName = tradeName;
    Document = document;
    StateRegistration = stateRegistration;
    Email = email;
    Phone = phone;
    ContactName = contactName;
    Address = address;
    Number = number;
    Complement = complement;
    District = district;
    City = city;
    State = state;
    ZipCode = zipCode;
    IsActive = isActive;
}

    public void SetBankData(
        string? pixKey,
        string? bank,
        string? agency,
        string? account)
    {
        PixKey = pixKey;
        Bank = bank;
        Agency = agency;
        Account = account;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
