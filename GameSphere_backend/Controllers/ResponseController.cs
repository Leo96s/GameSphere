using GameSphere_backend.ServicesResponses;
using Microsoft.AspNetCore.Mvc;

namespace GameSphere_backend.Controllers
{
    public class ResponseController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ResponseController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        protected IActionResult HandleResponse<T>(ServiceResponse<T> serviceResponse)
        {

            var mode = _configuration["MessageMode"]; // Obtem o valor de MessageMode

            var message = mode == "Development"
                ? serviceResponse.Message
                : "Something goes wrong";

            if (!serviceResponse.Success)
            {
                return serviceResponse.Type switch
                {
                    "NotFound" => NotFound(message),
                    "BadRequest" => BadRequest(message),
                    "Conflict" => Conflict(message),
                    "Unauthorized" => Unauthorized(message),
                    _ => StatusCode(500, message) // InternalServerError
                };
            }

            return serviceResponse.Type switch
            {
                "Ok" => Ok(serviceResponse.Data),
                "NoContent" => NoContent(),
                _ => StatusCode(500, message)
            };

        }

        protected IActionResult HandleCreatedAtAction<T>(
            ServiceResponse<T> serviceResponse,
            string actionName,
            object routeValues
        )
        {
            return CreatedAtAction(actionName, routeValues, serviceResponse.Data);
        }

    }
}
