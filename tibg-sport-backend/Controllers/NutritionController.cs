using Microsoft.AspNetCore.Mvc;
using TIBG.Contracts.DataAccess;
using TIBG.Models;

namespace tibg_sport_backend.Controllers
{
    /// <summary>
    /// Controller for nutritional data
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    public class NutritionController : ControllerBase
    {
        private readonly INutritionService _nutritionService;
        private readonly ILogger<NutritionController> _logger;

        public NutritionController(
            INutritionService nutritionService,
            ILogger<NutritionController> logger)
        {
            _nutritionService = nutritionService;
            _logger = logger;
        }

        /// <summary>
        /// Get nutritional information for an ingredient
        /// </summary>
        [HttpGet("ingredients/{id}")]
        public async Task<IActionResult> GetIngredientNutrition(int id)
        {
            try
            {
                var nutrition = await _nutritionService.GetNutritionByIngredientIdAsync(id);
                if (nutrition == null)
                {
                    return NotFound(new { error = $"Ingredient with ID {id} not found" });
                }
                return Ok(new { data = nutrition });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting nutrition for ingredient {Id}", id);
                return StatusCode(500, new { error = "An error occurred while retrieving nutritional data" });
            }
        }

        /// <summary>
        /// Get nutritional breakdown for a recipe (list of ingredients with quantities)
        /// </summary>
        [HttpPost("recipe")]
        public async Task<IActionResult> GetRecipeNutrition([FromBody] List<RecipeIngredientRequest> ingredients)
        {
            try
            {
                if (ingredients == null || ingredients.Count == 0)
                {
                    return BadRequest(new { error = "At least one ingredient is required" });
                }

                var nutrition = await _nutritionService.GetRecipeNutritionAsync(ingredients);
                return Ok(new { data = nutrition });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating recipe nutrition");
                return StatusCode(500, new { error = "An error occurred while calculating recipe nutrition" });
            }
        }
    }
}
