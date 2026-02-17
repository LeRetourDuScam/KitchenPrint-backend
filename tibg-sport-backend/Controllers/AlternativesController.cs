using Microsoft.AspNetCore.Mvc;
using TIBG.Contracts.DataAccess;

namespace tibg_sport_backend.Controllers
{
    /// <summary>
    /// Controller for ingredient alternative suggestions (AI-powered)
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AlternativesController : ControllerBase
    {
        private readonly IAlternativesService _alternativesService;
        private readonly ILogger<AlternativesController> _logger;

        public AlternativesController(
            IAlternativesService alternativesService,
            ILogger<AlternativesController> logger)
        {
            _alternativesService = alternativesService;
            _logger = logger;
        }

        /// <summary>
        /// Get alternative ingredient suggestions by ingredient ID
        /// Returns lower-impact alternatives from the database + AI insights
        /// </summary>
        [HttpGet("ingredients/{id}/alternatives")]
        public async Task<IActionResult> GetAlternatives(int id)
        {
            try
            {
                _logger.LogInformation("Getting alternatives for ingredient ID: {Id}", id);
                var result = await _alternativesService.GetAlternativesAsync(id);
                return Ok(new { data = result });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Ingredient not found: {Id}", id);
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Database operation failed for ingredient {Id}", id);
                return StatusCode(500, new { error = "Database error. Please ensure the database is seeded with ingredients.", details = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting alternatives for ingredient {Id}. Message: {Message}", id, ex.Message);
                return StatusCode(500, new { error = "An error occurred while getting alternative suggestions", details = ex.Message });
            }
        }

        /// <summary>
        /// Get alternative suggestions by ingredient name (search-based)
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> GetAlternativesByName(
            [FromQuery] string name,
            [FromQuery] string? category = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return BadRequest(new { error = "Ingredient name is required" });
                }

                _logger.LogInformation("Getting alternatives for ingredient: {Name}", name);
                var result = await _alternativesService.GetAlternativesByNameAsync(name, category);
                return Ok(new { data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting alternatives for ingredient {Name}", name);
                return StatusCode(500, new { error = "An error occurred while getting alternative suggestions" });
            }
        }
    }
}
