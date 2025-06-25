using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using KontrolaNawykow.Models;
using System.Runtime.CompilerServices;

namespace KontrolaNawykow.Pages.Dietician
{
    [Authorize(Roles = "Dietitian")]
    public class ClientScheduleModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ClientScheduleModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public User CurrentUser { get; set; }

        public Dietetyk CurrentDietician { get; set; }

        [BindProperty(SupportsGet = true)]
        public string CheckSchedule { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToPage("/Account/Dietitian/Login");
                }

                var dietIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(dietIdClaim) || !int.TryParse(dietIdClaim, out int dietId))
                {
                    return RedirectToPage("/Account/Dietitian/Login");
                }

                CurrentDietician = await _context.Dietetycy
                    .FirstOrDefaultAsync(u => u.Id == dietId);

                if (CurrentDietician == null)
                {
                    return RedirectToPage("/Account/Dietician/Login");
                }

                if (CheckSchedule == null)
                {
                    return RedirectToPage("/Dietician/Clients");
                }

                CurrentUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == int.Parse(CheckSchedule));

                if(CurrentUser == null)
                {
                    return RedirectToPage("/Dietician/Clients");
                }

                if (!CurrentUser.CustomCaloriesDeficit.HasValue ||
                !CurrentUser.CustomProteinGrams.HasValue ||
                !CurrentUser.CustomCarbsGrams.HasValue ||
                !CurrentUser.CustomFatGrams.HasValue)
                {
                   
                    if (!CurrentUser.Waga.HasValue || !CurrentUser.Wzrost.HasValue || !CurrentUser.Wiek.HasValue)
                    {
                        return RedirectToPage("/Profile/Setup");
                    }

                    CalculateNutritionData(CurrentUser);
                    await _context.SaveChangesAsync();
                }

                return Page();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"B³¹d podczas ³adowania strony Dietician/Clients: {ex.Message}");
                return RedirectToPage("/Error");
            }
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

            // Makrosk³adniki
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

            if (user.CustomCarbsGrams < 50) user.CustomCarbsGrams = 50;
            if (user.CustomProteinGrams < 40) user.CustomProteinGrams = 40;
            if (user.CustomFatGrams < 20) user.CustomFatGrams = 20;
        }

    }
}
