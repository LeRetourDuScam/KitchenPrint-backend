using KitchenPrint.Contracts.DataAccess;
using KitchenPrint.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace KitchenPrint_backend.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        private string? GetIpAddress()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString();
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        [HttpPost("register")]
        [EnableRateLimiting("auth")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                if (request == null)
                {
                    _logger.LogWarning("Register attempt with null request");
                    return BadRequest(new ErrorResponse(
                        ErrorCodes.INVALID_INPUT,
                        "Request data is required"
                    ));
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Register attempt with invalid model state");
                    return BadRequest(new ErrorResponse(
                        ErrorCodes.VALIDATION_FAILED,
                        "Validation failed"
                    ));
                }

                _logger.LogInformation("Attempting to register user");

                var response = await _authService.RegisterAsync(request, GetIpAddress());

                if (response == null)
                {
                    _logger.LogWarning("Registration failed - email may already exist");
                    return BadRequest(new ErrorResponse(
                        ErrorCodes.EMAIL_ALREADY_EXISTS,
                        "Registration failed. Email might already be in use."
                    ));
                }

                _logger.LogInformation("User successfully registered");
                return CreatedAtAction(nameof(Register), new { email = response.Email }, response);
            }
            catch (Exception ex)
            {
                var supportId = Guid.NewGuid().ToString();
                _logger.LogError(ex, "Error occurred during registration. SupportId: {SupportId}", supportId);
                return StatusCode(500, new ErrorResponse(
                    ErrorCodes.INTERNAL_SERVER_ERROR,
                    "An error occurred while processing your request",
                    supportId: supportId
                ));
            }
        }

        /// <summary>
        /// Login an existing user
        /// </summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (request == null)
                {
                    _logger.LogWarning("Login attempt with null request");
                    return BadRequest(new ErrorResponse(
                        ErrorCodes.INVALID_INPUT,
                        "Request data is required"
                    ));
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Login attempt with invalid model state");
                    return BadRequest(new ErrorResponse(
                        ErrorCodes.VALIDATION_FAILED,
                        "Validation failed"
                    ));
                }

                _logger.LogInformation("Login attempt");

                var response = await _authService.LoginAsync(request, GetIpAddress());

                if (response == null)
                {
                    _logger.LogWarning("Login failed - invalid credentials");
                    return Unauthorized(new ErrorResponse(
                        ErrorCodes.INVALID_CREDENTIALS,
                        "Invalid email or password"
                    ));
                }

                _logger.LogInformation("User successfully logged in");
                return Ok(response);
            }
            catch (Exception ex)
            {
                var supportId = Guid.NewGuid().ToString();
                _logger.LogError(ex, "Error occurred during login. SupportId: {SupportId}", supportId);
                return StatusCode(500, new ErrorResponse(
                    ErrorCodes.INTERNAL_SERVER_ERROR,
                    "An error occurred while processing your request",
                    supportId: supportId
                ));
            }
        }

        /// <summary>
        /// Refresh access token using refresh token
        /// </summary>
        [HttpPost("refresh")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.RefreshToken))
                {
                    _logger.LogWarning("Refresh token attempt with invalid request");
                    return BadRequest(new ErrorResponse(
                        ErrorCodes.INVALID_INPUT,
                        "Refresh token is required"
                    ));
                }

                var response = await _authService.RefreshTokenAsync(request.RefreshToken, GetIpAddress());

                if (response == null)
                {
                    _logger.LogWarning("Refresh token failed - invalid or expired token");
                    return Unauthorized(new ErrorResponse(
                        ErrorCodes.REFRESH_TOKEN_INVALID,
                        "Invalid or expired refresh token"
                    ));
                }

                _logger.LogInformation("Token refreshed successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                var supportId = Guid.NewGuid().ToString();
                _logger.LogError(ex, "Error occurred during token refresh. SupportId: {SupportId}", supportId);
                return StatusCode(500, new ErrorResponse(
                    ErrorCodes.INTERNAL_SERVER_ERROR,
                    "An error occurred while processing your request",
                    supportId: supportId
                ));
            }
        }

        /// <summary>
        /// Logout user by revoking refresh token
        /// </summary>
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.RefreshToken))
                {
                    _logger.LogWarning("Logout attempt with invalid request");
                    return BadRequest(new ErrorResponse(
                        ErrorCodes.INVALID_INPUT,
                        "Refresh token is required"
                    ));
                }

                var success = await _authService.RevokeTokenAsync(request.RefreshToken, GetIpAddress());

                if (!success)
                {
                    _logger.LogWarning("Logout failed - token not found or already revoked");
                }

                _logger.LogInformation("User logged out successfully");
                return Ok(new { message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                var supportId = Guid.NewGuid().ToString();
                _logger.LogError(ex, "Error occurred during logout. SupportId: {SupportId}", supportId);
                return StatusCode(500, new ErrorResponse(
                    ErrorCodes.INTERNAL_SERVER_ERROR,
                    "An error occurred while processing your request",
                    supportId: supportId
                ));
            }
        }

        /// <summary>
        /// Get user by ID
        /// </summary>
        [HttpGet("user/{id}")]
        [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUserById(int id)
        {
            try
            {
                _logger.LogInformation("Fetching user by ID");

                var user = await _authService.GetUserByIdAsync(id);

                if (user == null)
                {
                    _logger.LogWarning("User not found");
                    return NotFound(new ErrorResponse(
                        ErrorCodes.RESOURCE_NOT_FOUND,
                        "User not found"
                    ));
                }

                _logger.LogInformation("User found");
                return Ok(user);
            }
            catch (Exception ex)
            {
                var supportId = Guid.NewGuid().ToString();
                _logger.LogError(ex, "Error occurred while fetching user. SupportId: {SupportId}", supportId);
                return StatusCode(500, new ErrorResponse(
                    ErrorCodes.INTERNAL_SERVER_ERROR,
                    "An error occurred while processing your request",
                    supportId: supportId
                ));
            }
        }

        /// <summary>
        /// Get user by email
        /// </summary>
        [HttpGet("user/email/{email}")]
        [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUserByEmail(string email)
        {
            try
            {
                _logger.LogInformation("Fetching user by email");

                var user = await _authService.GetUserByEmailAsync(email);

                if (user == null)
                {
                    _logger.LogWarning("User not found");
                    return NotFound(new ErrorResponse(
                        ErrorCodes.RESOURCE_NOT_FOUND,
                        "User not found"
                    ));
                }

                _logger.LogInformation("User found");
                return Ok(user);
            }
            catch (Exception ex)
            {
                var supportId = Guid.NewGuid().ToString();
                _logger.LogError(ex, "Error occurred while fetching user. SupportId: {SupportId}", supportId);
                return StatusCode(500, new ErrorResponse(
                    ErrorCodes.INTERNAL_SERVER_ERROR,
                    "An error occurred while processing your request",
                    supportId: supportId
                ));
            }
        }

        /// <summary>
        /// Get current user profile
        /// </summary>
        [HttpGet("profile")]
        [Authorize]
        [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                var profile = await _authService.GetProfileAsync(userId);

                if (profile == null)
                    return NotFound(new ErrorResponse(ErrorCodes.RESOURCE_NOT_FOUND, "User not found"));

                return Ok(profile);
            }
            catch (Exception ex)
            {
                var supportId = Guid.NewGuid().ToString();
                _logger.LogError(ex, "Error fetching profile. SupportId: {SupportId}", supportId);
                return StatusCode(500, new ErrorResponse(ErrorCodes.INTERNAL_SERVER_ERROR, "An error occurred", supportId: supportId));
            }
        }

        /// <summary>
        /// Update current user profile (username)
        /// </summary>
        [HttpPatch("profile")]
        [Authorize]
        [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new ErrorResponse(ErrorCodes.VALIDATION_FAILED, "Validation failed"));

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                var profile = await _authService.UpdateProfileAsync(userId, request);

                if (profile == null)
                    return NotFound(new ErrorResponse(ErrorCodes.RESOURCE_NOT_FOUND, "User not found"));

                return Ok(profile);
            }
            catch (Exception ex)
            {
                var supportId = Guid.NewGuid().ToString();
                _logger.LogError(ex, "Error updating profile. SupportId: {SupportId}", supportId);
                return StatusCode(500, new ErrorResponse(ErrorCodes.INTERNAL_SERVER_ERROR, "An error occurred", supportId: supportId));
            }
        }

        /// <summary>
        /// Change password
        /// </summary>
        [HttpPost("change-password")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new ErrorResponse(ErrorCodes.VALIDATION_FAILED, "Validation failed"));

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                var success = await _authService.ChangePasswordAsync(userId, request);

                if (!success)
                    return BadRequest(new ErrorResponse(ErrorCodes.INVALID_CREDENTIALS, "Current password is incorrect"));

                return Ok(new { message = "Password changed successfully" });
            }
            catch (Exception ex)
            {
                var supportId = Guid.NewGuid().ToString();
                _logger.LogError(ex, "Error changing password. SupportId: {SupportId}", supportId);
                return StatusCode(500, new ErrorResponse(ErrorCodes.INTERNAL_SERVER_ERROR, "An error occurred", supportId: supportId));
            }
        }

        /// <summary>
        /// Delete account (requires password confirmation)
        /// </summary>
        [HttpDelete("account")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new ErrorResponse(ErrorCodes.VALIDATION_FAILED, "Validation failed"));

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                var success = await _authService.DeleteAccountAsync(userId, request.Password);

                if (!success)
                    return BadRequest(new ErrorResponse(ErrorCodes.INVALID_CREDENTIALS, "Incorrect password"));

                return Ok(new { message = "Account deleted successfully" });
            }
            catch (Exception ex)
            {
                var supportId = Guid.NewGuid().ToString();
                _logger.LogError(ex, "Error deleting account. SupportId: {SupportId}", supportId);
                return StatusCode(500, new ErrorResponse(ErrorCodes.INTERNAL_SERVER_ERROR, "An error occurred", supportId: supportId));
            }
        }
    }
}
