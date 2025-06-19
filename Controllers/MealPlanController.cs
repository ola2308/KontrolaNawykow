using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KontrolaNawykow.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KontrolaNawykow.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MealPlanController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MealPlanController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/mealplan
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MealPlanViewModel>>> GetMealPlans()
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized("Nie można zidentyfikować użytkownika");
                }

                Console.WriteLine($"Loading meal plans for user ID: {userId}");

                var mealPlans = await _context.MealPlans
                    .Where(mp => mp.UserId == userId)
                    .Include(mp => mp.Recipe)
                    .Include(mp => mp.PlanPosilkowPrzepisy)
                        .ThenInclude(ppp => ppp.Przepis)
                    .OrderBy(mp => mp.Date)
                    .ThenBy(mp => mp.MealType)
                    .ToListAsync();

                Console.WriteLine($"Found {mealPlans.Count} meal plans");

                // Convert to view model for compatibility with frontend
                var mealPlanViewModels = new List<MealPlanViewModel>();

                foreach (var mp in mealPlans)
                {
                    try
                    {
                        var viewModel = new MealPlanViewModel
                        {
                            Id = mp.Id,
                            UserId = mp.UserId,
                            Date = mp.Date,
                            MealType = mp.MealType,
                            CustomEntry = mp.CustomEntry ?? string.Empty,
                            Eaten = mp.Eaten,
                            Gramature = mp.Gramature ?? 100,

                            // Nutrition data - prefer custom values if available, otherwise use recipe
                            Calories = mp.CustomCalories ?? (mp.Recipe?.Calories ?? mp.PlanPosilkowPrzepisy?.FirstOrDefault()?.Przepis?.Calories ?? 0),
                            Protein = mp.CustomProtein ?? (mp.Recipe?.Protein ?? mp.PlanPosilkowPrzepisy?.FirstOrDefault()?.Przepis?.Protein ?? 0),
                            Carbs = mp.CustomCarbs ?? (mp.Recipe?.Carbs ?? mp.PlanPosilkowPrzepisy?.FirstOrDefault()?.Przepis?.Carbs ?? 0),
                            Fat = mp.CustomFat ?? (mp.Recipe?.Fat ?? mp.PlanPosilkowPrzepisy?.FirstOrDefault()?.Przepis?.Fat ?? 0),

                            // Preferuj przepis z bezpośredniej relacji, jeśli istnieje
                            Recipe = mp.Recipe ?? mp.PlanPosilkowPrzepisy?.FirstOrDefault()?.Przepis,
                            RecipeId = mp.RecipeId ?? mp.PlanPosilkowPrzepisy?.FirstOrDefault()?.PrzepisId,

                            // Custom nutrition flags
                            HasCustomNutrition = mp.CustomCalories.HasValue || mp.CustomProtein.HasValue || mp.CustomCarbs.HasValue || mp.CustomFat.HasValue
                        };

                        mealPlanViewModels.Add(viewModel);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing meal plan {mp.Id}: {ex.Message}");
                        // Skip this meal plan but continue with others
                        continue;
                    }
                }

                Console.WriteLine($"Successfully converted {mealPlanViewModels.Count} meal plans to view models");
                return Ok(mealPlanViewModels);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetMealPlans: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, $"Błąd podczas pobierania planu posiłków: {ex.Message}");
            }
        }

        // GET: api/mealplan/5
        [HttpGet("{id}")]
        public async Task<ActionResult<MealPlanViewModel>> GetMealPlan(int id)
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized("Nie można zidentyfikować użytkownika");
                }

                var mealPlan = await _context.MealPlans
                    .Include(mp => mp.Recipe)
                    .Include(mp => mp.PlanPosilkowPrzepisy)
                        .ThenInclude(ppp => ppp.Przepis)
                    .FirstOrDefaultAsync(mp => mp.Id == id && mp.UserId == userId);

                if (mealPlan == null)
                {
                    return NotFound($"Nie znaleziono posiłku o ID {id}");
                }

                // Convert to view model
                var mealPlanViewModel = new MealPlanViewModel
                {
                    Id = mealPlan.Id,
                    UserId = mealPlan.UserId,
                    Date = mealPlan.Date,
                    MealType = mealPlan.MealType,
                    CustomEntry = mealPlan.CustomEntry ?? string.Empty,
                    Eaten = mealPlan.Eaten,
                    Gramature = mealPlan.Gramature ?? 100,

                    // Nutrition data - prefer custom values if available
                    Calories = mealPlan.CustomCalories ?? (mealPlan.Recipe?.Calories ?? mealPlan.PlanPosilkowPrzepisy?.FirstOrDefault()?.Przepis?.Calories ?? 0),
                    Protein = mealPlan.CustomProtein ?? (mealPlan.Recipe?.Protein ?? mealPlan.PlanPosilkowPrzepisy?.FirstOrDefault()?.Przepis?.Protein ?? 0),
                    Carbs = mealPlan.CustomCarbs ?? (mealPlan.Recipe?.Carbs ?? mealPlan.PlanPosilkowPrzepisy?.FirstOrDefault()?.Przepis?.Carbs ?? 0),
                    Fat = mealPlan.CustomFat ?? (mealPlan.Recipe?.Fat ?? mealPlan.PlanPosilkowPrzepisy?.FirstOrDefault()?.Przepis?.Fat ?? 0),

                    // Preferuj przepis z bezpośredniej relacji, jeśli istnieje
                    Recipe = mealPlan.Recipe ?? mealPlan.PlanPosilkowPrzepisy?.FirstOrDefault()?.Przepis,
                    RecipeId = mealPlan.RecipeId ?? mealPlan.PlanPosilkowPrzepisy?.FirstOrDefault()?.PrzepisId,

                    HasCustomNutrition = mealPlan.CustomCalories.HasValue || mealPlan.CustomProtein.HasValue || mealPlan.CustomCarbs.HasValue || mealPlan.CustomFat.HasValue
                };

                return Ok(mealPlanViewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetMealPlan: {ex.Message}");
                return StatusCode(500, $"Błąd podczas pobierania posiłku: {ex.Message}");
            }
        }

        // POST: api/mealplan
        [HttpPost]
        public async Task<ActionResult<MealPlanViewModel>> PostMealPlan([FromBody] MealPlanDto mealPlanDto)
        {
            Console.WriteLine("PostMealPlan rozpoczęty");

            try
            {
                if (mealPlanDto == null)
                {
                    Console.WriteLine("Brak danych posiłku");
                    return BadRequest("Brak danych posiłku");
                }

                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized("Nie można zidentyfikować użytkownika");
                }

                Console.WriteLine($"Otrzymane dane: Date={mealPlanDto.Date}, MealType={mealPlanDto.MealType}, RecipeId={mealPlanDto.RecipeId}, Gramature={mealPlanDto.Gramature}");
                Console.WriteLine($"User ID: {userId}");

                using var transaction = await _context.Database.BeginTransactionAsync();
                Console.WriteLine("Transakcja rozpoczęta");

                try
                {
                    // Parse date safely
                    if (!DateTime.TryParse(mealPlanDto.Date, out DateTime parsedDate))
                    {
                        return BadRequest("Nieprawidłowy format daty");
                    }

                    // Parse meal type safely
                    if (!Enum.TryParse<MealType>(mealPlanDto.MealType, out MealType parsedMealType))
                    {
                        return BadRequest("Nieprawidłowy typ posiłku");
                    }

                    var mealPlan = new MealPlan
                    {
                        UserId = userId,
                        Date = parsedDate,
                        MealType = parsedMealType,
                        RecipeId = mealPlanDto.RecipeId, // Ustaw bezpośrednią relację (stary sposób)
                        Eaten = false,
                        CustomEntry = mealPlanDto.CustomEntry ?? string.Empty,
                        Gramature = mealPlanDto.Gramature ?? 100,

                        // Nowe pola dla własnych makroskładników
                        CustomCalories = mealPlanDto.CustomCalories,
                        CustomProtein = mealPlanDto.CustomProtein,
                        CustomCarbs = mealPlanDto.CustomCarbs,
                        CustomFat = mealPlanDto.CustomFat
                    };

                    Console.WriteLine($"Utworzono obiekt MealPlan: ID={mealPlan.Id}, Gramature={mealPlan.Gramature}");

                    _context.MealPlans.Add(mealPlan);
                    await _context.SaveChangesAsync();

                    Console.WriteLine($"Zapisano MealPlan do bazy, nowe ID: {mealPlan.Id}");

                    // If a recipe is selected, add it to the join table (nowy sposób)
                    if (mealPlanDto.RecipeId.HasValue)
                    {
                        Console.WriteLine($"Dodaję relację do tabeli łączącej: PlanID={mealPlan.Id}, RecipeID={mealPlanDto.RecipeId.Value}");

                        var planPosilkowPrzepis = new PlanPosilkowPrzepis
                        {
                            PlanPosilkowId = mealPlan.Id,
                            PrzepisId = mealPlanDto.RecipeId.Value
                        };

                        _context.PlanPosilkowPrzepisy.Add(planPosilkowPrzepis);
                        await _context.SaveChangesAsync();

                        Console.WriteLine("Relacja zapisana w tabeli łączącej");
                    }

                    await transaction.CommitAsync();
                    Console.WriteLine("Transakcja zatwierdzona");

                    // Reload the meal plan with its recipe
                    var savedMealPlan = await _context.MealPlans
                        .Include(mp => mp.Recipe)
                        .Include(mp => mp.PlanPosilkowPrzepisy)
                            .ThenInclude(ppp => ppp.Przepis)
                        .FirstOrDefaultAsync(mp => mp.Id == mealPlan.Id);

                    Console.WriteLine($"Przeładowano MealPlan z relacjami");

                    // Convert to view model
                    var mealPlanViewModel = new MealPlanViewModel
                    {
                        Id = savedMealPlan.Id,
                        UserId = savedMealPlan.UserId,
                        Date = savedMealPlan.Date,
                        MealType = savedMealPlan.MealType,
                        CustomEntry = savedMealPlan.CustomEntry,
                        Eaten = savedMealPlan.Eaten,
                        Gramature = savedMealPlan.Gramature,

                        // Use custom nutrition if available, otherwise use recipe nutrition
                        Calories = savedMealPlan.CustomCalories ?? (savedMealPlan.Recipe?.Calories ?? savedMealPlan.PlanPosilkowPrzepisy?.FirstOrDefault()?.Przepis?.Calories ?? 0),
                        Protein = savedMealPlan.CustomProtein ?? (savedMealPlan.Recipe?.Protein ?? savedMealPlan.PlanPosilkowPrzepisy?.FirstOrDefault()?.Przepis?.Protein ?? 0),
                        Carbs = savedMealPlan.CustomCarbs ?? (savedMealPlan.Recipe?.Carbs ?? savedMealPlan.PlanPosilkowPrzepisy?.FirstOrDefault()?.Przepis?.Carbs ?? 0),
                        Fat = savedMealPlan.CustomFat ?? (savedMealPlan.Recipe?.Fat ?? savedMealPlan.PlanPosilkowPrzepisy?.FirstOrDefault()?.Przepis?.Fat ?? 0),

                        Recipe = savedMealPlan.Recipe ?? savedMealPlan.PlanPosilkowPrzepisy?.FirstOrDefault()?.Przepis,
                        RecipeId = savedMealPlan.RecipeId ?? savedMealPlan.PlanPosilkowPrzepisy?.FirstOrDefault()?.PrzepisId,

                        HasCustomNutrition = savedMealPlan.CustomCalories.HasValue || savedMealPlan.CustomProtein.HasValue || savedMealPlan.CustomCarbs.HasValue || savedMealPlan.CustomFat.HasValue
                    };

                    Console.WriteLine($"PostMealPlan zakończony sukcesem, zwracam MealPlan ID: {mealPlanViewModel.Id}");

                    return CreatedAtAction(nameof(GetMealPlan), new { id = mealPlan.Id }, mealPlanViewModel);
                }
                catch (Exception innerEx)
                {
                    Console.WriteLine($"Błąd w transakcji: {innerEx.Message}");
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd w PostMealPlan: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, $"Błąd podczas dodawania posiłku: {ex.Message}");
            }
        }

        // PUT: api/mealplan/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutMealPlan(int id, [FromBody] MealPlanDto mealPlanDto)
        {
            try
            {
                if (mealPlanDto == null)
                {
                    return BadRequest("Brak danych posiłku");
                }

                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized("Nie można zidentyfikować użytkownika");
                }

                var mealPlan = await _context.MealPlans
                    .Include(mp => mp.PlanPosilkowPrzepisy)
                    .FirstOrDefaultAsync(mp => mp.Id == id && mp.UserId == userId);

                if (mealPlan == null)
                {
                    return NotFound($"Nie znaleziono posiłku o ID {id}");
                }

                // Begin transaction
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Parse date and meal type safely
                    if (!DateTime.TryParse(mealPlanDto.Date, out DateTime parsedDate))
                    {
                        return BadRequest("Nieprawidłowy format daty");
                    }

                    if (!Enum.TryParse<MealType>(mealPlanDto.MealType, out MealType parsedMealType))
                    {
                        return BadRequest("Nieprawidłowy typ posiłku");
                    }

                    // Update basic properties
                    mealPlan.Date = parsedDate;
                    mealPlan.MealType = parsedMealType;
                    mealPlan.CustomEntry = mealPlanDto.CustomEntry ?? mealPlan.CustomEntry;
                    mealPlan.Gramature = mealPlanDto.Gramature ?? mealPlan.Gramature;

                    // Update custom nutrition
                    mealPlan.CustomCalories = mealPlanDto.CustomCalories;
                    mealPlan.CustomProtein = mealPlanDto.CustomProtein;
                    mealPlan.CustomCarbs = mealPlanDto.CustomCarbs;
                    mealPlan.CustomFat = mealPlanDto.CustomFat;

                    // Aktualizuj starą relację
                    mealPlan.RecipeId = mealPlanDto.RecipeId;

                    // Handle recipe changes via join table
                    var existingRelation = mealPlan.PlanPosilkowPrzepisy?.FirstOrDefault();

                    // If we have a recipeId in the dto
                    if (mealPlanDto.RecipeId.HasValue)
                    {
                        // If there's no existing relation, create one
                        if (existingRelation == null)
                        {
                            var planPosilkowPrzepis = new PlanPosilkowPrzepis
                            {
                                PlanPosilkowId = mealPlan.Id,
                                PrzepisId = mealPlanDto.RecipeId.Value
                            };
                            _context.PlanPosilkowPrzepisy.Add(planPosilkowPrzepis);
                        }
                        // If there is an existing relation but with a different recipe, update it
                        else if (existingRelation.PrzepisId != mealPlanDto.RecipeId.Value)
                        {
                            existingRelation.PrzepisId = mealPlanDto.RecipeId.Value;
                            _context.PlanPosilkowPrzepisy.Update(existingRelation);
                        }
                    }
                    // If no recipeId in dto but we have an existing relation, remove it
                    else if (existingRelation != null)
                    {
                        _context.PlanPosilkowPrzepisy.Remove(existingRelation);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return NoContent();
                }
                catch (DbUpdateConcurrencyException)
                {
                    await transaction.RollbackAsync();
                    if (!MealPlanExists(id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in PutMealPlan: {ex.Message}");
                return StatusCode(500, $"Błąd podczas aktualizacji posiłku: {ex.Message}");
            }
        }

        // PUT: api/mealplan/5/eaten
        [HttpPut("{id}/eaten")]
        public async Task<IActionResult> MarkMealEaten(int id)
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized("Nie można zidentyfikować użytkownika");
                }

                var mealPlan = await _context.MealPlans
                    .FirstOrDefaultAsync(mp => mp.Id == id && mp.UserId == userId);

                if (mealPlan == null)
                {
                    return NotFound($"Nie znaleziono posiłku o ID {id}");
                }

                mealPlan.Eaten = true;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in MarkMealEaten: {ex.Message}");
                return StatusCode(500, $"Błąd podczas oznaczania posiłku jako zjedzonego: {ex.Message}");
            }
        }

        // PUT: api/mealplan/5/uneaten
        [HttpPut("{id}/uneaten")]
        public async Task<IActionResult> UnmarkMealEaten(int id)
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized("Nie można zidentyfikować użytkownika");
                }

                var mealPlan = await _context.MealPlans
                    .FirstOrDefaultAsync(mp => mp.Id == id && mp.UserId == userId);

                if (mealPlan == null)
                {
                    return NotFound($"Nie znaleziono posiłku o ID {id}");
                }

                mealPlan.Eaten = false;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UnmarkMealEaten: {ex.Message}");
                return StatusCode(500, $"Błąd podczas oznaczania posiłku jako niezjedzonego: {ex.Message}");
            }
        }

        // DELETE: api/mealplan/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMealPlan(int id)
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized("Nie można zidentyfikować użytkownika");
                }

                var mealPlan = await _context.MealPlans
                    .Include(mp => mp.PlanPosilkowPrzepisy)
                    .FirstOrDefaultAsync(mp => mp.Id == id && mp.UserId == userId);

                if (mealPlan == null)
                {
                    return NotFound($"Nie znaleziono posiłku o ID {id}");
                }

                // Using transaction for safety
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Remove related recipes in join table first
                    if (mealPlan.PlanPosilkowPrzepisy != null && mealPlan.PlanPosilkowPrzepisy.Any())
                    {
                        _context.PlanPosilkowPrzepisy.RemoveRange(mealPlan.PlanPosilkowPrzepisy);
                    }

                    // Remove shopping lists related to this meal plan if any
                    var shoppingLists = await _context.ListyZakupow
                        .Where(lz => lz.PlanPosilkowId == id)
                        .ToListAsync();

                    if (shoppingLists.Any())
                    {
                        _context.ListyZakupow.RemoveRange(shoppingLists);
                    }

                    // Then remove the meal plan itself
                    _context.MealPlans.Remove(mealPlan);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                    return NoContent();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeleteMealPlan: {ex.Message}");
                return StatusCode(500, $"Błąd podczas usuwania posiłku: {ex.Message}");
            }
        }

        private bool MealPlanExists(int id)
        {
            return _context.MealPlans.Any(e => e.Id == id);
        }
    }

    public class MealPlanDto
    {
        public string Date { get; set; }
        public string MealType { get; set; }
        public int? RecipeId { get; set; }
        public string CustomEntry { get; set; }
        public float? Gramature { get; set; }

        // Nowe pola dla własnych makroskładników
        public int? CustomCalories { get; set; }
        public float? CustomProtein { get; set; }
        public float? CustomCarbs { get; set; }
        public float? CustomFat { get; set; }
    }

    // View model to maintain compatibility with frontend
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
        public float? Gramature { get; set; }

        public int Calories { get; set; }
        public float Protein { get; set; }
        public float Carbs { get; set; }
        public float Fat { get; set; }
        public bool HasCustomNutrition { get; set; }
    }
}