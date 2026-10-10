using GameSphere_backend.ServicesResponses;
using Microsoft.AspNetCore.Mvc;

namespace GameSphere_backend.Controllers;

internal static class AdminErrorResultMapper
{
    public static ActionResult ToActionResult<T>(
        ControllerBase controller,
        ServiceResponse<T> response) => response.Type switch
        {
            "NotFound" => controller.NotFound(new { message = response.Message }),
            "Conflict" => controller.Conflict(new { message = response.Message }),
            _ => controller.BadRequest(new { message = response.Message })
        };
}
