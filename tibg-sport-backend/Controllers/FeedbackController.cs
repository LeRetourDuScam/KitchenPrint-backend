using Microsoft.AspNetCore.Mvc;
using TIBG.Contracts.DataAccess;
using TIBG.Models;

namespace tibg_sport_backend.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class FeedbackController : ControllerBase
    {
        private readonly IFeedbackService _feedbackService;
        private readonly ILogger<FeedbackController> _logger;

        public FeedbackController(IFeedbackService feedbackService, ILogger<FeedbackController> logger)
        {
            _feedbackService = feedbackService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> SubmitFeedback([FromBody] UserFeedback feedback)
        {
            if (feedback == null)
            {
                return BadRequest(new { error = "Feedback data is required" });
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new { error = "Validation failed", details = errors });
            }

            if (string.IsNullOrWhiteSpace(feedback.Rating))
            {
                return BadRequest(new { error = "Rating is required" });
            }

            var validRatings = new[] { "perfect", "good", "meh", "bad" };
            if (!validRatings.Contains(feedback.Rating.ToLower()))
            {
                return BadRequest(new { error = "Invalid rating value" });
            }

            feedback.CreatedAt = DateTime.UtcNow;

            var success = await _feedbackService.SaveFeedbackAsync(feedback);

            if (!success)
            {
                return StatusCode(500, new { error = "Failed to save feedback" });
            }

            return Ok(new { message = "Feedback submitted successfully" });
        }

        [HttpGet]
        public async Task<IActionResult> GetAllFeedbacks([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var (feedbacks, totalCount) = await _feedbackService.GetPagedFeedbacksAsync(page, pageSize);
            
            return Ok(new 
            { 
                data = feedbacks,
                pagination = new 
                {
                    currentPage = page,
                    pageSize = pageSize,
                    totalCount = totalCount,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                }
            });
        }
    }
}
