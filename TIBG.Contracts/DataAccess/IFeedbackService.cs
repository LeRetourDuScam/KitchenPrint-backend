using TIBG.Models;

namespace TIBG.Contracts.DataAccess
{
    public interface IFeedbackService
    {
        Task<bool> SaveFeedbackAsync(UserFeedback feedback);
        Task<List<UserFeedback>> GetAllFeedbacksAsync();
        Task<(List<UserFeedback> feedbacks, int totalCount)> GetPagedFeedbacksAsync(int page, int pageSize);
    }
}
