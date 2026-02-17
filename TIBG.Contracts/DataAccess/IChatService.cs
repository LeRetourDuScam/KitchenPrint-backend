using TIBG.Models;

namespace TIBG.API.Core.DataAccess
{
    public interface IChatService
    {
        Task<string> GetHealthChatResponseAsync(HealthChatRequest request);
        Task<ExercisesResponse> GetRecommendedExercisesAsync(ExercisesRequest request);
    }
}
