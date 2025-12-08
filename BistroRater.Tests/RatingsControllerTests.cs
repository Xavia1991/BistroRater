using Database;
using Database.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Contract.Model.Requests;
using Contract.Model.DTO;
using BistroRater.Controllers;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace BistroRater.Tests;

public class RatingsControllerTests
{
    private BistroContext GetInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<BistroContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new BistroContext(options);
    }

    private RatingsController CreateControllerWithUser(BistroContext context, string userName = "testuser")
    {
        var controller = new RatingsController(context);
        
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, userName)
        }, "TestAuthentication"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        return controller;
    }

    [Fact]
    public async Task RateMeal_WithValidTodayMeal_CreatesRating()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var today = DateOnly.FromDateTime(DateTime.Now.Date);
        var meal = new DailyMeal
        {
            DayNumber = today.DayNumber,
            Description = "Test Meal",
            Option = DailyMeal.MealOption.Just_Good_Food
        };
        context.DailyMeals.Add(meal);
        await context.SaveChangesAsync();

        var controller = CreateControllerWithUser(context);
        var request = new RateMealRequest(meal.Id, 4, "testuser");

        // Act
        var result = await controller.RateMeal(request);

        // Assert
        Assert.IsType<OkResult>(result);
        var rating = await context.MealRatings.FirstOrDefaultAsync(r => r.DailyMealId == meal.Id);
        Assert.NotNull(rating);
        Assert.Equal(4, rating.Stars);
        Assert.Equal("testuser", rating.UserId);
    }

    [Fact]
    public async Task RateMeal_WithPastMeal_ReturnsBadRequest()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var yesterday = DateOnly.FromDateTime(DateTime.Now.Date.AddDays(-1));
        var meal = new DailyMeal
        {
            DayNumber = yesterday.DayNumber,
            Description = "Yesterday's Meal",
            Option = DailyMeal.MealOption.Just_Good_Food
        };
        context.DailyMeals.Add(meal);
        await context.SaveChangesAsync();

        var controller = CreateControllerWithUser(context);
        var request = new RateMealRequest(meal.Id, 5, "testuser");

        // Act
        var result = await controller.RateMeal(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Only today's meals can be rated.", badRequestResult.Value);
    }

    [Fact]
    public async Task RateMeal_WithInvalidStars_ReturnsBadRequest()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var today = DateOnly.FromDateTime(DateTime.Now.Date);
        var meal = new DailyMeal
        {
            DayNumber = today.DayNumber,
            Description = "Test Meal",
            Option = DailyMeal.MealOption.Just_Good_Food
        };
        context.DailyMeals.Add(meal);
        await context.SaveChangesAsync();

        var controller = CreateControllerWithUser(context);
        var request = new RateMealRequest(meal.Id, 6, "testuser"); // Invalid: > 5

        // Act
        var result = await controller.RateMeal(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Stars must be between 1 and 5.", badRequestResult.Value);
    }

    [Fact]
    public async Task RateMeal_UpdateExistingRating_UpdatesStars()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var today = DateOnly.FromDateTime(DateTime.Now.Date);
        var meal = new DailyMeal
        {
            DayNumber = today.DayNumber,
            Description = "Test Meal",
            Option = DailyMeal.MealOption.Just_Good_Food
        };
        context.DailyMeals.Add(meal);
        await context.SaveChangesAsync();

        var existingRating = new MealRating
        {
            DailyMealId = meal.Id,
            UserId = "testuser",
            Stars = 3,
            DayNumber = today.DayNumber
        };
        context.MealRatings.Add(existingRating);
        await context.SaveChangesAsync();

        var controller = CreateControllerWithUser(context);
        var request = new RateMealRequest(meal.Id, 5, "testuser");

        // Act
        var result = await controller.RateMeal(request);

        // Assert
        Assert.IsType<OkResult>(result);
        var updatedRating = await context.MealRatings.FirstOrDefaultAsync(r => r.UserId == "testuser");
        Assert.NotNull(updatedRating);
        Assert.Equal(5, updatedRating.Stars);
    }

    [Fact]
    public async Task RateMeal_RatingDifferentMealSameDay_ReturnsBadRequest()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var today = DateOnly.FromDateTime(DateTime.Now.Date);
        var meal1 = new DailyMeal
        {
            DayNumber = today.DayNumber,
            Description = "Meal 1",
            Option = DailyMeal.MealOption.Just_Good_Food
        };
        var meal2 = new DailyMeal
        {
            DayNumber = today.DayNumber,
            Description = "Meal 2",
            Option = DailyMeal.MealOption.Grill_Sandwiches
        };
        context.DailyMeals.AddRange(meal1, meal2);
        await context.SaveChangesAsync();

        // User already rated meal1
        var existingRating = new MealRating
        {
            DailyMealId = meal1.Id,
            UserId = "testuser",
            Stars = 4,
            DayNumber = today.DayNumber
        };
        context.MealRatings.Add(existingRating);
        await context.SaveChangesAsync();

        var controller = CreateControllerWithUser(context);
        var request = new RateMealRequest(meal2.Id, 5, "testuser"); // Try to rate different meal

        // Act
        var result = await controller.RateMeal(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Du hast heute schon ein anderes Essen bewertet.", badRequestResult.Value);
    }

    [Fact]
    public async Task GetTopMenus_ReturnsOrderedByAvgStars()
    {
        // Arrange
        using var context = GetInMemoryContext();
        
        var meal1 = new DailyMeal
        {
            DayNumber = DateOnly.FromDateTime(DateTime.Now).DayNumber,
            Description = "Excellent Meal",
            Option = DailyMeal.MealOption.Just_Good_Food
        };
        var meal2 = new DailyMeal
        {
            DayNumber = DateOnly.FromDateTime(DateTime.Now).DayNumber,
            Description = "Good Meal",
            Option = DailyMeal.MealOption.Grill_Sandwiches
        };
        var meal3 = new DailyMeal
        {
            DayNumber = DateOnly.FromDateTime(DateTime.Now).DayNumber,
            Description = "Excellent Meal", // Same as meal1
            Option = DailyMeal.MealOption.Just_Good_Food
        };
        context.DailyMeals.AddRange(meal1, meal2, meal3);
        await context.SaveChangesAsync();

        // Add ratings
        context.MealRatings.AddRange(
            new MealRating { DailyMealId = meal1.Id, UserId = "user1", Stars = 5, DayNumber = meal1.DayNumber, DailyMeal = meal1 },
            new MealRating { DailyMealId = meal3.Id, UserId = "user2", Stars = 5, DayNumber = meal3.DayNumber, DailyMeal = meal3 },
            new MealRating { DailyMealId = meal2.Id, UserId = "user3", Stars = 3, DayNumber = meal2.DayNumber, DailyMeal = meal2 }
        );
        await context.SaveChangesAsync();

        var controller = new RatingsController(context);

        // Act
        var result = await controller.GetTopMenus(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var topMenus = Assert.IsAssignableFrom<List<TopMenuDto>>(okResult.Value);
        
        Assert.Equal(2, topMenus.Count);
        Assert.Equal("Excellent Meal", topMenus[0].Description);
        Assert.Equal(5.0, topMenus[0].AvgStars);
        Assert.Equal(2, topMenus[0].Count); // Combined ratings from meal1 and meal3
    }

    [Fact]
    public async Task GetTopMenus_WithMinRatings_FiltersResults()
    {
        // Arrange
        using var context = GetInMemoryContext();
        
        var meal1 = new DailyMeal
        {
            DayNumber = DateOnly.FromDateTime(DateTime.Now).DayNumber,
            Description = "Popular Meal",
            Option = DailyMeal.MealOption.Just_Good_Food
        };
        var meal2 = new DailyMeal
        {
            DayNumber = DateOnly.FromDateTime(DateTime.Now).DayNumber,
            Description = "Unpopular Meal",
            Option = DailyMeal.MealOption.Grill_Sandwiches
        };
        context.DailyMeals.AddRange(meal1, meal2);
        await context.SaveChangesAsync();

        // meal1 has 3 ratings, meal2 has 1 rating
        context.MealRatings.AddRange(
            new MealRating { DailyMealId = meal1.Id, UserId = "user1", Stars = 5, DayNumber = meal1.DayNumber, DailyMeal = meal1 },
            new MealRating { DailyMealId = meal1.Id, UserId = "user2", Stars = 4, DayNumber = meal1.DayNumber, DailyMeal = meal1 },
            new MealRating { DailyMealId = meal1.Id, UserId = "user3", Stars = 5, DayNumber = meal1.DayNumber, DailyMeal = meal1 },
            new MealRating { DailyMealId = meal2.Id, UserId = "user4", Stars = 5, DayNumber = meal2.DayNumber, DailyMeal = meal2 }
        );
        await context.SaveChangesAsync();

        var controller = new RatingsController(context);

        // Act
        var result = await controller.GetTopMenus(minRatings: 3);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var topMenus = Assert.IsAssignableFrom<List<TopMenuDto>>(okResult.Value);
        
        Assert.Single(topMenus);
        Assert.Equal("Popular Meal", topMenus[0].Description);
    }
}
