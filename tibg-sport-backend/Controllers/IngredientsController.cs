using Microsoft.AspNetCore.Mvc;
using TIBG.Contracts.DataAccess;
using TIBG.Models;

namespace tibg_sport_backend.Controllers
{
    /// <summary>
    /// Controller for ingredient management and search
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    public class IngredientsController : ControllerBase
    {
        private readonly IIngredientService _ingredientService;
        private readonly ILogger<IngredientsController> _logger;

        public IngredientsController(
            IIngredientService ingredientService,
            ILogger<IngredientsController> logger)
        {
            _ingredientService = ingredientService;
            _logger = logger;
        }

        /// <summary>
        /// Search ingredients with filters and pagination
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> Search(
            [FromQuery] string? query,
            [FromQuery] string? category,
            [FromQuery] string? season,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var request = new IngredientSearchRequest
                {
                    Query = query,
                    Category = category,
                    Season = season,
                    Page = page,
                    PageSize = pageSize
                };

                var result = await _ingredientService.SearchAsync(request);

                return Ok(new
                {
                    data = result.Ingredients,
                    pagination = new
                    {
                        currentPage = result.Page,
                        pageSize = result.PageSize,
                        totalCount = result.TotalCount,
                        totalPages = result.TotalPages
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching ingredients");
                return StatusCode(500, new { error = "An error occurred while searching ingredients" });
            }
        }

        /// <summary>
        /// Get ingredient by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var ingredient = await _ingredientService.GetByIdAsync(id);

                if (ingredient == null)
                {
                    return NotFound(new { error = $"Ingredient with ID {id} not found" });
                }

                return Ok(new { data = ingredient });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving ingredient: {Id}", id);
                return StatusCode(500, new { error = "An error occurred while retrieving the ingredient" });
            }
        }

        /// <summary>
        /// Sync ingredients from external APIs (Agribalyse, Open Food Facts)
        /// </summary>
        [HttpPost("sync")]
        public async Task<IActionResult> SyncFromExternalApi([FromBody] List<string> ingredientNames)
        {
            try
            {
                if (ingredientNames == null || ingredientNames.Count == 0)
                {
                    return BadRequest(new { error = "Ingredient names list is required" });
                }

                if (ingredientNames.Count > 50)
                {
                    return BadRequest(new { error = "Maximum 50 ingredients can be synced at once" });
                }

                _logger.LogInformation("Syncing {Count} ingredients from external API", ingredientNames.Count);

                var syncedIngredients = await _ingredientService.SyncFromExternalApiAsync(ingredientNames);

                return Ok(new
                {
                    message = $"Successfully synced {syncedIngredients.Count} out of {ingredientNames.Count} ingredients",
                    data = syncedIngredients
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing ingredients from external API");
                return StatusCode(500, new { error = "An error occurred while syncing ingredients" });
            }
        }

        /// <summary>
        /// Get autocomplete suggestions for ingredient search
        /// </summary>
        [HttpGet("autocomplete")]
        public async Task<IActionResult> Autocomplete([FromQuery] string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                {
                    return BadRequest(new { error = "Query must be at least 2 characters long" });
                }

                var request = new IngredientSearchRequest
                {
                    Query = query,
                    Page = 1,
                    PageSize = 10
                };

                var result = await _ingredientService.SearchAsync(request);

                // Return essential fields for autocomplete (includes season & CO2 for UI)
                var suggestions = result.Ingredients.Select(i => new
                {
                    i.Id,
                    i.Name,
                    i.Category,
                    i.Season,
                    i.CarbonEmissionKgPerKg
                }).ToList();

                return Ok(new { data = suggestions });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting autocomplete suggestions");
                return StatusCode(500, new { error = "An error occurred while getting suggestions" });
            }
        }
    }
}
