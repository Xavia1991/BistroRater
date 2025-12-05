using Database;
using Database.Model;
using Contract.Model.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Database.Model.DailyMeal;
using Contract;
using Contract.Model.Requests;
using Microsoft.AspNetCore.Authorization;

/// <summary>
/// Provides API endpoints for managing and retrieving daily meal menus, including weekly menu listings, menu renaming,
/// and autocomplete suggestions for menu descriptions.
/// </summary>
[ApiController, Route("api/menu")]
public class MenuController : ControllerBase
{
    private readonly BistroContext _db;

    public MenuController(BistroContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Retrieves the list of daily meal menus for the specified week, starting from Monday.
    /// </summary>
    /// <remarks>The returned menus cover Monday through Friday of the specified week. If any daily menus are
    /// missing for the week, they are automatically filled before returning the result.</remarks>
    /// <param name="date">An optional date that determines the week for which menus are retrieved. If not specified, the current week is
    /// used.</param>
    /// <returns>An <see cref="IActionResult"/> containing a collection of daily meal menus for the requested week. The result is
    /// an HTTP 200 response with the list of menus.</returns>
    [HttpGet("week")]
    public async Task<IActionResult> GetWeeklyMenus([FromQuery] DateTime? date)
    {
        int monday = GetLastMondayDayNumber(date);
        // Monday to Friday
        var friday = monday + 4; 

        // 1) Load existing meals for the week
        var meals = await _db.DailyMeals
            .Where(m => m.DayNumber >= monday && m.DayNumber <= friday)
            .OrderBy(m => m.DayNumber)
            .ThenBy(m => m.Option)
            .ToListAsync();

        if (!HasAllMeals(meals))
        {
            FillMissingMeals(ref meals, monday);
        }

        return Ok(meals);
    }


    /// <summary>
    /// Renames the description of an existing daily meal menu using the provided request data.
    /// </summary>
    /// <param name="request">The request containing the daily meal identifier and the new description to apply. The daily meal must exist,
    /// and the new description cannot be null, empty, or whitespace.</param>
    /// <returns>An <see cref="IActionResult"/> indicating the result of the operation. Returns <see cref="OkResult"/> if the
    /// menu was successfully renamed; <see cref="NotFoundResult"/> if the specified daily meal does not exist; or <see
    /// cref="BadRequestResult"/> if the new description is invalid.</returns>
    [HttpPost("rename")]
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


    /// <summary>
    /// Retrieves a list of distinct menu descriptions that contain the specified search query for use in autocomplete
    /// suggestions.
    /// </summary>
    /// <remarks>The search is case-insensitive and matches any part of the menu description. Results are
    /// limited to a maximum of ten suggestions to optimize performance and usability.</remarks>
    /// <param name="query">The search term to match against menu descriptions. Must be at least two characters long; otherwise, an empty
    /// list is returned.</param>
    /// <returns>An <see cref="IActionResult"/> containing a list of up to ten distinct menu descriptions that include the search
    /// query. Returns an empty list if the query is null, whitespace, or shorter than two characters.</returns>
    [HttpGet("autocomplete")]
    public async Task<IActionResult> AutocompleteMenuDescriptions([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return Ok(new List<string>());
        var normalized = query.Trim();
        var results = await _db.DailyMeals
            .Where(m => EF.Functions.Like(m.Description, $"%{normalized}%"))
            .Select(m => m.Description!)
            .Distinct()
            .Take(10)
            .ToListAsync();
        return Ok(results);
    }


    // Helper methods

    private static int GetLastMondayDayNumber(DateTime? date)
    {
        var refDate = date ?? DateTime.Now;
        int daysToMonday = (int)(DayOfWeek.Monday - refDate.DayOfWeek);
        int weekStart = DateOnly.FromDateTime(refDate)
            .AddDays(daysToMonday)
            .DayNumber;
        return weekStart;
    }

    /// <summary>
    /// Ensures that the provided list of daily meals contains an entry for every meal option for each weekday, starting
    /// from the specified day number. Missing meal entries are added as needed.
    /// </summary>
    /// <remarks>The method processes five consecutive days, typically representing a standard workweek
    /// (Monday to Friday). For each day, it verifies that an entry exists for every defined meal option. If an entry is
    /// missing, it is added to both the provided list and the underlying data store. Changes are persisted at the end
    /// of the operation.</remarks>
    /// <param name="meals">A reference to the list of daily meals to be checked and updated. The method adds missing meal entries for each
    /// weekday and meal option.</param>
    /// <param name="startDayNumber">The day number corresponding to the first weekday (typically Monday) for which to ensure meal entries are
    /// present.</param>
    private void FillMissingMeals(ref List<DailyMeal> meals, int startDayNumber)
    {
        int optionsCount = Enum.GetValues<MealOption>().Length;
        int weekDays = 5; // Monday to Friday

        for (int dayOffset = 0; dayOffset < weekDays; dayOffset++)
        {
            int dayNumber = startDayNumber + dayOffset;
            foreach (MealOption option in Enum.GetValues<MealOption>())
            {
                if (!meals.Any(m => m.DayNumber == dayNumber && m.Option == option))
                {
                    meals.Add(new DailyMeal
                    {
                        DayNumber = dayNumber,
                        Description = string.Empty,
                        Option = option
                    });
                    _db.DailyMeals.Add(meals.Last());
                }
            }
        }
        _db.SaveChanges();
    }

    private bool HasAllMeals(List<DailyMeal> meals)
    {
        int optionsCount = Enum.GetValues<MealOption>().Length;
        int weekDays = 5; // Monday to Friday
        return meals.Count == optionsCount * weekDays;
    }

   
}