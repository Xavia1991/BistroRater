namespace Database.Model;

public class MealRating
{
    public int Id { get; set; }

    public int DailyMealId { get; set; }
    public DailyMeal DailyMeal { get; set; } = null!;

    // z.B. aus Login/Token oder als Platzhalter: E-Mail / UPN
    public string UserId { get; set; } = null!;

    public int Stars { get; set; } // 1–5
    public DateTime CreatedAtUtc { get; set; }
}