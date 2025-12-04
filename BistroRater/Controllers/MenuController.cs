using Database;
using Database.Model;
using Contract.Model.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Database.Model.DailyMeal;
using Contract;
using Contract.Model.Requests;

[ApiController]
[Route("api/menu")]
public class MenuController : ControllerBase
{
    private readonly BistroContext _db;

    public MenuController(BistroContext db)
    {
        _db = db;
    }

    // Returns the menus for the week that contains the specified date (or today if none provided).
    [HttpGet("week", Name = Routing.Menu.Weekly)]
    public async Task<IActionResult> GetWeeklyMenus([FromQuery] DateTime? date)
    {
        var refDate = date ?? DateTime.UtcNow;
        int daysToMonday = (int) (DayOfWeek.Monday - refDate.DayOfWeek);
        var weekStart = new DateOnly(refDate.Year, refDate.Month, refDate.Day).AddDays(daysToMonday);
        // Monday through Sunday
        var weekEnd = weekStart.AddDays(6); 

        // 1) Load existing meals for the week
        var meals = await _db.DailyMeals
            .Where(m => m.Date >= weekStart && m.Date <= weekEnd)
            .OrderBy(m => m.Date)
            .ThenBy(m => m.Option)
            .ToListAsync();

        return Ok(meals);
    }


    // Rename the description for a specific daily meal.
    [HttpPost("rename", Name = Routing.Menu.Rename)]
    public async Task<IActionResult> RenameMenu([FromBody] RenameMenuRequest request)
    {
        var meal = await _db.DailyMeals.FindAsync(request.DailyMealId);
        if (meal == null)
            return NotFound("Meal not found.");

        if (string.IsNullOrWhiteSpace(request.NewDescription))
            return BadRequest("Description cannot be empty.");

        var normalized = request.NewDescription.Trim();
        meal.Description = normalized;

        await _db.SaveChangesAsync();
        return Ok();
    }


    // Return the top-rated menus (grouped by description) with optional minimum rating count.
    [HttpGet("top", Name = Routing.Menu.Top)]
    public async Task<IActionResult> GetTopMenus([FromQuery] int minRatings = 1)
    {
        // Load ratings and group by meal description
        var result = await _db.MealRatings
            .Where(r => r.DailyMeal.Description != null)
            .GroupBy(r => r.DailyMeal.Description!)
            .Select(g => new TopMenuDto(
                Description: g.Key,
                AvgStars: g.Average(x => x.Stars),
                Count: g.Count()
            ))
            .Where(x => x.Count >= minRatings)
            .OrderByDescending(x => x.AvgStars)
            .ThenByDescending(x => x.Count)
            .ToListAsync();

        return Ok(result);
    }


    // Helper methods

    private static DateOnly GetWeekStart(DateOnly date)
    {
        // ISO: Monday = 1 ... Sunday = 7
        int dayOfWeek = (int)date.DayOfWeek;
        dayOfWeek = dayOfWeek == 0 ? 7 : dayOfWeek; // Adjust Sunday to 7

        int offset = dayOfWeek - 1;
        return date.AddDays(-offset);
    }

    private bool WeekHasAllMeals(List<DailyMeal> meals, DateOnly start)
    {
        var expectedDays = Enumerable.Range(0, 5).Select(i => start.AddDays(i));

        foreach (var day in expectedDays)
        {
            foreach (MealOption option in Enum.GetValues(typeof(MealOption)))
            {
                if (!meals.Any(m => m.Date == day && m.Option == option))
                    return false;
            }
        }
        return true;
    }

   
}