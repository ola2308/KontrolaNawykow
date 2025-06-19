using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using KontrolaNawykow.Models;

namespace KontrolaNawykow.Pages.Profile
{
    [Authorize]
    public class SetupModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public SetupModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public int? Wiek { get; set; }

        [BindProperty]
        public double? Waga { get; set; }

        [BindProperty]
        public double? Wzrost { get; set; }

        [BindProperty]
        public string AktywnoscFizyczna { get; set; }

        [BindProperty]
        public string RodzajPracy { get; set; }

        [BindProperty]
        public Gender Plec { get; set; }

        [BindProperty]
        public UserGoal Cel { get; set; }

        public string UserName { get; set; }
        public string ErrorMessage { get; set; }
        public string SuccessMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login");
            }

            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null)
            {
                return RedirectToPage("/Account/Login");
            }

            UserName = user.Username;

            // Za³aduj istniej¹ce dane u¿ytkownika
            if (user.Wiek.HasValue) Wiek = user.Wiek;
            if (user.Waga.HasValue) Waga = user.Waga;
            if (user.Wzrost.HasValue) Wzrost = user.Wzrost;
            if (user.Plec.HasValue) Plec = user.Plec.Value;
            if (!string.IsNullOrEmpty(user.AktywnoscFizyczna)) AktywnoscFizyczna = user.AktywnoscFizyczna;
            if (!string.IsNullOrEmpty(user.RodzajPracy)) RodzajPracy = user.RodzajPracy;
            if (user.Cel.HasValue) Cel = user.Cel.Value;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                Console.WriteLine("=== POST Setup started ===");

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToPage("/Account/Login");
                }

                var user = await _context.Users.FindAsync(int.Parse(userId));
                if (user == null)
                {
                    return RedirectToPage("/Account/Login");
                }

                UserName = user.Username;

                // Walidacja
                if (!ModelState.IsValid)
                {
                    ErrorMessage = "Proszê wype³niæ wszystkie pola poprawnie.";
                    return Page();
                }

                if (!Wiek.HasValue || Wiek < 16 || Wiek > 100)
                {
                    ErrorMessage = "Wiek musi byæ miêdzy 16 a 100 lat.";
                    return Page();
                }

                if (!Waga.HasValue || Waga < 30 || Waga > 250)
                {
                    ErrorMessage = "Waga musi byæ miêdzy 30 a 250 kg.";
                    return Page();
                }

                if (!Wzrost.HasValue || Wzrost < 120 || Wzrost > 250)
                {
                    ErrorMessage = "Wzrost musi byæ miêdzy 120 a 250 cm.";
                    return Page();
                }

                if (string.IsNullOrEmpty(AktywnoscFizyczna))
                {
                    ErrorMessage = "Proszê wybraæ poziom aktywnoœci fizycznej.";
                    return Page();
                }

                if (string.IsNullOrEmpty(RodzajPracy))
                {
                    ErrorMessage = "Proszê wybraæ rodzaj pracy.";
                    return Page();
                }

                // Aktualizacja danych u¿ytkownika
                user.Wiek = Wiek;
                user.Waga = Waga;
                user.Wzrost = Wzrost;
                user.Plec = Plec;
                user.AktywnoscFizyczna = AktywnoscFizyczna;
                user.RodzajPracy = RodzajPracy;
                user.Cel = Cel;

                // Obliczanie danych ¿ywieniowych
                CalculateNutritionData(user);

                // Zapisywanie do bazy
                _context.Entry(user).State = EntityState.Modified;
                var result = await _context.SaveChangesAsync();

                Console.WriteLine($"SaveChanges returned: {result} records affected");

                if (result > 0)
                {
                    Console.WriteLine("Data saved successfully!");
                    SuccessMessage = "Dane zosta³y zapisane pomyœlnie!";

                    // Przekierowanie do strony diety po 2 sekundach
                    TempData["SuccessMessage"] = "Profil zosta³ zaktualizowany pomyœlnie!";
                    return RedirectToPage("/Diet/Index");
                }
                else
                {
                    ErrorMessage = "Nie uda³o siê zapisaæ zmian.";
                    return Page();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving setup: {ex.Message}");
                ErrorMessage = "Wyst¹pi³ b³¹d podczas zapisywania danych: " + ex.Message;
                return Page();
            }
        }

        private void CalculateNutritionData(User user)
        {
            Console.WriteLine("Calculating nutrition data...");

            // BMI
            if (user.Wzrost.HasValue && user.Waga.HasValue)
            {
                double heightInMeters = user.Wzrost.Value / 100.0;
                double bmi = user.Waga.Value / (heightInMeters * heightInMeters);
                user.CustomBmi = bmi;
                Console.WriteLine($"Calculated BMI: {bmi:F1}");
            }

            // Kalorie
            int caloriesDeficit = CalculateCaloriesDeficit(user);
            user.CustomCaloriesDeficit = caloriesDeficit;
            Console.WriteLine($"Calculated calories: {caloriesDeficit}");

            // Makrosk³adniki
            CalculateMacronutrients(user);
            Console.WriteLine($"Calculated macros - P: {user.CustomProteinGrams}g, C: {user.CustomCarbsGrams}g, F: {user.CustomFatGrams}g");
        }

        private int CalculateCaloriesDeficit(User user)
        {
            if (!user.Waga.HasValue || !user.Wzrost.HasValue || !user.Wiek.HasValue || !user.Plec.HasValue)
            {
                return 2000;
            }

            double bmr;
            if (user.Plec == Gender.Mezczyzna)
            {
                bmr = 10 * user.Waga.Value + 6.25 * user.Wzrost.Value - 5 * user.Wiek.Value + 5;
            }
            else
            {
                bmr = 10 * user.Waga.Value + 6.25 * user.Wzrost.Value - 5 * user.Wiek.Value - 161;
            }

            double pal = 1.2;
            if (user.AktywnoscFizyczna != null)
            {
                if (user.AktywnoscFizyczna.Contains("0 treningów"))
                    pal = 1.2;
                else if (user.AktywnoscFizyczna.Contains("1-3"))
                    pal = 1.375;
                else if (user.AktywnoscFizyczna.Contains("4-5"))
                    pal = 1.55;
            }

            if (user.RodzajPracy != null)
            {
                if (user.RodzajPracy == "Fizyczna")
                    pal += 0.1;
                else if (user.RodzajPracy == "Pó³ na pó³")
                    pal += 0.05;
            }

            double tdee = bmr * pal;

            if (user.Wiek > 40)
                tdee *= 0.98;
            if (user.Wiek > 60)
                tdee *= 0.97;

            int deficit = 0;
            if (user.Cel == UserGoal.Schudniecie)
            {
                deficit = (int)(tdee * 0.8);
            }
            else if (user.Cel == UserGoal.PrzybranieMasy)
            {
                deficit = (int)(tdee * 1.1);
            }
            else
            {
                deficit = (int)tdee;
            }

            int minCalories = user.Plec == Gender.Mezczyzna ? 1500 : 1200;
            if (deficit < minCalories)
                deficit = minCalories;

            return deficit;
        }

        private void CalculateMacronutrients(User user)
        {
            if (user.CustomCaloriesDeficit <= 0 || !user.Waga.HasValue)
            {
                user.CustomProteinGrams = 100;
                user.CustomCarbsGrams = 200;
                user.CustomFatGrams = 80;
                return;
            }

            if (user.Cel == UserGoal.Schudniecie)
            {
                double proteinMultiplier = user.Plec == Gender.Mezczyzna ? 2.0 : 1.8;
                user.CustomProteinGrams = (int)(user.Waga.Value * proteinMultiplier);
                user.CustomFatGrams = (int)(user.CustomCaloriesDeficit * 0.25 / 9);
                user.CustomCarbsGrams = (int)((user.CustomCaloriesDeficit - (user.CustomProteinGrams.Value * 4) - (user.CustomFatGrams.Value * 9)) / 4);
            }
            else if (user.Cel == UserGoal.PrzybranieMasy)
            {
                double proteinMultiplier = user.Plec == Gender.Mezczyzna ? 1.8 : 1.6;
                user.CustomProteinGrams = (int)(user.Waga.Value * proteinMultiplier);
                user.CustomFatGrams = (int)(user.CustomCaloriesDeficit * 0.25 / 9);
                user.CustomCarbsGrams = (int)((user.CustomCaloriesDeficit - (user.CustomProteinGrams.Value * 4) - (user.CustomFatGrams.Value * 9)) / 4);
            }
            else
            {
                double proteinMultiplier = user.Plec == Gender.Mezczyzna ? 1.6 : 1.4;
                user.CustomProteinGrams = (int)(user.Waga.Value * proteinMultiplier);
                user.CustomFatGrams = (int)(user.CustomCaloriesDeficit * 0.3 / 9);
                user.CustomCarbsGrams = (int)((user.CustomCaloriesDeficit - (user.CustomProteinGrams.Value * 4) - (user.CustomFatGrams.Value * 9)) / 4);
            }

            if (user.AktywnoscFizyczna != null && user.AktywnoscFizyczna.Contains("4-5"))
            {
                int extraCarbs = (int)(user.Waga.Value * 0.5);
                user.CustomCarbsGrams += extraCarbs;
            }

            // Minimum values
            if (user.CustomCarbsGrams < 50) user.CustomCarbsGrams = 50;
            if (user.CustomProteinGrams < 40) user.CustomProteinGrams = 40;
            if (user.CustomFatGrams < 20) user.CustomFatGrams = 20;
        }
    }
}