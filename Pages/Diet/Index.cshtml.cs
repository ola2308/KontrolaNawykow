using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using KontrolaNawykow.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

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

        // Właściwości widoku
        public User CurrentUser { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Pobierz dane użytkownika z bazy
            CurrentUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (CurrentUser == null)
            {
                return RedirectToPage("/Account/Login");
            }

            // Jeśli użytkownik nie ma wypełnionych danych żywieniowych, oblicz je
            if (!CurrentUser.CustomCaloriesDeficit.HasValue ||
                !CurrentUser.CustomProteinGrams.HasValue ||
                !CurrentUser.CustomCarbsGrams.HasValue ||
                !CurrentUser.CustomFatGrams.HasValue)
            {
                // Jeśli brak podstawowych danych, przekieruj do setup
                if (!CurrentUser.Waga.HasValue || !CurrentUser.Wzrost.HasValue || !CurrentUser.Wiek.HasValue)
                {
                    return RedirectToPage("/Profile/Setup");
                }

                // Oblicz brakujące dane żywieniowe używając tej samej logiki co w Edit.cshtml.cs
                CalculateNutritionData(CurrentUser);
                await _context.SaveChangesAsync();
            }

            return Page();
        }

        private void CalculateNutritionData(User user)
        {
            // BMI
            if (user.Wzrost.HasValue && user.Waga.HasValue)
            {
                double heightInMeters = user.Wzrost.Value / 100.0;
                double bmi = user.Waga.Value / (heightInMeters * heightInMeters);
                user.CustomBmi = bmi;
            }

            // Kalorie
            int caloriesDeficit = CalculateCaloriesDeficit(user);
            user.CustomCaloriesDeficit = caloriesDeficit;

            // Makroskładniki
            CalculateMacronutrients(user);
        }

        private int CalculateCaloriesDeficit(User user)
        {
            if (!user.Waga.HasValue || !user.Wzrost.HasValue || !user.Wiek.HasValue)
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
                else if (user.RodzajPracy == "Pół na pół")
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

            if (user.CustomCarbsGrams < 50) user.CustomCarbsGrams = 50;
            if (user.CustomProteinGrams < 40) user.CustomProteinGrams = 40;
            if (user.CustomFatGrams < 20) user.CustomFatGrams = 20;
        }
    }
}