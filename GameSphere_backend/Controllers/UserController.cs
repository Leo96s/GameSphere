using GameSphere_backend.Interfaces;
using GameSphere_backend.Models.BackendModels;
using GameSphere_backend.Models.FrontendModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace GameSphere_backend.Controllers
{
    /// <summary>
    /// Controller responsible for all endpoints relating to User
    /// Has a constructor that receives an IUserService and an IFavoritesService instance
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : BaseCrudController<UserDto>
    {
        private readonly IUserService _userService;
        public UserController(IUserService userService, IConfiguration configuration) : base(configuration)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }
        /// <summary>
        /// Endpoint that gets a certain User by his id
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Returns NotFound() if is not sucessefully or OK() with the User Dto if it is</returns>
        [HttpGet("by-id/{id}")]
        public override async Task<IActionResult> GetEntityById(int id)
        {
            var serviceResponse = await _userService.GetUserByIdAsync(id);

            return HandleResponse(serviceResponse);
        }

        /// <summary>
        /// Endpoint that creates a new User
        /// </summary>
        /// <param name="user"></param>
        /// <returns>Returns BadRequest() if userService responds "BadRequest", 
        /// NotFound() if userService responds "NotFound", or CreatedAtAction()
        /// with the path for the newly created user</returns>
        [HttpPost]
        public override async Task<IActionResult> CreateEntity(UserDto user)
        {
            var serviceResponse = await _userService.CreateNewUserAsync(user);

            if (!serviceResponse.Success)
            {
                return HandleResponse(serviceResponse);
            }

            return HandleCreatedAtAction(serviceResponse, nameof(GetEntityById), new { id = serviceResponse.Data.Id });
        }

        /// <summary>
        /// Endpoint that deletes the User by his id
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Returns BadRequest() if userService responds "BadRequest", 
        /// NotFound() if userService responds "NotFound", or NoContent() if
        /// user is correctly deleted from the system</returns>
        [HttpDelete("{id}")]
        [Authorize]
        public override async Task<IActionResult> DeleteEntity(int id)
        {
            {
                var serviceResponse = await _userService.DeleteUserAsync(id);

                return HandleResponse(serviceResponse);
            }
        }

        /// <summary>
        /// Endpoint that edits the user based on an updated dto that it receives by his id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="updatedUser"></param>
        /// <returns>Returns BadRequest() if userService responds "BadRequest", 
        /// NotFound() if userService responds "NotFound", or Ok() with
        /// updated User dto if updated correctly</returns>
        [HttpPut("{id}")]
        [Authorize]
        public override async Task<IActionResult> UpdateEntity(int id, UserDto updatedUser)
        {
            var serviceResponse = await _userService.EditUserAsync(id, updatedUser);

            return HandleResponse(serviceResponse);
        }

        /// <summary>
        /// Endpoint that makes the Login of the User
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Returns BadRequest() if userService responds "BadRequest", 
        /// NotFound() if userService responds "NotFound", Unauthorized if
        /// userService responds with "Unauthorized or Ok() with the
        /// User's Jwt Token and Info</returns>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var serviceResponse = await _userService.LoginAsync(request);

            return HandleResponse(serviceResponse);
        }

        /// <summary>
        /// Endpoint that informs the user if his email is already registered in the plataform
        /// </summary>
        /// <param name="email"></param>
        /// <returns>Returns BadRequest() if userService responds "BadRequest", 
        /// NotFound() if userService responds "NotFound", or Ok() with
        /// a message according with if the user's email is already registered or not</returns>
        [HttpGet("get-email-availability/{email}")]
        public async Task<IActionResult> GetEmailAvailability(string email)
        {
            var serviceResponse = await _userService.CheckEmailAvailabilityAsync(email);

            return HandleResponse(serviceResponse);
        }

        [HttpGet("user-exist/{uid}/{email}")]
        public async Task<IActionResult> GetUserExist(string uid, string email)
        {
            var serviceResponse = await _userService.CheckExistsUserExtern(uid,email);

            return HandleResponse(serviceResponse);
        }

        [HttpGet("by-email/{email}")]
        public async Task<IActionResult> GetEntityByEmail(string email)
        {
            var serviceResponse = await _userService.GetUserByEmailAsync(email);

            return HandleResponse(serviceResponse);
        }

        [HttpPost("send-reset-code")]
        public async Task<IActionResult> SendResetCode([FromBody] string email)
        {
            var response = await _userService.SendPasswordResetCode(email);
            return HandleResponse(response);
        }

        [HttpPost("validate-reset-code")]
        public async Task<IActionResult> ValidateResetCode([FromBody] ValidateResetCodeRequest request)
        {
            var response = await _userService.ValidateResetCode(request.Email, request.ResetCode);
            return HandleResponse(response);
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] Models.FrontendModels.ResetPasswordRequest request)
        {
            var response = await _userService.ResetPassword(request.Email, request.ResetCode, request.NewPassword);
            return HandleResponse(response);
        }

    }
}
