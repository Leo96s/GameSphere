using GameSphere_backend.ServicesResponses;
using Microsoft.AspNetCore.Mvc;

namespace GameSphere_backend.Controllers
{
    /// <summary>
    /// Base controller class providing standardized response handling for API controllers.
    /// </summary>
    /// <remarks>
    /// This controller provides consistent response formatting and error handling
    /// across all API endpoints. It handles different response types based on
    /// the ServiceResponse format and current environment configuration.
    /// </remarks>
    public class ResponseController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the ResponseController class.
        /// </summary>
        /// <param name="configuration">Application configuration settings.</param>
        public ResponseController(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Handles API responses consistently based on the ServiceResponse object.
        /// </summary>
        /// <typeparam name="T">Type of the data payload in the response.</typeparam>
        /// <param name="serviceResponse">The service response to handle.</param>
        /// <returns>
        /// An IActionResult formatted according to the response type and environment:
        /// - In Development: Detailed error messages
        /// - In Production: Generic error messages
        /// </returns>
        /// <remarks>
        /// Response type mapping:
        /// - "NotFound" → 404 Not Found
        /// - "BadRequest" → 400 Bad Request
        /// - "Conflict" → 409 Conflict
        /// - "Unauthorized" → 401 Unauthorized
        /// - "Ok" → 200 OK with data
        /// - "NoContent" → 204 No Content
        /// - Default → 500 Internal Server Error
        /// </remarks>
        protected IActionResult HandleResponse<T>(ServiceResponse<T> serviceResponse)
        {
            var mode = _configuration["MessageMode"];
            var message = mode == "Development" ? serviceResponse.Message : "Something went wrong";

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

        /// <summary>
        /// Handles successful creation responses with location header.
        /// </summary>
        /// <typeparam name="T">Type of the created resource.</typeparam>
        /// <param name="serviceResponse">The successful service response.</param>
        /// <param name="actionName">Name of the action to route to.</param>
        /// <param name="routeValues">Route values for the location header.</param>
        /// <returns>
        /// A 201 Created response with:
        /// - Location header pointing to the new resource
        /// - The created resource in the response body
        /// </returns>
        protected IActionResult HandleCreatedAtAction<T>(
            ServiceResponse<T> serviceResponse,
            string actionName,
            object routeValues)
        {
            return CreatedAtAction(actionName, routeValues, serviceResponse.Data);
        }
    }
}
