using GameSphere_backend.Authorization;
using GameSphere_backend.Interfaces;
using GameSphere_backend.Models.FrontendModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameSphere_backend.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = ActiveAdminRequirement.PolicyName)]
public sealed class AdminQuestionsController : AdminControllerBase
{
    private readonly IQuizAdministrationService _quizAdministrationService;

    public AdminQuestionsController(IQuizAdministrationService quizAdministrationService)
    {
        _quizAdministrationService = quizAdministrationService;
    }

    [HttpPost("quizzes/{quizId:int}/questions")]
    public async Task<ActionResult<AdminQuestionUpsertDto>> CreateQuestion(
        int quizId,
        [FromBody] AdminQuestionUpsertDto request) =>
        await RunAdminCommandAsync(
            adminId => _quizAdministrationService.CreateQuestionAsync(adminId, quizId, request),
            question => Created($"/api/admin/questions/{question.Id}", question));

    [HttpPut("questions/{id:int}")]
    public async Task<ActionResult<AdminQuestionUpsertDto>> UpdateQuestion(
        int id,
        [FromBody] AdminQuestionUpsertDto request) =>
        await RunAdminCommandAsync(
            adminId => _quizAdministrationService.UpdateQuestionAsync(adminId, id, request),
            question => Ok(question));

    [HttpDelete("questions/{id:int}")]
    public async Task<IActionResult> DeleteQuestion(int id) =>
        await RunAdminDeleteAsync(adminId => _quizAdministrationService.DeleteQuestionAsync(adminId, id));

}
