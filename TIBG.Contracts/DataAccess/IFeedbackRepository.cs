using TIBG.Models;

namespace TIBG.Contracts.DataAccess
{
    /// <summary>
    /// Repository interface for feedback data access
    /// </summary>
    public interface IFeedbackRepository
    {
        Task<UserFeedback> AddAsync(UserFeedback feedback);
        Task<UserFeedback?> GetByIdAsync(int id);
        Task<List<UserFeedback>> GetAllAsync();
        Task<(List<UserFeedback> feedbacks, int totalCount)> GetPagedAsync(int page, int pageSize);
        Task<List<UserFeedback>> GetBySportAsync(string sport);
        Task<double> GetAverageRatingBySportAsync(string sport);
    }
}
