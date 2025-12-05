using Contract;
using Contract.Model.Requests;
using Database.Model;
using Microsoft.AspNetCore.Components.Authorization;
using System.Globalization;
using static Database.Model.DailyMeal;

namespace BistroRater.Components.Pages;

public partial class Menu
{
    private readonly MealOption[] mealOptions = Enum.GetValues<MealOption>();
    private List<DailyMeal> weeklyMeals = new();
    private readonly Dictionary<int, string> renameDraft = new();
    private readonly HashSet<int> editingMeals = new();
    private readonly Dictionary<int, string> mealMessages = new();
    private AuthenticationState? authenthication;

    private bool isLoading = true;
    private string? errorMessage;
    private DateOnly weekStart = GetCurrentWeekStart();
    private DateOnly weekEnd => weekStart.AddDays(4);

    private IEnumerable<DateOnly> WeekDays => Enumerable.Range(0, 5).Select(i => weekStart.AddDays(i));

    protected override async Task OnInitializedAsync()
    {
        authenthication = await AuthState.GetAuthenticationStateAsync();
        await LoadMenusAsync();
    }

    private async Task LoadMenusAsync()
    {
        isLoading = true;
        errorMessage = null;

        try
        {
            var client = HttpClientFactory.CreateClient("ApiClient");
            var result = await client.GetStringAsync("api/menu/week");
            var menus = await client.GetFromJsonAsync<List<DailyMeal>>(Routing.Menu.Weekly);
            weeklyMeals = menus ?? new List<DailyMeal>();
        }
        catch (Exception ex)
        {
            errorMessage = $"Menü konnte nicht geladen werden: {ex.Message}";
        }
        finally
        {
            isLoading = false;
        }
    }

    private DailyMeal? GetMeal(DateOnly day, MealOption option)
        => weeklyMeals.FirstOrDefault(m => m.DayNumber == day.DayNumber && m.Option == option);

    private static DateOnly GetCurrentWeekStart() => GetWeekStart(DateOnly.FromDateTime(DateTime.Now.Date));

    private static DateOnly GetWeekStart(DateOnly date)
    {
        int day = (int)date.DayOfWeek;
        day = day == 0 ? 7 : day;
        return date.AddDays(-(day - 1));
    }

    private string FormatDayName(DateOnly date)
    {
        var culture = CultureInfo.GetCultureInfo("de-DE");
        return culture.DateTimeFormat.GetDayName(date.ToDateTime(TimeOnly.MinValue).DayOfWeek);
    }

    private static string FormatOption(MealOption option) => option switch
    {
        MealOption.Grill_Sandwiches => "Grill Sandwiches",
        MealOption.Smuts_Leibspeise => "Smuts Leibspeise",
        MealOption.Just_Good_Food => "Just Good Food",
        _ => option.ToString()
    };

    private bool IsToday(DateOnly date) => date == DateOnly.FromDateTime(DateTime.Now.Date);
    private bool IsToday(int dayNumber) => dayNumber == DateOnly.FromDateTime(DateTime.Now.Date).DayNumber;

    private bool IsEditing(int mealId) => editingMeals.Contains(mealId);

    private void StartEdit(DailyMeal meal)
    {
        editingMeals.Add(meal.Id);
        renameDraft[meal.Id] = meal.Description;
    }

    private void CancelEdit(int mealId)
    {
        editingMeals.Remove(mealId);
        renameDraft.Remove(mealId);
        mealMessages.Remove(mealId);
    }

    private async Task SaveRenameAsync(DailyMeal meal)
    {
        if (!renameDraft.TryGetValue(meal.Id, out var newName) || string.IsNullOrWhiteSpace(newName))
        {
            mealMessages[meal.Id] = "Bitte einen gültigen Namen eingeben.";
            return;
        }

        try
        {
            var client = HttpClientFactory.CreateClient("ApiClient");
            var request = new RenameMenuRequest(meal.Id, newName.Trim());
            var response = await client.PostAsJsonAsync("api/menu/rename", request);

            if (response.IsSuccessStatusCode)
            {
                meal.Description = newName.Trim();
                editingMeals.Remove(meal.Id);
                mealMessages[meal.Id] = "Umbenannt.";
            }
            else
            {
                var serverMessage = await response.Content.ReadAsStringAsync();
                mealMessages[meal.Id] = string.IsNullOrWhiteSpace(serverMessage) ? "Umbenennen fehlgeschlagen." : serverMessage;
            }
        }
        catch (Exception ex)
        {
            mealMessages[meal.Id] = $"Fehler: {ex.Message}";
        }
    }

    private async Task RateMealAsync(DailyMeal meal, int stars)
    {
        if (!IsToday(meal.DayNumber))
        {
            mealMessages[meal.Id] = "Bewertungen sind nur am jeweiligen Tag möglich.";
            return;
        }

        try
        {
            var client = HttpClientFactory.CreateClient("ApiClient");
            var request = new RateMealRequest(meal.Id, stars, authenthication?.User?.Identity?.Name ?? "demo_user");
            var response = await client.PostAsJsonAsync(Routing.Ratings.Rate, request);

            mealMessages[meal.Id] = response.IsSuccessStatusCode
                ? $"Danke für {stars} Sterne!"
                : "Bewertung konnte nicht gespeichert werden.";
        }
        catch (Exception ex)
        {
            mealMessages[meal.Id] = $"Fehler: {ex.Message}";
        }
    }
}
