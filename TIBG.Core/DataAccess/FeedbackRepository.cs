using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TIBG.Contracts.DataAccess;
using TIBG.ENTITIES;
using TIBG.Models;

namespace TIBG.API.Core.DataAccess
{
    /// <summary>
    /// Repository implementation for feedback data access
    /// </summary>
    public class FeedbackRepository : IFeedbackRepository
    {
        private readonly FytAiDbContext _context;
        private readonly ILogger<FeedbackRepository> _logger;

        public FeedbackRepository(FytAiDbContext context, ILogger<FeedbackRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<UserFeedback> AddAsync(UserFeedback feedback)
        {
            try
            {
                _context.Feedbacks.Add(feedback);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Feedback added to database: Id={Id}, Context={Context}", 
                    feedback.Id, feedback.Context);
                
                return feedback;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding feedback to database");
                throw;
            }
        }

        public async Task<UserFeedback?> GetByIdAsync(int id)
        {
            try
            {
                return await _context.Feedbacks.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving feedback by id: {Id}", id);
                throw;
            }
        }

        public async Task<List<UserFeedback>> GetAllAsync()
        {
            try
            {
                return await _context.Feedbacks
                    .OrderByDescending(f => f.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all feedbacks");
                throw;
            }
        }

        public async Task<(List<UserFeedback> feedbacks, int totalCount)> GetPagedAsync(int page, int pageSize)
        {
            try
            {
                var totalCount = await _context.Feedbacks.CountAsync();
                
                var feedbacks = await _context.Feedbacks
                    .OrderByDescending(f => f.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
                
                _logger.LogInformation("Retrieved {Count} feedbacks (page {Page}, pageSize {PageSize})", 
                    feedbacks.Count, page, pageSize);
                
                return (feedbacks, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paged feedbacks");
                throw;
            }
        }

        public async Task<List<UserFeedback>> GetBySportAsync(string sport)
        {
            try
            {
                return await _context.Feedbacks
                    .Where(f => f.Context != null && f.Context.ToLower() == sport.ToLower())
                    .OrderByDescending(f => f.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving feedbacks for sport: {Sport}", sport);
                throw;
            }
        }

        public async Task<double> GetAverageRatingBySportAsync(string sport)
        {
            try
            {
                var feedbacks = await _context.Feedbacks
                    .Where(f => f.Context != null && f.Context.ToLower() == sport.ToLower())
                    .ToListAsync();

                if (!feedbacks.Any())
                {
                    return 0;
                }

                var ratingValues = feedbacks.Select(f => f.Rating switch
                {
                    "Excellent" => 5,
                    "Good" => 4,
                    "Average" => 3,
                    "Poor" => 2,
                    "VeryPoor" => 1,
                    _ => 3
                }).ToList();

                return ratingValues.Average();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating average rating for sport: {Sport}", sport);
                throw;
            }
        }
    }
}
