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
        [FromQuery] int pageSize = 50)
    {
        return Ok(await _quizCatalogService.GetPublishedAsync(page, pageSize));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<QuizPlayDto>> GetPublishedById(int id)
    {
        var quiz = await _quizCatalogService.GetPublishedByIdAsync(id);

        return quiz is null ? NotFound() : Ok(quiz);
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
            return result.Type == "NotFound" ? NotFound() : BadRequest();
        }

        return Ok(result.Data);
    }
}
