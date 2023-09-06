using Microsoft.AspNetCore.Mvc;
using selflix.Models;

namespace selflix.Extensions;

public static class BadRequestFactory
{
    public static Func<ActionContext, IActionResult> Handle = ctx =>
    {
        var ve = new ValidationError();
        var errors = ctx.ModelState.Where(s => s.Value?.ValidationState == Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Invalid);
        foreach (var error in errors)
            ve.Errors.Add(error.Key, error.Value?.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid");

        return new BadRequestObjectResult(ve);
    };
}