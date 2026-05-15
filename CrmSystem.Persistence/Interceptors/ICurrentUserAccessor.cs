namespace CrmSystem.Persistence.Interceptors;

/// <summary>
/// Абстракция над текущим пользователем, доступная из Persistence-слоя
/// (чтобы Persistence не зависел от Api). Реализуется в Api через CurrentUserService.
/// </summary>
public interface ICurrentUserAccessor
{
    bool IsAuthenticated { get; }
    Guid? UserId { get; }
    Guid? CompanyId { get; }
}
