using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using KontrolaNawykow.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace KontrolaNawykow.Pages.Recipes
{
    [Authorize]
    public class AddModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public AddModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        [Required(ErrorMessage = "Nazwa przepisu jest wymagana")]
        [StringLength(200, ErrorMessage = "Nazwa przepisu nie mo¿e byæ d³u¿sza ni¿ 200 znaków")]
        public string Name { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Kalorie s¹ wymagane")]
        [Range(0, 9999, ErrorMessage = "Kalorie musz¹ byæ miêdzy 0 a 9999")]
        public int Calories { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Bia³ko jest wymagane")]
        [Range(0, 100, ErrorMessage = "Bia³ko musi byæ miêdzy 0 a 100g")]
        public float Protein { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Wêglowodany s¹ wymagane")]
        [Range(0, 100, ErrorMessage = "Wêglowodany musz¹ byæ miêdzy 0 a 100g")]
        public float Carbs { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "T³uszcze s¹ wymagane")]
        [Range(0, 100, ErrorMessage = "T³uszcze musz¹ byæ miêdzy 0 a 100g")]
        public float Fat { get; set; }

        [BindProperty]
        [StringLength(5000, ErrorMessage = "Instrukcje nie mog¹ byæ d³u¿sze ni¿ 5000 znaków")]
        public string Instructions { get; set; }

        [BindProperty]
        public bool IsPublic { get; set; }

        [BindProperty]
        public IFormFile Image { get; set; }

        [BindProperty]
        public string RecipeIngredients { get; set; }

        public string SuccessMessage { get; set; }
        public string ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // SprawdŸ czy u¿ytkownik jest zalogowany
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                Console.WriteLine("=== POST Recipe Add started ===");
                Console.WriteLine($"ModelState.IsValid: {ModelState.IsValid}");

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    Console.WriteLine("User not authenticated");
                    return RedirectToPage("/Account/Login");
                }

                Console.WriteLine($"User ID: {userId}");

                // Debugowanie danych formularza
                Console.WriteLine($"Name: '{Name}'");
                Console.WriteLine($"Calories: {Calories}");
                Console.WriteLine($"Protein: {Protein}");
                Console.WriteLine($"Carbs: {Carbs}");
                Console.WriteLine($"Fat: {Fat}");
                Console.WriteLine($"IsPublic: {IsPublic}");
                Console.WriteLine($"Instructions length: {Instructions?.Length ?? 0}");
                Console.WriteLine($"Image: {Image?.FileName ?? "null"} ({Image?.Length ?? 0} bytes)");
                Console.WriteLine($"RecipeIngredients: '{RecipeIngredients}'");

                // Debug ModelState errors
                if (!ModelState.IsValid)
                {
                    Console.WriteLine("ModelState errors:");
                    foreach (var modelState in ModelState)
                    {
                        foreach (var error in modelState.Value.Errors)
                        {
                            Console.WriteLine($"- {modelState.Key}: {error.ErrorMessage}");
                        }
                    }
                    ErrorMessage = "Proszê poprawiæ b³êdy w formularzu.";
                    return Page();
                }

                // Rozpocznij transakcjê
                using var transaction = await _context.Database.BeginTransactionAsync();
                Console.WriteLine("Transaction started");

                try
                {
                    // Utwórz nowy przepis
                    var recipe = new Recipe
                    {
                        Name = Name,
                        Calories = Calories,
                        Protein = Protein,
                        Carbs = Carbs,
                        Fat = Fat,
                        Instructions = Instructions ?? string.Empty,
                        IsPublic = IsPublic,
                        UserId = int.Parse(userId)
                    };

                    Console.WriteLine("Recipe object created");

                    // Obs³uga obrazu
                    if (Image != null && Image.Length > 0)
                    {
                        Console.WriteLine($"Processing image: {Image.FileName}, Size: {Image.Length}");

                        // SprawdŸ typ pliku
                        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
                        if (!allowedTypes.Contains(Image.ContentType.ToLower()))
                        {
                            Console.WriteLine($"Invalid image type: {Image.ContentType}");
                            ErrorMessage = "Dozwolone s¹ tylko pliki obrazów (JPG, PNG, GIF, WebP).";
                            return Page();
                        }

                        // SprawdŸ rozmiar pliku (max 5MB)
                        if (Image.Length > 5 * 1024 * 1024)
                        {
                            Console.WriteLine($"Image too large: {Image.Length} bytes");
                            ErrorMessage = "Plik nie mo¿e byæ wiêkszy ni¿ 5MB.";
                            return Page();
                        }

                        using (var memoryStream = new MemoryStream())
                        {
                            await Image.CopyToAsync(memoryStream);
                            recipe.ImageData = memoryStream.ToArray();
                        }

                        Console.WriteLine("Image processed successfully");
                    }
                    else
                    {
                        Console.WriteLine("No image provided");
                    }

                    // Dodaj przepis do bazy
                    _context.Recipes.Add(recipe);
                    await _context.SaveChangesAsync();

                    Console.WriteLine($"Recipe saved with ID: {recipe.Id}");

                    // Przetwórz sk³adniki
                    if (!string.IsNullOrEmpty(RecipeIngredients))
                    {
                        Console.WriteLine("Processing ingredients...");

                        try
                        {
                            var ingredientsData = JsonSerializer.Deserialize<List<RecipeIngredientDto>>(RecipeIngredients);
                            Console.WriteLine($"Parsed {ingredientsData?.Count ?? 0} ingredients");

                            if (ingredientsData != null && ingredientsData.Count > 0)
                            {
                                // Filtruj nieprawid³owe sk³adniki
                                var validIngredients = ingredientsData
                                    .Where(ing => ing.IngredientId > 0 && ing.Amount > 0)
                                    .ToList();

                                Console.WriteLine($"Valid ingredients: {validIngredients.Count}");

                                if (validIngredients.Count == 0)
                                {
                                    Console.WriteLine("No valid ingredients found");
                                    await transaction.RollbackAsync();
                                    ErrorMessage = "Nie znaleziono prawid³owych sk³adników. Dodaj przynajmniej jeden sk³adnik z iloœci¹.";
                                    return Page();
                                }

                                foreach (var ingredient in validIngredients)
                                {
                                    Console.WriteLine($"Processing ingredient: ID={ingredient.IngredientId}, Amount={ingredient.Amount}");

                                    // SprawdŸ czy sk³adnik istnieje w bazie
                                    var ingredientExists = await _context.Ingredients
                                        .AnyAsync(i => i.Id == ingredient.IngredientId);

                                    if (!ingredientExists)
                                    {
                                        Console.WriteLine($"Ingredient with ID {ingredient.IngredientId} does not exist");
                                        await transaction.RollbackAsync();
                                        ErrorMessage = $"Sk³adnik o ID {ingredient.IngredientId} nie istnieje w bazie danych.";
                                        return Page();
                                    }

                                    var recipeIngredient = new RecipeIngredient
                                    {
                                        RecipeId = recipe.Id,
                                        IngredientId = ingredient.IngredientId,
                                        Amount = ingredient.Amount
                                    };

                                    _context.RecipeIngredients.Add(recipeIngredient);
                                    Console.WriteLine($"Added ingredient: ID={ingredient.IngredientId}, Amount={ingredient.Amount}");
                                }

                                await _context.SaveChangesAsync();
                                Console.WriteLine("All ingredients saved successfully");
                            }
                            else
                            {
                                Console.WriteLine("No ingredients data provided");
                            }
                        }
                        catch (JsonException ex)
                        {
                            Console.WriteLine($"JSON parsing error: {ex.Message}");
                            await transaction.RollbackAsync();
                            ErrorMessage = "B³¹d podczas przetwarzania sk³adników. SprawdŸ format danych.";
                            return Page();
                        }
                    }
                    else
                    {
                        Console.WriteLine("No ingredients JSON provided");
                    }

                    // ZatwierdŸ transakcjê
                    await transaction.CommitAsync();
                    Console.WriteLine("Transaction committed successfully");

                    SuccessMessage = $"Przepis '{Name}' zosta³ dodany pomyœlnie!";

                    // Wyczyœæ formularz po pomyœlnym dodaniu
                    Name = string.Empty;
                    Calories = 0;
                    Protein = 0;
                    Carbs = 0;
                    Fat = 0;
                    Instructions = string.Empty;
                    IsPublic = false;
                    RecipeIngredients = string.Empty;

                    // Redirect to diet page to see the new recipe
                    TempData["SuccessMessage"] = SuccessMessage;
                    return RedirectToPage("/Diet/Index");
                }
                catch (Exception innerEx)
                {
                    Console.WriteLine($"Error in transaction: {innerEx.Message}");
                    Console.WriteLine($"Stack trace: {innerEx.StackTrace}");
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OnPostAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                ErrorMessage = $"Wyst¹pi³ b³¹d podczas dodawania przepisu: {ex.Message}";
                return Page();
            }
        }

        // DTO dla sk³adników przepisu
        public class RecipeIngredientDto
        {
            public int IngredientId { get; set; }
            public float Amount { get; set; }
        }

        public bool adminCheck()
        {
            return false;
            //var CurrentAdmin = _context.Admins.Where(a => a.UzytkownikId == CurrentUser.Id);
            //return CurrentAdmin.Any();
        }
    }
}