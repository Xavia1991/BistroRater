using Database.Model;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection.Emit;

namespace Database;

public class BistroContext : DbContext
{
    public DbSet<DailyMeal> DailyMeals => Set<DailyMeal>();
    public DbSet<MealRating> MealRatings => Set<MealRating>();

    public BistroContext(DbContextOptions<BistroContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DailyMeal>(entity =>
        {
            entity.Property(x => x.Date)
                  .HasConversion<DateOnlyConverter>();
            entity.HasIndex(x => x.Description);
        });

        modelBuilder.Entity<MealRating>(entity =>
        {
            entity.HasIndex(x => new { x.DailyMealId, x.UserId }).IsUnique();
            entity.Property(x => x.Stars)
                  .IsRequired();
        });

    }
}
