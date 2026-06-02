namespace CrmSystem.Application.Companies.Dtos;

public sealed record CompanyDto(
    string Name,
    string? Inn,
    string? Kpp,
    string? Ogrn,
    string? LegalAddress,
    string? ActualAddress,
    string? BankName,
    string? Bik,
    string? AccountNumber,
    string? CorrAccount,
    bool VatPayer);
