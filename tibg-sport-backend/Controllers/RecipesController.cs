using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TIBG.Contracts.DataAccess;
using TIBG.Models;

namespace tibg_sport_backend.Controllers
{
    /// <summary>
    /// Controller for recipe calculation and management
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    public class RecipesController : ControllerBase
    {
        private readonly IRecipeService _recipeService;
        private readonly ILogger<RecipesController> _logger;

        public RecipesController(
            IRecipeService recipeService,
            ILogger<RecipesController> logger)
        {
            _recipeService = recipeService;
            _logger = logger;
        }

        /// <summary>
        /// Calculate environmental impact of a recipe
        /// Optionally save if user is authenticated
        /// </summary>
        [HttpPost("calculate")]
        public async Task<IActionResult> CalculateImpact(
            [FromBody] RecipeCalculationRequest request,
            [FromQuery] bool save = false)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(new { error = "Validation failed", details = errors });
                }

                // Get user ID if authenticated
                int? userId = null;
                if (User.Identity?.IsAuthenticated == true)
                {
                    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (int.TryParse(userIdClaim, out var parsedUserId))
                    {
                        userId = parsedUserId;
                    }
                }

                // If save is requested but user is not authenticated, return error
                if (save && !userId.HasValue)
                {
                    return Unauthorized(new { error = "You must be logged in to save recipes" });
                }

                _logger.LogInformation("Calculating impact for recipe: {Name}", request.Name);

                var result = await _recipeService.CalculateImpactAsync(request, userId, save);

                return Ok(new
                {
                    data = result,
                    message = save && result.RecipeId.HasValue
                        ? "Recipe calculated and saved successfully"
                        : "Recipe calculated successfully"
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid input for recipe calculation");
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating recipe impact");
                return StatusCode(500, new { error = "An error occurred while calculating recipe impact" });
            }
        }

        /// <summary>
        /// Get user's saved recipes (requires authentication)
        /// </summary>
        [HttpGet("my-recipes")]
        [Authorize]
        public async Task<IActionResult> GetMyRecipes(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { error = "Invalid user ID" });
                }

                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var (recipes, totalCount) = await _recipeService.GetUserRecipesAsync(userId, page, pageSize);

                return Ok(new
                {
                    data = recipes,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize = pageSize,
                        totalCount = totalCount,
                        totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user recipes");
                return StatusCode(500, new { error = "An error occurred while retrieving recipes" });
            }
        }

        /// <summary>
        /// Get a specific recipe by ID (requires authentication and ownership)
        /// </summary>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { error = "Invalid user ID" });
                }

                var recipe = await _recipeService.GetByIdAsync(id, userId);

                if (recipe == null)
                {
                    return NotFound(new { error = "Recipe not found or you don't have permission to access it" });
                }

                return Ok(new { data = recipe });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recipe: {Id}", id);
                return StatusCode(500, new { error = "An error occurred while retrieving the recipe" });
            }
        }

        /// <summary>
        /// Delete a recipe (requires authentication and ownership)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { error = "Invalid user ID" });
                }

                var success = await _recipeService.DeleteAsync(id, userId);

                if (!success)
                {
                    return NotFound(new { error = "Recipe not found or you don't have permission to delete it" });
                }

                return Ok(new { message = "Recipe deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting recipe: {Id}", id);
                return StatusCode(500, new { error = "An error occurred while deleting the recipe" });
            }
        }
    }
}
