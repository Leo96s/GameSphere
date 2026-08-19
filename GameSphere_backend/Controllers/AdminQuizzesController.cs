using GameSphere_backend.Authorization;
using GameSphere_backend.Interfaces;
using GameSphere_backend.Models.FrontendModels;
using GameSphere_backend.ServicesResponses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace GameSphere_backend.Controllers;

[ApiController]
[Route("api/admin/quizzes")]
[Authorize(Policy = ActiveAdminRequirement.PolicyName)]
public sealed class AdminQuizzesController : ControllerBase
{
    private readonly IQuizAdministrationService _quizAdministrationService;

    public AdminQuizzesController(IQuizAdministrationService quizAdministrationService)
    {
        _quizAdministrationService = quizAdministrationService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminQuizUpsertDto>>> GetQuizzes()
    {
        return Ok(await _quizAdministrationService.GetQuizzesAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AdminQuizUpsertDto>> GetQuizById(int id)
    {
        var quiz = await _quizAdministrationService.GetQuizByIdAsync(id);
        return quiz is null ? NotFound() : Ok(quiz);
    }

    [HttpPost]
    public async Task<ActionResult<AdminQuizUpsertDto>> CreateQuiz([FromBody] AdminQuizUpsertDto request)
    {
        if (!TryGetAdminId(out var adminId))
        {
            return Unauthorized();
        }

        var result = await _quizAdministrationService.CreateQuizAsync(adminId, request);
        if (!result.Success || result.Data is null)
        {
            return ToErrorResult(result);
        }

        return CreatedAtAction(nameof(GetQuizById), new { id = result.Data.Id }, result.Data);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AdminQuizUpsertDto>> UpdateQuiz(int id, [FromBody] AdminQuizUpsertDto request)
    {
        if (!TryGetAdminId(out var adminId))
        {
            return Unauthorized();
        }

        var result = await _quizAdministrationService.UpdateQuizAsync(adminId, id, request);
        return result.Success && result.Data is not null ? Ok(result.Data) : ToErrorResult(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteQuiz(int id)
    {
        if (!TryGetAdminId(out var adminId))
        {
            return Unauthorized();
        }

        var result = await _quizAdministrationService.DeleteQuizAsync(adminId, id);
        return result.Success ? NoContent() : ToErrorResult(result);
    }

    private bool TryGetAdminId(out int adminId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(claim, out adminId);
    }

    private ActionResult ToErrorResult<T>(ServiceResponse<T> response) => response.Type switch
    {
        "NotFound" => NotFound(),
        "Conflict" => Conflict(),
        _ => BadRequest()
    };
}
