using GameSphere_backend.Authorization;
using GameSphere_backend.Interfaces;
using GameSphere_backend.Models.FrontendModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameSphere_backend.Controllers;

[ApiController]
[Route("api/admin/quizzes")]
[Authorize(Policy = ActiveAdminRequirement.PolicyName)]
public sealed class AdminQuizzesController : AdminControllerBase
{
    private readonly IQuizAdministrationService _quizAdministrationService;

    public AdminQuizzesController(IQuizAdministrationService quizAdministrationService)
    {
        _quizAdministrationService = quizAdministrationService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminQuizUpsertDto>>> GetQuizzes(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        return Ok(await _quizAdministrationService.GetQuizzesAsync(page, pageSize));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AdminQuizUpsertDto>> GetQuizById(int id)
    {
        var quiz = await _quizAdministrationService.GetQuizByIdAsync(id);
        return quiz is null ? NotFound() : Ok(quiz);
    }

    [HttpPost]
    public async Task<ActionResult<AdminQuizUpsertDto>> CreateQuiz([FromBody] AdminQuizUpsertDto request) =>
        await RunAdminCommandAsync(
            adminId => _quizAdministrationService.CreateQuizAsync(adminId, request),
            quiz => CreatedAtAction(nameof(GetQuizById), new { id = quiz.Id }, quiz));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AdminQuizUpsertDto>> UpdateQuiz(int id, [FromBody] AdminQuizUpsertDto request) =>
        await RunAdminCommandAsync(
            adminId => _quizAdministrationService.UpdateQuizAsync(adminId, id, request),
            quiz => Ok(quiz));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteQuiz(int id) =>
        await RunAdminDeleteAsync(adminId => _quizAdministrationService.DeleteQuizAsync(adminId, id));

}
