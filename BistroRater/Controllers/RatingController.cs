using Database;
using Database.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Contract;
using Contract.Model.Requests;

namespace BistroRater.Controllers;

// Controller for creating and updating meal ratings.
[ApiController]
[Route("api/ratings")]
public class RatingsController : ControllerBase
{
    private readonly BistroContext _db;

    public RatingsController(BistroContext db)
    {
        _db = db;
    }

    // Submit a rating for a meal (creates or updates the user's rating for today's meal).
    [HttpPost("rate", Name = Routing.Ratings.Rate)]
    public async Task<IActionResult> RateMeal([FromBody] RateMealRequest request)
    {
        var meal = await _db.DailyMeals.FindAsync(request.DailyMealId);
        if (meal == null)
            return NotFound();

        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        if (meal.Date != today)
            return BadRequest("Only today's meals can be rated.");

        if(request.Stars < 1 || request.Stars > 5)
            return BadRequest("Stars must be between 1 and 5.");

        var userId = User.Identity?.Name ?? "demo-user";

        var rating = await _db.MealRatings
            .FirstOrDefaultAsync(r => r.DailyMealId == request.DailyMealId && r.UserId == userId);

        if (rating == null)
        {
            rating = new MealRating
            {
                DailyMealId = meal.Id,
                UserId = userId,
                Stars = request.Stars,
                CreatedAtUtc = DateTime.UtcNow
            };
            _db.MealRatings.Add(rating);
        }
        else
        {
            // Update existing rating
            rating.Stars = request.Stars;
            rating.CreatedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return Ok();
    }
}

