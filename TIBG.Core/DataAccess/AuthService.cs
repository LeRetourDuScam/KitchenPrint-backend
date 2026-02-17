using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TIBG.Contracts.DataAccess;
using TIBG.ENTITIES;
using TIBG.Models;
using BCrypt.Net;

namespace TIBG.API.Core.DataAccess
{
    public class AuthService : IAuthService
    {
        private readonly FytAiDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly ILogger<AuthService> _logger;
        private readonly IConfiguration _configuration;

        public AuthService(
            FytAiDbContext context, 
            IJwtService jwtService,
            ILogger<AuthService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _jwtService = jwtService;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<AuthResponse?> RegisterAsync(RegisterRequest request, string? ipAddress = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

                if (existingUser != null)
                {
                    _logger.LogWarning("Registration attempt with existing email");
                    return null;
                }

                var passwordHash = HashPassword(request.Password);

                var user = new User
                {
                    Username = request.Username,
                    Email = request.Email,
                    PasswordHash = passwordHash,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email, user.Username);
                var refreshToken = await CreateRefreshTokenAsync(user.Id, ipAddress);

                await transaction.CommitAsync();

                _logger.LogInformation("User registered successfully with ID: {UserId}", user.Id);

                var accessTokenExpiry = DateTime.UtcNow.AddMinutes(
                    int.Parse(_configuration["JwtSettings:AccessTokenExpirationMinutes"] ?? "15"));

                return new AuthResponse
                {
                    Token = accessToken,
                    RefreshToken = refreshToken.Token,
                    Username = user.Username,
                    Email = user.Email,
                    ExpiresAt = accessTokenExpiry
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error during user registration");
                return null;
            }
        }

        public async Task<AuthResponse?> LoginAsync(LoginRequest request, string? ipAddress = null)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

                if (user == null)
                {
                    _logger.LogWarning("Failed login attempt - user not found for email: {Email}", request.Email);
                    return null;
                }

                _logger.LogInformation("Login attempt for user {UserId}. Password length: {PwdLength}, Hash length: {HashLength}", 
                    user.Id, request.Password?.Length ?? 0, user.PasswordHash?.Length ?? 0);

                if (!VerifyPassword(request.Password, user.PasswordHash))
                {
                    _logger.LogWarning("Failed login attempt - password verification failed for user {UserId}", user.Id);
                    return null;
                }

                if (!user.IsActive)
                {
                    _logger.LogWarning("Login attempt for inactive user {UserId}", user.Id);
                    return null;
                }

                user.LastLoginAt = DateTime.UtcNow;

                var oldTokens = await _context.RefreshTokens
                    .Where(t => t.UserId == user.Id && t.RevokedAt == null && t.ExpiresAt > DateTime.UtcNow)
                    .OrderByDescending(t => t.CreatedAt)
                    .Skip(4)
                    .ToListAsync();

                foreach (var token in oldTokens)
                {
                    token.RevokedAt = DateTime.UtcNow;
                    token.RevokedByIp = ipAddress;
                }

                var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email, user.Username);
                var refreshToken = await CreateRefreshTokenAsync(user.Id, ipAddress);

                await _context.SaveChangesAsync();

                _logger.LogInformation("User logged in successfully with ID: {UserId}", user.Id);

                var accessTokenExpiry = DateTime.UtcNow.AddMinutes(
                    int.Parse(_configuration["JwtSettings:AccessTokenExpirationMinutes"] ?? "15"));

                return new AuthResponse
                {
                    Token = accessToken,
                    RefreshToken = refreshToken.Token,
                    Username = user.Username,
                    Email = user.Email,
                    ExpiresAt = accessTokenExpiry
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user login");
                return null;
            }
        }

        public async Task<AuthResponse?> RefreshTokenAsync(string refreshToken, string? ipAddress = null)
        {
            try
            {
                var token = await _context.RefreshTokens
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.Token == refreshToken);

                if (token == null || !token.IsActive)
                {
                    _logger.LogWarning("Invalid or inactive refresh token");
                    return null;
                }

                token.RevokedAt = DateTime.UtcNow;
                token.RevokedByIp = ipAddress;

                var user = token.User;
                var newAccessToken = _jwtService.GenerateAccessToken(user.Id, user.Email, user.Username);
                var newRefreshToken = await CreateRefreshTokenAsync(user.Id, ipAddress);

                await _context.SaveChangesAsync();

                _logger.LogInformation("Tokens refreshed for user ID: {UserId}", user.Id);

                var accessTokenExpiry = DateTime.UtcNow.AddMinutes(
                    int.Parse(_configuration["JwtSettings:AccessTokenExpirationMinutes"] ?? "15"));

                return new AuthResponse
                {
                    Token = newAccessToken,
                    RefreshToken = newRefreshToken.Token,
                    Username = user.Username,
                    Email = user.Email,
                    ExpiresAt = accessTokenExpiry
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return null;
            }
        }

        public async Task<bool> RevokeTokenAsync(string refreshToken, string? ipAddress = null)
        {
            try
            {
                var token = await _context.RefreshTokens
                    .FirstOrDefaultAsync(t => t.Token == refreshToken);

                if (token == null || !token.IsActive)
                {
                    return false;
                }

                token.RevokedAt = DateTime.UtcNow;
                token.RevokedByIp = ipAddress;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Token revoked for user ID: {UserId}", token.UserId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking token");
                return false;
            }
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && u.IsActive);
        }

        private async Task<RefreshToken> CreateRefreshTokenAsync(int userId, string? ipAddress)
        {
            var refreshTokenDays = int.Parse(_configuration["JwtSettings:RefreshTokenExpirationDays"] ?? "7");

            var refreshToken = new RefreshToken
            {
                UserId = userId,
                Token = _jwtService.GenerateRefreshToken(),
                ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenDays),
                CreatedAt = DateTime.UtcNow,
                CreatedByIp = ipAddress
            };

            _context.RefreshTokens.Add(refreshToken);
            return refreshToken;
        }
        private static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        }

        private static bool VerifyPassword(string password, string hash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch
            {
                return false;
            }
        }
    }
}
