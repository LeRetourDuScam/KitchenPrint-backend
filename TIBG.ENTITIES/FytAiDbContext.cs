using Microsoft.EntityFrameworkCore;
using TIBG.Models;

namespace TIBG.ENTITIES
{
    /// <summary>
    /// Database context for CarbonFootprint application
    /// Renamed from FytAiDbContext for environmental domain
    /// </summary>
    public class FytAiDbContext : DbContext
    {
        public FytAiDbContext(DbContextOptions<FytAiDbContext> options) : base(options)
        {
        }

        // Authentication & Users
        public DbSet<User> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        // Environmental Impact Entities
        public DbSet<Ingredient> Ingredients { get; set; }
        public DbSet<Recipe> Recipes { get; set; }
        public DbSet<RecipeIngredient> RecipeIngredients { get; set; }

        // Feedback
        public DbSet<UserFeedback> Feedbacks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Email).IsRequired();
                entity.Property(e => e.Username).IsRequired();
                entity.Property(e => e.PasswordHash).IsRequired();
            });

            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Token).IsUnique();
                entity.HasIndex(e => e.UserId);
                entity.Property(e => e.Token).IsRequired();
                entity.Property(e => e.UserId).IsRequired();
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<UserFeedback>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.RecipeId);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasOne(e => e.Recipe)
                    .WithMany()
                    .HasForeignKey(e => e.RecipeId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Ingredient entity configuration
            modelBuilder.Entity<Ingredient>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.Category);
                entity.HasIndex(e => e.ExternalId).IsUnique();
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.CarbonEmissionKgPerKg).HasPrecision(10, 4);
                entity.Property(e => e.WaterFootprintLitersPerKg).HasPrecision(10, 2);
            });

            // Recipe entity configuration
            modelBuilder.Entity<Recipe>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.EcoScore);
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.TotalCarbonKg).HasPrecision(10, 4);
                entity.Property(e => e.TotalWaterLiters).HasPrecision(10, 2);
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // RecipeIngredient entity configuration
            modelBuilder.Entity<RecipeIngredient>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.RecipeId);
                entity.HasIndex(e => e.IngredientId);
                entity.Property(e => e.QuantityGrams).HasPrecision(10, 2);
                entity.Property(e => e.CarbonContributionKg).HasPrecision(10, 4);
                entity.Property(e => e.WaterContributionLiters).HasPrecision(10, 2);
                entity.HasOne(e => e.Recipe)
                    .WithMany(r => r.RecipeIngredients)
                    .HasForeignKey(e => e.RecipeId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Ingredient)
                    .WithMany()
                    .HasForeignKey(e => e.IngredientId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
