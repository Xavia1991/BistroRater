using Database;
using Database.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Contract.Model.Requests;
using static Database.Model.DailyMeal;

namespace BistroRater.Tests;

public class MenuControllerTests
{
    private BistroContext GetInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<BistroContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new BistroContext(options);
    }

    [Fact]
    public async Task GetWeeklyMenus_ReturnsAllMealsForWeek()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var controller = new MenuController(context);

        // Act
        var result = await controller.GetWeeklyMenus(DateTime.Now);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var meals = Assert.IsAssignableFrom<List<DailyMeal>>(okResult.Value);
        
        // Should create 5 days * 3 options = 15 meals
        Assert.Equal(15, meals.Count);
    }

    [Fact]
    public async Task RenameMenu_WithValidData_UpdatesDescription()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var meal = new DailyMeal
        {
            DayNumber = DateOnly.FromDateTime(DateTime.Now).DayNumber,
            Description = "Old Name",
            Option = MealOption.Just_Good_Food
        };
        context.DailyMeals.Add(meal);
        await context.SaveChangesAsync();

        var controller = new MenuController(context);
        var request = new RenameMenuRequest(meal.Id, "New Name");

        // Act
        var result = await controller.RenameMenu(request);

        // Assert
        Assert.IsType<OkResult>(result);
        var updatedMeal = await context.DailyMeals.FindAsync(meal.Id);
        Assert.Equal("New Name", updatedMeal!.Description);
    }

    [Fact]
    public async Task RenameMenu_WithEmptyDescription_ReturnsBadRequest()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var meal = new DailyMeal
        {
            DayNumber = DateOnly.FromDateTime(DateTime.Now).DayNumber,
            Description = "Old Name",
            Option = MealOption.Just_Good_Food
        };
        context.DailyMeals.Add(meal);
        await context.SaveChangesAsync();

        var controller = new MenuController(context);
        var request = new RenameMenuRequest(meal.Id, "   ");

        // Act
        var result = await controller.RenameMenu(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task RenameMenu_WithNonExistentMeal_ReturnsNotFound()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var controller = new MenuController(context);
        var request = new RenameMenuRequest(999, "New Name");

        // Act
        var result = await controller.RenameMenu(request);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task AutocompleteMenuDescriptions_ReturnsMatchingDescriptions()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var meal1 = new DailyMeal
        {
            DayNumber = DateOnly.FromDateTime(DateTime.Now).DayNumber,
            Description = "Jägerschnitzel",
            Option = MealOption.Just_Good_Food
        };
        var meal2 = new DailyMeal
        {
            DayNumber = DateOnly.FromDateTime(DateTime.Now).AddDays(-1).DayNumber,
            Description = "Jägerschnitzel",
            Option = MealOption.Just_Good_Food
        };
        var meal3 = new DailyMeal
        {
            DayNumber = DateOnly.FromDateTime(DateTime.Now).DayNumber,
            Description = "Pizza",
            Option = MealOption.Grill_Sandwiches
        };
        context.DailyMeals.AddRange(meal1, meal2, meal3);
        await context.SaveChangesAsync();

        var controller = new MenuController(context);

        // Act
        var result = await controller.AutocompleteMenuDescriptions("Jäger");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var suggestions = Assert.IsAssignableFrom<List<string>>(okResult.Value);
        Assert.Single(suggestions); // Should only return distinct values
        Assert.Contains("Jägerschnitzel", suggestions);
    }

    [Fact]
    public async Task AutocompleteMenuDescriptions_WithShortQuery_ReturnsEmpty()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var controller = new MenuController(context);

        // Act
        var result = await controller.AutocompleteMenuDescriptions("J");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var suggestions = Assert.IsAssignableFrom<List<string>>(okResult.Value);
        Assert.Empty(suggestions);
    }
}
