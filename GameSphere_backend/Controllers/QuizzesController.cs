using GameSphere_backend.Enums;
using GameSphere_backend.Interfaces;
using GameSphere_backend.Authorization;
using GameSphere_backend.Models.FrontendModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace GameSphere_backend.Controllers;

[ApiController]
[Route("api/quizzes")]
[Authorize(Policy = ActiveUserRequirement.PolicyName)]
public sealed class QuizzesController : ControllerBase
{
    private readonly IQuizCatalogService _quizCatalogService;

    public QuizzesController(IQuizCatalogService quizCatalogService)
    {
        _quizCatalogService = quizCatalogService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<QuizCatalogItemDto>>> GetPublished(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? category = null,
        [FromQuery] string? difficulty = null)
    {
        // Bound as text so non-numeric and out-of-range values share the same 400 instead of the generic model-binding error.
        if (!TryParseFilter<QuizCategory>(category, out var parsedCategory)
            || !TryParseFilter<Difficulty>(difficulty, out var parsedDifficulty))
        {
            return BadRequest(new { message = "Invalid catalog filter." });
        }

        return Ok(await _quizCatalogService.GetPublishedAsync(
            page,
            pageSize,
            parsedCategory,
            parsedDifficulty));
    }

    private static bool TryParseFilter<TEnum>(string? raw, out TEnum? value) where TEnum : struct, Enum
    {
        value = null;
        if (string.IsNullOrEmpty(raw))
        {
            return true;
        }

        if (!int.TryParse(raw, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var number)
            || !Enum.IsDefined(typeof(TEnum), number))
        {
            return false;
        }

        value = (TEnum)Enum.ToObject(typeof(TEnum), number);
        return true;
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<QuizPlayDto>> GetPublishedById(int id)
    {
        var quiz = await _quizCatalogService.GetPublishedByIdAsync(id);

        return quiz is null ? NotFound(new { message = "Quiz not found." }) : Ok(quiz);
    }

    [HttpPost("{id:int}/attempts")]
    public async Task<ActionResult<QuizAttemptResultDto>> SubmitAttempt(
        int id,
        [FromBody] QuizAttemptRequest request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var result = await _quizCatalogService.SubmitAttemptAsync(id, userId, request);

        if (!result.Success)
        {
            return result.Type switch
            {
                "NotFound" => NotFound(new { message = "Quiz not found." }),
                "Conflict" => Conflict(new { message = result.Message }),
                _ => BadRequest()
            };
        }

        return Ok(result.Data);
    }
}
