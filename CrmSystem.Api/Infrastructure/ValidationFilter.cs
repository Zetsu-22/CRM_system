using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CrmSystem.Api.Infrastructure;

/// <summary>
/// Валидирует все входящие DTO, для которых зарегистрирован IValidator&lt;T&gt;.
/// Падает через ValidationException → GlobalExceptionHandler вернёт ProblemDetails 400.
/// </summary>
public sealed class ValidationFilter(IServiceProvider services) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null) continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (services.GetService(validatorType) is not IValidator validator) continue;

            var result = await validator.ValidateAsync(new ValidationContext<object>(argument));
            if (!result.IsValid)
                throw new ValidationException(result.Errors);
        }

        await next();
    }
}
