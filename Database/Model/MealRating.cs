namespace Database.Model;

public class MealRating
{
    public int Id { get; set; }

    public int DailyMealId { get; set; }
    public DailyMeal DailyMeal { get; set; } = null!;

    // z.B. aus Login/Token oder als Platzhalter: E-Mail / UPN
    public string UserId { get; set; } = null!;

    public int Stars { get; set; } // 1–5
    // Numeric identifier for the specific day (number of days since year 1, day 1 in gregorian calendar.
    public int DayNumber { get; set; }
}