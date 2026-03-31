using KitchenPrint.Contracts.DataAccess;
using KitchenPrint.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace KitchenPrint_backend.Controllers
{
    [ApiController]
    [Route("api/v1/eco-chat")]
    public class EcoChatController : ControllerBase
    {
        private readonly IEcoChatService _ecoChatService;
        private readonly ILogger<EcoChatController> _logger;

        public EcoChatController(
            IEcoChatService ecoChatService,
            ILogger<EcoChatController> logger)
        {
            _ecoChatService = ecoChatService;
            _logger = logger;
        }

        /// <summary>
        /// Send a message to the eco chatbot
        /// </summary>
        [HttpPost("message")]
        public async Task<IActionResult> SendMessage([FromBody] EcoChatRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.UserMessage))
                {
                    return BadRequest(new ErrorResponse(ErrorCodes.VALIDATION_FAILED, "User message is required"));
                }

                _logger.LogInformation("Eco chat message received for recipe: {RecipeName}", request.RecipeName ?? "N/A");
                var response = await _ecoChatService.SendMessageAsync(request);
                return Ok(new { data = response });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing eco chat message");
                return StatusCode(500, new ErrorResponse(ErrorCodes.INTERNAL_SERVER_ERROR, "An error occurred while processing your message"));
            }
        }
    }
}
