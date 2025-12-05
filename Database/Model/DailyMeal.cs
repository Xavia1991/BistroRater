using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Database.Model;

public class DailyMeal
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    // Numeric identifier for the specific day (number of days since year 1, day 1 in gregorian calendar.
    public int DayNumber { get; set; }
    [Required, MaxLength(200), MinLength(2)]
    public string Description { get; set; } = string.Empty;
    public ICollection<MealRating> Ratings { get; set; } = new List<MealRating>();
    public MealOption Option { get; set; }

    public enum MealOption
    {
        Grill_Sandwiches,
        Smuts_Leibspeise,
        Just_Good_Food
    }
}