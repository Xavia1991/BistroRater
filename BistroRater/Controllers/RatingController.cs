using Contract;
using Contract.Model.DTO;
using Contract.Model.Requests;
using Database;
using Database.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BistroRater.Controllers;

// Controller for creating and updating meal ratings.
[ApiController, Route("api/ratings")]
public class RatingsController : ControllerBase
{
    private readonly BistroContext _db;

    public RatingsController(BistroContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Rates a daily meal for the current user based on the provided rating request.
    /// </summary>
    /// <remarks>A user can rate only today's meal and may update their rating for the same meal within the
    /// day. Attempting to rate a different meal on the same day will result in a bad request.</remarks>
    /// <param name="request">The rating request containing the daily meal identifier and the number of stars to assign. The number of stars
    /// must be between 1 and 5.</param>
    /// <returns>An <see cref="IActionResult"/> indicating the result of the operation. Returns <see cref="OkResult"/> if the
    /// rating is successfully created or updated; <see cref="NotFoundResult"/> if the specified meal does not exist;
    /// <see cref="BadRequestObjectResult"/> if the meal is not for today, the stars are out of range, or the user has
    /// already rated a different meal today; or <see cref="UnauthorizedResult"/> if the user is not authenticated.</returns>
    [HttpPost("rate")]
    public async Task<IActionResult> RateMeal([FromBody] RateMealRequest request)
    {
        var meal = await _db.DailyMeals.FindAsync(request.DailyMealId);
        if (meal == null)
            return NotFound();

        var today = DateOnly.FromDateTime(DateTime.Now.Date);
        if (meal.DayNumber != today.DayNumber)
            return BadRequest("Only today's meals can be rated.");

        if (request.Stars < 1 || request.Stars > 5)
            return BadRequest("Stars must be between 1 and 5.");

        var userId = User.Identity?.Name ?? request.userID;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized("No UserID found");



        var rating = await _db.MealRatings
            .FirstOrDefaultAsync(r => r.DayNumber == today.DayNumber && r.UserId == userId);

        if (rating == null)
        {
            rating = new MealRating
            {
                DailyMealId = meal.Id,
                UserId = userId,
                Stars = request.Stars,
                DayNumber = DateOnly.FromDateTime(DateTime.Now.Date).DayNumber
            };
            _db.MealRatings.Add(rating);
         
        }
        else if (rating.DailyMealId != request.DailyMealId)
        {
            return BadRequest("Du hast heute schon ein anderes Essen bewertet.");
        }
        else
        {
            // Update existing rating
            rating.Stars = request.Stars;
            rating.DayNumber = today.DayNumber;
        }

        await _db.SaveChangesAsync();
        return Ok();
    }

    /// <summary>
    /// Retrieves a list of top-rated meal menus, ordered by average star rating and number of ratings.
    /// </summary>
    /// <remarks>Menus are grouped by their description. Only menus with a non-null description and at least
    /// the specified number of ratings are included. Results are sorted first by average star rating in descending
    /// order, then by rating count in descending order.</remarks>
    /// <param name="minRatings">The minimum number of ratings required for a menu to be included in the results. Must be greater than or equal
    /// to 1.</param>
    /// <returns>An <see cref="IActionResult"/> containing a collection of top menu items with their descriptions, average star
    /// ratings, and rating counts. The collection is empty if no menus meet the criteria.</returns>
    [HttpGet("top")]
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
}

