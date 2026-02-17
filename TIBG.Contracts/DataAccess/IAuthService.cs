using TIBG.Models;

namespace TIBG.Contracts.DataAccess
{
    /// <summary>
    /// Interface for authentication service
    /// </summary>
    public interface IAuthService
    {
        Task<AuthResponse?> RegisterAsync(RegisterRequest request, string? ipAddress = null);
        Task<AuthResponse?> LoginAsync(LoginRequest request, string? ipAddress = null);
        Task<AuthResponse?> RefreshTokenAsync(string refreshToken, string? ipAddress = null);
        Task<bool> RevokeTokenAsync(string refreshToken, string? ipAddress = null);
        Task<User?> GetUserByIdAsync(int userId);
        Task<User?> GetUserByEmailAsync(string email);
    }
}
