namespace CrmSystem.Application.Common;

/// <summary>
/// Дискриминированный результат операции. Используется сервисами Application-слоя
/// вместо бросания типизированных исключений на «штатные» бизнес-ошибки.
/// </summary>
public readonly record struct Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public Error? Error { get; }

    private Result(bool ok, T? value, Error? error)
    {
        IsSuccess = ok;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(Error error) => new(false, default, error);

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);
}

/// <summary>Машинно-читаемый код + человеко-читаемое сообщение + опциональный payload.</summary>
public sealed record Error(string Code, string Message, object? Details = null)
{
    public static Error NotFound(string message = "Ресурс не найден") => new(ErrorCodes.NotFound, message);
    public static Error Forbidden(string message = "Доступ запрещён") => new(ErrorCodes.Forbidden, message);
    public static Error Conflict(string message, object? details = null) => new(ErrorCodes.Conflict, message, details);
    public static Error Validation(string message, object? details = null) => new(ErrorCodes.Validation, message, details);
    public static Error InvalidState(string message) => new(ErrorCodes.InvalidState, message);
    public static Error InvalidTransition(string message) => new(ErrorCodes.InvalidTransition, message);
    public static Error ConcurrencyConflict(string message, object? details = null) => new(ErrorCodes.ConcurrencyConflict, message, details);
}

public static class ErrorCodes
{
    public const string NotFound             = "not_found";
    public const string Forbidden            = "forbidden";
    public const string Conflict             = "conflict";
    public const string Validation           = "validation";
    public const string InvalidState         = "invalid_state";
    public const string InvalidTransition    = "invalid_transition";
    public const string ConcurrencyConflict  = "concurrency_conflict";
}
