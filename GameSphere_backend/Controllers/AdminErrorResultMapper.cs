using GameSphere_backend.ServicesResponses;
using Microsoft.AspNetCore.Mvc;

namespace GameSphere_backend.Controllers;

internal static class AdminErrorResultMapper
{
    public static ActionResult ToActionResult<T>(
        ControllerBase controller,
        ServiceResponse<T> response) => response.Type switch
        {
            "NotFound" => controller.NotFound(),
            "Conflict" => controller.Conflict(),
            _ => controller.BadRequest()
        };
}
