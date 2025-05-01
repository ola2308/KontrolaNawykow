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
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var mealPlans = await _context.MealPlans
                    .Where(mp => mp.UserId == userId)
                    .Include(mp => mp.Recipe)
                    .Include(mp => mp.PlanPosilkowPrzepisy)
                        .ThenInclude(ppp => ppp.Przepis)
                    .OrderBy(mp => mp.Date)
                    .ThenBy(mp => mp.MealType)
                    .ToListAsync();

                // Convert to view model for compatibility with frontend
                var mealPlanViewModels = mealPlans.Select(mp => new MealPlanViewModel
                {
                    Id = mp.Id,
                    UserId = mp.UserId,
                    Date = mp.Date,
                    MealType = mp.MealType,
                    CustomEntry = mp.CustomEntry,
                    Eaten = mp.Eaten,

                    // Preferuj przepis z bezpośredniej relacji, jeśli istnieje
                    Recipe = mp.Recipe ?? mp.PlanPosilkowPrzepisy.FirstOrDefault()?.Przepis,
                    RecipeId = mp.RecipeId ?? mp.PlanPosilkowPrzepisy.FirstOrDefault()?.PrzepisId
                }).ToList();

                return mealPlanViewModels;
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd podczas pobierania planu posiłków: {ex.Message}");
            }
        }

        // GET: api/mealplan/5
        [HttpGet("{id}")]
        public async Task<ActionResult<MealPlanViewModel>> GetMealPlan(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
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
                    CustomEntry = mealPlan.CustomEntry,
                    Eaten = mealPlan.Eaten,

                    // Preferuj przepis z bezpośredniej relacji, jeśli istnieje
                    Recipe = mealPlan.Recipe ?? mealPlan.PlanPosilkowPrzepisy.FirstOrDefault()?.Przepis,
                    RecipeId = mealPlan.RecipeId ?? mealPlan.PlanPosilkowPrzepisy.FirstOrDefault()?.PrzepisId
                };

                return mealPlanViewModel;
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd podczas pobierania posiłku: {ex.Message}");
            }
        }

        // POST: api/mealplan
        [HttpPost]
        public async Task<ActionResult<MealPlanViewModel>> PostMealPlan([FromBody] MealPlanDto mealPlanDto)
        {
            try
            {
                if (mealPlanDto == null)
                {
                    return BadRequest("Brak danych posiłku");
                }

                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                // Begin transaction
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Create the meal plan
                    var mealPlan = new MealPlan
                    {
                        UserId = userId,
                        Date = DateTime.Parse(mealPlanDto.Date),
                        MealType = (MealType)Enum.Parse(typeof(MealType), mealPlanDto.MealType),
                        RecipeId = mealPlanDto.RecipeId, // Ustaw bezpośrednią relację (stary sposób)
                        Eaten = false,
                        CustomEntry = mealPlanDto.CustomEntry ?? string.Empty
                    };

                    _context.MealPlans.Add(mealPlan);
                    await _context.SaveChangesAsync();

                    // If a recipe is selected, add it to the join table (nowy sposób)
                    if (mealPlanDto.RecipeId.HasValue)
                    {
                        var planPosilkowPrzepis = new PlanPosilkowPrzepis
                        {
                            PlanPosilkowId = mealPlan.Id,
                            PrzepisId = mealPlanDto.RecipeId.Value
                        };

                        _context.PlanPosilkowPrzepisy.Add(planPosilkowPrzepis);
                        await _context.SaveChangesAsync();
                    }

                    await transaction.CommitAsync();

                    // Reload the meal plan with its recipe
                    var savedMealPlan = await _context.MealPlans
                        .Include(mp => mp.Recipe)
                        .Include(mp => mp.PlanPosilkowPrzepisy)
                            .ThenInclude(ppp => ppp.Przepis)
                        .FirstOrDefaultAsync(mp => mp.Id == mealPlan.Id);

                    // Convert to view model
                    var mealPlanViewModel = new MealPlanViewModel
                    {
                        Id = savedMealPlan.Id,
                        UserId = savedMealPlan.UserId,
                        Date = savedMealPlan.Date,
                        MealType = savedMealPlan.MealType,
                        CustomEntry = savedMealPlan.CustomEntry,
                        Eaten = savedMealPlan.Eaten,
                        Recipe = savedMealPlan.Recipe ?? savedMealPlan.PlanPosilkowPrzepisy.FirstOrDefault()?.Przepis,
                        RecipeId = savedMealPlan.RecipeId ?? savedMealPlan.PlanPosilkowPrzepisy.FirstOrDefault()?.PrzepisId
                    };

                    return CreatedAtAction(nameof(GetMealPlan), new { id = mealPlan.Id }, mealPlanViewModel);
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
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

                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
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
                    // Update basic properties
                    mealPlan.Date = DateTime.Parse(mealPlanDto.Date);
                    mealPlan.MealType = (MealType)Enum.Parse(typeof(MealType), mealPlanDto.MealType);
                    mealPlan.CustomEntry = mealPlanDto.CustomEntry ?? mealPlan.CustomEntry;

                    // Aktualizuj starą relację
                    mealPlan.RecipeId = mealPlanDto.RecipeId;

                    // Handle recipe changes via join table
                    var existingRelation = mealPlan.PlanPosilkowPrzepisy.FirstOrDefault();

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
                return StatusCode(500, $"Błąd podczas aktualizacji posiłku: {ex.Message}");
            }
        }

        // PUT: api/mealplan/5/eaten
        [HttpPut("{id}/eaten")]
        public async Task<IActionResult> MarkMealEaten(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
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
                return StatusCode(500, $"Błąd podczas oznaczania posiłku jako zjedzonego: {ex.Message}");
            }
        }

        // PUT: api/mealplan/5/uneaten
        [HttpPut("{id}/uneaten")]
        public async Task<IActionResult> UnmarkMealEaten(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
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
                return StatusCode(500, $"Błąd podczas oznaczania posiłku jako niezjedzonego: {ex.Message}");
            }
        }

        // DELETE: api/mealplan/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMealPlan(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
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
    }
}