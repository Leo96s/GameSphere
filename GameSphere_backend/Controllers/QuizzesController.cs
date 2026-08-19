using GameSphere_backend.Interfaces;
using GameSphere_backend.Models.FrontendModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameSphere_backend.Controllers;

[ApiController]
[Route("api/quizzes")]
[Authorize]
public sealed class QuizzesController : ControllerBase
{
    private readonly IQuizCatalogService _quizCatalogService;

    public QuizzesController(IQuizCatalogService quizCatalogService)
    {
        _quizCatalogService = quizCatalogService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<QuizCatalogItemDto>>> GetPublished()
    {
        return Ok(await _quizCatalogService.GetPublishedAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<QuizPlayDto>> GetPublishedById(int id)
    {
        var quiz = await _quizCatalogService.GetPublishedByIdAsync(id);

        return quiz is null ? NotFound() : Ok(quiz);
    }
}
