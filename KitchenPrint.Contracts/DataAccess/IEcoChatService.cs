using KitchenPrint.Core.Models;

namespace KitchenPrint.Contracts.DataAccess
{
    public interface IEcoChatService
    {
        Task<string> SendMessageAsync(EcoChatRequest request);
    }
}
