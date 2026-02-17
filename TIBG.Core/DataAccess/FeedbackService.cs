using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using TIBG.Contracts.DataAccess;
using TIBG.Models;

namespace TIBG.API.Core.DataAccess
{
    /// <summary>
    /// Service for feedback business logic using Repository Pattern
    /// </summary>
    public class FeedbackService : IFeedbackService
    {
        private readonly IFeedbackRepository _repository;
        private readonly ILogger<FeedbackService> _logger;

        public FeedbackService(IFeedbackRepository repository, ILogger<FeedbackService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<bool> SaveFeedbackAsync(UserFeedback feedback)
        {
            try
            {
                await _repository.AddAsync(feedback);
                
                _logger.LogInformation("Feedback saved successfully: Rating={Rating}, Context={Context}", 
                    feedback.Rating, feedback.Context);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save feedback");
                return false;
            }
        }

        public async Task<List<UserFeedback>> GetAllFeedbacksAsync()
        {
            try
            {
                return await _repository.GetAllAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all feedbacks");
                return new List<UserFeedback>();
            }
        }

        public async Task<(List<UserFeedback> feedbacks, int totalCount)> GetPagedFeedbacksAsync(int page, int pageSize)
        {
            try
            {
                return await _repository.GetPagedAsync(page, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paged feedbacks");
                return (new List<UserFeedback>(), 0);
            }
        }
    }
}
