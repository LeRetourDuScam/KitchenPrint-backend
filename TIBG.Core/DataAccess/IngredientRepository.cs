using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TIBG.Contracts.DataAccess;
using TIBG.ENTITIES;
using TIBG.Models;

namespace TIBG.API.Core.DataAccess
{
    /// <summary>
    /// Repository implementation for ingredient data access
    /// </summary>
    public class IngredientRepository : IIngredientRepository
    {
        private readonly FytAiDbContext _context;
        private readonly ILogger<IngredientRepository> _logger;

        public IngredientRepository(FytAiDbContext context, ILogger<IngredientRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Ingredient> AddAsync(Ingredient ingredient)
        {
            try
            {
                _context.Ingredients.Add(ingredient);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Ingredient added: Id={Id}, Name={Name}",
                    ingredient.Id, ingredient.Name);

                return ingredient;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding ingredient: {Name}", ingredient.Name);
                throw;
            }
        }

        public async Task<Ingredient?> GetByIdAsync(int id)
        {
            try
            {
                return await _context.Ingredients
                    .Where(i => i.IsActive)
                    .FirstOrDefaultAsync(i => i.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving ingredient by id: {Id}", id);
                throw;
            }
        }

        public async Task<Ingredient?> GetByExternalIdAsync(string externalId)
        {
            try
            {
                return await _context.Ingredients
                    .Where(i => i.IsActive)
                    .FirstOrDefaultAsync(i => i.ExternalId == externalId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving ingredient by external id: {ExternalId}", externalId);
                throw;
            }
        }

        public async Task<List<Ingredient>> GetAllAsync()
        {
            try
            {
                return await _context.Ingredients
                    .Where(i => i.IsActive)
                    .OrderBy(i => i.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all ingredients");
                throw;
            }
        }

        public async Task<(List<Ingredient> ingredients, int totalCount)> GetPagedAsync(int page, int pageSize)
        {
            try
            {
                var query = _context.Ingredients.Where(i => i.IsActive);
                var totalCount = await query.CountAsync();

                var ingredients = await query
                    .OrderBy(i => i.Name)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} ingredients (page {Page})",
                    ingredients.Count, page);

                return (ingredients, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paged ingredients");
                throw;
            }
        }

        public async Task<(List<Ingredient> ingredients, int totalCount)> SearchAsync(
            string? query, string? category, string? season, int page, int pageSize)
        {
            try
            {
                var ingredientsQuery = _context.Ingredients.Where(i => i.IsActive);

                if (!string.IsNullOrWhiteSpace(query))
                {
                    var searchTerm = query.ToLower();
                    ingredientsQuery = ingredientsQuery.Where(i =>
                        i.Name.ToLower().Contains(searchTerm));
                }

                if (!string.IsNullOrWhiteSpace(category))
                {
                    ingredientsQuery = ingredientsQuery.Where(i =>
                        i.Category != null && i.Category.ToLower() == category.ToLower());
                }

                if (!string.IsNullOrWhiteSpace(season))
                {
                    ingredientsQuery = ingredientsQuery.Where(i =>
                        i.Season != null && (i.Season.ToLower() == season.ToLower() || i.Season.ToLower() == "all-year"));
                }

                var totalCount = await ingredientsQuery.CountAsync();

                var ingredients = await ingredientsQuery
                    .OrderBy(i => i.Name)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                _logger.LogInformation("Search returned {Count} ingredients for query: {Query}",
                    ingredients.Count, query);

                return (ingredients, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching ingredients with query: {Query}", query);
                throw;
            }
        }

        public async Task<List<Ingredient>> GetByCategoryAsync(string category)
        {
            try
            {
                return await _context.Ingredients
                    .Where(i => i.IsActive && i.Category != null && i.Category.ToLower() == category.ToLower())
                    .OrderBy(i => i.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving ingredients for category: {Category}", category);
                throw;
            }
        }

        public async Task<Ingredient> UpdateAsync(Ingredient ingredient)
        {
            try
            {
                ingredient.UpdatedAt = DateTime.UtcNow;
                _context.Ingredients.Update(ingredient);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Ingredient updated: Id={Id}", ingredient.Id);

                return ingredient;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating ingredient: {Id}", ingredient.Id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var ingredient = await _context.Ingredients.FindAsync(id);
                if (ingredient == null)
                {
                    return false;
                }

                // Soft delete
                ingredient.IsActive = false;
                ingredient.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Ingredient soft-deleted: Id={Id}", id);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting ingredient: {Id}", id);
                throw;
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            try
            {
                return await _context.Ingredients
                    .Where(i => i.IsActive)
                    .AnyAsync(i => i.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking ingredient existence: {Id}", id);
                throw;
            }
        }
    }
}
