using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace GameSphere_backend.Http;

/// <summary>
/// Builds the automatic [ApiController] 400 response as <c>{ message }</c>, without exposing internal .NET type names.
/// </summary>
public static class InvalidModelStateResponse
{
    public const string InvalidBodyMessage = "The request body is invalid.";
    public const string InvalidRequestMessage = "The request is invalid.";

    public static IActionResult Create(ActionContext context) =>
        new BadRequestObjectResult(new { message = SelectMessage(context.ModelState) });

    /// <summary>
    /// Returns a fixed message when the JSON parser rejected the body; otherwise the first validation message.
    /// </summary>
    public static string SelectMessage(ModelStateDictionary modelState)
    {
        var invalidEntries = modelState
            .Where(entry => entry.Value is { Errors.Count: > 0 })
            .ToList();

        if (invalidEntries.Any(entry => IsParserError(entry.Key, entry.Value!)))
        {
            return InvalidBodyMessage;
        }

        // Route/query binding errors carry the client-sent value; never echo it back.
        if (invalidEntries.Any(entry => entry.Value!.AttemptedValue is not null))
        {
            return InvalidRequestMessage;
        }

        return invalidEntries
            .SelectMany(entry => entry.Value!.Errors)
            .Select(error => error.ErrorMessage)
            .FirstOrDefault(message => !string.IsNullOrWhiteSpace(message))
            ?? InvalidBodyMessage;
    }

    private static bool IsParserError(string key, ModelStateEntry entry) =>
        key.StartsWith('$') || entry.Errors.Any(error => error.Exception is not null);
}
