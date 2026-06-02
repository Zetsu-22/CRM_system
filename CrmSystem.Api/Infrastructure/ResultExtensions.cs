using CrmSystem.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace CrmSystem.Api.Infrastructure;

/// <summary>
/// Универсальный маппинг <see cref="Result{T}"/> в <see cref="IActionResult"/>.
/// Используется тонкими контроллерами, чтобы не дублировать switch по кодам ошибок.
/// </summary>
public static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result, ControllerBase ctrl)
    {
        if (result.IsSuccess) return ctrl.Ok(result.Value);
        return ToErrorResult(result.Error!, ctrl);
    }

    public static IActionResult ToActionResult<T>(this Result<T> result, ControllerBase ctrl, Func<T, IActionResult> onSuccess)
    {
        if (result.IsSuccess) return onSuccess(result.Value!);
        return ToErrorResult(result.Error!, ctrl);
    }

    private static IActionResult ToErrorResult(Error error, ControllerBase ctrl) => error.Code switch
    {
        ErrorCodes.NotFound            => ctrl.NotFound(new { error = error.Message }),
        ErrorCodes.Forbidden           => ctrl.StatusCode(StatusCodes.Status403Forbidden, new { error = error.Message }),
        ErrorCodes.Validation          => ctrl.BadRequest(new { error = error.Message }),
        ErrorCodes.InvalidState        => ctrl.Conflict(new { error = new { code = error.Code, message = error.Message } }),
        ErrorCodes.InvalidTransition   => ctrl.Conflict(new { error = new { code = error.Code, message = error.Message } }),
        ErrorCodes.ConcurrencyConflict => ctrl.Conflict(new { error = new { code = error.Code, message = error.Message, details = error.Details } }),
        ErrorCodes.Conflict            => ctrl.Conflict(new { error = new { code = error.Code, message = error.Message, details = error.Details } }),
        // Phase 4.5 — бизнес-правила (неизвестный код → 409 с code+message)
        _                              => ctrl.Conflict(new { error = new { code = error.Code, message = error.Message } }),
    };
}
