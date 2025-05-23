using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using KontrolaNawykow.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KontrolaNawykow.Pages.Diet
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<DayInfo> WeekDays { get; set; }
        public Dictionary<DateTime, List<MealPlanViewModel>> MealPlans { get; set; } = new Dictionary<DateTime, List<MealPlanViewModel>>();
        public List<Recipe> Recipes { get; set; } = new List<Recipe>();
        public List<Ingredient> Ingredients { get; set; } = new List<Ingredient>();
        public User CurrentUser { get; set; }

        // Parametr dla nawigacji tygodniowej
        [BindProperty(SupportsGet = true)]
        public int WeekOffset { get; set; } = 0;

        public class DayInfo
        {
            public string Name { get; set; }
            public DateTime Date { get; set; }
            public bool IsToday { get; set; }
        }

        public class DailyTotals
        {
            public int Calories { get; set; } = 0;
            public float Protein { get; set; } = 0;
            public float Fat { get; set; } = 0;
            public float Carbs { get; set; } = 0;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Pobierz ID zalogowanego użytkownika
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToPage("/Account/Login");
                }

                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return RedirectToPage("/Account/Login");
                }

                // Pobierz dane użytkownika
                CurrentUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (CurrentUser == null)
                {
                    return RedirectToPage("/Account/Login");
                }

                // Przygotuj informacje o dniach tygodnia z uwzględnieniem offsetu
                WeekDays = GetWeekDays(WeekOffset);

                // Pobierz przepisy użytkownika i publiczne
                Recipes = await _context.Recipes
                    .Where(r => r.UserId == userId || r.IsPublic)
                    .Include(r => r.RecipeIngredients)
                        .ThenInclude(ri => ri.Ingredient)
                    .Include(r => r.Ratings) // Dodaj ładowanie ocen
                    .ToListAsync();

                // Pobierz składniki
                Ingredients = await _context.Ingredients
                    .OrderBy(i => i.Name)
                    .ToListAsync();

                // Przygotuj daty dla zapytań
                var startDate = WeekDays.First().Date;
                var endDate = WeekDays.Last().Date.AddDays(1).AddSeconds(-1); // Koniec ostatniego dnia

                // Pobierz plany posiłków z relacją do przepisów przez tabelę łączącą
                var mealPlans = await _context.MealPlans
                   .Where(mp => mp.UserId == userId &&
                          mp.Date >= startDate && mp.Date <= endDate)
                   .Include(mp => mp.PlanPosilkowPrzepisy)
                       .ThenInclude(ppp => ppp.Przepis)
                           .ThenInclude(r => r.RecipeIngredients)
                               .ThenInclude(ri => ri.Ingredient)
                   .Include(mp => mp.PlanPosilkowPrzepisy)
                       .ThenInclude(ppp => ppp.Przepis)
                           .ThenInclude(r => r.Ratings) // Dodaj ładowanie ocen przepisów
                   .OrderBy(mp => mp.Date)
                   .ThenBy(mp => mp.MealType)
                   .ToListAsync();

                // Przekształć plany posiłków do modelu widoku, który zachowuje kompatybilność
                // z poprzednią strukturą dla szablonu
                foreach (var mealPlan in mealPlans)
                {
                    if (mealPlan.Date.HasValue)
                    {
                        var date = mealPlan.Date.Value.Date;
                        if (!MealPlans.ContainsKey(date))
                        {
                            MealPlans[date] = new List<MealPlanViewModel>();
                        }

                        // Tworzenie modelu widoku
                        var viewModel = new MealPlanViewModel
                        {
                            Id = mealPlan.Id,
                            UserId = mealPlan.UserId,
                            Date = mealPlan.Date,
                            MealType = mealPlan.MealType,
                            CustomEntry = mealPlan.CustomEntry,
                            Eaten = mealPlan.Eaten
                        };

                        // Przypisanie przepisu z relacji, jeśli istnieje
                        var planPosilkowPrzepis = mealPlan.PlanPosilkowPrzepisy?.FirstOrDefault();
                        if (planPosilkowPrzepis != null)
                        {
                            viewModel.Recipe = planPosilkowPrzepis.Przepis;
                            viewModel.RecipeId = planPosilkowPrzepis.PrzepisId;
                        }

                        MealPlans[date].Add(viewModel);
                    }
                }

                return Page();
            }
            catch (Exception ex)
            {
                // Logowanie błędu
                Console.WriteLine($"Błąd podczas ładowania strony diety: {ex.Message}");
                return RedirectToPage("/Error", new { message = ex.Message });
            }
        }

        // Metoda zwracająca informacje o dniach tygodnia (poniedziałek-niedziela) z uwzględnieniem offsetu
        private List<DayInfo> GetWeekDays(int weekOffset = 0)
        {
            var days = new List<DayInfo>();
            var today = DateTime.Today;

            // Znajdź poniedziałek bieżącego tygodnia
            var monday = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
            if (today.DayOfWeek == DayOfWeek.Sunday)
                monday = monday.AddDays(-7); // Jeśli dziś niedziela, cofnij do poprzedniego poniedziałku

            // Zastosuj offset tygodniowy
            monday = monday.AddDays(weekOffset * 7);

            // Dodaj 7 dni od poniedziałku
            for (int i = 0; i < 7; i++)
            {
                var date = monday.AddDays(i);
                days.Add(new DayInfo
                {
                    Name = GetPolishDayName(date.DayOfWeek),
                    Date = date,
                    IsToday = date.Date == today
                });
            }

            return days;
        }

        // Metoda zwracająca polskie nazwy dni tygodnia
        private string GetPolishDayName(DayOfWeek dayOfWeek)
        {
            switch (dayOfWeek)
            {
                case DayOfWeek.Monday: return "Poniedziałek";
                case DayOfWeek.Tuesday: return "Wtorek";
                case DayOfWeek.Wednesday: return "Środa";
                case DayOfWeek.Thursday: return "Czwartek";
                case DayOfWeek.Friday: return "Piątek";
                case DayOfWeek.Saturday: return "Sobota";
                case DayOfWeek.Sunday: return "Niedziela";
                default: return string.Empty;
            }
        }

        // Metoda obliczająca całkowitą wartość odżywczą posiłków dla danego dnia
        public DailyTotals GetDailyTotals(DateTime date)
        {
            var totals = new DailyTotals();

            if (MealPlans.ContainsKey(date))
            {
                foreach (var meal in MealPlans[date])
                {
                    if (meal.Recipe != null)
                    {
                        totals.Calories += meal.Recipe.Calories;
                        totals.Protein += meal.Recipe.Protein;
                        totals.Fat += meal.Recipe.Fat;
                        totals.Carbs += meal.Recipe.Carbs;
                    }
                    else
                    {
                        // Próba analizy zawartości CustomEntry, jeśli istnieje
                        // Np. jeśli zawiera informacje o makroskładnikach
                        // To logika, którą możesz zaimplementować w przyszłości
                    }
                }
            }

            // Zaokrąglanie wartości dla lepszej czytelności
            totals.Protein = (float)Math.Round(totals.Protein, 1);
            totals.Fat = (float)Math.Round(totals.Fat, 1);
            totals.Carbs = (float)Math.Round(totals.Carbs, 1);

            return totals;
        }

        // Metoda pomocnicza do debugowania - zwraca dostępne składniki dla przepisu
        public List<RecipeIngredient> GetIngredients(int recipeId)
        {
            var recipe = Recipes.FirstOrDefault(r => r.Id == recipeId);
            return recipe?.RecipeIngredients?.ToList() ?? new List<RecipeIngredient>();
        }

        // Pomocnicza metoda do pobierania zjedzone/niezjedzone posiłki na dany dzień
        public List<MealPlanViewModel> GetMealsByStatus(DateTime date, bool eaten)
        {
            if (MealPlans.ContainsKey(date))
            {
                return MealPlans[date].Where(m => m.Eaten == eaten).ToList();
            }
            return new List<MealPlanViewModel>();
        }

        // Metoda zwracająca kalorie dla aktualnego użytkownika (można dodać kalkulacje na podstawie wagi, wzrostu, aktywności)
        public int GetRecommendedCalories()
        {
            if (CurrentUser == null)
                return 2000; // Domyślna wartość

            // Tutaj możesz dodać bardziej zaawansowaną logikę na podstawie profilu użytkownika
            // Np. wykorzystując Harris-Benedict lub inne równania dla BMR i TDEE

            if (CurrentUser.Plec == Gender.Mezczyzna)
            {
                return 2500; // Przykładowa wartość dla mężczyzny
            }
            else if (CurrentUser.Plec == Gender.Kobieta)
            {
                return 2000; // Przykładowa wartość dla kobiety
            }

            return 2200; // Domyślna wartość, jeśli płeć nie jest określona
        }

        // Metoda do sprawdzania czy użytkownik ukończył swoje cele żywieniowe na dany dzień
        public bool IsNutritionGoalCompleted(DateTime date)
        {
            var totals = GetDailyTotals(date);
            var recommendedCalories = GetRecommendedCalories();

            // Przykładowa logika - uznajemy cel za ukończony, jeśli spożycie kalorii 
            // mieści się w zakresie 90-110% zalecanego poziomu
            var minCalories = recommendedCalories * 0.9;
            var maxCalories = recommendedCalories * 1.1;

            return totals.Calories >= minCalories && totals.Calories <= maxCalories;
        }

        // Funkcja generująca HTML z gwiazdkami dla ocen
        public string GetStarsHtml(double rating)
        {
            int fullStars = (int)Math.Floor(rating);
            bool hasHalfStar = rating % 1 >= 0.5;
            string starsHtml = "";

            for (int i = 1; i <= 5; i++)
            {
                if (i <= fullStars)
                {
                    starsHtml += "★";  // Pełna gwiazdka
                }
                else if (i == fullStars + 1 && hasHalfStar)
                {
                    starsHtml += "☆";  // Pół gwiazdki (można użyć ½ lub specjalnego znaku)
                }
                else
                {
                    starsHtml += "☆";  // Pusta gwiazdka
                }
            }

            return starsHtml;
        }
    }

    // Model widoku dla kompatybilności z widokiem
    public class MealPlanViewModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime? Date { get; set; }
        public MealType MealType { get; set; }
        public string CustomEntry { get; set; }
        public bool Eaten { get; set; }
        public Recipe Recipe { get; set; }
        public int? RecipeId { get; set; }
    }
}