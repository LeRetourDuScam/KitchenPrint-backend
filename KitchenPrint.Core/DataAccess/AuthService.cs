using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using BCrypt.Net;
using KitchenPrint.Contracts.DataAccess;
using KitchenPrint.Core.Models;
using KitchenPrint.ENTITIES;

namespace KitchenPrint.API.Core.DataAccess
{
    public class AuthService : IAuthService
    {
        private readonly kitchenPrintDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly ILogger<AuthService> _logger;
        private readonly IConfiguration _configuration;

        public AuthService(
            kitchenPrintDbContext context, 
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

        public async Task<UserProfileResponse?> GetProfileAsync(int userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
            if (user == null) return null;

            var recipeCount = await _context.Recipes.CountAsync(r => r.UserId == userId);

            return new UserProfileResponse
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username,
                CreatedAt = user.CreatedAt,
                RecipeCount = recipeCount
            };
        }

        public async Task<UserProfileResponse?> UpdateProfileAsync(int userId, UpdateProfileRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
            if (user == null) return null;

            user.Username = request.Username;
            await _context.SaveChangesAsync();

            return await GetProfileAsync(userId);
        }

        public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
            if (user == null) return false;

            if (!VerifyPassword(request.CurrentPassword, user.PasswordHash))
            {
                _logger.LogWarning("Change password failed — wrong current password for user {UserId}", userId);
                return false;
            }

            user.PasswordHash = HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Password changed for user {UserId}", userId);
            return true;
        }

        public async Task<bool> DeleteAccountAsync(int userId, string password)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
                if (user == null) return false;

                if (!VerifyPassword(password, user.PasswordHash))
                {
                    _logger.LogWarning("Delete account failed — wrong password for user {UserId}", userId);
                    return false;
                }

                // Remove refresh tokens
                var tokens = await _context.RefreshTokens.Where(t => t.UserId == userId).ToListAsync();
                _context.RefreshTokens.RemoveRange(tokens);

                // Remove recipe ingredients then recipes
                var recipes = await _context.Recipes
                    .Include(r => r.RecipeIngredients)
                    .Where(r => r.UserId == userId)
                    .ToListAsync();

                foreach (var recipe in recipes)
                {
                    _context.RecipeIngredients.RemoveRange(recipe.RecipeIngredients);
                }
                _context.Recipes.RemoveRange(recipes);

                // Remove user
                _context.Users.Remove(user);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Account deleted for user {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error deleting account for user {UserId}", userId);
                return false;
            }
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
