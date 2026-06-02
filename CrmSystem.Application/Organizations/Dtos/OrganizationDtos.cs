namespace CrmSystem.Application.Organizations.Dtos;

public sealed record OrganizationDto(string Name, string? Address, bool IsDefault);
