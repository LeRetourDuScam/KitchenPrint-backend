namespace TIBG.Contracts.DataAccess
{
    public interface IJwtService
    {
        string GenerateAccessToken(int userId, string email, string username);

        string GenerateRefreshToken();

        int? ValidateToken(string token);

        [Obsolete("Use GenerateAccessToken instead")]
        string GenerateToken(int userId, string email, string username);
    }
}
