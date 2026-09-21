using SGE.Application.DTOs.Supplier;
using SGE.Application.Interfaces.Repositories.Companies;
using SGE.Application.Interfaces.Services.Companies;
using SGE.Domain.Entities.Companies;

namespace SGE.Application.Services.Companies;

public class SupplierService : ISupplierService
{
    private readonly ISupplierRepository _repository;
    private readonly ICompanyRepository _companyRepository;

    public SupplierService(
        ISupplierRepository repository,
        ICompanyRepository companyRepository)
    {
        _repository = repository;
        _companyRepository = companyRepository;
    }

    public async Task<IEnumerable<SupplierDto>> GetAllAsync()
    {
        var suppliers = await _repository.GetAllAsync();

        return suppliers.OrderByDescending(x => x.CreatedAt).Select(MapToDto);
    }

    public async Task<SupplierDto?> GetByIdAsync(Guid id)
    {
        var supplier = await _repository.GetByIdAsync(id);

        return supplier == null
            ? null
            : MapToDto(supplier);
    }

    public async Task<SupplierDto> CreateAsync(CreateSupplierDto dto)
    {
        if (dto.CompanyId.HasValue)
        {
            var company = await _companyRepository.GetByIdAsync(dto.CompanyId.Value);

            if (company == null)
                throw new ArgumentException("A empresa informada nao existe.");
        }

        var document = NormalizeDocument(dto.Document);
        await EnsureValidDocumentAsync(document);

        var supplier = new Supplier(
            dto.CompanyId,
            dto.CorporateName.Trim(),
            dto.TradeName.Trim(),
            document,
            dto.Email.Trim(),
            dto.Phone.Trim(),
            dto.ContactName.Trim(),
            dto.Address.Trim(),
            dto.Number.Trim(),
            dto.Complement?.Trim(),
            dto.District.Trim(),
            dto.City.Trim(),
            dto.State.Trim().ToUpperInvariant(),
            dto.ZipCode.Trim(),
            dto.StateRegistration?.Trim(),
            dto.IsActive);

        supplier.SetBankData(
            dto.PixKey,
            dto.Bank,
            dto.Agency,
            dto.Account);

        await _repository.AddAsync(supplier);
        await _repository.SaveChangesAsync();

        return MapToDto(supplier);
    }

    public async Task<SupplierDto?> UpdateAsync(
        Guid id,
        UpdateSupplierDto dto)
    {
        var supplier = await _repository.GetByIdAsync(id);

        if (supplier == null)
            return null;

        var document = NormalizeDocument(dto.Document);
        await EnsureValidDocumentAsync(document, supplier.Id);

        supplier.Update(
            dto.CorporateName.Trim(),
            dto.TradeName.Trim(),
            document,
            dto.StateRegistration?.Trim(),
            dto.Email.Trim(),
            dto.Phone.Trim(),
            dto.ContactName.Trim(),
            dto.Address.Trim(),
            dto.Number.Trim(),
            dto.Complement?.Trim(),
            dto.District.Trim(),
            dto.City.Trim(),
            dto.State.Trim().ToUpperInvariant(),
            dto.ZipCode.Trim(),
            dto.IsActive);

        supplier.SetBankData(
            dto.PixKey,
            dto.Bank,
            dto.Agency,
            dto.Account);

        _repository.Update(supplier);
        await _repository.SaveChangesAsync();

        return MapToDto(supplier);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var supplier = await _repository.GetByIdAsync(id);

        if (supplier == null)
            return false;

        supplier.Deactivate();
        _repository.Update(supplier);
        await _repository.SaveChangesAsync();

        return true;
    }

    private static SupplierDto MapToDto(Supplier supplier)
    {
        return new SupplierDto
        {
            Id = supplier.Id,
            CompanyId = supplier.CompanyId,
            CorporateName = supplier.CorporateName,
            TradeName = supplier.TradeName,
            Document = supplier.Document,
            StateRegistration = supplier.StateRegistration,
            Email = supplier.Email,
            Phone = supplier.Phone,
            ContactName = supplier.ContactName,
            Address = supplier.Address,
            Number = supplier.Number,
            Complement = supplier.Complement,
            District = supplier.District,
            City = supplier.City,
            State = supplier.State,
            ZipCode = supplier.ZipCode,
            PixKey = supplier.PixKey,
            Bank = supplier.Bank,
            Agency = supplier.Agency,
            Account = supplier.Account,
            IsActive = supplier.IsActive
        };
    }

    private async Task EnsureValidDocumentAsync(string document, Guid? ignoredId = null)
    {
        if (document.Length != 14)
            throw new ArgumentException("O CNPJ do fornecedor deve conter 14 digitos.");

        if (await _repository.ExistsByDocumentAsync(document, ignoredId))
            throw new InvalidOperationException("Ja existe um fornecedor cadastrado com este CNPJ.");
    }

    private static string NormalizeDocument(string document)
    {
        return new string((document ?? string.Empty)
            .Where(char.IsDigit)
            .ToArray());
    }
}
