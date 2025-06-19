using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KontrolaNawykow.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;

namespace KontrolaNawykow.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SetupController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SetupController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/setup
        [HttpGet]
        public async Task<ActionResult<UserSetupDto>> GetUserSetup()
        {
            Console.WriteLine("=== GET USER SETUP ===");

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            Console.WriteLine($"User ID: {userId}");

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound("Użytkownik nie znaleziony");
            }

            var setupDto = new UserSetupDto
            {
                Username = user.Username,
                Wiek = user.Wiek,
                Waga = user.Waga,
                Wzrost = user.Wzrost,
                Plec = user.Plec?.ToString(),
                AktywnoscFizyczna = user.AktywnoscFizyczna,
                RodzajPracy = user.RodzajPracy,
                Cel = user.Cel?.ToString()
            };

            Console.WriteLine($"Returning setup data for user: {user.Username}");
            return Ok(setupDto);
        }

        // POST: api/setup
        [HttpPost]
        public async Task<ActionResult<SetupResponseDto>> SaveUserSetup([FromBody] UserSetupDto setupDto)
        {
            Console.WriteLine("=== SAVE USER SETUP ===");
            Console.WriteLine($"Received data: {System.Text.Json.JsonSerializer.Serialize(setupDto)}");

            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                Console.WriteLine($"User ID: {userId}");

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound("Użytkownik nie znaleziony");
                }

                // Walidacja danych
                var validationErrors = ValidateSetupData(setupDto);
                if (validationErrors.Any())
                {
                    Console.WriteLine($"Validation errors: {string.Join(", ", validationErrors)}");
                    return BadRequest(new { errors = validationErrors });
                }

                // Aktualizacja danych użytkownika
                Console.WriteLine("Updating user data...");
                user.Wiek = setupDto.Wiek;
                user.Waga = setupDto.Waga;
                user.Wzrost = setupDto.Wzrost;
                user.Plec = Enum.Parse<Gender>(setupDto.Plec);
                user.AktywnoscFizyczna = setupDto.AktywnoscFizyczna;
                user.RodzajPracy = setupDto.RodzajPracy;
                user.Cel = Enum.Parse<UserGoal>(setupDto.Cel);

                // Obliczanie danych żywieniowych
                Console.WriteLine("Calculating nutrition data...");
                CalculateNutritionData(user);

                // Zapisywanie do bazy
                _context.Entry(user).State = EntityState.Modified;
                var result = await _context.SaveChangesAsync();

                Console.WriteLine($"SaveChanges returned: {result} records affected");

                if (result > 0)
                {
                    // Weryfikacja zapisanych danych
                    var savedUser = await _context.Users.FindAsync(userId);
                    Console.WriteLine("Data saved successfully!");
                    Console.WriteLine($"Verified data - BMI: {savedUser.CustomBmi}, Calories: {savedUser.CustomCaloriesDeficit}");

                    var response = new SetupResponseDto
                    {
                        Success = true,
                        Message = "Dane zostały zapisane pomyślnie",
                        UserData = new UserNutritionDto
                        {
                            BMI = savedUser.CustomBmi ?? 0,
                            CaloriesDeficit = savedUser.CustomCaloriesDeficit ?? 0,
                            ProteinGrams = savedUser.CustomProteinGrams ?? 0,
                            CarbsGrams = savedUser.CustomCarbsGrams ?? 0,
                            FatGrams = savedUser.CustomFatGrams ?? 0
                        }
                    };

                    return Ok(response);
                }
                else
                {
                    Console.WriteLine("No changes were saved");
                    return BadRequest(new { message = "Nie udało się zapisać zmian" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving setup: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { message = "Wystąpił błąd podczas zapisywania danych", error = ex.Message });
            }
        }

        private List<string> ValidateSetupData(UserSetupDto setupDto)
        {
            var errors = new List<string>();

            if (!setupDto.Wiek.HasValue || setupDto.Wiek < 16 || setupDto.Wiek > 100)
                errors.Add("Wiek musi być między 16 a 100 lat");

            if (!setupDto.Waga.HasValue || setupDto.Waga < 30 || setupDto.Waga > 250)
                errors.Add("Waga musi być między 30 a 250 kg");

            if (!setupDto.Wzrost.HasValue || setupDto.Wzrost < 120 || setupDto.Wzrost > 250)
                errors.Add("Wzrost musi być między 120 a 250 cm");

            if (string.IsNullOrEmpty(setupDto.Plec))
                errors.Add("Proszę wybrać płeć");

            if (string.IsNullOrEmpty(setupDto.AktywnoscFizyczna))
                errors.Add("Proszę wybrać poziom aktywności fizycznej");

            if (string.IsNullOrEmpty(setupDto.RodzajPracy))
                errors.Add("Proszę wybrać rodzaj pracy");

            if (string.IsNullOrEmpty(setupDto.Cel))
                errors.Add("Proszę wybrać cel");

            return errors;
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

            // Makroskładniki
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

            // Minimum values
            if (user.CustomCarbsGrams < 50) user.CustomCarbsGrams = 50;
            if (user.CustomProteinGrams < 40) user.CustomProteinGrams = 40;
            if (user.CustomFatGrams < 20) user.CustomFatGrams = 20;
        }
    }

    // DTO Classes
    public class UserSetupDto
    {
        public string Username { get; set; }
        public int? Wiek { get; set; }
        public double? Waga { get; set; }
        public double? Wzrost { get; set; }
        public string Plec { get; set; }
        public string AktywnoscFizyczna { get; set; }
        public string RodzajPracy { get; set; }
        public string Cel { get; set; }
    }

    public class SetupResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public UserNutritionDto UserData { get; set; }
    }

    public class UserNutritionDto
    {
        public double BMI { get; set; }
        public int CaloriesDeficit { get; set; }
        public int ProteinGrams { get; set; }
        public int CarbsGrams { get; set; }
        public int FatGrams { get; set; }
    }
}