using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TIBG.Contracts.DataAccess;
using TIBG.ENTITIES;
using TIBG.Models;

namespace TIBG.API.Core.DataAccess
{
    /// <summary>
    /// Repository implementation for recipe data access
    /// </summary>
    public class RecipeRepository : IRecipeRepository
    {
        private readonly FytAiDbContext _context;
        private readonly ILogger<RecipeRepository> _logger;

        public RecipeRepository(FytAiDbContext context, ILogger<RecipeRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Recipe> AddAsync(Recipe recipe)
        {
            try
            {
                _context.Recipes.Add(recipe);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Recipe added: Id={Id}, Name={Name}, UserId={UserId}",
                    recipe.Id, recipe.Name, recipe.UserId);

                return recipe;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding recipe: {Name}", recipe.Name);
                throw;
            }
        }

        public async Task<Recipe?> GetByIdAsync(int id)
        {
            try
            {
                return await _context.Recipes
                    .FirstOrDefaultAsync(r => r.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recipe by id: {Id}", id);
                throw;
            }
        }

        public async Task<Recipe?> GetByIdWithIngredientsAsync(int id)
        {
            try
            {
                return await _context.Recipes
                    .Include(r => r.RecipeIngredients)
                        .ThenInclude(ri => ri.Ingredient)
                    .FirstOrDefaultAsync(r => r.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recipe with ingredients by id: {Id}", id);
                throw;
            }
        }

        public async Task<List<Recipe>> GetByUserIdAsync(int userId)
        {
            try
            {
                return await _context.Recipes
                    .Where(r => r.UserId == userId)
                    .Include(r => r.RecipeIngredients)
                        .ThenInclude(ri => ri.Ingredient)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recipes for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<(List<Recipe> recipes, int totalCount)> GetPagedByUserIdAsync(int userId, int page, int pageSize)
        {
            try
            {
                var query = _context.Recipes.Where(r => r.UserId == userId);
                var totalCount = await query.CountAsync();

                var recipes = await query
                    .Include(r => r.RecipeIngredients)
                        .ThenInclude(ri => ri.Ingredient)
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} recipes for user {UserId} (page {Page})",
                    recipes.Count, userId, page);

                return (recipes, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paged recipes for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<Recipe> UpdateAsync(Recipe recipe)
        {
            try
            {
                recipe.UpdatedAt = DateTime.UtcNow;
                _context.Recipes.Update(recipe);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Recipe updated: Id={Id}", recipe.Id);

                return recipe;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating recipe: {Id}", recipe.Id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var recipe = await _context.Recipes
                    .Include(r => r.RecipeIngredients)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (recipe == null)
                {
                    return false;
                }

                _context.Recipes.Remove(recipe);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Recipe deleted: Id={Id}", id);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting recipe: {Id}", id);
                throw;
            }
        }
    }
}
