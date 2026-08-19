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
[Route("api/admin")]
[Authorize(Policy = ActiveAdminRequirement.PolicyName)]
public sealed class AdminQuestionsController : ControllerBase
{
    private readonly IQuizAdministrationService _quizAdministrationService;

    public AdminQuestionsController(IQuizAdministrationService quizAdministrationService)
    {
        _quizAdministrationService = quizAdministrationService;
    }

    [HttpPost("quizzes/{quizId:int}/questions")]
    public async Task<ActionResult<AdminQuestionUpsertDto>> CreateQuestion(
        int quizId,
        [FromBody] AdminQuestionUpsertDto request)
    {
        if (!TryGetAdminId(out var adminId))
        {
            return Unauthorized();
        }

        var result = await _quizAdministrationService.CreateQuestionAsync(adminId, quizId, request);
        if (!result.Success || result.Data is null)
        {
            return ToErrorResult(result);
        }

        return Created($"/api/admin/questions/{result.Data.Id}", result.Data);
    }

    [HttpPut("questions/{id:int}")]
    public async Task<ActionResult<AdminQuestionUpsertDto>> UpdateQuestion(
        int id,
        [FromBody] AdminQuestionUpsertDto request)
    {
        if (!TryGetAdminId(out var adminId))
        {
            return Unauthorized();
        }

        var result = await _quizAdministrationService.UpdateQuestionAsync(adminId, id, request);
        return result.Success && result.Data is not null ? Ok(result.Data) : ToErrorResult(result);
    }

    [HttpDelete("questions/{id:int}")]
    public async Task<IActionResult> DeleteQuestion(int id)
    {
        if (!TryGetAdminId(out var adminId))
        {
            return Unauthorized();
        }

        var result = await _quizAdministrationService.DeleteQuestionAsync(adminId, id);
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
