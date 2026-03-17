using BE_AI_Tourism.Shared.Core;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BE_AI_Tourism.Application.Validators;

// Auto-validate request DTOs via FluentValidation
public class ValidatorFilter<T> : IAsyncActionFilter where T : class
{
    private readonly IValidator<T> _validator;

    public ValidatorFilter(IValidator<T> validator)
    {
        _validator = validator;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var argument = context.ActionArguments.Values.OfType<T>().FirstOrDefault();
        if (argument is null)
        {
            await next();
            return;
        }

        var validationResult = await _validator.ValidateAsync(argument);
        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            var result = Result.Fail(errors, StatusCodes.Status400BadRequest);
            context.Result = new BadRequestObjectResult(result);
            return;
        }

        await next();
    }
}
