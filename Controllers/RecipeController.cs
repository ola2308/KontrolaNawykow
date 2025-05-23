using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KontrolaNawykow.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;

namespace KontrolaNawykow.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RecipeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RecipeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/recipe
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Recipe>>> GetRecipes()
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                return await _context.Recipes
                    .Where(r => r.UserId == userId || r.IsPublic)
                    .Include(r => r.RecipeIngredients)
                        .ThenInclude(ri => ri.Ingredient)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd podczas pobierania przepisów: {ex.Message}");
            }
        }

        // GET: api/recipe/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Recipe>> GetRecipe(int id)
        {
            Console.WriteLine($"GetRecipe wywołana z ID: {id}");

            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                Console.WriteLine($"User ID: {userId}");

                var recipe = await _context.Recipes
                    .Include(r => r.RecipeIngredients)
                        .ThenInclude(ri => ri.Ingredient)
                    .Include(r => r.Ratings) // Dodaj oceny
                    .FirstOrDefaultAsync(r => r.Id == id && (r.UserId == userId || r.IsPublic));

                if (recipe == null)
                {
                    Console.WriteLine($"Nie znaleziono przepisu o ID {id} dla użytkownika {userId}");
                    return NotFound($"Nie znaleziono przepisu o ID {id}");
                }

                Console.WriteLine($"Znaleziono przepis: {recipe.Name}, składniki: {recipe.RecipeIngredients?.Count ?? 0}, oceny: {recipe.Ratings?.Count ?? 0}");

                return Ok(recipe);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd w GetRecipe: {ex.Message}");
                return StatusCode(500, $"Błąd podczas pobierania przepisu: {ex.Message}");
            }
        }

        // POST: api/recipe
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<Recipe>> PostRecipe([FromForm] RecipeDto recipeDto, IFormFile image)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                var recipe = new Recipe
                {
                    Name = recipeDto.Name,
                    Instructions = recipeDto.Instructions,
                    Calories = recipeDto.Calories,
                    Protein = recipeDto.Protein,
                    Fat = recipeDto.Fat,
                    Carbs = recipeDto.Carbs,
                    IsPublic = recipeDto.IsPublic,
                    UserId = userId
                };

                // Obsługa obrazu
                if (image != null && image.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await image.CopyToAsync(memoryStream);
                        recipe.ImageData = memoryStream.ToArray();
                    }
                }

                // Dodawanie składników do przepisu
                if (!string.IsNullOrEmpty(recipeDto.RecipeIngredients))
                {
                    try
                    {
                        var ingredientsData = JsonSerializer.Deserialize<List<RecipeIngredientDto>>(recipeDto.RecipeIngredients);

                        if (ingredientsData != null && ingredientsData.Count > 0)
                        {
                            // Filtrownie nieprawidłowych ID składników
                            ingredientsData = ingredientsData.Where(ing => ing.IngredientId > 0).ToList();

                            if (ingredientsData.Count == 0)
                            {
                                return BadRequest("Błąd: Wszystkie składniki miały nieprawidłowe ID. Sprawdź czy wybrano prawidłowe składniki z listy.");
                            }

                            recipe.RecipeIngredients = new List<RecipeIngredient>();

                            foreach (var ingredient in ingredientsData)
                            {
                                // Sprawdź czy składnik istnieje w bazie danych
                                var ingredientExists = await _context.Ingredients.AnyAsync(i => i.Id == ingredient.IngredientId);
                                if (!ingredientExists)
                                {
                                    return BadRequest($"Błąd: Składnik o ID {ingredient.IngredientId} nie istnieje w bazie danych.");
                                }

                                recipe.RecipeIngredients.Add(new RecipeIngredient
                                {
                                    IngredientId = ingredient.IngredientId,
                                    Amount = ingredient.Amount
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        return BadRequest($"Błąd podczas przetwarzania składników: {ex.Message}");
                    }
                }

                _context.Recipes.Add(recipe);
                await _context.SaveChangesAsync();

                // Załaduj przepis z relacjami
                var savedRecipe = await _context.Recipes
                    .Include(r => r.RecipeIngredients)
                    .ThenInclude(ri => ri.Ingredient)
                    .FirstOrDefaultAsync(r => r.Id == recipe.Id);

                return CreatedAtAction(nameof(GetRecipe), new { id = recipe.Id }, savedRecipe);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd podczas dodawania przepisu: {ex.Message}");
            }
        }

        // PUT: api/recipe/5
        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> PutRecipe(int id, [FromForm] RecipeDto recipeDto, IFormFile image)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var existingRecipe = await _context.Recipes
                    .Include(r => r.RecipeIngredients)
                    .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

                if (existingRecipe == null)
                {
                    return NotFound($"Nie znaleziono przepisu o ID {id}");
                }

                // Aktualizuj właściwości
                existingRecipe.Name = recipeDto.Name;
                existingRecipe.Calories = recipeDto.Calories;
                existingRecipe.Protein = recipeDto.Protein;
                existingRecipe.Fat = recipeDto.Fat;
                existingRecipe.Carbs = recipeDto.Carbs;
                existingRecipe.Instructions = recipeDto.Instructions;
                existingRecipe.IsPublic = recipeDto.IsPublic;

                // Obsługa obrazu
                if (image != null && image.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await image.CopyToAsync(memoryStream);
                        existingRecipe.ImageData = memoryStream.ToArray();
                    }
                }

                // Aktualizacja składników
                if (!string.IsNullOrEmpty(recipeDto.RecipeIngredients))
                {
                    try
                    {
                        var ingredientsData = JsonSerializer.Deserialize<List<RecipeIngredientDto>>(recipeDto.RecipeIngredients);

                        // Usuń istniejące składniki
                        if (existingRecipe.RecipeIngredients != null && existingRecipe.RecipeIngredients.Any())
                        {
                            _context.RecipeIngredients.RemoveRange(existingRecipe.RecipeIngredients);
                            await _context.SaveChangesAsync(); // Zapisz zmiany przed dodaniem nowych składników
                        }

                        // Dodaj nowe składniki
                        if (ingredientsData != null && ingredientsData.Count > 0)
                        {
                            // Filtruj nieprawidłowe ID składników
                            ingredientsData = ingredientsData.Where(ing => ing.IngredientId > 0).ToList();

                            if (ingredientsData.Count == 0)
                            {
                                return BadRequest("Błąd: Wszystkie składniki miały nieprawidłowe ID. Sprawdź czy wybrano prawidłowe składniki z listy.");
                            }

                            existingRecipe.RecipeIngredients = new List<RecipeIngredient>();

                            foreach (var ingredient in ingredientsData)
                            {
                                // Sprawdź czy składnik istnieje w bazie danych
                                var ingredientExists = await _context.Ingredients.AnyAsync(i => i.Id == ingredient.IngredientId);
                                if (!ingredientExists)
                                {
                                    return BadRequest($"Błąd: Składnik o ID {ingredient.IngredientId} nie istnieje w bazie danych.");
                                }

                                existingRecipe.RecipeIngredients.Add(new RecipeIngredient
                                {
                                    RecipeId = id,
                                    IngredientId = ingredient.IngredientId,
                                    Amount = ingredient.Amount
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        return BadRequest($"Błąd podczas przetwarzania składników: {ex.Message}");
                    }
                }

                // Zapisz zmiany
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RecipeExists(id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd podczas aktualizacji przepisu: {ex.Message}");
            }
        }

        // DELETE: api/recipe/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRecipe(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var recipe = await _context.Recipes
                    .Include(r => r.RecipeIngredients)
                    .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

                if (recipe == null)
                {
                    return NotFound($"Nie znaleziono przepisu o ID {id}");
                }

                // Usuń powiązane składniki
                if (recipe.RecipeIngredients != null && recipe.RecipeIngredients.Any())
                {
                    _context.RecipeIngredients.RemoveRange(recipe.RecipeIngredients);
                }

                _context.Recipes.Remove(recipe);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd podczas usuwania przepisu: {ex.Message}");
            }
        }

        // POST: api/recipe/5/ingredients
        [HttpPost("{id}/ingredients")]
        public async Task<ActionResult<RecipeIngredient>> AddIngredient(int id, RecipeIngredientDto ingredientDto)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var recipe = await _context.Recipes
                    .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

                if (recipe == null)
                {
                    return NotFound($"Nie znaleziono przepisu o ID {id}");
                }

                // Sprawdź czy składnik istnieje
                var ingredientExists = await _context.Ingredients.AnyAsync(i => i.Id == ingredientDto.IngredientId);
                if (!ingredientExists)
                {
                    return BadRequest($"Błąd: Składnik o ID {ingredientDto.IngredientId} nie istnieje w bazie danych.");
                }

                var recipeIngredient = new RecipeIngredient
                {
                    RecipeId = id,
                    IngredientId = ingredientDto.IngredientId,
                    Amount = ingredientDto.Amount
                };

                _context.RecipeIngredients.Add(recipeIngredient);
                await _context.SaveChangesAsync();

                return Ok(recipeIngredient);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd podczas dodawania składnika do przepisu: {ex.Message}");
            }
        }

        // DELETE: api/recipe/ingredients/5
        [HttpDelete("ingredients/{id}")]
        public async Task<IActionResult> DeleteIngredient(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var recipeIngredient = await _context.RecipeIngredients
                    .Include(ri => ri.Recipe)
                    .FirstOrDefaultAsync(ri => ri.Id == id && ri.Recipe.UserId == userId);

                if (recipeIngredient == null)
                {
                    return NotFound($"Nie znaleziono składnika o ID {id}");
                }

                _context.RecipeIngredients.Remove(recipeIngredient);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd podczas usuwania składnika: {ex.Message}");
            }
        }

        // POST: api/recipe/5/rate
        [HttpPost("{id}/rate")]
        public async Task<IActionResult> RateRecipe(int id, [FromBody] RecipeRatingDto ratingDto)
        {
            Console.WriteLine($"RateRecipe wywołana dla przepisu ID: {id}, ocena: {ratingDto.Rating}");

            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                // Sprawdź czy przepis istnieje i użytkownik ma do niego dostęp
                var recipe = await _context.Recipes
                    .FirstOrDefaultAsync(r => r.Id == id && (r.UserId == userId || r.IsPublic));

                if (recipe == null)
                {
                    return NotFound("Nie znaleziono przepisu");
                }

                // Sprawdź czy użytkownik już ocenił ten przepis
                var existingRating = await _context.RecipeRatings
                    .FirstOrDefaultAsync(rr => rr.RecipeId == id && rr.UserId == userId);

                if (existingRating != null)
                {
                    // Aktualizuj istniejącą ocenę
                    existingRating.Rating = ratingDto.Rating;
                    existingRating.Comment = ratingDto.Comment;
                    Console.WriteLine($"Aktualizacja istniejącej oceny dla użytkownika {userId}");
                }
                else
                {
                    // Dodaj nową ocenę
                    var newRating = new RecipeRating
                    {
                        RecipeId = id,
                        UserId = userId,
                        Rating = ratingDto.Rating,
                        Comment = ratingDto.Comment,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.RecipeRatings.Add(newRating);
                    Console.WriteLine($"Dodano nową ocenę dla użytkownika {userId}");
                }

                await _context.SaveChangesAsync();

                // Pobierz zaktualizowane statystyki
                var avgRating = await _context.RecipeRatings
                    .Where(rr => rr.RecipeId == id)
                    .AverageAsync(rr => (double)rr.Rating);

                var ratingCount = await _context.RecipeRatings
                    .CountAsync(rr => rr.RecipeId == id);

                Console.WriteLine($"Ocena zapisana. Średnia: {avgRating:F1}, Liczba ocen: {ratingCount}");

                return Ok(new
                {
                    message = "Ocena zapisana",
                    averageRating = Math.Round(avgRating, 1),
                    ratingCount = ratingCount
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas oceniania przepisu: {ex.Message}");
                return StatusCode(500, $"Błąd podczas zapisywania oceny: {ex.Message}");
            }
        }

        // GET: api/recipe/5/rating
        [HttpGet("{id}/rating")]
        public async Task<IActionResult> GetRecipeRating(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                // Pobierz ocenę użytkownika dla tego przepisu
                var userRating = await _context.RecipeRatings
                    .FirstOrDefaultAsync(rr => rr.RecipeId == id && rr.UserId == userId);

                // Pobierz statystyki przepisu
                var ratings = await _context.RecipeRatings
                    .Where(rr => rr.RecipeId == id)
                    .ToListAsync();

                var averageRating = ratings.Any() ? ratings.Average(r => r.Rating) : 0;
                var ratingCount = ratings.Count;

                return Ok(new
                {
                    userRating = userRating?.Rating,
                    userComment = userRating?.Comment,
                    averageRating = Math.Round(averageRating, 1),
                    ratingCount = ratingCount
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas pobierania oceny: {ex.Message}");
                return StatusCode(500, $"Błąd podczas pobierania oceny: {ex.Message}");
            }
        }

        private bool RecipeExists(int id)
        {
            return _context.Recipes.Any(e => e.Id == id);
        }
    }

    // DTO klasy - muszą być poza klasą kontrolera
    public class RecipeDto
    {
        public string Name { get; set; }
        public string Instructions { get; set; }
        public int Calories { get; set; }
        public float Protein { get; set; }
        public float Fat { get; set; }
        public float Carbs { get; set; }
        public bool IsPublic { get; set; }
        public string RecipeIngredients { get; set; } // JSON string z listą składników
    }

    public class RecipeIngredientDto
    {
        public int IngredientId { get; set; }
        public float Amount { get; set; }
    }

    // DTO dla oceny
    public class RecipeRatingDto
    {
        [Range(1, 5, ErrorMessage = "Ocena musi być między 1 a 5")]
        public int Rating { get; set; }

        [MaxLength(500)]
        public string Comment { get; set; }
    }
}