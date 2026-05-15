namespace CrmSystem.Application.Abstractions;

/// <summary>
/// Интерфейс текущего пользователя для Application-слоя (богаче, чем Persistence
/// <c>ICurrentUserAccessor</c>: включает permission-флаги).
/// Реализуется в Api как <c>CurrentUserService</c>.
/// </summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    Guid UserId { get; }
    Guid? CompanyId { get; }
    string Email { get; }
    string FullName { get; }

    bool IsCompanyOwner { get; }
    bool IsBasicProfile { get; }

    bool IsAccessOrdersEdit { get; }
    bool IsOrderPicker { get; }
    bool IsExpeditor { get; }
    bool IsDriver { get; }
    bool CanViewOrders { get; }

    bool IsAccessContractors { get; }
    bool IsAccessContractorsEdit { get; }
    bool IsAccessContractorDiscountEdit { get; }
    bool IsAccessNomenclature { get; }
    bool IsAccessNomenclatureEdit { get; }
    bool IsAccessFinancialJournal { get; }
    bool IsAccessFinancialJournalEdit { get; }
    bool IsAccessWarehouses { get; }
    bool IsAccessWarehousesEdit { get; }
    bool CanManageUsers { get; }
}
