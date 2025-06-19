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
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized("Nie można zidentyfikować użytkownika");
                }

                var recipes = await _context.Recipes
                    .Where(r => r.UserId == userId || r.IsPublic)
                    .Include(r => r.RecipeIngredients)
                        .ThenInclude(ri => ri.Ingredient)
                    .Include(r => r.Ratings)
                    .ToListAsync();

                return Ok(recipes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetRecipes: {ex.Message}");
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
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized("Nie można zidentyfikować użytkownika");
                }

                Console.WriteLine($"User ID: {userId}");

                var recipe = await _context.Recipes
                    .Include(r => r.RecipeIngredients)
                        .ThenInclude(ri => ri.Ingredient)
                    .Include(r => r.Ratings)
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
                Console.WriteLine("=== PostRecipe started ===");

                // Debug wszystkich otrzymanych danych
                Console.WriteLine("=== RECEIVED FORM DATA ===");
                foreach (var key in Request.Form.Keys)
                {
                    Console.WriteLine($"Form key: {key} = {Request.Form[key]}");
                }

                if (Request.Form.Files.Any())
                {
                    foreach (var file in Request.Form.Files)
                    {
                        Console.WriteLine($"File: {file.Name} = {file.FileName} ({file.Length} bytes)");
                    }
                }

                Console.WriteLine($"RecipeDto object: Name={recipeDto?.Name}, Calories={recipeDto?.Calories}, Protein={recipeDto?.Protein}");
                Console.WriteLine($"Image parameter: {image?.FileName} ({image?.Length} bytes)");
                Console.WriteLine($"RecipeIngredients JSON: {recipeDto?.RecipeIngredients}");

                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized("Nie można zidentyfikować użytkownika");
                }

                // Validation
                if (recipeDto == null)
                {
                    Console.WriteLine("ERROR: recipeDto is null");
                    return BadRequest("Brak danych przepisu");
                }

                // Debug ModelState
                if (!ModelState.IsValid)
                {
                    Console.WriteLine("=== MODEL STATE ERRORS ===");
                    foreach (var modelState in ModelState)
                    {
                        foreach (var error in modelState.Value.Errors)
                        {
                            Console.WriteLine($"Field: {modelState.Key}, Error: {error.ErrorMessage}");
                        }
                    }
                    return BadRequest(ModelState);
                }

                if (string.IsNullOrWhiteSpace(recipeDto.Name))
                {
                    Console.WriteLine("ERROR: Recipe name is empty");
                    return BadRequest("Nazwa przepisu jest wymagana");
                }

                if (recipeDto.Calories < 0 || recipeDto.Calories > 9999)
                {
                    Console.WriteLine($"ERROR: Invalid calories: {recipeDto.Calories}");
                    return BadRequest("Kalorie muszą być między 0 a 9999");
                }

                if (recipeDto.Protein < 0 || recipeDto.Protein > 100)
                {
                    Console.WriteLine($"ERROR: Invalid protein: {recipeDto.Protein}");
                    return BadRequest("Białko musi być między 0 a 100g");
                }

                if (recipeDto.Carbs < 0 || recipeDto.Carbs > 100)
                {
                    Console.WriteLine($"ERROR: Invalid carbs: {recipeDto.Carbs}");
                    return BadRequest("Węglowodany muszą być między 0 a 100g");
                }

                if (recipeDto.Fat < 0 || recipeDto.Fat > 100)
                {
                    Console.WriteLine($"ERROR: Invalid fat: {recipeDto.Fat}");
                    return BadRequest("Tłuszcze muszą być między 0 a 100g");
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                Console.WriteLine("Transaction started");

                try
                {
                    var recipe = new Recipe
                    {
                        Name = recipeDto.Name.Trim(),
                        Instructions = recipeDto.Instructions?.Trim() ?? string.Empty,
                        Calories = recipeDto.Calories,
                        Protein = recipeDto.Protein,
                        Fat = recipeDto.Fat,
                        Carbs = recipeDto.Carbs,
                        IsPublic = recipeDto.IsPublic,
                        UserId = userId
                    };

                    Console.WriteLine("Recipe object created successfully");

                    // Obsługa obrazu
                    if (image != null && image.Length > 0)
                    {
                        Console.WriteLine($"Processing image: {image.FileName}, Size: {image.Length}");

                        // Sprawdź typ pliku
                        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
                        if (!allowedTypes.Contains(image.ContentType.ToLower()))
                        {
                            Console.WriteLine($"Invalid image type: {image.ContentType}");
                            return BadRequest("Dozwolone są tylko pliki obrazów (JPG, PNG, GIF, WebP).");
                        }

                        // Sprawdź rozmiar pliku (max 5MB)
                        if (image.Length > 5 * 1024 * 1024)
                        {
                            Console.WriteLine($"Image too large: {image.Length} bytes");
                            return BadRequest("Plik nie może być większy niż 5MB.");
                        }

                        using (var memoryStream = new MemoryStream())
                        {
                            await image.CopyToAsync(memoryStream);
                            recipe.ImageData = memoryStream.ToArray();
                        }

                        Console.WriteLine("Image processed successfully");
                    }
                    else
                    {
                        Console.WriteLine("No image provided");
                    }

                    // Dodaj przepis do bazy NAJPIERW
                    _context.Recipes.Add(recipe);
                    await _context.SaveChangesAsync();

                    Console.WriteLine($"Recipe saved with ID: {recipe.Id}");

                    // POPRAWIONE PRZETWARZANIE SKŁADNIKÓW
                    if (!string.IsNullOrEmpty(recipeDto.RecipeIngredients))
                    {
                        Console.WriteLine($"=== PROCESSING INGREDIENTS ===");
                        Console.WriteLine($"Raw JSON: {recipeDto.RecipeIngredients}");

                        try
                        {
                            // Spróbuj deserializować JSON
                            var ingredientsData = JsonSerializer.Deserialize<List<RecipeIngredientDto>>(recipeDto.RecipeIngredients);
                            Console.WriteLine($"Successfully parsed {ingredientsData?.Count ?? 0} ingredients from JSON");

                            if (ingredientsData != null && ingredientsData.Count > 0)
                            {
                                Console.WriteLine("=== RAW INGREDIENTS DATA ===");
                                foreach (var ing in ingredientsData)
                                {
                                    Console.WriteLine($"Raw ingredient: ID={ing.IngredientId}, Amount={ing.Amount}");
                                }

                                // Filtruj składniki - ZMIENIAMY WARUNKI
                                var validIngredients = ingredientsData
                                    .Where(ing => ing.IngredientId > 0 && ing.Amount > 0)
                                    .ToList();

                                Console.WriteLine($"=== VALID INGREDIENTS ===");
                                Console.WriteLine($"Valid ingredients count: {validIngredients.Count}");

                                if (validIngredients.Count == 0)
                                {
                                    Console.WriteLine("WARNING: No valid ingredients found, but continuing...");
                                    // NIE PRZERYWAMY - pozwalamy na przepis bez składników
                                }
                                else
                                {
                                    Console.WriteLine("=== SAVING INGREDIENTS ===");

                                    foreach (var ingredient in validIngredients)
                                    {
                                        Console.WriteLine($"Processing ingredient: ID={ingredient.IngredientId}, Amount={ingredient.Amount}");

                                        // Sprawdź czy składnik istnieje w bazie
                                        var ingredientExists = await _context.Ingredients
                                            .AnyAsync(i => i.Id == ingredient.IngredientId);

                                        if (!ingredientExists)
                                        {
                                            Console.WriteLine($"ERROR: Ingredient with ID {ingredient.IngredientId} does not exist in database");
                                            await transaction.RollbackAsync();
                                            return BadRequest($"Składnik o ID {ingredient.IngredientId} nie istnieje w bazie danych.");
                                        }

                                        // Utwórz RecipeIngredient
                                        var recipeIngredient = new RecipeIngredient
                                        {
                                            RecipeId = recipe.Id,
                                            IngredientId = ingredient.IngredientId,
                                            Amount = ingredient.Amount
                                        };

                                        _context.RecipeIngredients.Add(recipeIngredient);
                                        Console.WriteLine($"Added RecipeIngredient: RecipeId={recipe.Id}, IngredientId={ingredient.IngredientId}, Amount={ingredient.Amount}");
                                    }

                                    // Zapisz składniki
                                    var savedIngredientsCount = await _context.SaveChangesAsync();
                                    Console.WriteLine($"Saved {savedIngredientsCount} ingredient records to database");
                                }
                            }
                            else
                            {
                                Console.WriteLine("No ingredients data in JSON or JSON is empty");
                            }
                        }
                        catch (JsonException ex)
                        {
                            Console.WriteLine($"JSON parsing error: {ex.Message}");
                            Console.WriteLine($"JSON content that failed: {recipeDto.RecipeIngredients}");
                            await transaction.RollbackAsync();
                            return BadRequest($"Błąd podczas przetwarzania składników JSON: {ex.Message}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Unexpected error processing ingredients: {ex.Message}");
                            Console.WriteLine($"Stack trace: {ex.StackTrace}");
                            await transaction.RollbackAsync();
                            return BadRequest($"Błąd podczas przetwarzania składników: {ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("No ingredients JSON provided - recipe will be saved without ingredients");
                    }

                    // Zatwierdź transakcję
                    await transaction.CommitAsync();
                    Console.WriteLine("=== TRANSACTION COMMITTED SUCCESSFULLY ===");

                    // Załaduj przepis z relacjami dla odpowiedzi
                    var savedRecipe = await _context.Recipes
                        .Include(r => r.RecipeIngredients)
                        .ThenInclude(ri => ri.Ingredient)
                        .FirstOrDefaultAsync(r => r.Id == recipe.Id);

                    Console.WriteLine($"=== RECIPE CREATED SUCCESSFULLY ===");
                    Console.WriteLine($"Recipe ID: {savedRecipe.Id}");
                    Console.WriteLine($"Recipe Name: {savedRecipe.Name}");
                    Console.WriteLine($"Recipe Ingredients Count: {savedRecipe.RecipeIngredients?.Count ?? 0}");

                    return CreatedAtAction(nameof(GetRecipe), new { id = recipe.Id }, savedRecipe);
                }
                catch (Exception innerEx)
                {
                    Console.WriteLine($"=== TRANSACTION ERROR ===");
                    Console.WriteLine($"Error: {innerEx.Message}");
                    Console.WriteLine($"Stack trace: {innerEx.StackTrace}");
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== GENERAL ERROR ===");
                Console.WriteLine($"Error in PostRecipe: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
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
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized("Nie można zidentyfikować użytkownika");
                }

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
                            await _context.SaveChangesAsync();
                        }

                        // Dodaj nowe składniki
                        if (ingredientsData != null && ingredientsData.Count > 0)
                        {
                            var validIngredients = ingredientsData.Where(ing => ing.IngredientId > 0).ToList();

                            if (validIngredients.Count == 0)
                            {
                                return BadRequest("Błąd: Wszystkie składniki miały nieprawidłowe ID.");
                            }

                            existingRecipe.RecipeIngredients = new List<RecipeIngredient>();

                            foreach (var ingredient in validIngredients)
                            {
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
                Console.WriteLine($"Error in PutRecipe: {ex.Message}");
                return StatusCode(500, $"Błąd podczas aktualizacji przepisu: {ex.Message}");
            }
        }

        // DELETE: api/recipe/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRecipe(int id)
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized("Nie można zidentyfikować użytkownika");
                }

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
                Console.WriteLine($"Error in DeleteRecipe: {ex.Message}");
                return StatusCode(500, $"Błąd podczas usuwania przepisu: {ex.Message}");
            }
        }

        // POST: api/recipe/5/rate
        [HttpPost("{id}/rate")]
        public async Task<IActionResult> RateRecipe(int id, [FromBody] RecipeRatingDto ratingDto)
        {
            Console.WriteLine($"RateRecipe wywołana dla przepisu ID: {id}, ocena: {ratingDto.Rating}");

            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized("Nie można zidentyfikować użytkownika");
                }

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
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized("Nie można zidentyfikować użytkownika");
                }

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

    // DTO klasy
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